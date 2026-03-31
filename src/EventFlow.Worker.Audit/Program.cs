using EventFlow.Application.DependencyInjection;
using EventFlow.Infrastructure.DependencyInjection;
using EventFlow.Worker.Audit;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddHostedService<AuditWorker>();

var host = builder.Build();
host.Run();