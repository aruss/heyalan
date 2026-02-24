namespace SquareBuddy.WebApi.Core;

using Microsoft.AspNetCore.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;
using SquareBuddy.Configuration;
using SquareBuddy.Data;
using SquareBuddy.Data.Entities;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Text.Json;
using System.Threading;
using System.Threading.Channels;

public record StoryValidationResult(bool isValid, string reason);

public record StoryCorrectionResult(string correctedSentence);

public record StorySegment(string sentence, bool isStoryFinished, string? title);

public record StoryConsumptionResult(int DurationSeconds);

public record StoryProductionResult(
    string? FinalTitle,
    string LastSafeSentence,
    int SentenceCount,
    bool EndedByModelSignal);

public class StoryStreamingService
{
    private readonly MainDataContext db;
    private readonly IMinioClient minio;
    private readonly MinioOptions minioOptions;
    private readonly IChatClient storyAgent;
    private readonly IAudioGenerator audioGenerator;
    private readonly IOptions<JsonOptions> jsonOptions;
    private readonly ILogger<StoryStreamingService> logger;

    public StoryStreamingService(
        MainDataContext db,
        IMinioClient minio,
        MinioOptions minioOptions,
        IChatClient storyAgent,
        IAudioGenerator audioGenerator,
        IOptions<JsonOptions> jsonOptions,
        ILogger<StoryStreamingService> logger)
    {
        this.db = db ?? throw new ArgumentNullException(nameof(db));
        this.minio = minio ?? throw new ArgumentNullException(nameof(minio));
        this.minioOptions = minioOptions ?? throw new ArgumentNullException(nameof(minioOptions));
        this.storyAgent = storyAgent ?? throw new ArgumentNullException(nameof(storyAgent));
        this.audioGenerator = audioGenerator ?? throw new ArgumentNullException(nameof(audioGenerator));
        this.jsonOptions = jsonOptions ?? throw new ArgumentNullException(nameof(jsonOptions));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task ProcessStoryRequestAsync(
        StreamStoryInput input,
        SceneGraph sceneGraph,
        BoardConfig boardConfig,
        Stream responseStream,
        CancellationToken ct)
    {
        this.logger.LogDebug(
            "ProcessStoryRequest: Started. BoardId: {BoardId}, FigurineCount: {FigurineCount}",
            input.BoardId,
            input.Figurines.Length);
               

        // 1. Initialize the Session Entity
        // TODO: move to board service ...
        StoryRequest request = new()
        {
            BoardId = boardConfig.BoardId,
            ConfigId = boardConfig.Id,
            Input = JsonSerializer.Serialize(input, AppJsonContext.Default.StreamStoryInput),
            SceneGraph = JsonSerializer.Serialize(sceneGraph, AppJsonContext.Default.SceneGraph),
            Title = "Untitled Story",
            CreatedWith = this.CreateSceneGraphSummary(sceneGraph),
            Duration = 0,
            Status = StoryRequestStatus.Processing,
        };

        this.db.StoryRequests.Add(request);
        await this.db.SaveChangesAsync(ct);
        // END TODO

        logger.LogDebug("ProcessStoryRequest: Request entity persisted. ID: {RequestId}", request.Id);

        // 2. Setup the Producer-Consumer Channel for decoupled text/audio
        Channel<string> sentenceChannel = Channel.CreateUnbounded<string>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = true
        });

        // 3. Execution: Start Producer (Iterative Generation) and Consumer (Audio)
        logger.LogDebug("ProcessStoryRequest: Forking Producer/Consumer tasks.");
        Task<StoryProductionResult> producerTask = this.ProduceCoherentSentencesAsync(boardConfig, sceneGraph, sentenceChannel.Writer, ct);

