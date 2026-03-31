FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/EventFlow.Api/EventFlow.Api.csproj", "src/EventFlow.Api/"]
COPY ["src/EventFlow.Application/EventFlow.Application.csproj", "src/EventFlow.Application/"]
COPY ["src/EventFlow.Domain/EventFlow.Domain.csproj", "src/EventFlow.Domain/"]
COPY ["src/EventFlow.Infrastructure/EventFlow.Infrastructure.csproj", "src/EventFlow.Infrastructure/"]
COPY ["src/EventFlow.Contracts/EventFlow.Contracts.csproj", "src/EventFlow.Contracts/"]

RUN dotnet restore "src/EventFlow.Api/EventFlow.Api.csproj"

COPY . .
WORKDIR "/src/src/EventFlow.Api"
RUN dotnet publish "EventFlow.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "EventFlow.Api.dll"]