import { Link } from 'react-router-dom';
import { Button } from '../components/ui/button';
import { Package, Search, User, Shield } from 'lucide-react';
import { useAuth } from '../contexts/AuthContext';

export const Home = () => {
  const { isAuthenticated, user } = useAuth();

  return (
    <div className="space-y-12">
      {/* Hero Section */}
      <section className="text-center space-y-6 py-12">
        <h1 className="text-5xl font-bold tracking-tight">
          Welcome to <span className="text-primary">UberPrints</span>
        </h1>
        <p className="text-xl text-muted-foreground max-w-2xl mx-auto">
          Your friendly 3D printing service for friends. Submit requests, track progress, and get your prints delivered!
        </p>
        <div className="flex justify-center gap-4 pt-4">
          <Link to="/request/new">
            <Button size="lg">
              <Package className="w-5 h-5 mr-2" />
              Submit Request
            </Button>
          </Link>
          <Link to="/requests">
            <Button size="lg" variant="outline">
              <Search className="w-5 h-5 mr-2" />
              View All Requests
            </Button>
          </Link>
        </div>
      </section>

      {/* Features Section */}
      <section className="grid md:grid-cols-3 gap-8 py-8">
        <div className="text-center space-y-4 p-6 border rounded-lg">
          <div className="mx-auto w-12 h-12 bg-primary/10 rounded-full flex items-center justify-center">
            <Package className="w-6 h-6 text-primary" />
          </div>
          <h3 className="text-xl font-semibold">Easy Requests</h3>
          <p className="text-muted-foreground">
            Submit your 3D printing requests with just a few clicks. No account required for guest users!
          </p>
        </div>

        <div className="text-center space-y-4 p-6 border rounded-lg">
          <div className="mx-auto w-12 h-12 bg-primary/10 rounded-full flex items-center justify-center">
            <Search className="w-6 h-6 text-primary" />
          </div>
          <h3 className="text-xl font-semibold">Track Progress</h3>
          <p className="text-muted-foreground">
            Follow your request through every step of the process with real-time status updates.
          </p>
        </div>

        <div className="text-center space-y-4 p-6 border rounded-lg">
          <div className="mx-auto w-12 h-12 bg-primary/10 rounded-full flex items-center justify-center">
            <User className="w-6 h-6 text-primary" />
          </div>
          <h3 className="text-xl font-semibold">Discord Login</h3>
          <p className="text-muted-foreground">
            Sign in with Discord to manage your requests, edit details, and view your history.
          </p>
        </div>
      </section>

      {/* User-specific Section */}
      {isAuthenticated && user ? (
        <section className="bg-primary/5 rounded-lg p-8 text-center space-y-4">
          <h2 className="text-2xl font-semibold">
            Welcome back, {user.username}!
          </h2>
          <p className="text-muted-foreground">
            {user.isAdmin
              ? 'You have admin access. Manage requests and filaments from your admin panel.'
              : 'View and manage your requests from your dashboard.'}
          </p>
          <div className="flex justify-center gap-4 pt-2">
            <Link to="/dashboard">
              <Button>
                <User className="w-4 h-4 mr-2" />
                My Dashboard
              </Button>
            </Link>
            {user.isAdmin && (
              <Link to="/admin">
                <Button variant="outline">
                  <Shield className="w-4 h-4 mr-2" />
                  Admin Panel
                </Button>
              </Link>
            )}
          </div>
        </section>
      ) : (
        <section className="bg-muted/50 rounded-lg p-8 text-center space-y-4">
          <h2 className="text-2xl font-semibold">
            Get Started
          </h2>
          <p className="text-muted-foreground max-w-2xl mx-auto">
            You can submit requests as a guest and track them with a token, or sign in with Discord for enhanced features like editing and request history.
          </p>
        </section>
      )}
    </div>
  );
};
