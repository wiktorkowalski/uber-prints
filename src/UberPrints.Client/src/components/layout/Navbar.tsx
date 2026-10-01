import { Link, NavLink } from 'react-router-dom';
import { useAuth } from '../../hooks/use-auth';
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
import { User, LogOut, Settings, LayoutDashboard, Menu, Plus, Search } from 'lucide-react';
import { api } from '../../lib/api';
import { cn, getDisplayName } from '../../lib/utils';
import { Avatar, AvatarFallback, AvatarImage } from '../ui/avatar';
import { Sheet, SheetContent, SheetHeader, SheetTitle, SheetTrigger } from '../ui/sheet';
import { ThemeToggle } from './ThemeToggle';

const navItems = [
  { to: '/requests', label: 'Requests' },
  { to: '/live-view', label: 'Printer' },
  { to: '/filaments', label: 'Filaments' },
];

const LayersLogo = () => (
  <svg width="22" height="22" viewBox="0 0 22 22" fill="none" stroke="currentColor" strokeWidth="2.4" strokeLinecap="round" aria-hidden="true" className="text-primary">
    <path d="M3 19h16M5 15h12M7 11h8M9 7h4" />
  </svg>
);

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

  const closeMobileMenu = () => setMobileMenuOpen(false);

  return (
    <nav className="sticky top-0 z-50 border-b bg-card">
      <div className="container mx-auto px-4 py-3">
        <div className="flex items-center justify-between gap-4">
          <div className="flex items-center gap-8">
            {/* Mobile Menu Button */}
            <Sheet open={mobileMenuOpen} onOpenChange={setMobileMenuOpen}>
              <SheetTrigger asChild>
                <Button variant="ghost" size="sm" className="md:hidden" aria-label="Open menu">
                  <Menu className="w-5 h-5" />
                </Button>
              </SheetTrigger>
              <SheetContent side="left" className="w-[300px]">
                <SheetHeader>
                  <SheetTitle className="flex items-center gap-2 font-heading">
                    <LayersLogo />
                    UberPrints
                  </SheetTitle>
                </SheetHeader>
                <nav className="flex flex-col gap-1 mt-6">
                  {navItems.map(item => (
                    <NavLink
                      key={item.to}
                      to={item.to}
                      onClick={closeMobileMenu}
                      className={({ isActive }) => cn(
                        'rounded-md px-3 py-2.5 font-semibold transition-colors hover:bg-accent',
                        isActive && 'bg-accent text-accent-foreground'
                      )}
                    >
                      {item.label}
                    </NavLink>
                  ))}
                  <Link to="/track" onClick={closeMobileMenu} className="rounded-md px-3 py-2.5 text-muted-foreground transition-colors hover:bg-accent">
                    Track a request
                  </Link>
                  <Link to="/requests/new" onClick={closeMobileMenu} className="mt-4">
                    <Button className="w-full">
                      <Plus className="w-4 h-4 mr-2" />
                      New request
                    </Button>
                  </Link>
                </nav>
              </SheetContent>
            </Sheet>

            <Link to="/" className="flex items-center gap-2.5 font-heading text-xl font-extrabold text-foreground">
              <LayersLogo />
              UberPrints
            </Link>

            <div className="hidden md:flex items-center gap-7">
              {navItems.map(item => (
                <NavLink
                  key={item.to}
                  to={item.to}
                  className={({ isActive }) => cn(
                    'py-1.5 text-[15px] font-semibold transition-colors hover:text-primary',
                    isActive ? 'text-primary shadow-[0_3px_0_hsl(var(--primary))]' : 'text-foreground'
                  )}
                >
                  {item.label}
                </NavLink>
              ))}
            </div>
          </div>

          <div className="flex items-center gap-2 sm:gap-3">
            <ThemeToggle />
            <Link to="/requests/new" className="hidden sm:block">
              <Button size="sm">
                <Plus className="w-4 h-4 mr-1.5" />
                New request
              </Button>
            </Link>

            {isAuthenticated && user ? (
              <DropdownMenu>
                <DropdownMenuTrigger asChild>
                  <Button variant="ghost" size="sm" className="gap-2 px-1.5" aria-label="Account menu">
                    <Avatar className="w-8 h-8">
                      <AvatarImage src={getAvatarUrl(user)} alt={getDisplayName(user)} />
                      <AvatarFallback className="text-xs bg-accent text-accent-foreground font-semibold">
                        {getInitials(getDisplayName(user))}
                      </AvatarFallback>
                    </Avatar>
                  </Button>
                </DropdownMenuTrigger>
                <DropdownMenuContent align="end" className="w-56">
                  <DropdownMenuLabel className="font-heading">{getDisplayName(user)}</DropdownMenuLabel>
                  <DropdownMenuSeparator />
                  <DropdownMenuItem asChild>
                    <Link to="/dashboard" className="cursor-pointer">
                      <LayoutDashboard className="w-4 h-4 mr-2" />
                      My requests
                    </Link>
                  </DropdownMenuItem>
                  <DropdownMenuItem asChild>
                    <Link to="/track" className="cursor-pointer">
                      <Search className="w-4 h-4 mr-2" />
                      Track a request
                    </Link>
                  </DropdownMenuItem>
                  <DropdownMenuItem asChild>
                    <Link to="/profile" className="cursor-pointer">
                      <User className="w-4 h-4 mr-2" />
                      Profile
                    </Link>
                  </DropdownMenuItem>
                  {isAdmin && (
                    <DropdownMenuItem asChild>
                      <Link to="/admin" className="cursor-pointer">
                        <Settings className="w-4 h-4 mr-2" />
                        Admin panel
                      </Link>
                    </DropdownMenuItem>
                  )}
                  <DropdownMenuSeparator />
                  <DropdownMenuItem onClick={handleLogout} className="cursor-pointer text-destructive focus:text-destructive">
                    <LogOut className="w-4 h-4 mr-2" />
                    Log out
                  </DropdownMenuItem>
                </DropdownMenuContent>
              </DropdownMenu>
            ) : (
              <Button onClick={handleLogin} size="sm" variant="outline">
                Log in with Discord
              </Button>
            )}
          </div>
        </div>
      </div>
    </nav>
  );
};
