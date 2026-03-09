namespace HeyAlan.WebApi.Agents;

using HeyAlan.Data;
using HeyAlan.Data.Entities;
using HeyAlan.Extensions;
using Microsoft.AspNetCore.Mvc;
using HeyAlan.Agents;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

public static class AgentEndpoints
{
    public static IEndpointRouteBuilder MapAgentEndpoints(this IEndpointRouteBuilder routeBuilder)
    {
        RouteGroupBuilder routeGroup = routeBuilder
            .MapGroup("/agents")
            .WithTags("Agents")
            .RequireAuthorization();

        routeGroup
            .MapGet(String.Empty, GetAgentsAsync)
            .Produces<GetAgentsResult>(StatusCodes.Status200OK)
            .Produces<AgentErrorResult>(StatusCodes.Status401Unauthorized)
            .Produces<AgentErrorResult>(StatusCodes.Status403Forbidden);

        routeGroup
            .MapPost(String.Empty, PostAgentsAsync)
            .Produces<AgentResult>(StatusCodes.Status200OK)
            .Produces<AgentErrorResult>(StatusCodes.Status401Unauthorized)
            .Produces<AgentErrorResult>(StatusCodes.Status403Forbidden);

        routeGroup
            .MapGet("{agentId:guid}", GetAgentAsync)
            .Produces<AgentResult>(StatusCodes.Status200OK)
            .Produces<AgentErrorResult>(StatusCodes.Status401Unauthorized)
            .Produces<AgentErrorResult>(StatusCodes.Status403Forbidden)
            .Produces<AgentErrorResult>(StatusCodes.Status404NotFound);

        routeGroup
            .MapPost("{agentId:guid}", PostAgentAsync)
            .Produces<AgentResult>(StatusCodes.Status200OK)
            .Produces<AgentErrorResult>(StatusCodes.Status400BadRequest)
            .Produces<AgentErrorResult>(StatusCodes.Status401Unauthorized)
            .Produces<AgentErrorResult>(StatusCodes.Status403Forbidden)
            .Produces<AgentErrorResult>(StatusCodes.Status404NotFound);

        routeGroup
            .MapDelete("{agentId:guid}", DeleteAgentAsync)
            .Produces<DeleteAgentResult>(StatusCodes.Status200OK)
            .Produces<AgentErrorResult>(StatusCodes.Status401Unauthorized)
            .Produces<AgentErrorResult>(StatusCodes.Status403Forbidden)
            .Produces<AgentErrorResult>(StatusCodes.Status404NotFound);

        routeGroup
            .MapGet("{agentId:guid}/catalog/products", GetAgentCatalogProductsAsync)
            .Produces<GetAgentCatalogProductsResult>(StatusCodes.Status200OK)
            .Produces<AgentErrorResult>(StatusCodes.Status401Unauthorized)
            .Produces<AgentErrorResult>(StatusCodes.Status403Forbidden)
            .Produces<AgentErrorResult>(StatusCodes.Status404NotFound);

        routeGroup
            .MapPut("{agentId:guid}/catalog/products", PutAgentCatalogProductsAsync)
            .Produces<AgentCatalogProductAccessStateResult>(StatusCodes.Status200OK)
            .Produces<AgentErrorResult>(StatusCodes.Status400BadRequest)
            .Produces<AgentErrorResult>(StatusCodes.Status401Unauthorized)
            .Produces<AgentErrorResult>(StatusCodes.Status403Forbidden)
            .Produces<AgentErrorResult>(StatusCodes.Status404NotFound);

        routeGroup
            .MapDelete("{agentId:guid}/catalog/products", DeleteAgentCatalogProductsAsync)
            .Produces<AgentCatalogProductAccessStateResult>(StatusCodes.Status200OK)
            .Produces<AgentErrorResult>(StatusCodes.Status401Unauthorized)
            .Produces<AgentErrorResult>(StatusCodes.Status403Forbidden)
            .Produces<AgentErrorResult>(StatusCodes.Status404NotFound);

        routeGroup
            .MapGet("{agentId:guid}/sales-zips", GetAgentSalesZipCodesAsync)
            .Produces<AgentSalesZipCodeStateResult>(StatusCodes.Status200OK)
            .Produces<AgentErrorResult>(StatusCodes.Status401Unauthorized)
            .Produces<AgentErrorResult>(StatusCodes.Status403Forbidden)
            .Produces<AgentErrorResult>(StatusCodes.Status404NotFound);

        routeGroup
            .MapPut("{agentId:guid}/sales-zips", PutAgentSalesZipCodesAsync)
            .Produces<AgentSalesZipCodeStateResult>(StatusCodes.Status200OK)
            .Produces<AgentErrorResult>(StatusCodes.Status400BadRequest)
            .Produces<AgentErrorResult>(StatusCodes.Status401Unauthorized)
            .Produces<AgentErrorResult>(StatusCodes.Status403Forbidden)
            .Produces<AgentErrorResult>(StatusCodes.Status404NotFound);

        routeGroup
            .MapDelete("{agentId:guid}/sales-zips", DeleteAgentSalesZipCodesAsync)
            .Produces<AgentSalesZipCodeStateResult>(StatusCodes.Status200OK)
            .Produces<AgentErrorResult>(StatusCodes.Status401Unauthorized)
            .Produces<AgentErrorResult>(StatusCodes.Status403Forbidden)
            .Produces<AgentErrorResult>(StatusCodes.Status404NotFound);

        return routeBuilder;
    }

