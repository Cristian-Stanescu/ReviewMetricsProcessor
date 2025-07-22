# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/aspnet:8.0-noble-chiseled-extra AS base

# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:8.0-noble AS build
WORKDIR /src

COPY ["Directory.Build.props", "."]
COPY ["ReviewMetricsProcessor.Migrations/ReviewMetricsProcessor.Migrations.csproj", "ReviewMetricsProcessor.Migrations/"]
COPY ["ReviewMetricsProcessor.Data/ReviewMetricsProcessor.Data.csproj", "ReviewMetricsProcessor.Data/"]
RUN dotnet restore "ReviewMetricsProcessor.Migrations/ReviewMetricsProcessor.Migrations.csproj"

COPY . .
WORKDIR "/src/ReviewMetricsProcessor.Migrations"
RUN dotnet build "ReviewMetricsProcessor.Migrations.csproj" -c Release -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
RUN dotnet publish "ReviewMetricsProcessor.Migrations.csproj" -c Release -o /app/publish --no-restore

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ReviewMetricsProcessor.Migrations.dll"]