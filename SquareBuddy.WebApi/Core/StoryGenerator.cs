namespace SquareBuddy.WebApi.Core;

using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using System.Threading;
using static SquareBuddy.WebApi.Core.StoryEndpoints;

public class StoryGenerator : IStoryGenerator
{
    private readonly IChatClient chatClient;
    private readonly IOptions<JsonOptions> jsonOptions; 

    public StoryGenerator(
        IChatClient chatClient, 
        IOptions<JsonOptions> jsonOptions)
    {
        this.chatClient = chatClient;
        this.jsonOptions = jsonOptions; 
    }

    public async Task<Story> GenerateStoryAsync(
        StreamStoryInput input, 
        CancellationToken cancellationToken = default)
    {
        ChatResponse<Story> response = await this.chatClient.GetResponseAsync<Story>(
           new ChatMessage(ChatRole.User, "Write a gentle, calming bedtime story about a cat. Make it suitable for a 5-year-old."),
           serializerOptions: this.jsonOptions.Value.SerializerOptions,
           cancellationToken: cancellationToken
        );

        return response.Result;
    }
}