    private static async Task<IResult> GetAgentsAsync(
        [AsParameters] GetAgentsInput input,
        ClaimsPrincipal user,
        ISubscriptionAgentService service,
        CancellationToken cancellationToken)
    {
        Guid? userId = user.GetUserId();
        if (userId is null)
        {
            return UnauthorizedError("unauthenticated");
        }

        HeyAlan.Agents.GetSubscriptionAgentsResult result = await service.GetAgentsAsync(
            new GetSubscriptionAgentsInput(input.Subscription, userId.Value),
            cancellationToken);

        if (result is HeyAlan.Agents.GetSubscriptionAgentsResult.Failure failure)
        {
            return MapError(failure.ErrorCode);
        }

        HeyAlan.Agents.GetSubscriptionAgentsResult.Success success =
            (HeyAlan.Agents.GetSubscriptionAgentsResult.Success)result;

        List<AgentItem> pagedAgents = success.Agents
            .Select(item => new AgentItem(
                item.AgentId,
                item.Name,
                item.Personality,
                item.IsOperationalReady,
                item.CreatedAt,
                item.UpdatedAt))
            .Skip(input.Skip)
            .Take(input.Take + 1)
            .ToList();

        return TypedResults.Ok(new GetAgentsResult(pagedAgents, input.Skip, input.Take));
    }

    private static async Task<IResult> PostAgentsAsync(
        [AsParameters] PostAgentsInput input,
        ClaimsPrincipal user,
        ISubscriptionAgentService service,
        CancellationToken cancellationToken)
    {
        Guid? userId = user.GetUserId();
        if (userId is null)
        {
            return UnauthorizedError("unauthenticated");
        }

        CreateSubscriptionAgentResult result = await service.CreateAgentAsync(
            new CreateSubscriptionAgentInput(input.Subscription, userId.Value),
            cancellationToken);

        if (result is CreateSubscriptionAgentResult.Failure failure)
        {
            return MapError(failure.ErrorCode);
        }

        CreateSubscriptionAgentResult.Success success = (CreateSubscriptionAgentResult.Success)result;
        return TypedResults.Ok(ToAgentResult(success.Agent));
    }

