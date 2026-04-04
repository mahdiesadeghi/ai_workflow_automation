# =============================================================================
# Stage 1: Build
# =============================================================================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files first to leverage Docker layer caching for restore
COPY src/WorkflowAutomation.Domain/WorkflowAutomation.Domain.csproj \
     src/WorkflowAutomation.Domain/
COPY src/WorkflowAutomation.Application/WorkflowAutomation.Application.csproj \
     src/WorkflowAutomation.Application/

# If an API project exists, copy its csproj too (wildcard-safe)
COPY src/WorkflowAutomation.Api/*.csproj src/WorkflowAutomation.Api/ 2>/dev/null || true
COPY src/WorkflowAutomation.Infrastructure/*.csproj src/WorkflowAutomation.Infrastructure/ 2>/dev/null || true

# Restore dependencies
RUN dotnet restore src/WorkflowAutomation.Api/WorkflowAutomation.Api.csproj \
    2>/dev/null || dotnet restore src/WorkflowAutomation.Domain/WorkflowAutomation.Domain.csproj

# Copy the rest of the source code
COPY src/ src/

# Build and publish in Release mode
RUN dotnet publish src/WorkflowAutomation.Api/WorkflowAutomation.Api.csproj \
    -c Release -o /app/publish --no-restore \
    2>/dev/null || \
    dotnet build src/WorkflowAutomation.Domain/WorkflowAutomation.Domain.csproj \
    -c Release -o /app/publish

# =============================================================================
# Stage 2: Runtime
# =============================================================================
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Create a non-root user for security
RUN adduser --disabled-password --gecos "" appuser

COPY --from=build /app/publish .

# Expose the API port
EXPOSE 5000

# Run as non-root
USER appuser

ENTRYPOINT ["dotnet", "WorkflowAutomation.Api.dll"]
