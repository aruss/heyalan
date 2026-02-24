namespace SquareBuddy.WebApi.Core;

public static class CoreBuilderExtensions
{
    public static TBuilder AddCoreServices<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        builder.Services.AddScoped<StoryStreamingService>();
        builder.Services.AddScoped<ISceneGraphGenerator, SceneGraphGenerator>();
        builder.Services.AddScoped<IAudioGenerator, OpenAiAudioGenerator>();

        return builder; 
    }
}