FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/EventFlow.Worker.Audit/EventFlow.Worker.Audit.csproj", "src/EventFlow.Worker.Audit/"]
COPY ["src/EventFlow.Application/EventFlow.Application.csproj", "src/EventFlow.Application/"]
COPY ["src/EventFlow.Domain/EventFlow.Domain.csproj", "src/EventFlow.Domain/"]
COPY ["src/EventFlow.Infrastructure/EventFlow.Infrastructure.csproj", "src/EventFlow.Infrastructure/"]
COPY ["src/EventFlow.Contracts/EventFlow.Contracts.csproj", "src/EventFlow.Contracts/"]

RUN dotnet restore "src/EventFlow.Worker.Audit/EventFlow.Worker.Audit.csproj"

COPY . .
WORKDIR "/src/src/EventFlow.Worker.Audit"
RUN dotnet publish "EventFlow.Worker.Audit.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/runtime:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "EventFlow.Worker.Audit.dll"]