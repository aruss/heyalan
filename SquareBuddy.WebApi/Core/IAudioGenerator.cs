namespace SquareBuddy.WebApi.Core;

public interface IAudioGenerator
{
    IAsyncEnumerable<ReadOnlyMemory<byte>> GenerateAudioStreamAsync(
        string text,
        string? voice = null,
        CancellationToken cancellationToken = default);
}
