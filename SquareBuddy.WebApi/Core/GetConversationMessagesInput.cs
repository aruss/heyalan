namespace SquareBuddy.WebApi.Core;

using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

public class GetConversationMessagesInput
{
    public Guid AgentId { get; init; }

    public Guid ConversationId { get; init; }

    [JsonPropertyName("skip")]
    [DefaultValue(Constants.SkipDefault)]
    [Range(Constants.SkipMin, Constants.SkipMax)]
    public int Skip { get; init; } = Constants.SkipDefault;

    [JsonPropertyName("take")]
    [DefaultValue(Constants.TakeDefault)]
    [Range(Constants.TakeMin, Constants.TakeMax)]
    public int Take { get; init; } = Constants.TakeDefault;
}
