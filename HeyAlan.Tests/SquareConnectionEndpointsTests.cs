namespace HeyAlan.Tests;

using System.Reflection;
using System.Text.Json;
using HeyAlan.WebApi.SquareIntegration;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

public class SquareConnectionEndpointsTests
{
    [Theory]
    [InlineData("subscription_owner_required", StatusCodes.Status403Forbidden)]
    [InlineData("connection_not_found", StatusCodes.Status404NotFound)]
    [InlineData("square_not_configured", StatusCodes.Status400BadRequest)]
    public async Task MapError_MapsStatusCodesWithoutRegression(string errorCode, int expectedStatusCode)
    {
        MethodInfo method = typeof(SquareConnectionEndpoints).GetMethod(
            "MapError",
            BindingFlags.NonPublic | BindingFlags.Static)!;

        IResult result = (IResult)method.Invoke(null, [errorCode])!;
        HttpContext context = CreateHttpContext();
        context.Response.Body = new MemoryStream();

        await result.ExecuteAsync(context);

        Assert.Equal(expectedStatusCode, context.Response.StatusCode);
        context.Response.Body.Position = 0;
        using StreamReader reader = new(context.Response.Body);
        string json = await reader.ReadToEndAsync();
        Assert.Contains(errorCode, json, StringComparison.Ordinal);
    }

    [Fact]
    public async Task UnauthorizedError_Returns401WithExpectedMessage()
    {
        MethodInfo method = typeof(SquareConnectionEndpoints).GetMethod(
            "UnauthorizedError",
            BindingFlags.NonPublic | BindingFlags.Static)!;

        IResult result = (IResult)method.Invoke(null, ["unauthenticated"])!;
        HttpContext context = CreateHttpContext();
        context.Response.Body = new MemoryStream();

        await result.ExecuteAsync(context);

        Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
        context.Response.Body.Position = 0;
        using StreamReader reader = new(context.Response.Body);
        string json = await reader.ReadToEndAsync();
        using JsonDocument document = JsonDocument.Parse(json);
        string? message = document.RootElement.GetProperty("message").GetString();
        Assert.Equal("Authentication is required.", message);
    }

    private static HttpContext CreateHttpContext()
    {
        ServiceCollection services = new();
        services.AddLogging();
        services.ConfigureHttpJsonOptions(_ => { });

        DefaultHttpContext context = new();
        context.RequestServices = services.BuildServiceProvider();
        return context;
    }
}
