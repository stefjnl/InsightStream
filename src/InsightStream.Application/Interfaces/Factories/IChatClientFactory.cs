using Microsoft.Extensions.AI;

namespace InsightStream.Application.Interfaces.Factories;

public interface IChatClientFactory
{
    IChatClient CreateClient(string? providerName = null, string? modelId = null);
}