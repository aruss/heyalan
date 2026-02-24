namespace SquareBuddy.WebApi.Core;

using System.Threading;

public interface IStoryGenerator
{
    Task<Story> GenerateStoryAsync(
        StreamStoryInput input, 
        CancellationToken cancellationToken = default);
}
