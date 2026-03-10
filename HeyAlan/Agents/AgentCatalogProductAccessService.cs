namespace HeyAlan.Agents;

using HeyAlan.Data;
using HeyAlan.Data.Entities;
using Microsoft.EntityFrameworkCore;

public sealed class AgentCatalogProductAccessService : IAgentCatalogProductAccessService
{
    private readonly MainDataContext dbContext;

    public AgentCatalogProductAccessService(MainDataContext dbContext)
    {
        this.dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<GetAgentCatalogProductAccessStateOperationResult> GetStateAsync(
        GetAgentCatalogProductAccessStateInput input,
        CancellationToken cancellationToken = default)
    {
        Agent? agent = await this.dbContext.Agents
            .SingleOrDefaultAsync(item => item.Id == input.AgentId, cancellationToken);

        if (agent is null)
        {
            return new GetAgentCatalogProductAccessStateOperationResult.Failure("agent_not_found");
        }

        AgentCatalogProductAccessStateResult state = await this.BuildStateAsync(
            agent.Id,
            agent.SubscriptionId,
            cancellationToken);

        return new GetAgentCatalogProductAccessStateOperationResult.Success(state);
    }

    public async Task<ReplaceAgentCatalogProductAccessResult> ReplaceAssignmentsAsync(
        ReplaceAgentCatalogProductAccessInput input,
        CancellationToken cancellationToken = default)
    {
        Agent? agent = await this.dbContext.Agents
            .SingleOrDefaultAsync(item => item.Id == input.AgentId, cancellationToken);

        if (agent is null)
        {
            return new ReplaceAgentCatalogProductAccessResult.Failure("agent_not_found");
        }

        if (input.SubscriptionCatalogProductIds.Any(item => item == Guid.Empty))
        {
            return new ReplaceAgentCatalogProductAccessResult.Failure("agent_catalog_assignment_invalid");
        }

        IReadOnlyList<Guid> normalizedProductIds = NormalizeProductIds(input.SubscriptionCatalogProductIds);
        if (normalizedProductIds.Count == 0)
        {
            ClearAgentCatalogProductAccessResult.Success clearResult = await this.ClearAssignmentsInternalAsync(agent, cancellationToken);
            return new ReplaceAgentCatalogProductAccessResult.Success(clearResult.State);
        }

        int requestedCount = input.SubscriptionCatalogProductIds.Count;
        if (requestedCount != normalizedProductIds.Count)
        {
            return new ReplaceAgentCatalogProductAccessResult.Failure("agent_catalog_assignment_invalid");
        }

        List<Guid> matchingProductIds = await this.dbContext.SubscriptionCatalogProducts
            .Where(item =>
                item.SubscriptionId == agent.SubscriptionId &&
                normalizedProductIds.Contains(item.Id))
            .Select(item => item.Id)
            .ToListAsync(cancellationToken);

        if (matchingProductIds.Count != normalizedProductIds.Count)
        {
            return new ReplaceAgentCatalogProductAccessResult.Failure("catalog_product_not_found");
        }

        List<AgentCatalogProductAccess> existingAssignments = await this.dbContext.AgentCatalogProductAccesses
            .Where(item =>
                item.SubscriptionId == agent.SubscriptionId &&
                item.AgentId == agent.Id)
            .ToListAsync(cancellationToken);

        HashSet<Guid> requestedProductIds = normalizedProductIds.ToHashSet();
        List<AgentCatalogProductAccess> assignmentsToRemove = existingAssignments
            .Where(item => !requestedProductIds.Contains(item.SubscriptionCatalogProductId))
            .ToList();

        HashSet<Guid> existingProductIds = existingAssignments
            .Select(item => item.SubscriptionCatalogProductId)
            .ToHashSet();

        List<Guid> productIdsToAdd = normalizedProductIds
            .Where(item => !existingProductIds.Contains(item))
            .ToList();

        if (assignmentsToRemove.Count > 0)
        {
            this.dbContext.AgentCatalogProductAccesses.RemoveRange(assignmentsToRemove);
        }

        foreach (Guid productId in productIdsToAdd)
        {
            AgentCatalogProductAccess assignment = new()
            {
                AgentId = agent.Id,
                SubscriptionId = agent.SubscriptionId,
                SubscriptionCatalogProductId = productId
            };

            this.dbContext.AgentCatalogProductAccesses.Add(assignment);
        }

        await this.dbContext.SaveChangesAsync(cancellationToken);

        AgentCatalogProductAccessStateResult state = await this.BuildStateAsync(
            agent.Id,
            agent.SubscriptionId,
            cancellationToken);

        return new ReplaceAgentCatalogProductAccessResult.Success(state);
    }

    public async Task<ClearAgentCatalogProductAccessResult> ClearAssignmentsAsync(
        ClearAgentCatalogProductAccessInput input,
        CancellationToken cancellationToken = default)
    {
        Agent? agent = await this.dbContext.Agents
            .SingleOrDefaultAsync(item => item.Id == input.AgentId, cancellationToken);

        if (agent is null)
        {
            return new ClearAgentCatalogProductAccessResult.Failure("agent_not_found");
        }

        return await this.ClearAssignmentsInternalAsync(agent, cancellationToken);
    }

    private async Task<ClearAgentCatalogProductAccessResult.Success> ClearAssignmentsInternalAsync(
        Agent agent,
        CancellationToken cancellationToken)
    {
        List<AgentCatalogProductAccess> existingAssignments = await this.dbContext.AgentCatalogProductAccesses
            .Where(item =>
                item.SubscriptionId == agent.SubscriptionId &&
                item.AgentId == agent.Id)
            .ToListAsync(cancellationToken);

        if (existingAssignments.Count > 0)
        {
            this.dbContext.AgentCatalogProductAccesses.RemoveRange(existingAssignments);
            await this.dbContext.SaveChangesAsync(cancellationToken);
        }

        AgentCatalogProductAccessStateResult state = new(
            agent.Id,
            agent.SubscriptionId,
            false,
            []);

        return new ClearAgentCatalogProductAccessResult.Success(state);
    }

    private async Task<AgentCatalogProductAccessStateResult> BuildStateAsync(
        Guid agentId,
        Guid subscriptionId,
        CancellationToken cancellationToken)
    {
        List<Guid> productIds = await this.dbContext.AgentCatalogProductAccesses
            .Where(item =>
                item.SubscriptionId == subscriptionId &&
                item.AgentId == agentId)
            .OrderBy(item => item.SubscriptionCatalogProductId)
            .Select(item => item.SubscriptionCatalogProductId)
            .ToListAsync(cancellationToken);

        return new AgentCatalogProductAccessStateResult(
            agentId,
            subscriptionId,
            productIds.Count > 0,
            productIds);
    }

    private static IReadOnlyList<Guid> NormalizeProductIds(IReadOnlyCollection<Guid> subscriptionCatalogProductIds)
    {
        List<Guid> normalizedProductIds = subscriptionCatalogProductIds
            .Where(item => item != Guid.Empty)
            .Distinct()
            .OrderBy(item => item)
            .ToList();

        return normalizedProductIds;
    }
}
