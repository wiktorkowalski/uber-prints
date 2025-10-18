import { Link } from 'react-router-dom';
import { useAuth } from '../../contexts/AuthContext';
import { Button } from '../ui/button';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from '../ui/dropdown-menu';
import { User, LogOut, Settings, Home, List, Plus, Search, Palette, LayoutDashboard } from 'lucide-react';
import { api } from '../../lib/api';

export const Navbar = () => {
  const { user, isAuthenticated, isAdmin, logout } = useAuth();

  const handleLogin = () => {
    window.location.href = api.getDiscordLoginUrl();
  };

  const handleLogout = async () => {
    await logout();
    window.location.href = '/';
  };

  return (
    <nav className="border-b bg-background">
      <div className="container mx-auto px-4 py-3">
        <div className="flex items-center justify-between">
          <div className="flex items-center space-x-6">
            <Link to="/" className="flex items-center space-x-2">
              <span className="text-2xl font-bold text-primary">UberPrints</span>
            </Link>

            <div className="hidden md:flex items-center space-x-4">
              <Link to="/">
                <Button variant="ghost" size="sm">
                  <Home className="w-4 h-4 mr-2" />
                  Home
                </Button>
              </Link>
              <Link to="/dashboard">
                <Button variant="ghost" size="sm">
                  <LayoutDashboard className="w-4 h-4 mr-2" />
                  Dashboard
                </Button>
              </Link>
              <Link to="/filaments">
                <Button variant="ghost" size="sm">
                  <Palette className="w-4 h-4 mr-2" />
                  Filaments
                </Button>
              </Link>
              <Link to="/requests">
                <Button variant="ghost" size="sm">
                  <List className="w-4 h-4 mr-2" />
                  All Requests
                </Button>
              </Link>
              <Link to="/request/new">
                <Button variant="ghost" size="sm">
                  <Plus className="w-4 h-4 mr-2" />
                  New Request
                </Button>
              </Link>
              <Link to="/track">
                <Button variant="ghost" size="sm">
                  <Search className="w-4 h-4 mr-2" />
                  Track
                </Button>
              </Link>
            </div>
          </div>

          <div className="flex items-center space-x-4">
            {isAuthenticated && user ? (
              <>
                {isAdmin && (
                  <Link to="/admin">
                    <Button variant="outline" size="sm">
                      <Settings className="w-4 h-4 mr-2" />
                      Admin
                    </Button>
                  </Link>
                )}

                <DropdownMenu>
                  <DropdownMenuTrigger asChild>
                    <Button variant="outline" size="sm">
                      <User className="w-4 h-4 mr-2" />
                      {user.username}
                    </Button>
                  </DropdownMenuTrigger>
                  <DropdownMenuContent align="end" className="w-56">
                    <DropdownMenuLabel>My Account</DropdownMenuLabel>
                    <DropdownMenuSeparator />
                    <DropdownMenuItem asChild>
                      <Link to="/dashboard" className="cursor-pointer">
                        <User className="w-4 h-4 mr-2" />
                        Dashboard
                      </Link>
                    </DropdownMenuItem>
                    {isAdmin && (
                      <DropdownMenuItem asChild>
                        <Link to="/admin" className="cursor-pointer">
                          <Settings className="w-4 h-4 mr-2" />
                          Admin Panel
                        </Link>
                      </DropdownMenuItem>
                    )}
                    <DropdownMenuSeparator />
                    <DropdownMenuItem onClick={handleLogout} className="cursor-pointer">
                      <LogOut className="w-4 h-4 mr-2" />
                      Logout
                    </DropdownMenuItem>
                  </DropdownMenuContent>
                </DropdownMenu>
              </>
            ) : (
              <Button onClick={handleLogin} size="sm">
                Login with Discord
              </Button>
            )}
          </div>
        </div>
      </div>
    </nav>
  );
};
