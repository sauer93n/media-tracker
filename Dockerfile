# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:9.0@sha256:3fcf6f1e809c0553f9feb222369f58749af314af6f063f389cbd2f913b4ad556 AS build
WORKDIR /src

RUN dotnet tool install --global dotnet-ef --version 9.0.11
ENV PATH="${PATH}:/root/.dotnet/tools"

# Copy solution and project files first for better layer caching
COPY Src/MediaTracker.sln ./
COPY Src/Api/Api.csproj ./Api/
COPY Src/Application/Application.csproj ./Application/
COPY Src/Domain/Domain.csproj ./Domain/
COPY Src/Infrastructure/Infrastructure.csproj ./Infrastructure/
COPY Src/Tests ./Tests

# Restore dependencies as a separate layer
RUN dotnet restore

# Copy the rest of the source code
COPY Src/ ./

ENV ConnectionStrings__MediaTracker="Host=localhost;Database=mediatracker;Username=postgres;Password=dummy"

# Build and publish the application
RUN dotnet publish ./Api/Api.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    /p:UseAppHost=false

RUN dotnet ef migrations bundle \
    --project ./Infrastructure/Infrastructure.csproj \
    --startup-project ./Api/Api.csproj \
    --configuration Release \
    --runtime linux-x64 \
    --self-contained \
    --output /app/publish/efbundle \
    --verbose

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0@sha256:b4bea3a52a0a77317fa93c5bbdb076623f81e3e2f201078d89914da71318b5d8 AS runtime
WORKDIR /app

# Create non-root user for security
RUN groupadd -r appuser && useradd -r -g appuser appuser

# Copy published files from build stage
COPY --from=build /app/publish .

RUN chmod +x ./efbundle

# Set ownership to non-root user
RUN chown -R appuser:appuser /app

# Switch to non-root user
USER appuser

# Configure environment
ENV ASPNETCORE_URLS=http://+:5000 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_RUNNING_IN_CONTAINER=true \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

# Expose port
EXPOSE 5000

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:5000/health || exit 1

# Run the application
ENTRYPOINT ["dotnet", "Api.dll"]