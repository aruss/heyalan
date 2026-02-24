namespace SquareBuddy.WebApi.Core;

using OpenAI.Audio;
using System.ClientModel;
using System.Runtime.CompilerServices;

public class OpenAiAudioGenerator : IAudioGenerator
{
    private readonly AudioClient audioClient;

    public OpenAiAudioGenerator(AudioClient audioClient)
    {
        this.audioClient = audioClient ?? throw new ArgumentNullException(nameof(audioClient));
    }

    public async IAsyncEnumerable<ReadOnlyMemory<byte>> GenerateAudioStreamAsync(
        string text,
        string? voice = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        GeneratedSpeechVoice selectedVoice = this.ParseVoice(voice);

        foreach (string segment in this.GetTextSegments(text))
        {
            ClientResult<BinaryData> result;

            try
            {
                // Request atomic chunk from OpenAI
                result = await this.audioClient.GenerateSpeechAsync(
                    segment,
                    selectedVoice,
                    new SpeechGenerationOptions { ResponseFormat = GeneratedSpeechFormat.Mp3 },
                    cancellationToken
                );
            }
            catch (ClientResultException)
            {
                // Policy: In a kids' toy, we skip failed audio segments to keep the flow
                continue;
            }

            // Yield the binary data to the consumer
            yield return result.Value.ToMemory();
        }
    }

    private IEnumerable<string> GetTextSegments(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            yield break;
        }

        // Split by punctuation while maintaining flow. 
        // Max limit is 4096, but for streaming we prefer sentence chunks.
        string[] sentences = System.Text.RegularExpressions.Regex.Split(text, @"(?<=[.!?])\s+");

        foreach (string sentence in sentences)
        {
            if (string.IsNullOrWhiteSpace(sentence))
            {
                continue;
            }

            if (sentence.Length > 4096)
            {
                int iterations = (sentence.Length / 4096) + 1;
                for (int i = 0; i < iterations; i++)
                {
                    int start = i * 4096;
                    int length = Math.Min(4096, sentence.Length - start);
                    if (length <= 0) break;

                    yield return sentence.Substring(start, length);
                }
            }
            else
            {
                yield return sentence.Trim();
            }
        }
    }

    private GeneratedSpeechVoice ParseVoice(string? voice)
    {
        return voice?.ToLower() switch
        {
            "echo" => GeneratedSpeechVoice.Echo,
            "fable" => GeneratedSpeechVoice.Fable,
            "onyx" => GeneratedSpeechVoice.Onyx,
            "nova" => GeneratedSpeechVoice.Nova,
            "shimmer" => GeneratedSpeechVoice.Shimmer,
            _ => GeneratedSpeechVoice.Alloy
        };
    }
}