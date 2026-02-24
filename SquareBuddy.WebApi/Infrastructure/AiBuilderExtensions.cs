namespace SquareBuddy.WebApi.Infrastructure;

using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using OpenAI;
using SquareBuddy.Configuration;
using System.ClientModel;

public static class AiBuilderExtensions
{
    public static TBuilder AddAiServices<TBuilder>(this TBuilder builder) 
        where TBuilder : IHostApplicationBuilder
    {
        LiteLlmOptions liteLlmOptions = builder.Configuration.TryGetLiteLlmOptions();

        OpenAIClientOptions openAiOptions = new()
        {
            Endpoint = liteLlmOptions.Endpoint,
            NetworkTimeout = TimeSpan.FromMinutes(5),
        };

        ApiKeyCredential openAiCredential = new(liteLlmOptions.ApiKey);

        builder.Services.AddChatClient(sp =>
        {
            OpenAI.Chat.ChatClient openAIClient = new(
                model: "gpt-4o-mini",
                credential: openAiCredential,
                options: openAiOptions);

            return openAIClient.AsIChatClient();
        })
        .UseDistributedCache()
        .UseLogging();

        builder.Services.AddSingleton(sp =>
        {
            return new OpenAI.Audio.AudioClient("tts-1", openAiCredential, openAiOptions);
        });


        return builder;
    }
}