    private static async Task<IResult> GetAgentAsync(
        [FromRoute] Guid agentId,
        ClaimsPrincipal user,
        ISubscriptionAgentService service,
        CancellationToken cancellationToken)
    {
        Guid? userId = user.GetUserId();
        if (userId is null)
        {
            return UnauthorizedError("unauthenticated");
        }

        HeyAlan.Agents.GetAgentResult result = await service.GetAgentAsync(
            new GetAgentInput(agentId, userId.Value),
            cancellationToken);

        if (result is HeyAlan.Agents.GetAgentResult.Failure failure)
        {
            return MapError(failure.ErrorCode);
        }

        HeyAlan.Agents.GetAgentResult.Success success = (HeyAlan.Agents.GetAgentResult.Success)result;
        return TypedResults.Ok(ToAgentResult(success.Agent));
    }

    private static async Task<IResult> PostAgentAsync(
        [FromRoute] Guid agentId,
        [FromBody] PostAgentInput input,
        ClaimsPrincipal user,
        ISubscriptionAgentService service,
        CancellationToken cancellationToken)
    {
        Guid? userId = user.GetUserId();
        if (userId is null)
        {
            return UnauthorizedError("unauthenticated");
        }

        UpdateAgentResult result = await service.UpdateAgentAsync(
            new UpdateAgentInput(
                agentId,
                userId.Value,
                input.Name,
                input.Personality,
                input.PersonalityPromptRaw,
                input.TwilioPhoneNumber,
                input.TelegramBotToken,
                input.WhatsappNumber),
            cancellationToken);

        if (result is UpdateAgentResult.Failure failure)
        {
            return MapError(failure.ErrorCode);
        }

        UpdateAgentResult.Success success = (UpdateAgentResult.Success)result;
        return TypedResults.Ok(ToAgentResult(success.Agent));
    }

    private static async Task<IResult> DeleteAgentAsync(
        [FromRoute] Guid agentId,
        ClaimsPrincipal user,
        ISubscriptionAgentService service,
        CancellationToken cancellationToken)
    {
        Guid? userId = user.GetUserId();
        if (userId is null)
        {
            return UnauthorizedError("unauthenticated");
        }

        HeyAlan.Agents.DeleteAgentResult result = await service.DeleteAgentAsync(
            new DeleteAgentInput(agentId, userId.Value),
            cancellationToken);

        if (result is HeyAlan.Agents.DeleteAgentResult.Failure failure)
        {
            return MapError(failure.ErrorCode);
        }

        return TypedResults.Ok(new DeleteAgentResult(true));
    }

    private static async Task<IResult> GetAgentCatalogProductsAsync(
        [FromRoute] Guid agentId,
        [AsParameters] GetAgentCatalogProductsInput input,
        ClaimsPrincipal user,
        MainDataContext dbContext,
        IAgentCatalogProductAccessService accessService,
        CancellationToken cancellationToken)
    {
        AuthorizedAgentResult authorizedAgent = await GetAuthorizedAgentAsync(
            agentId,
            user,
            dbContext,
            cancellationToken);

        if (authorizedAgent.ErrorResult is not null)
        {
            return authorizedAgent.ErrorResult;
        }

        HeyAlan.Agents.GetAgentCatalogProductAccessStateOperationResult accessStateResult = await accessService.GetStateAsync(
            new HeyAlan.Agents.GetAgentCatalogProductAccessStateInput(agentId),
            cancellationToken);

        if (accessStateResult is HeyAlan.Agents.GetAgentCatalogProductAccessStateOperationResult.Failure accessFailure)
        {
            return MapError(accessFailure.ErrorCode);
        }

        HeyAlan.Agents.GetAgentCatalogProductAccessStateOperationResult.Success accessSuccess =
            (HeyAlan.Agents.GetAgentCatalogProductAccessStateOperationResult.Success)accessStateResult;

        HashSet<Guid> assignedProductIds = accessSuccess.State.SubscriptionCatalogProductIds.ToHashSet();
        IQueryable<SubscriptionCatalogProduct> query = dbContext.SubscriptionCatalogProducts
            .Where(item => item.SubscriptionId == authorizedAgent.Agent!.SubscriptionId);

        string? normalizedQuery = NormalizeCatalogQuery(input.Query);
        if (!String.IsNullOrWhiteSpace(normalizedQuery))
        {
            query = query.Where(item => item.SearchText.Contains(normalizedQuery));
        }

        CursorList<AgentCatalogProductItem> cursor = await query
            .OrderBy(item => item.ItemName)
            .ThenBy(item => item.VariationName)
            .ThenBy(item => item.Id)
            .ToCursorListAsync(
                item => new AgentCatalogProductItem(
                    item.Id,
                    item.SquareItemId,
                    item.SquareVariationId,
                    item.ItemName,
                    item.VariationName,
                    item.Description,
                    item.Sku,
                    item.BasePriceAmount,
                    item.BasePriceCurrency,
                    item.IsSellable,
                    item.IsDeleted,
                    assignedProductIds.Contains(item.Id)),
                input.Skip,
                input.Take,
                cancellationToken);

        return TypedResults.Ok(new GetAgentCatalogProductsResult(
            cursor.Items.ToList(),
            cursor.Skip,
            cursor.Take,
            accessSuccess.State.HasExplicitAssignments));
    }

