namespace HeyAlan.Agents;

using HeyAlan.Data;
using HeyAlan.Data.Entities;
using Microsoft.EntityFrameworkCore;

public sealed class AgentSalesZipCodeService : IAgentSalesZipCodeService
{
    private readonly MainDataContext dbContext;

    public AgentSalesZipCodeService(MainDataContext dbContext)
    {
        this.dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<GetAgentSalesZipCodeStateOperationResult> GetStateAsync(
        GetAgentSalesZipCodeStateInput input,
        CancellationToken cancellationToken = default)
    {
        Agent? agent = await this.dbContext.Agents
            .SingleOrDefaultAsync(item => item.Id == input.AgentId, cancellationToken);

        if (agent is null)
        {
            return new GetAgentSalesZipCodeStateOperationResult.Failure("agent_not_found");
        }

        AgentSalesZipCodeStateResult state = await this.BuildStateAsync(
            agent.Id,
            agent.SubscriptionId,
            cancellationToken);

        return new GetAgentSalesZipCodeStateOperationResult.Success(state);
    }

    public async Task<ReplaceAgentSalesZipCodesResult> ReplaceZipCodesAsync(
        ReplaceAgentSalesZipCodesInput input,
        CancellationToken cancellationToken = default)
    {
        Agent? agent = await this.dbContext.Agents
            .SingleOrDefaultAsync(item => item.Id == input.AgentId, cancellationToken);

        if (agent is null)
        {
            return new ReplaceAgentSalesZipCodesResult.Failure("agent_not_found");
        }

        NormalizeAgentSalesZipCodesResult normalizationResult = NormalizeZipCodes(input.ZipCodes);
        if (!String.IsNullOrWhiteSpace(normalizationResult.ErrorCode))
        {
            return new ReplaceAgentSalesZipCodesResult.Failure(normalizationResult.ErrorCode);
        }

        if (normalizationResult.ZipCodesNormalized.Count == 0)
        {
            ClearAgentSalesZipCodesResult.Success clearResult = await this.ClearZipCodesInternalAsync(agent, cancellationToken);
            return new ReplaceAgentSalesZipCodesResult.Success(clearResult.State);
        }

        List<AgentSalesZipCode> existingZipCodes = await this.dbContext.AgentSalesZipCodes
            .Where(item =>
                item.SubscriptionId == agent.SubscriptionId &&
                item.AgentId == agent.Id)
            .ToListAsync(cancellationToken);

        HashSet<string> requestedZipCodes = normalizationResult.ZipCodesNormalized.ToHashSet(StringComparer.Ordinal);
        List<AgentSalesZipCode> zipCodesToRemove = existingZipCodes
            .Where(item => !requestedZipCodes.Contains(item.ZipCodeNormalized))
            .ToList();

        HashSet<string> existingZipCodeSet = existingZipCodes
            .Select(item => item.ZipCodeNormalized)
            .ToHashSet(StringComparer.Ordinal);

        List<string> zipCodesToAdd = normalizationResult.ZipCodesNormalized
            .Where(item => !existingZipCodeSet.Contains(item))
            .ToList();

        if (zipCodesToRemove.Count > 0)
        {
            this.dbContext.AgentSalesZipCodes.RemoveRange(zipCodesToRemove);
        }

        foreach (string zipCode in zipCodesToAdd)
        {
            AgentSalesZipCode agentSalesZipCode = new()
            {
                SubscriptionId = agent.SubscriptionId,
                AgentId = agent.Id,
                ZipCodeNormalized = zipCode
            };

            this.dbContext.AgentSalesZipCodes.Add(agentSalesZipCode);
        }

        await this.dbContext.SaveChangesAsync(cancellationToken);

        AgentSalesZipCodeStateResult state = await this.BuildStateAsync(
            agent.Id,
            agent.SubscriptionId,
            cancellationToken);

        return new ReplaceAgentSalesZipCodesResult.Success(state);
    }

    public async Task<ClearAgentSalesZipCodesResult> ClearZipCodesAsync(
        ClearAgentSalesZipCodesInput input,
        CancellationToken cancellationToken = default)
    {
        Agent? agent = await this.dbContext.Agents
            .SingleOrDefaultAsync(item => item.Id == input.AgentId, cancellationToken);

        if (agent is null)
        {
            return new ClearAgentSalesZipCodesResult.Failure("agent_not_found");
        }

        return await this.ClearZipCodesInternalAsync(agent, cancellationToken);
    }

    private async Task<ClearAgentSalesZipCodesResult.Success> ClearZipCodesInternalAsync(
        Agent agent,
        CancellationToken cancellationToken)
    {
        List<AgentSalesZipCode> existingZipCodes = await this.dbContext.AgentSalesZipCodes
            .Where(item =>
                item.SubscriptionId == agent.SubscriptionId &&
                item.AgentId == agent.Id)
            .ToListAsync(cancellationToken);

        if (existingZipCodes.Count > 0)
        {
            this.dbContext.AgentSalesZipCodes.RemoveRange(existingZipCodes);
            await this.dbContext.SaveChangesAsync(cancellationToken);
        }

        AgentSalesZipCodeStateResult state = new(
            agent.Id,
            agent.SubscriptionId,
            false,
            []);

        return new ClearAgentSalesZipCodesResult.Success(state);
    }

    private async Task<AgentSalesZipCodeStateResult> BuildStateAsync(
        Guid agentId,
        Guid subscriptionId,
        CancellationToken cancellationToken)
    {
        List<string> zipCodesNormalized = await this.dbContext.AgentSalesZipCodes
            .Where(item =>
                item.SubscriptionId == subscriptionId &&
                item.AgentId == agentId)
            .OrderBy(item => item.ZipCodeNormalized)
            .Select(item => item.ZipCodeNormalized)
            .ToListAsync(cancellationToken);

        return new AgentSalesZipCodeStateResult(
            agentId,
            subscriptionId,
            zipCodesNormalized.Count > 0,
            zipCodesNormalized);
    }

    private static NormalizeAgentSalesZipCodesResult NormalizeZipCodes(IReadOnlyCollection<string> rawZipCodes)
    {
        List<string> normalizedZipCodes = [];
        HashSet<string> seenZipCodes = new(StringComparer.Ordinal);

        foreach (string rawZipCode in rawZipCodes)
        {
            if (!AgentSalesZipCodeRules.TryNormalizeZipCode(rawZipCode, out string normalizedZipCode))
            {
                return new NormalizeAgentSalesZipCodesResult([], "agent_sales_zip_invalid");
            }

            if (!seenZipCodes.Add(normalizedZipCode))
            {
                return new NormalizeAgentSalesZipCodesResult([], "agent_sales_zip_conflict");
            }

            normalizedZipCodes.Add(normalizedZipCode);
        }

        normalizedZipCodes.Sort(StringComparer.Ordinal);
        return new NormalizeAgentSalesZipCodesResult(normalizedZipCodes, null);
    }

    private sealed record NormalizeAgentSalesZipCodesResult(
        IReadOnlyList<string> ZipCodesNormalized,
        string? ErrorCode);
}
