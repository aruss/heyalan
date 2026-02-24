namespace SquareBuddy;

using System.Text.Json.Serialization;

[JsonConverter(typeof(JsonStringEnumConverter<StoryRequestStatus>))]
public enum StoryRequestStatus
{
    Processing = 0,
    Failed = 1,
    Completed = 2,
    Canceled = 3,
}
