namespace EventFlow.Application.UseCases.StartWorkflow;

public sealed class StartWorkflowCommand
{
    public string Name { get; }
    public string PayloadJson { get; }

    public StartWorkflowCommand(
        string name,
        string payloadJson)
    {
        Name = name;
        PayloadJson = payloadJson;
    }
}