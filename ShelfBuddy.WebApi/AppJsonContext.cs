namespace ShelfBuddy.WebApi;

using System.Text.Json.Serialization;

[JsonSourceGenerationOptions(
    WriteIndented = false, 
    UseStringEnumConverter = true, 
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(int))]
[JsonSerializable(typeof(bool))]
public partial class AppJsonContext : JsonSerializerContext
{
}

