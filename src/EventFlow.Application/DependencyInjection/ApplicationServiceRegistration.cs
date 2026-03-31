using Microsoft.Extensions.DependencyInjection;
using EventFlow.Application.UseCases.StartWorkflow;

namespace EventFlow.Application.DependencyInjection;

public static class ApplicationServiceRegistration
{
    public static IServiceCollection AddApplicationServices(
        this IServiceCollection services)
    {
        services.AddScoped<StartWorkflowCommandHandler>();

        return services;
    }
}