    private static async Task<IResult> PutAgentCatalogProductsAsync(
        [FromRoute] Guid agentId,
        [FromBody] PutAgentCatalogProductsInput input,
        ClaimsPrincipal user,
        MainDataContext dbContext,
        IAgentCatalogProductAccessService accessService,
        CancellationToken cancellationToken)
    {
        AuthorizedAgentResult authorizedAgent = await GetAuthorizedAgentAsync(
            agentId,
            user,
            dbContext,
            cancellationToken);

        if (authorizedAgent.ErrorResult is not null)
        {
            return authorizedAgent.ErrorResult;
        }

        ReplaceAgentCatalogProductAccessResult result = await accessService.ReplaceAssignmentsAsync(
            new ReplaceAgentCatalogProductAccessInput(
                agentId,
                input.SubscriptionCatalogProductIds),
            cancellationToken);

        if (result is ReplaceAgentCatalogProductAccessResult.Failure failure)
        {
            return MapError(failure.ErrorCode);
        }

        ReplaceAgentCatalogProductAccessResult.Success success = (ReplaceAgentCatalogProductAccessResult.Success)result;
        return TypedResults.Ok(ToAgentCatalogProductAccessStateResult(success.State));
    }

    private static async Task<IResult> DeleteAgentCatalogProductsAsync(
        [FromRoute] Guid agentId,
        ClaimsPrincipal user,
        MainDataContext dbContext,
        IAgentCatalogProductAccessService accessService,
        CancellationToken cancellationToken)
    {
        AuthorizedAgentResult authorizedAgent = await GetAuthorizedAgentAsync(
            agentId,
            user,
            dbContext,
            cancellationToken);

        if (authorizedAgent.ErrorResult is not null)
        {
            return authorizedAgent.ErrorResult;
        }

        ClearAgentCatalogProductAccessResult result = await accessService.ClearAssignmentsAsync(
            new ClearAgentCatalogProductAccessInput(agentId),
            cancellationToken);

        if (result is ClearAgentCatalogProductAccessResult.Failure failure)
        {
            return MapError(failure.ErrorCode);
        }

        ClearAgentCatalogProductAccessResult.Success success = (ClearAgentCatalogProductAccessResult.Success)result;
        return TypedResults.Ok(ToAgentCatalogProductAccessStateResult(success.State));
    }

