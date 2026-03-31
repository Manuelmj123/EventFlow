namespace EventFlow.Contracts.Requests;

public sealed class StartWorkflowRequest
{
    public string Name { get; init; } = string.Empty;
    public string PayloadJson { get; init; } = string.Empty;
}