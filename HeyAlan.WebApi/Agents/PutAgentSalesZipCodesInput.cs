namespace HeyAlan.WebApi.Agents;

public sealed record PutAgentSalesZipCodesInput(
    IReadOnlyCollection<string> ZipCodes);