    private static async Task<IResult> GetAgentSalesZipCodesAsync(
        [FromRoute] Guid agentId,
        ClaimsPrincipal user,
        MainDataContext dbContext,
        IAgentSalesZipCodeService zipCodeService,
        CancellationToken cancellationToken)
    {
        AuthorizedAgentResult authorizedAgent = await GetAuthorizedAgentAsync(
            agentId,
            user,
            dbContext,
            cancellationToken);

        if (authorizedAgent.ErrorResult is not null)
        {
            return authorizedAgent.ErrorResult;
        }

        HeyAlan.Agents.GetAgentSalesZipCodeStateOperationResult result = await zipCodeService.GetStateAsync(
            new HeyAlan.Agents.GetAgentSalesZipCodeStateInput(agentId),
            cancellationToken);

        if (result is HeyAlan.Agents.GetAgentSalesZipCodeStateOperationResult.Failure failure)
        {
            return MapError(failure.ErrorCode);
        }

        HeyAlan.Agents.GetAgentSalesZipCodeStateOperationResult.Success success =
            (HeyAlan.Agents.GetAgentSalesZipCodeStateOperationResult.Success)result;

        return TypedResults.Ok(ToAgentSalesZipCodeStateResult(success.State));
    }

    private static async Task<IResult> PutAgentSalesZipCodesAsync(
        [FromRoute] Guid agentId,
        [FromBody] PutAgentSalesZipCodesInput input,
        ClaimsPrincipal user,
        MainDataContext dbContext,
        IAgentSalesZipCodeService zipCodeService,
        CancellationToken cancellationToken)
    {
        AuthorizedAgentResult authorizedAgent = await GetAuthorizedAgentAsync(
            agentId,
            user,
            dbContext,
            cancellationToken);

        if (authorizedAgent.ErrorResult is not null)
        {
            return authorizedAgent.ErrorResult;
        }

        ReplaceAgentSalesZipCodesResult result = await zipCodeService.ReplaceZipCodesAsync(
            new ReplaceAgentSalesZipCodesInput(
                agentId,
                input.ZipCodes),
            cancellationToken);

        if (result is ReplaceAgentSalesZipCodesResult.Failure failure)
        {
            return MapError(failure.ErrorCode);
        }

        ReplaceAgentSalesZipCodesResult.Success success = (ReplaceAgentSalesZipCodesResult.Success)result;
        return TypedResults.Ok(ToAgentSalesZipCodeStateResult(success.State));
    }

    private static async Task<IResult> DeleteAgentSalesZipCodesAsync(
        [FromRoute] Guid agentId,
        ClaimsPrincipal user,
        MainDataContext dbContext,
        IAgentSalesZipCodeService zipCodeService,
        CancellationToken cancellationToken)
    {
        AuthorizedAgentResult authorizedAgent = await GetAuthorizedAgentAsync(
            agentId,
            user,
            dbContext,
            cancellationToken);

        if (authorizedAgent.ErrorResult is not null)
        {
            return authorizedAgent.ErrorResult;
        }

        ClearAgentSalesZipCodesResult result = await zipCodeService.ClearZipCodesAsync(
            new ClearAgentSalesZipCodesInput(agentId),
            cancellationToken);

        if (result is ClearAgentSalesZipCodesResult.Failure failure)
        {
            return MapError(failure.ErrorCode);
        }

        ClearAgentSalesZipCodesResult.Success success = (ClearAgentSalesZipCodesResult.Success)result;
        return TypedResults.Ok(ToAgentSalesZipCodeStateResult(success.State));
    }

    private static AgentResult ToAgentResult(AgentDetailsResult agent)
    {
        return new AgentResult(
            agent.AgentId,
            agent.SubscriptionId,
            agent.Name,
            agent.Personality,
            agent.PersonalityPromptRaw,
            agent.TwilioPhoneNumber,
            agent.WhatsappNumber,
            agent.TelegramBotToken,
            agent.IsOperationalReady,
            agent.CreatedAt,
            agent.UpdatedAt);
    }

