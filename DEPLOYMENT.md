# UberPrints Deployment Guide

This guide walks you through deploying UberPrints locally using Docker Compose with Cloudflare Tunnel for public access via your domain.

## Architecture Overview

The deployment consists of 3 Docker containers:

1. **database** - PostgreSQL 18 with persistent storage
2. **server** - ASP.NET Core 9.0 serving both API and React frontend from wwwroot
3. **cloudflared** - Cloudflare Tunnel for secure public access

## Prerequisites

- Docker and Docker Compose installed
- A Cloudflare account with a domain
- Discord OAuth application created
- Basic command line knowledge

## Step 1: Set Up Cloudflare Tunnel

1. Go to [Cloudflare Zero Trust Dashboard](https://one.dash.cloudflare.com/)
2. Navigate to **Networks** → **Tunnels**
3. Click **Create a tunnel**
4. Choose **Cloudflared** as the connector type
5. Name your tunnel (e.g., "uberprints")
6. Copy the tunnel token (you'll need this for `.env`)
7. Configure a public hostname:
   - **Public hostname**: your subdomain (e.g., `prints.yourdomain.com`)
   - **Service**: `http://server:8080`
   - Click **Save tunnel**

## Step 2: Set Up Discord OAuth

1. Go to [Discord Developer Portal](https://discord.com/developers/applications)
2. Create a new application (or use existing one)
3. Go to **OAuth2** section
4. Add a redirect URI:
   - For production: `https://your-domain.com/api/auth/discord/callback`
   - For local dev: `https://localhost:7001/api/auth/discord/callback`
5. Copy your **Client ID** and **Client Secret**

## Step 3: Configure Environment Variables

1. Copy the example environment file:
   ```bash
   cp .env.example .env
   ```

2. Edit `.env` and fill in all values:
   ```env
   # Strong password for PostgreSQL
   POSTGRES_PASSWORD=your-strong-password-here

   # JWT Secret - MUST be at least 32 characters
   JWT_SECRET_KEY=your-secret-key-minimum-32-characters-long-change-this

   # Discord OAuth credentials
   DISCORD_CLIENT_ID=your-discord-client-id-from-step-2
   DISCORD_CLIENT_SECRET=your-discord-client-secret-from-step-2

   # Cloudflare Tunnel Token from step 1
   CLOUDFLARE_TUNNEL_TOKEN=your-cloudflare-tunnel-token
   ```

3. Update production settings in `src/UberPrints.Server/appsettings.Production.json`:
   ```json
   {
     "Frontend": {
       "Url": "https://your-domain.com"
     }
   }
   ```

## Step 4: Initial Database Setup

Before running for the first time, you need to apply database migrations.

1. Start only the database:
   ```bash
   docker compose up -d database
   ```

2. Wait for the database to be ready (check with `docker compose logs database`)

3. Apply migrations using the dotnet CLI:
   ```bash
   cd src/UberPrints.Server
   dotnet ef database update --connection "Host=localhost;Database=uberprints;Username=postgres;Password=YOUR_PASSWORD_FROM_ENV"
   ```

   Alternatively, you can modify the Dockerfile to run migrations on startup.

## Step 5: Build and Start All Services

```bash
# Build and start all containers
docker compose up -d --build

# Check logs
docker compose logs -f

# Check status
docker compose ps
```

The build process will:
1. Build the React frontend
2. Copy built files to `src/UberPrints.Server/wwwroot/`
3. Build the ASP.NET Core application with embedded frontend
4. Start all services

## Step 6: Verify Deployment

1. **Local access**: http://localhost:8080
2. **Public access**: https://your-domain.com (via Cloudflare Tunnel)

Check that:
- Frontend loads correctly
- API endpoints work (`/api/filaments`)
- Discord login works
- Database connections are successful

## Common Commands

```bash
# View logs
docker compose logs -f server
docker compose logs -f database
docker compose logs -f cloudflared

# Restart a service
docker compose restart server

# Stop all services
docker compose down

# Stop and remove volumes (careful: deletes database!)
docker compose down -v

# Rebuild after code changes
docker compose up -d --build server

# Access database directly
docker compose exec database psql -U postgres -d uberprints
```

## Database Backup

```bash
# Backup
docker compose exec database pg_dump -U postgres uberprints > backup.sql

# Restore
docker compose exec -T database psql -U postgres uberprints < backup.sql
```

## Updating the Application

1. Pull latest code:
   ```bash
   git pull
   ```

2. Rebuild and restart:
   ```bash
   docker compose up -d --build server
   ```

3. Apply any new migrations:
   ```bash
   cd src/UberPrints.Server
   dotnet ef database update --connection "Host=localhost;Database=uberprints;Username=postgres;Password=YOUR_PASSWORD"
   ```

## Security Considerations

1. **Never commit `.env`** - It contains sensitive credentials
2. **Use strong passwords** - For both PostgreSQL and JWT secret
3. **Keep Discord secrets safe** - Don't expose OAuth credentials
4. **HTTPS only in production** - Cloudflare Tunnel provides this automatically
5. **Regular updates** - Keep Docker images and dependencies updated
6. **Database backups** - Set up regular automated backups

## Troubleshooting

### Server won't start
- Check logs: `docker compose logs server`
- Verify environment variables in `.env`
- Ensure database is running and healthy

### Discord login fails
- Verify redirect URI in Discord app matches your domain
- Check `Discord__ClientId` and `Discord__ClientSecret` in environment
- Ensure `Frontend__Url` in appsettings.Production.json is correct

### Cloudflare Tunnel not working
- Verify `CLOUDFLARE_TUNNEL_TOKEN` is correct
- Check tunnel status in Cloudflare dashboard
- Ensure public hostname points to `http://server:8080`
- Review cloudflared logs: `docker compose logs cloudflared`

### Database connection issues
- Ensure database service is healthy: `docker compose ps`
- Check connection string has correct password
- Verify network connectivity: `docker compose exec server ping database`

### Frontend not loading
- Check that frontend was built: `ls src/UberPrints.Server/wwwroot/`
- Rebuild if empty: `docker compose up -d --build server`
- Verify static files middleware is configured in Program.cs

## Production Best Practices

1. **Use Docker secrets** instead of environment variables for sensitive data
2. **Set up automated backups** for PostgreSQL data volume
3. **Configure rate limiting** on Cloudflare
4. **Enable Cloudflare WAF** for additional security
5. **Set up monitoring** (e.g., Uptime Kuma, Grafana)
6. **Configure log aggregation** (e.g., Seq, ELK stack)
7. **Set resource limits** in docker-compose.yml:
   ```yaml
   deploy:
     resources:
       limits:
         cpus: '1.0'
         memory: 512M
   ```

## Architecture Notes

### Why serve frontend from backend?

- **Simplified deployment**: One container instead of two
- **No CORS issues**: Same origin for API and frontend
- **Easier SSL/TLS**: Single endpoint through Cloudflare
- **Reduced complexity**: Fewer moving parts to manage
- **Better performance**: Direct serving without proxy overhead

### How it works

1. Vite builds the React app to `src/UberPrints.Server/wwwroot/`
2. ASP.NET Core's `UseStaticFiles()` serves files from wwwroot
3. `MapFallbackToFile("index.html")` handles SPA client-side routing
4. API endpoints are registered first with `MapControllers()`
5. Fallback only triggers for non-API routes

This is a common pattern for deploying SPAs with ASP.NET Core and simplifies the deployment architecture significantly.

## Support

For issues or questions:
- Check application logs: `docker compose logs -f`
- Review CLAUDE.md for development documentation
- File an issue in the project repository
