import { Link } from 'react-router-dom';
import { Button } from '../components/ui/button';
import { Package, Search, User, Shield, Printer, Eye, Sparkles } from 'lucide-react';
import { useAuth } from '../contexts/AuthContext';
import { getDisplayName } from '../lib/utils';

export const Home = () => {
  const { isAuthenticated, user } = useAuth();

  return (
    <div className="space-y-16 pb-8">
      {/* Hero Section with Grid Pattern */}
      <section className="relative -mt-8 -mx-4 px-4 py-20 overflow-hidden bg-gradient-to-br from-background via-background to-primary/5">
        {/* Subtle grid background */}
        <div className="absolute inset-0 bg-grid-pattern opacity-40" />

        {/* Accent shapes */}
        <div className="absolute top-20 right-20 w-72 h-72 bg-primary/10 rounded-full blur-3xl" />
        <div className="absolute bottom-20 left-20 w-96 h-96 bg-primary/5 rounded-full blur-3xl" />

        <div className="relative text-center space-y-8 max-w-4xl mx-auto">
          <div className="inline-flex items-center gap-2 px-4 py-2 rounded-full bg-primary/10 text-primary text-sm font-medium animate-snap-in border border-primary/20">
            <Sparkles className="w-4 h-4" />
            <span>3D Printing Made Simple</span>
          </div>

          <h1 className="text-5xl lg:text-6xl font-heading font-bold tracking-tight animate-slide-up text-primary" style={{ animationDelay: '100ms' }}>
            Welcome to UberPrints
          </h1>

          <p className="text-xl lg:text-2xl text-muted-foreground max-w-3xl mx-auto leading-relaxed animate-slide-up" style={{ animationDelay: '200ms' }}>
            Your friendly 3D printing service. Submit requests, track progress in real-time, and get your prints delivered.
          </p>

          <div className="flex flex-wrap justify-center gap-4 pt-6 animate-slide-up" style={{ animationDelay: '300ms' }}>
            <Link to="/requests/new">
              <Button size="lg" className="shadow-lg transition-all hover:shadow-xl hover:scale-105">
                <Package className="w-5 h-5 mr-2" />
                Submit Request
              </Button>
            </Link>
            <Link to="/requests">
              <Button size="lg" variant="outline" className="border-2 hover:border-primary/50 transition-all hover:scale-105">
                <Search className="w-5 h-5 mr-2" />
                View All Requests
              </Button>
            </Link>
            <Link to="/live-view">
              <Button size="lg" variant="outline" className="border-2 hover:border-primary/50 transition-all hover:scale-105">
                <Eye className="w-5 h-5 mr-2" />
                Live View
              </Button>
            </Link>
          </div>
        </div>
      </section>

      {/* Features Section */}
      <section className="max-w-6xl mx-auto">
        <div className="text-center space-y-3 mb-12">
          <h2 className="text-3xl lg:text-4xl font-heading font-semibold">How It Works</h2>
          <p className="text-lg text-muted-foreground">Simple, transparent, and maker-friendly</p>
        </div>

        <div className="grid md:grid-cols-3 gap-6">
          {/* Feature Card 1 */}
          <div className="group relative card-interactive bg-card p-8 space-y-4 overflow-hidden">
            {/* Card accent */}
            <div className="absolute top-0 right-0 w-24 h-24 bg-primary/10 rounded-full blur-2xl transition-all group-hover:w-32 group-hover:h-32" />

            <div className="relative">
              <div className="w-14 h-14 bg-gradient-to-br from-primary/20 to-primary/10 rounded-xl flex items-center justify-center mb-4 transition-transform group-hover:scale-110 group-hover:rotate-3">
                <Package className="w-7 h-7 text-primary" />
              </div>
              <h3 className="text-xl font-heading font-semibold mb-3">Easy Requests</h3>
              <p className="text-muted-foreground leading-relaxed">
                Submit your 3D printing requests with just a few clicks. No account required for guest users!
              </p>
            </div>
          </div>

          {/* Feature Card 2 */}
          <div className="group relative card-interactive bg-card p-8 space-y-4 overflow-hidden">
            {/* Card accent */}
            <div className="absolute top-0 right-0 w-24 h-24 bg-primary/10 rounded-full blur-2xl transition-all group-hover:w-32 group-hover:h-32" />

            <div className="relative">
              <div className="w-14 h-14 bg-gradient-to-br from-primary/20 to-primary/10 rounded-xl flex items-center justify-center mb-4 transition-transform group-hover:scale-110 group-hover:rotate-3">
                <Printer className="w-7 h-7 text-primary" />
              </div>
              <h3 className="text-xl font-heading font-semibold mb-3">Real-Time Monitoring</h3>
              <p className="text-muted-foreground leading-relaxed">
                Watch your prints come to life with live camera feeds and real-time status updates from the printer.
              </p>
            </div>
          </div>

          {/* Feature Card 3 */}
          <div className="group relative card-interactive bg-card p-8 space-y-4 overflow-hidden">
            {/* Card accent */}
            <div className="absolute top-0 right-0 w-24 h-24 bg-primary/10 rounded-full blur-2xl transition-all group-hover:w-32 group-hover:h-32" />

            <div className="relative">
              <div className="w-14 h-14 bg-gradient-to-br from-primary/20 to-primary/10 rounded-xl flex items-center justify-center mb-4 transition-transform group-hover:scale-110 group-hover:rotate-3">
                <User className="w-7 h-7 text-primary" />
              </div>
              <h3 className="text-xl font-heading font-semibold mb-3">Discord Integration</h3>
              <p className="text-muted-foreground leading-relaxed">
                Sign in with Discord to manage your requests, get notifications, and view your complete print history.
              </p>
            </div>
          </div>
        </div>
      </section>

      {/* User-specific Section */}
      {isAuthenticated && user ? (
        <section className="relative max-w-4xl mx-auto overflow-hidden">
          <div className="relative card-enhanced bg-gradient-to-br from-primary/10 to-primary/5 p-10 text-center space-y-6">
            {/* Decorative grid */}
            <div className="absolute inset-0 bg-grid-pattern-fine opacity-20" />

            <div className="relative">
              <div className="inline-flex items-center gap-2 px-3 py-1.5 rounded-full bg-primary/20 text-primary text-xs font-semibold mb-4">
                {user.isAdmin && <Shield className="w-3.5 h-3.5" />}
                <span>{user.isAdmin ? 'ADMIN ACCESS' : 'AUTHENTICATED'}</span>
              </div>

              <h2 className="text-3xl lg:text-4xl font-heading font-semibold mb-4">
                Welcome back, <span className="text-primary">{getDisplayName(user)}</span>!
              </h2>

              <p className="text-lg text-muted-foreground max-w-2xl mx-auto">
                {user.isAdmin
                  ? 'You have admin access. Manage all requests, filaments, and printer settings from your admin panel.'
                  : 'View and manage your requests, track their progress, and receive real-time notifications.'}
              </p>

              <div className="flex flex-wrap justify-center gap-3 pt-8">
                <Link to="/dashboard">
                  <Button size="lg" className="transition-all hover:scale-105">
                    <User className="w-5 h-5 mr-2" />
                    My Dashboard
                  </Button>
                </Link>
                {user.isAdmin && (
                  <Link to="/admin">
                    <Button size="lg" variant="outline" className="border-2 transition-all hover:scale-105 hover:border-primary">
                      <Shield className="w-5 h-5 mr-2" />
                      Admin Panel
                    </Button>
                  </Link>
                )}
              </div>
            </div>
          </div>
        </section>
      ) : (
        <section className="max-w-4xl mx-auto">
          <div className="relative card-enhanced bg-gradient-to-br from-muted/50 to-muted/30 p-10 text-center space-y-6 overflow-hidden">
            {/* Decorative grid */}
            <div className="absolute inset-0 bg-grid-pattern-fine opacity-20" />

            <div className="relative">
              <h2 className="text-3xl lg:text-4xl font-heading font-semibold mb-4">
                Get Started Today
              </h2>
              <p className="text-lg text-muted-foreground max-w-2xl mx-auto leading-relaxed">
                You can submit requests as a guest and track them with a token, or sign in with Discord for enhanced features like editing, history, and notifications.
              </p>

              <div className="flex flex-wrap justify-center gap-3 pt-8">
                <Link to="/requests/new">
                  <Button size="lg" className="transition-all hover:scale-105">
                    <Package className="w-5 h-5 mr-2" />
                    Submit as Guest
                  </Button>
                </Link>
                <Link to="/api/auth/discord">
                  <Button size="lg" variant="outline" className="border-2 transition-all hover:scale-105">
                    <User className="w-5 h-5 mr-2" />
                    Sign in with Discord
                  </Button>
                </Link>
              </div>
            </div>
          </div>
        </section>
      )}
    </div>
  );
};
