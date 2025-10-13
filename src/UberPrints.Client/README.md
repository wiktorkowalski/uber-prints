# UberPrints.Client

React frontend application for the UberPrints 3D Request System, built with Vite, TypeScript, and shadcn/ui.

## Technology Stack

- **Framework**: React 18 with TypeScript
- **Build Tool**: Vite 5.0
- **UI Library**: shadcn/ui (built on Radix UI)
- **Styling**: Tailwind CSS
- **HTTP Client**: Axios
- **Routing**: React Router v6

## Prerequisites

- Node.js 20+ (LTS recommended)
- npm or yarn
- .NET 10 SDK (for integrated builds)

## Getting Started

### 1. Install Dependencies

```bash
cd src/UberPrints.Client
npm install
```

### 2. Configure Environment

Copy the example environment file and configure it:

```bash
cp .env.example .env
```

Edit `.env` with your configuration:

```env
VITE_API_BASE_URL=https://localhost:7001
VITE_DISCORD_CLIENT_ID=your_discord_client_id
VITE_DISCORD_REDIRECT_URI=http://localhost:5173/auth/callback
```

### 3. Development Mode

#### Option A: Standalone Development (Recommended for frontend work)

Run the Vite dev server independently:

```bash
npm run dev
```

The application will be available at `http://localhost:5173` with:
- Hot Module Replacement (HMR)
- Fast refresh
- Proxy to backend API at `https://localhost:7001`

Make sure the backend server is running separately.

#### Option B: Integrated .NET Build

Build and run through the .NET project:

```bash
# From the solution root
dotnet build src/UberPrints.Client/UberPrints.Client.csproj
```

This will:
1. Install npm packages if needed
2. Build the React app
3. Output static files to `dist/`

### 4. Production Build

```bash
npm run build
```

This creates an optimized production build in the `dist/` directory.

To preview the production build:

```bash
npm run preview
```

## Project Structure

```
src/UberPrints.Client/
├── src/
│   ├── components/         # Reusable React components
│   ├── lib/               # Utility functions and helpers
│   │   └── utils.ts       # shadcn/ui utilities
│   ├── App.tsx            # Main application component
│   ├── main.tsx           # Application entry point
│   ├── index.css          # Global styles with Tailwind
│   └── vite-env.d.ts      # Vite type definitions
├── public/                # Static assets
├── index.html             # HTML template
├── vite.config.ts         # Vite configuration
├── tailwind.config.js     # Tailwind CSS configuration
├── tsconfig.json          # TypeScript configuration
├── package.json           # npm dependencies and scripts
└── UberPrints.Client.csproj  # .NET project file
```

## Development Workflow

### Adding shadcn/ui Components

shadcn/ui components are copied into your project for full customization. To add a new component:

1. Visit [ui.shadcn.com](https://ui.shadcn.com)
2. Find the component you need
3. Copy the component code into `src/components/ui/`
4. Import and use in your application

Example components already configured:
- Button, Card, Dialog
- Dropdown Menu, Label, Select
- Separator, Toast

### API Integration

The application is configured to proxy API requests to the backend:

```typescript
// In vite.config.ts
server: {
  proxy: {
    '/api': {
      target: 'https://localhost:7001',
      changeOrigin: true,
      secure: false,
    },
  },
}
```

Use relative API paths in your code:

```typescript
import axios from 'axios';

const response = await axios.get('/api/requests');
```

### Path Aliases

TypeScript path aliases are configured for cleaner imports:

```typescript
// Instead of: import { cn } from '../../lib/utils'
import { cn } from '@/lib/utils'
```

## Building with .NET

### Development Build

The `.csproj` file is configured to automatically run `npm install` when needed:

```bash
dotnet build
```

### Production Build

When publishing the .NET solution, the React app is automatically built and included:

```bash
dotnet publish -c Release
```

The build output includes static files in `wwwroot/` ready to be served by the backend.

## Scripts

- `npm run dev` - Start Vite dev server with HMR
- `npm run build` - Build for production
- `npm run preview` - Preview production build locally
- `npm run lint` - Run ESLint

## Integration with Server

The client can run in two modes:

### Standalone Mode (Development)
- Client: `http://localhost:5173` (Vite dev server)
- Server: `https://localhost:7001` (ASP.NET)
- API calls proxied from client to server

### Integrated Mode (Production)
- Static files served from Server's `wwwroot/`
- Single deployment artifact
- No CORS configuration needed

## Troubleshooting

### TypeScript Errors

If you see TypeScript errors after cloning, run:

```bash
npm install
```

### Port Already in Use

If port 5173 is already in use, Vite will automatically try the next available port.

### API Connection Issues

1. Ensure the backend server is running at `https://localhost:7001`
2. Check CORS configuration in the backend
3. Verify the `VITE_API_BASE_URL` in your `.env` file

## Learn More

- [React Documentation](https://react.dev)
- [Vite Documentation](https://vitejs.dev)
- [shadcn/ui Documentation](https://ui.shadcn.com)
- [Tailwind CSS Documentation](https://tailwindcss.com)
- [TypeScript Documentation](https://www.typescriptlang.org)
