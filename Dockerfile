# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files
COPY *.sln .
COPY ToyStore.API/*.csproj ./ToyStore.API/
COPY ToyStore.Application/*.csproj ./ToyStore.Application/
COPY ToyStore.Domain/*.csproj ./ToyStore.Domain/
COPY ToyStore.Infrastructure/*.csproj ./ToyStore.Infrastructure/
COPY ToyStore.Recommendation/*.csproj ./ToyStore.Recommendation/
COPY ToyStore.Worker/*.csproj ./ToyStore.Worker/

# Restore dependencies
RUN dotnet restore

# Copy source code
COPY . .

# Build and publish API
WORKDIR /src/ToyStore.API
RUN dotnet publish -c Release -o /app/publish --no-restore

# Runtime Stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Install cultures for Vietnamese support
RUN apt-get update && apt-get install -y locales && \
    sed -i '/vi_VN.UTF-8/s/^# //g' /etc/locale.gen && \
    locale-gen

ENV LANG=vi_VN.UTF-8
ENV LANGUAGE=vi_VN:vi
ENV LC_ALL=vi_VN.UTF-8

# Copy published files
COPY --from=build /app/publish .

# Expose port
EXPOSE 8080

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

# Start application
ENTRYPOINT ["dotnet", "ToyStore.API.dll"]
