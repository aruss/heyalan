namespace SquareBuddy.WebApi;

using SquareBuddy.Data.Entities;
using SquareBuddy.WebApi.Core;
using System.Text.Json.Serialization;

[JsonSourceGenerationOptions(
    WriteIndented = false, 
    UseStringEnumConverter = true, 
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(int))]
[JsonSerializable(typeof(bool))]
[JsonSerializable(typeof(StreamStoryInput))]
[JsonSerializable(typeof(Story))]
[JsonSerializable(typeof(Figurine))]
[JsonSerializable(typeof(StoryRequest))]
[JsonSerializable(typeof(SceneGraph))]
[JsonSerializable(typeof(SceneCluster))]
[JsonSerializable(typeof(SceneRelation))]
[JsonSerializable(typeof(Point))]
[JsonSerializable(typeof(StoryRequestStatus))]
[JsonSerializable(typeof(BoardConfig))]
[JsonSerializable(typeof(StoryValidationResult))]
[JsonSerializable(typeof(StoryCorrectionResult))]
[JsonSerializable(typeof(StorySegment))]
public partial class AppJsonContext : JsonSerializerContext
{
}