    private static AgentCatalogProductAccessStateResult ToAgentCatalogProductAccessStateResult(
        HeyAlan.Agents.AgentCatalogProductAccessStateResult state)
    {
        return new AgentCatalogProductAccessStateResult(
            state.AgentId,
            state.SubscriptionId,
            state.HasExplicitAssignments,
            state.SubscriptionCatalogProductIds);
    }

    private static AgentSalesZipCodeStateResult ToAgentSalesZipCodeStateResult(
        HeyAlan.Agents.AgentSalesZipCodeStateResult state)
    {
        return new AgentSalesZipCodeStateResult(
            state.AgentId,
            state.SubscriptionId,
            state.HasExplicitZipRestrictions,
            state.ZipCodesNormalized);
    }

    private static IResult UnauthorizedError(string errorCode)
    {
        AgentErrorResult payload = new(errorCode, "Authentication is required.");
        return TypedResults.Json(payload, statusCode: StatusCodes.Status401Unauthorized);
    }

    private static IResult MapError(string errorCode)
    {
        AgentErrorResult payload = new(errorCode, ResolveErrorMessage(errorCode));

        int statusCode = errorCode switch
        {
            "subscription_member_required" => StatusCodes.Status403Forbidden,
            "agent_not_found" => StatusCodes.Status404NotFound,
            _ => StatusCodes.Status400BadRequest
        };

        return TypedResults.Json(payload, statusCode: statusCode);
    }

    private static string ResolveErrorMessage(string errorCode)
    {
        return errorCode switch
        {
            "subscription_member_required" => "You must be a member of the subscription.",
            "agent_not_found" => "The requested agent was not found.",
            "agent_name_required" => "Agent name is required.",
            "agent_personality_required" => "Agent personality is required.",
            "catalog_product_not_found" => "The requested catalog product was not found for this agent subscription.",
            "agent_catalog_assignment_invalid" => "The requested catalog product assignments are invalid.",
            "agent_sales_zip_invalid" => "One or more sales zip codes are invalid. Use a 5-digit ZIP or ZIP+4 value.",
            "agent_sales_zip_conflict" => "Sales zip codes must be unique after normalization.",
            "telegram_bot_token_already_in_use" => "This Telegram bot token is already connected to another agent. Use a different token.",
            "telegram_webhook_registration_failed" => "Telegram webhook registration failed. Verify the bot token, webhook URL reachability, and BotFather webhook settings, then try again.",
            "telegram_bot_token_invalid" => "Telegram rejected the bot token. Verify the token from BotFather and try again.",
            _ => "Agent request failed."
        };
    }

    private static string? NormalizeCatalogQuery(string? query)
    {
        if (String.IsNullOrWhiteSpace(query))
        {
            return null;
        }

        return query.Trim().ToLowerInvariant();
    }

    private static async Task<AuthorizedAgentResult> GetAuthorizedAgentAsync(
        Guid agentId,
        ClaimsPrincipal user,
        MainDataContext dbContext,
        CancellationToken cancellationToken)
    {
        Guid? userId = user.GetUserId();
        if (userId is null)
        {
            return new AuthorizedAgentResult(null, UnauthorizedError("unauthenticated"));
        }

        Agent? agent = await dbContext.Agents
            .SingleOrDefaultAsync(item => item.Id == agentId, cancellationToken);

        if (agent is null)
        {
            return new AuthorizedAgentResult(null, MapError("agent_not_found"));
        }

        bool isMember = await dbContext.SubscriptionUsers
            .AnyAsync(
                membership =>
                    membership.SubscriptionId == agent.SubscriptionId &&
                    membership.UserId == userId.Value,
                cancellationToken);

        if (!isMember)
        {
            return new AuthorizedAgentResult(null, MapError("subscription_member_required"));
        }

        return new AuthorizedAgentResult(agent, null);
    }

    private sealed record AuthorizedAgentResult(
        Agent? Agent,
        IResult? ErrorResult);
}
