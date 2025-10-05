#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0-bullseye-slim AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0-bullseye-slim AS build
WORKDIR /src
COPY ["AIDungeonPromptsWeb/AIDungeonPrompts.Web.csproj", "AIDungeonPromptsWeb/"]
COPY ["AIDungeonPrompts.Application/AIDungeonPrompts.Application.csproj", "AIDungeonPrompts.Application/"]
COPY ["AIDungeonPrompts.Domain/AIDungeonPrompts.Domain.csproj", "AIDungeonPrompts.Domain/"]
COPY ["AIDungeonPrompts.Persistence/AIDungeonPrompts.Persistence.csproj", "AIDungeonPrompts.Persistence/"]
COPY ["AIDungeonPrompts.Infrastructure/AIDungeonPrompts.Infrastructure.csproj", "AIDungeonPrompts.Infrastructure/"]
COPY ["AIDungeonPrompts.Backup.Persistence/AIDungeonPrompts.Backup.Persistence.csproj", "AIDungeonPrompts.Backup.Persistence/"]
RUN dotnet restore "AIDungeonPromptsWeb/AIDungeonPrompts.Web.csproj"
COPY . .
WORKDIR "/src/AIDungeonPromptsWeb"
RUN dotnet build "AIDungeonPrompts.Web.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "AIDungeonPrompts.Web.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app

# Create non-root user for security
RUN groupadd -r appuser -g 5678 && \
    useradd -r -g appuser -u 5678 -d /app -s /sbin/nologin -c "Application user" appuser

# Copy published application
COPY --from=publish /app/publish .

# Create and set permissions for directories the app needs to write to
RUN mkdir -p /AIPromptDossier/backups && \
    chown -R appuser:appuser /app /AIPromptDossier

# Switch to non-root user
USER appuser

EXPOSE 80
ENTRYPOINT ["dotnet", "AIDungeonPrompts.Web.dll"]
