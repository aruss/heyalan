namespace HeyAlan.Tests;

using System.Reflection;
using HeyAlan.WebApi.Agents;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

public class AgentEndpointsTests
{
    [Theory]
    [InlineData("catalog_product_not_found", StatusCodes.Status400BadRequest)]
    [InlineData("agent_catalog_assignment_invalid", StatusCodes.Status400BadRequest)]
    [InlineData("agent_sales_zip_invalid", StatusCodes.Status400BadRequest)]
    [InlineData("agent_sales_zip_conflict", StatusCodes.Status400BadRequest)]
    [InlineData("agent_not_found", StatusCodes.Status404NotFound)]
    [InlineData("subscription_member_required", StatusCodes.Status403Forbidden)]
    public async Task MapError_MapsGateIStatusCodesWithoutRegression(string errorCode, int expectedStatusCode)
    {
        MethodInfo method = typeof(AgentEndpoints).GetMethod(
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
