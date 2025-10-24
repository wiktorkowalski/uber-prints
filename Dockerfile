# Multi-stage Dockerfile for UberPrints
# Stage 1: Build frontend
FROM node:20-alpine AS frontend-build

WORKDIR /app/client

# Copy package files
COPY src/UberPrints.Client/package*.json ./

# Install dependencies
RUN npm ci

# Copy frontend source
COPY src/UberPrints.Client/ ./

# Build frontend (outputs to ../UberPrints.Server/wwwroot)
# Set NODE_ENV to production to use .env.production
ENV NODE_ENV=production
RUN npm run build

# Stage 2: Build backend
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS backend-build

WORKDIR /app

# Copy solution and project files
COPY *.sln ./
COPY src/UberPrints.Server/*.csproj ./src/UberPrints.Server/

# Restore dependencies
RUN dotnet restore src/UberPrints.Server/UberPrints.Server.csproj

# Copy all backend source
COPY src/UberPrints.Server/ ./src/UberPrints.Server/

# Copy frontend build output from previous stage
COPY --from=frontend-build /app/UberPrints.Server/wwwroot ./src/UberPrints.Server/wwwroot

# Build and publish
RUN dotnet publish src/UberPrints.Server/UberPrints.Server.csproj -c Release -o /app/publish

# Stage 3: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime

WORKDIR /app

# Install FFmpeg for camera streaming
RUN apt-get update && \
    apt-get install -y --no-install-recommends ffmpeg && \
    rm -rf /var/lib/apt/lists/*

# Verify FFmpeg installation
RUN ffmpeg -version

# Copy published output
COPY --from=backend-build /app/publish .

# Expose port
EXPOSE 8080

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Run the application
ENTRYPOINT ["dotnet", "UberPrints.Server.dll"]
