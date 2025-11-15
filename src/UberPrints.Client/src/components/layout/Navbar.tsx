import { Link } from 'react-router-dom';
import { useAuth } from '../../contexts/AuthContext';
import { useState } from 'react';
import { Button } from '../ui/button';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from '../ui/dropdown-menu';
import { User, LogOut, Settings, Home, List, Plus, Search, Palette, LayoutDashboard, PackagePlus, Menu, Camera } from 'lucide-react';
import { api } from '../../lib/api';
import { getDisplayName } from '../../lib/utils';
import { Avatar, AvatarFallback, AvatarImage } from '../ui/avatar';
import { Sheet, SheetContent, SheetHeader, SheetTitle, SheetTrigger } from '../ui/sheet';

export const Navbar = () => {
  const { user, isAuthenticated, isAdmin, logout } = useAuth();
  const [mobileMenuOpen, setMobileMenuOpen] = useState(false);

  const handleLogin = () => {
    window.location.href = api.getDiscordLoginUrl();
  };

  const handleLogout = async () => {
    await logout();
    window.location.href = '/';
  };

  const getInitials = (name: string) => {
    return name
      .split(' ')
      .map(n => n[0])
      .slice(0, 2)
      .join('')
      .toUpperCase();
  };

  const getAvatarUrl = (userData: { discordId?: string; avatarHash?: string } | null) => {
    if (!userData) return undefined;
    if (userData.discordId && userData.avatarHash) {
      return `https://cdn.discordapp.com/avatars/${userData.discordId}/${userData.avatarHash}.png`;
    }
    return undefined;
  };

  return (
    <nav className="sticky top-0 z-50 border-b bg-background/95 backdrop-blur-sm supports-[backdrop-filter]:bg-background/80">
      <div className="container mx-auto px-4 py-3.5">
        <div className="flex items-center justify-between">
          <div className="flex items-center space-x-8">
            <Link to="/" className="flex items-center space-x-2 group">
              <div className="text-2xl font-heading font-bold text-primary group-hover:text-primary/80 transition-colors">
                UberPrints
              </div>
            </Link>

            {/* Mobile Menu Button */}
            <Sheet open={mobileMenuOpen} onOpenChange={setMobileMenuOpen}>
              <SheetTrigger asChild>
                <Button variant="ghost" size="sm" className="md:hidden">
                  <Menu className="w-5 h-5" />
                </Button>
              </SheetTrigger>
              <SheetContent side="left" className="w-[300px]">
                <SheetHeader>
                  <SheetTitle className="text-primary">UberPrints</SheetTitle>
                </SheetHeader>
                <nav className="flex flex-col gap-4 mt-6">
                  <Link to="/" onClick={() => setMobileMenuOpen(false)}>
                    <Button variant="ghost" size="sm" className="w-full justify-start">
                      <Home className="w-4 h-4 mr-2" />
                      Home
                    </Button>
                  </Link>
                  <Link to="/dashboard" onClick={() => setMobileMenuOpen(false)}>
                    <Button variant="ghost" size="sm" className="w-full justify-start">
                      <LayoutDashboard className="w-4 h-4 mr-2" />
                      Dashboard
                    </Button>
                  </Link>
                  <Link to="/filaments" onClick={() => setMobileMenuOpen(false)}>
                    <Button variant="ghost" size="sm" className="w-full justify-start">
                      <Palette className="w-4 h-4 mr-2" />
                      Filaments
                    </Button>
                  </Link>
                  <Link to="/filament-requests" onClick={() => setMobileMenuOpen(false)}>
                    <Button variant="ghost" size="sm" className="w-full justify-start">
                      <PackagePlus className="w-4 h-4 mr-2" />
                      Request Filament
                    </Button>
                  </Link>
                  <Link to="/live-view" onClick={() => setMobileMenuOpen(false)}>
                    <Button variant="ghost" size="sm" className="w-full justify-start">
                      <Camera className="w-4 h-4 mr-2" />
                      Live View
                    </Button>
                  </Link>
                  <Link to="/requests" onClick={() => setMobileMenuOpen(false)}>
                    <Button variant="ghost" size="sm" className="w-full justify-start">
                      <List className="w-4 h-4 mr-2" />
                      All Requests
                    </Button>
                  </Link>
                  <Link to="/requests/new" onClick={() => setMobileMenuOpen(false)}>
                    <Button variant="ghost" size="sm" className="w-full justify-start">
                      <Plus className="w-4 h-4 mr-2" />
                      New Request
                    </Button>
                  </Link>
                  <Link to="/track" onClick={() => setMobileMenuOpen(false)}>
                    <Button variant="ghost" size="sm" className="w-full justify-start">
                      <Search className="w-4 h-4 mr-2" />
                      Track
                    </Button>
                  </Link>
                  {isAdmin && (
                    <Link to="/admin" onClick={() => setMobileMenuOpen(false)}>
                      <Button variant="ghost" size="sm" className="w-full justify-start">
                        <Settings className="w-4 h-4 mr-2" />
                        Admin Panel
                      </Button>
                    </Link>
                  )}
                </nav>
              </SheetContent>
            </Sheet>

            <div className="hidden md:flex items-center space-x-1">
              <Link to="/">
                <Button variant="ghost" size="sm" className="transition-all hover:bg-primary/10">
                  <Home className="w-4 h-4 mr-1.5" />
                  Home
                </Button>
              </Link>
              <Link to="/dashboard">
                <Button variant="ghost" size="sm" className="transition-all hover:bg-primary/10">
                  <LayoutDashboard className="w-4 h-4 mr-1.5" />
                  Dashboard
                </Button>
              </Link>
              <Link to="/filaments">
                <Button variant="ghost" size="sm" className="transition-all hover:bg-primary/10">
                  <Palette className="w-4 h-4 mr-1.5" />
                  Filaments
                </Button>
              </Link>
              <Link to="/filament-requests">
                <Button variant="ghost" size="sm" className="transition-all hover:bg-primary/10">
                  <PackagePlus className="w-4 h-4 mr-1.5" />
                  Request Filament
                </Button>
              </Link>
              <Link to="/live-view">
                <Button variant="ghost" size="sm" className="transition-all hover:bg-primary/10">
                  <Camera className="w-4 h-4 mr-1.5" />
                  Live View
                </Button>
              </Link>
              <Link to="/requests">
                <Button variant="ghost" size="sm" className="transition-all hover:bg-primary/10">
                  <List className="w-4 h-4 mr-1.5" />
                  All Requests
                </Button>
              </Link>
              <Link to="/requests/new">
                <Button variant="ghost" size="sm" className="transition-all hover:bg-primary/10">
                  <Plus className="w-4 h-4 mr-1.5" />
                  New Request
                </Button>
              </Link>
              <Link to="/track">
                <Button variant="ghost" size="sm" className="transition-all hover:bg-primary/10">
                  <Search className="w-4 h-4 mr-1.5" />
                  Track
                </Button>
              </Link>
            </div>
          </div>

          <div className="flex items-center gap-2">

            {isAuthenticated && user ? (
              <DropdownMenu>
                <DropdownMenuTrigger asChild>
                  <Button variant="outline" size="sm" className="gap-2 transition-all hover:border-primary">
                    <Avatar className="w-6 h-6 ring-2 ring-primary/10">
                      <AvatarImage src={getAvatarUrl(user)} alt={getDisplayName(user)} />
                      <AvatarFallback className="text-xs bg-primary/10 text-primary font-medium">
                        {getInitials(getDisplayName(user))}
                      </AvatarFallback>
                    </Avatar>
                    <span className="hidden sm:inline">{getDisplayName(user)}</span>
                  </Button>
                </DropdownMenuTrigger>
                <DropdownMenuContent align="end" className="w-56">
                  <DropdownMenuLabel className="font-heading">My Account</DropdownMenuLabel>
                  <DropdownMenuSeparator />
                  <DropdownMenuItem asChild>
                    <Link to="/profile" className="cursor-pointer">
                      <User className="w-4 h-4 mr-2" />
                      Profile
                    </Link>
                  </DropdownMenuItem>
                  <DropdownMenuItem asChild>
                    <Link to="/dashboard" className="cursor-pointer">
                      <LayoutDashboard className="w-4 h-4 mr-2" />
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
                  <DropdownMenuItem onClick={handleLogout} className="cursor-pointer text-destructive focus:text-destructive">
                    <LogOut className="w-4 h-4 mr-2" />
                    Logout
                  </DropdownMenuItem>
                </DropdownMenuContent>
              </DropdownMenu>
            ) : (
              <Button onClick={handleLogin} size="sm" className="transition-all hover:scale-105">
                Login with Discord
              </Button>
            )}
          </div>
        </div>
      </div>
    </nav>
  );
};
