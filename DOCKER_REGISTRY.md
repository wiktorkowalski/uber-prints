# Docker Image Publishing with GitHub Actions

This document explains how to use the automated Docker image publishing pipeline.

## Overview

The GitHub Actions workflow (`.github/workflows/docker-publish.yml`) automatically builds and publishes Docker images to GitHub Container Registry (ghcr.io) when you push to the `master` branch or create version tags.

## What Gets Built

The pipeline builds a **single, self-contained Docker image** that includes:
- Built React frontend (bundled and optimized)
- ASP.NET Core backend with all dependencies
- All necessary runtime components

The image does **NOT** include:
- PostgreSQL database (run separately)
- Cloudflare tunnel (run separately)
- Environment-specific configuration (passed via environment variables)

## Image Tagging Strategy

The pipeline automatically creates multiple tags for each build:

- `latest` - Latest build from master branch
- `sha-<commit>` - Specific commit SHA (e.g., `sha-1234567`)
- `v1.0.0` - Semantic version tags (when you create git tags like `v1.0.0`)
- `1.0` - Major.minor version
- `1` - Major version only

## How to Use

### 1. Enable GitHub Container Registry

The workflow uses GitHub's built-in token (`GITHUB_TOKEN`), so no additional secrets are needed. However, you need to:

1. Go to your GitHub repository settings
2. Navigate to **Actions** → **General**
3. Scroll to **Workflow permissions**
4. Ensure **Read and write permissions** is selected
5. Save changes

### 2. Push Code to Trigger Build

The workflow triggers on:

```bash
# Push to master branch (builds 'latest' tag)
git push origin master

# Create and push a version tag (builds versioned tags)
git tag v1.0.0
git push origin v1.0.0

# Manual trigger via GitHub UI
# Go to Actions → Build and Publish Docker Image → Run workflow
```

### 3. Wait for Build to Complete

- Go to the **Actions** tab in your GitHub repository
- Watch the build progress
- Build typically takes 3-5 minutes

### 4. Make Image Public (Recommended)

By default, GitHub Container Registry images are private. To make them public:

1. Go to https://github.com/users/YOUR_USERNAME/packages/container/uber-prints/settings
2. Scroll to **Danger Zone**
3. Click **Change visibility**
4. Select **Public**
5. Confirm the change

This allows pulling the image without authentication.

### 5. Update docker-compose to Use Published Image

Replace the `build` section in your `docker-compose.yml`:

**Before:**
```yaml
server:
  build:
    context: .
    dockerfile: Dockerfile
```

**After:**
```yaml
server:
  image: ghcr.io/YOUR_GITHUB_USERNAME/uber-prints:latest
```

Or use the provided `docker-compose.ghcr.yml` file:

```bash
# Copy the example
cp docker-compose.ghcr.yml docker-compose.yml

# Edit to replace 'yourusername' with your actual GitHub username
sed -i 's/yourusername/YOUR_GITHUB_USERNAME/g' docker-compose.yml

# Or manually edit the file
nano docker-compose.yml
```

### 6. Deploy

```bash
# Pull the latest image
docker compose pull server

# Start services (database, server, cloudflared)
docker compose up -d

# View logs
docker compose logs -f server
```

## Using Different Image Versions

You can specify different image versions:

```yaml
# Use latest
server:
  image: ghcr.io/yourusername/uber-prints:latest

# Use specific version
server:
  image: ghcr.io/yourusername/uber-prints:v1.0.0

# Use specific commit
server:
  image: ghcr.io/yourusername/uber-prints:sha-1234567
```

## Authentication (for Private Images)

If your image is private, authenticate before pulling:

```bash
# Create a GitHub Personal Access Token with 'read:packages' scope
# https://github.com/settings/tokens

# Login to GitHub Container Registry
echo $GITHUB_TOKEN | docker login ghcr.io -u YOUR_USERNAME --password-stdin

# Now you can pull private images
docker compose pull
```

## Verifying the Image

After the workflow completes, verify the image:

```bash
# List available tags
curl https://ghcr.io/v2/YOUR_USERNAME/uber-prints/tags/list

# Pull and inspect
docker pull ghcr.io/YOUR_USERNAME/uber-prints:latest
docker inspect ghcr.io/YOUR_USERNAME/uber-prints:latest

# Test run (with required environment variables)
docker run --rm \
  -e ConnectionStrings__DefaultConnection="Host=localhost;Database=test;Username=postgres;Password=test" \
  -e Jwt__SecretKey="your-32-character-secret-key-here-test" \
  -e Discord__ClientId="your-client-id" \
  -e Discord__ClientSecret="your-client-secret" \
  -p 8080:8080 \
  ghcr.io/YOUR_USERNAME/uber-prints:latest
```

## Troubleshooting

### Build Fails

1. Check the Actions tab for error logs
2. Verify your Dockerfile builds locally: `docker build -t test .`
3. Ensure all required files are committed to git

### Cannot Pull Image

1. Verify the image exists: https://github.com/YOUR_USERNAME?tab=packages
2. Check if image is public or if you're authenticated
3. Verify the image name matches your GitHub username

### Image Runs But Fails

1. Check environment variables are set correctly
2. Verify database connection string
3. Check logs: `docker compose logs server`
4. Ensure database is healthy: `docker compose ps`

## Advantages of Using Published Images

1. **Faster Deployments** - No need to build on production server
2. **Consistency** - Same image across all environments
3. **Version Control** - Easy rollback to previous versions
4. **CI/CD Integration** - Automated testing and building
5. **Reproducibility** - Exact same build every time
6. **Bandwidth Savings** - Pull once, deploy anywhere

## Development vs Production

**Development** (current setup):
```bash
# Build locally each time
docker compose up --build
```

**Production** (with published images):
```bash
# Pull pre-built image
docker compose pull
docker compose up -d
```

## Next Steps

1. Push this code to trigger your first build
2. Make the package public for easier access
3. Update your `docker-compose.yml` to use the published image
4. Set up automatic deployments using webhooks or CD tools
