using EventFlow.Application.DependencyInjection;
using EventFlow.Infrastructure.DependencyInjection;
using EventFlow.Worker.Processor;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddHostedService<ProcessorWorker>();

var host = builder.Build();
host.Run();