        try
        {
            StoryConsumptionResult consumptionResult = await this.ConsumeSentencesToAudioAsync(
                request,
                boardConfig.Voice,
                sentenceChannel.Reader,
                responseStream,
                ct);

            // Await the producer to ensure it finished successfully (didn't crash mid-story)
            // If this throws, we jump to catch and mark as Failed.
            StoryProductionResult productionResult = await producerTask;
            this.logger.LogDebug("ProcessStoryRequest: Pipeline finished successfully. RequestId: {RequestId}", request.Id);

            string resolvedTitle = StoryStreamingPolicies.ResolveFinalTitle(productionResult.FinalTitle, productionResult.LastSafeSentence);
            if (!string.Equals(resolvedTitle, productionResult.FinalTitle, StringComparison.Ordinal))
            {
                this.logger.LogWarning(
                    "ProcessStoryRequest: Final title missing or invalid. Applied deterministic fallback. RequestId: {RequestId}",
                    request.Id);
            }

            request.Title = resolvedTitle;
            request.Duration = consumptionResult.DurationSeconds;
            request.Status = StoryRequestStatus.Completed;
            await this.db.SaveChangesAsync(ct);

            this.logger.LogDebug(
                "ProcessStoryRequest: Status transition persisted. RequestId: {RequestId}, Status: {Status}",
                request.Id,
                request.Status);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            this.logger.LogWarning("ProcessStoryRequest: Canceled by client/request token. RequestId: {RequestId}", request.Id);
            request.Status = StoryRequestStatus.Canceled;
            await this.db.SaveChangesAsync(CancellationToken.None);
            this.logger.LogWarning(
                "ProcessStoryRequest: Status transition persisted. RequestId: {RequestId}, Status: {Status}",
                request.Id,
                request.Status);
            throw;
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "ProcessStoryRequest: Pipeline failed. RequestId: {RequestId}", request.Id);
            request.Status = StoryRequestStatus.Failed;
            await this.db.SaveChangesAsync(CancellationToken.None);
            this.logger.LogError(
                "ProcessStoryRequest: Status transition persisted. RequestId: {RequestId}, Status: {Status}",
                request.Id,
                request.Status);
            throw;
        }
    }

    private string CreateSceneGraphSummary(SceneGraph sceneGraph)
    {
        if (sceneGraph is null || sceneGraph.Clusters.Count == 0)
        {
            return "Unknown Figurines";
        }

        Dictionary<string, int> figurineCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (SceneCluster cluster in sceneGraph.Clusters)
        {
            if (string.IsNullOrWhiteSpace(cluster.Contents))
            {
                continue;
            }

            List<(string Name, int Count)> parsedEntries = this.ParseClusterContents(cluster.Contents);
            foreach ((string Name, int Count) parsedEntry in parsedEntries)
            {
                if (figurineCounts.TryGetValue(parsedEntry.Name, out int currentCount))
                {
                    figurineCounts[parsedEntry.Name] = currentCount + parsedEntry.Count;
                    continue;
                }

                figurineCounts[parsedEntry.Name] = parsedEntry.Count;
            }
        }

        if (figurineCounts.Count == 0)
        {
            return "Unknown Figurines";
        }

        List<KeyValuePair<string, int>> sortedCounts = figurineCounts
            .OrderByDescending(x => x.Value)
            .ThenBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
            .ToList();

        List<string> summaryParts = new List<string>();
        foreach (KeyValuePair<string, int> entry in sortedCounts)
        {
            string displayName = this.ToDisplayName(entry.Key);
            if (entry.Value <= 1)
            {
                summaryParts.Add(this.Singularize(displayName));
                continue;
            }

            summaryParts.Add($"{entry.Value}x {this.Pluralize(displayName)}");
        }

        return string.Join(", ", summaryParts);
    }

    private List<(string Name, int Count)> ParseClusterContents(string contents)
    {
        string[] parts = contents.Split(" and ", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        List<(string Name, int Count)> result = new List<(string Name, int Count)>();

        foreach (string rawPart in parts)
        {
            string part = rawPart.Trim().Trim(',', '.');
            if (string.IsNullOrWhiteSpace(part))
            {
                continue;
            }

            Match countMatch = Regex.Match(part, @"^(?<count>\d+)\s+(?<name>.+)$", RegexOptions.CultureInvariant);
            if (countMatch.Success)
            {
                int parsedCount = int.Parse(countMatch.Groups["count"].Value, CultureInfo.InvariantCulture);
                string normalizedName = this.NormalizeName(countMatch.Groups["name"].Value);
                if (!string.IsNullOrWhiteSpace(normalizedName))
                {
                    result.Add((normalizedName, parsedCount));
                }

                continue;
            }

            Match articleMatch = Regex.Match(part, @"^(a|an)\s+(?<name>.+)$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            if (articleMatch.Success)
            {
                string normalizedName = this.NormalizeName(articleMatch.Groups["name"].Value);
                if (!string.IsNullOrWhiteSpace(normalizedName))
                {
                    result.Add((normalizedName, 1));
                }

                continue;
            }

            string fallbackName = this.NormalizeName(part);
            if (!string.IsNullOrWhiteSpace(fallbackName))
            {
                result.Add((fallbackName, 1));
            }
        }

        return result;
    }

    private string NormalizeName(string name)
    {
        string normalized = name.Trim().Trim(',', '.');
        normalized = normalized.Replace('_', ' ');
        normalized = normalized.Replace('-', ' ');
        normalized = Regex.Replace(normalized, @"\s+", " ", RegexOptions.CultureInvariant);
        normalized = normalized.ToLowerInvariant();

        if (normalized.EndsWith("es", StringComparison.Ordinal) && normalized.Length > 3)
        {
            string singularCandidate = normalized[..^2];
            if (singularCandidate.EndsWith("tre", StringComparison.Ordinal))
            {
                return singularCandidate + "e";
            }
        }

        if (normalized.EndsWith("s", StringComparison.Ordinal) &&
            !normalized.EndsWith("ss", StringComparison.Ordinal) &&
            normalized.Length > 2)
        {
            normalized = normalized[..^1];
        }

        return normalized;
    }

    private string ToDisplayName(string normalizedName)
    {
        TextInfo textInfo = CultureInfo.InvariantCulture.TextInfo;
        return textInfo.ToTitleCase(normalizedName);
    }

    private string Singularize(string displayName)
    {
        if (displayName.EndsWith("s", StringComparison.Ordinal) &&
            !displayName.EndsWith("ss", StringComparison.Ordinal) &&
            displayName.Length > 2)
        {
            return displayName[..^1];
        }

        return displayName;
    }

    private string Pluralize(string displayName)
    {
        if (displayName.EndsWith("s", StringComparison.Ordinal))
        {
            return displayName;
        }

        if (displayName.EndsWith("y", StringComparison.OrdinalIgnoreCase) && displayName.Length > 1)
        {
            string prefix = displayName[..^1];
            return $"{prefix}ies";
        }

        return $"{displayName}s";
    }

    /// <summary>
    /// Generates the story sentence-by-sentence. Each subsequent sentence is requested 
    /// only after the previous one is validated and added to the chat history.
    /// This ensures narrative coherence even if safety corrections happen.
    /// </summary>
    private async Task<StoryProductionResult> ProduceCoherentSentencesAsync(
        BoardConfig boardConfig,
        SceneGraph sceneGraph,
        ChannelWriter<string> writer,
        CancellationToken ct)
    {
        logger.LogDebug("Producer: Starting generation loop.");

        try
        {
            List<ChatMessage> chatHistory = new List<ChatMessage>
            {
                new ChatMessage(ChatRole.System,
                    $"Role: You are a gentle, engaging storyteller writing for a {boardConfig.AgeGroup} - year - old child.\n" +
                    $"Language: {boardConfig.Language}. \n" +
                    "Use culture specific character names and settings appropriate for the language.\n" +
                    "Instructions: Generate the story one segment (1-2 sentences) at a time. " +
                    "Respond ONLY with a JSON object containing 'sentence', 'isStoryFinished', and optional 'title'. " +
                    "When the story is finishing, include 'title' for the whole story."),
                new ChatMessage(ChatRole.User, $"Start a magical story for this scene: {JsonSerializer.Serialize(sceneGraph, AppJsonContext.Default.SceneGraph)}")
            };

            bool isFinished = false;
            string? finalTitle = null;
            string lastSafeSentence = string.Empty;
            int maxSentences = 15; // Safety cap
            int currentCount = 0;

            while (!isFinished && currentCount < maxSentences && !ct.IsCancellationRequested)
            {
                // Request the next segment of the story
                ChatResponse<StorySegment> response = await this.storyAgent.GetResponseAsync<StorySegment>(
                    chatHistory,
                    serializerOptions: this.jsonOptions.Value.SerializerOptions,
                    cancellationToken: ct);

                StorySegment segment = response.Result;
                string candidate = segment.sentence;
                logger.LogDebug("Producer: Generated raw segment {Index}. Finished: {IsFinished}", currentCount, segment.isStoryFinished);
                if (!string.IsNullOrWhiteSpace(segment.title))
                {
                    finalTitle = segment.title;
                }

                // Validate and potentially correct the sentence
                string safeSentence = await this.GetSafeSentenceWithRetryAsync(candidate, chatHistory, ct);
                lastSafeSentence = safeSentence;

                // Send the SAFE version to the audio consumer immediately
                await writer.WriteAsync(safeSentence, ct);
                logger.LogDebug("Producer: Wrote segment {Index} to channel.", currentCount);

                // CRITICAL: Append the SAFE version to history so the NEXT sentence 
                // is contextually aware of the corrected content.
                chatHistory.Add(new ChatMessage(ChatRole.Assistant, safeSentence));

                isFinished = segment.isStoryFinished;
                currentCount++;

                if (isFinished || currentCount >= maxSentences)
                {
                    continue;
                }

                bool shouldUseWrapUpPrompt = StoryStreamingPolicies.ShouldUseWrapUpPrompt(currentCount, maxSentences);
                string followUpPrompt = StoryStreamingPolicies.CreateFollowUpPrompt(shouldUseWrapUpPrompt);
                chatHistory.Add(new ChatMessage(ChatRole.User, followUpPrompt));
            }

            logger.LogDebug("Producer: Loop terminated. Count: {Count}", currentCount);
            return new StoryProductionResult(finalTitle, lastSafeSentence, currentCount, isFinished);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Producer: Fatal error during generation.");
            throw;
        }
        finally
        {
            writer.TryComplete();
            logger.LogDebug("Producer: Channel writer completed.");
        }
    }

    private async Task<StoryConsumptionResult> ConsumeSentencesToAudioAsync(
        StoryRequest request,
        string? voice,
        ChannelReader<string> reader,
        Stream responseStream,
        CancellationToken ct)
    {
        logger.LogDebug("Consumer: Starting audio processing.");
        double storyDurationSeconds = 0;
        int sequence = 0;

        await foreach (string sentence in reader.ReadAllAsync(ct))
        {
            sequence++;
            
            logger.LogDebug(
                "Consumer: Processing sentence for story request {RequestId}. Sequence: {Sequence}",
                request.Id,
                sequence);

            IAsyncEnumerable<ReadOnlyMemory<byte>> audioStream = this.audioGenerator.GenerateAudioStreamAsync(sentence, voice, ct);
            using MemoryStream sentenceAudioStream = new MemoryStream();

            await foreach (ReadOnlyMemory<byte> chunk in audioStream)
            {
                await responseStream.WriteAsync(chunk, ct);
                await responseStream.FlushAsync(ct);
                await sentenceAudioStream.WriteAsync(chunk, ct);
            }

            byte[] sentenceAudioBytes = sentenceAudioStream.ToArray();
            Mp3DurationParseResult parseResult = Mp3DurationParser.Parse(sentenceAudioBytes);
            storyDurationSeconds += parseResult.DurationSeconds;

            if (parseResult.InvalidHeaderCount > 0)
            {
                this.logger.LogWarning(
                    "Consumer: MP3 parse encountered invalid bytes. RequestId: {RequestId}, Sequence: {Sequence}, InvalidHeaderCount: {InvalidHeaderCount}, ParsedFrameCount: {ParsedFrameCount}",
                    request.Id,
                    sequence,
                    parseResult.InvalidHeaderCount,
                    parseResult.ParsedFrameCount);
            }

            string chunkObjectKey = await this.UploadChunkToMinioAsync(request.Id, sequence, sentenceAudioStream, ct);

            StoryRequestChunk persistedChunk = new StoryRequestChunk
            {
                StoryRequestId = request.Id,
                Sequence = sequence,
                Text = sentence,
                AudioObjectKey = chunkObjectKey
            };

            this.db.StoryRequestChunks.Add(persistedChunk);
            await this.db.SaveChangesAsync(ct);

            logger.LogDebug(
                "Consumer: Persisted chunk for story request {RequestId}. Sequence: {Sequence}, AudioObjectKey: {AudioObjectKey}",
                request.Id,
                sequence,
                chunkObjectKey);
        }

        int roundedDurationSeconds = (int)Math.Round(storyDurationSeconds, MidpointRounding.AwayFromZero);
        logger.LogDebug("Consumer: Audio generation complete for story request {RequestId}.", request.Id);
        return new StoryConsumptionResult(roundedDurationSeconds);
    }

    private async Task<string> GetSafeSentenceWithRetryAsync(string candidate, List<ChatMessage> history, CancellationToken ct)
    {
        (bool IsValid, string Reason) validationResult = await this.ValidateContentAsync(candidate, ct);

        if (validationResult.IsValid)
        {
            return candidate;
        }

        logger.LogWarning("SafetyCheck: Content rejected. Reason: {Reason}. Triggering correction.", validationResult.Reason);

        // Context for correction
        List<ChatMessage> correctionContext = new List<ChatMessage>(history)
        {
            new ChatMessage(ChatRole.Assistant, candidate),
            new ChatMessage(ChatRole.User, $"REJECTED: {validationResult.Reason}. Rewrite this specific segment to be kid-friendly while keeping the plot moving.")
        };

        ChatResponse<StoryCorrectionResult> response = await this.storyAgent.GetResponseAsync<StoryCorrectionResult>(
            correctionContext,
            serializerOptions: this.jsonOptions.Value.SerializerOptions,
            cancellationToken: ct);

        string corrected = response.Result.correctedSentence ?? "And then something magical happened.";
        logger.LogDebug("SafetyCheck: Correction received: {Snippet}...", corrected.Length > 20 ? corrected[..20] : corrected);
        return corrected;
    }

    private async Task<(bool IsValid, string Reason)> ValidateContentAsync(string text, CancellationToken ct)
    {
        ChatResponse<StoryValidationResult> response = await this.storyAgent.GetResponseAsync<StoryValidationResult>(
            new ChatMessage(ChatRole.User, $"Evaluate this sentence for a child: \"{text}\""),
            serializerOptions: this.jsonOptions.Value.SerializerOptions,
            cancellationToken: ct
        );

        return (response.Result.isValid, response.Result.reason);
    }

    // TODO: move to a minio service class
    private async Task<string> UploadChunkToMinioAsync(Guid requestId, int sequence, MemoryStream data, CancellationToken ct)
    {
        data.Position = 0;
        string key = $"stories/{requestId}/chunks/{sequence:D4}.mp3";
        logger.LogDebug(
            "Minio: Uploading chunk for story request {RequestId}. Sequence: {Sequence}, Size: {Size}, Key: {Key}",
            requestId,
            sequence,
            data.Length,
            key);

        PutObjectArgs args = new PutObjectArgs()
            .WithBucket(this.minioOptions.Bucket)
            .WithObject(key)
            .WithStreamData(data)
            .WithObjectSize(data.Length)
            .WithContentType("audio/mpeg");

        _ = await this.minio.PutObjectAsync(args, ct);
        this.logger.LogDebug(
            "Minio: Upload completed for story request {RequestId}. Sequence: {Sequence}, Key: {Key}",
            requestId,
            sequence,
            key);

        return key;
    }
}
