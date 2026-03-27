namespace BuyAlan.WebApi.Tests;

using System.Reflection;
using System.Text.Json;
using BuyAlan.SquareIntegration;
using BuyAlan.WebApi.SquareIntegration;
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

    [Fact]
    public async Task CompleteSquareConnectCallbackAsync_WhenStateIsValidConnectState_UsesConnectFlowRedirect()
    {
        MethodInfo method = typeof(SquareConnectionEndpoints).GetMethod(
            "CompleteSquareConnectCallbackAsync",
            BindingFlags.NonPublic | BindingFlags.Static)!;

        DefaultHttpContext context = (DefaultHttpContext)CreateHttpContext();
        context.Request.QueryString = new QueryString("?code=connect-code&state=connect-state");
        StubSquareService service = new()
        {
            CompleteConnectResult = new CompleteSquareConnectResult.Success("/onboarding?squareConnect=success")
        };
        StubOAuthStateProtector stateProtector = new()
        {
            TryUnprotectResult = true,
            Payload = new SquareConnectStatePayload(Guid.NewGuid(), Guid.NewGuid(), "/onboarding", DateTime.UtcNow)
        };

        string redirectUrl = await InvokeRedirectAsync(
            method,
            context,
            "connect-code",
            "connect-state",
            null,
            stateProtector,
            service);

        Assert.Equal("/onboarding?squareConnect=success", redirectUrl);
        Assert.Equal(1, service.CompleteConnectCallCount);
        Assert.NotNull(service.LastCompleteConnectInput);
        Assert.Equal("connect-state", service.LastCompleteConnectInput!.State);
        Assert.Equal("connect-code", service.LastCompleteConnectInput.AuthorizationCode);
    }

    [Fact]
    public async Task CompleteSquareConnectCallbackAsync_WhenStateIsOpaqueExternalAuthState_RedirectsToInternalAuthCallbackWithoutCallingConnectFlow()
    {
        MethodInfo method = typeof(SquareConnectionEndpoints).GetMethod(
            "CompleteSquareConnectCallbackAsync",
            BindingFlags.NonPublic | BindingFlags.Static)!;

        DefaultHttpContext context = (DefaultHttpContext)CreateHttpContext();
        context.Request.PathBase = new PathString("/tenant");
        context.Request.QueryString = new QueryString("?code=auth-code&state=opaque-state&error=access_denied&response_type=code");
        StubSquareService service = new();
        StubOAuthStateProtector stateProtector = new()
        {
            TryUnprotectResult = false
        };

        string redirectUrl = await InvokeRedirectAsync(
            method,
            context,
            "auth-code",
            "opaque-state",
            "access_denied",
            stateProtector,
            service);

        Assert.Equal(
            "/tenant/auth/providers/square/callback?code=auth-code&state=opaque-state&error=access_denied&response_type=code",
            redirectUrl);
        Assert.Equal(0, service.CompleteConnectCallCount);
    }

    [Fact]
    public async Task CompleteSquareConnectCallbackAsync_WhenStateMissing_PreservesExistingInvalidStateConnectFailureBehavior()
    {
        MethodInfo method = typeof(SquareConnectionEndpoints).GetMethod(
            "CompleteSquareConnectCallbackAsync",
            BindingFlags.NonPublic | BindingFlags.Static)!;

        DefaultHttpContext context = (DefaultHttpContext)CreateHttpContext();
        context.Request.QueryString = new QueryString("?code=connect-code");
        StubSquareService service = new()
        {
            CompleteConnectResult = new CompleteSquareConnectResult.Failure(
                "/onboarding?squareConnectError=square_oauth_state_invalid",
                "square_oauth_state_invalid")
        };
        StubOAuthStateProtector stateProtector = new()
        {
            TryUnprotectResult = false
        };

        string redirectUrl = await InvokeRedirectAsync(
            method,
            context,
            "connect-code",
            null,
            null,
            stateProtector,
            service);

        Assert.Equal("/onboarding?squareConnectError=square_oauth_state_invalid", redirectUrl);
        Assert.Equal(1, service.CompleteConnectCallCount);
        Assert.Null(service.LastCompleteConnectInput!.State);
    }

    private static async Task<string> InvokeRedirectAsync(
        MethodInfo method,
        HttpContext context,
        string? code,
        string? state,
        string? oauthError,
        IOAuthStateProtector stateProtector,
        ISquareService service)
    {
        Task task = (Task)method.Invoke(
            null,
            [context, code, state, oauthError, stateProtector, service, CancellationToken.None])!;

        await task;

        object? result = task.GetType().GetProperty("Result")?.GetValue(task);
        string? redirectUrl = result?.GetType().GetProperty("Url")?.GetValue(result) as string;
        return redirectUrl ?? throw new InvalidOperationException("Redirect result URL was not available.");
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

    private sealed class StubSquareService : ISquareService
    {
        public CompleteSquareConnectResult CompleteConnectResult { get; init; }
            = new CompleteSquareConnectResult.Success("/default");

        public int CompleteConnectCallCount { get; private set; }

        public CompleteSquareConnectInput? LastCompleteConnectInput { get; private set; }

        public Task<StartSquareConnectResult> StartConnectAsync(
            StartSquareConnectInput input,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<CompleteSquareConnectResult> CompleteConnectAsync(
            CompleteSquareConnectInput input,
            CancellationToken cancellationToken = default)
        {
            this.CompleteConnectCallCount++;
            this.LastCompleteConnectInput = input;
            return Task.FromResult(this.CompleteConnectResult);
        }

        public Task<DisconnectSquareConnectionResult> DisconnectAsync(
            DisconnectSquareConnectionInput input,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task StoreConnectionAsync(
            SquareTokenStoreInput input,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<SquareTokenResolution> GetValidAccessTokenAsync(
            Guid subscriptionId,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<IReadOnlyCollection<SquareTeamMemberResult>> GetTeamMembersAsync(
            Guid subscriptionId,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<SquareTokenExchangeResult> ExchangeAuthorizationCodeAsync(
            string authorizationCode,
            string redirectUri,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<SquareRevokeResult> RevokeAccessTokenAsync(
            string accessToken,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class StubOAuthStateProtector : IOAuthStateProtector
    {
        public bool TryUnprotectResult { get; init; }

        public SquareConnectStatePayload? Payload { get; init; }

        public string Protect(SquareConnectStatePayload payload)
        {
            throw new NotSupportedException();
        }

        public bool TryUnprotect(string protectedState, out SquareConnectStatePayload? payload)
        {
            payload = this.Payload;
            return this.TryUnprotectResult;
        }
    }
}
