import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { api } from '../lib/api';
import { useToast } from '../hooks/use-toast';
import { Button } from '../components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '../components/ui/card';
import { Skeleton } from '../components/ui/skeleton';
import { Shield, Package, PrinterIcon, Users, Camera, ArrowRight } from 'lucide-react';
import { PrinterStatusCard } from '../components/PrinterStatusCard';
import { PrinterStatusDto } from '../types/api';

export const AdminDashboard = () => {
  const { toast } = useToast();
  const [stats, setStats] = useState<{
    totalRequests: number;
    pendingRequests: number;
    totalFilaments: number;
    totalUsers: number;
    guestCount: number;
  } | null>(null);
  const [streamStats, setStreamStats] = useState<{
    isEnabled: boolean;
    isActive: boolean;
    activeViewers: number;
  } | null>(null);
  const [printerStatus, setPrinterStatus] = useState<PrinterStatusDto | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    loadStats();
    loadStreamStats();
    loadPrinterStatus();

    // Poll printer status every 10 seconds
    const interval = setInterval(loadPrinterStatus, 10000);
    return () => clearInterval(interval);
  }, []);

  const loadStats = async () => {
    try {
      setLoading(true);
      // Get requests
      const requests = await api.getAdminRequests();
      // Get filaments
      const filaments = await api.getFilaments();
      // Get users
      const users = await api.getAdminUsers();

      const pendingCount = requests.filter(r => r.currentStatus === 0).length;
      const guestCount = users.filter(u => u.isGuest).length;

      setStats({
        totalRequests: requests.length,
        pendingRequests: pendingCount,
        totalFilaments: filaments.length,
        totalUsers: users.length,
        guestCount: guestCount,
      });
    } catch (err) {
      console.error('Error loading stats:', err);
      toast({
        title: "Failed to load stats",
        description: "Could not load dashboard statistics",
        variant: "destructive",
      });
    } finally {
      setLoading(false);
    }
  };

  const loadStreamStats = async () => {
    try {
      const data = await api.getStreamStats();
      setStreamStats(data);
    } catch (err) {
      console.error('Error loading stream stats:', err);
    }
  };

  const loadPrinterStatus = async () => {
    try {
      const data = await api.getPrinterStatus();
      setPrinterStatus(data);
    } catch (err) {
      console.error('Error loading printer status:', err);
    }
  };

  if (loading || !stats) {
    return (
      <div className="space-y-6">
        <div>
          <Skeleton className="h-10 w-64" />
          <Skeleton className="h-4 w-96 mt-1" />
        </div>
        <div className="grid md:grid-cols-5 gap-4">
          {[1, 2, 3, 4, 5].map((i) => (
            <Card key={i}>
              <CardHeader className="pb-3">
                <Skeleton className="h-4 w-32" />
                <Skeleton className="h-8 w-16" />
              </CardHeader>
            </Card>
          ))}
        </div>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div>
        <h1 className="text-3xl font-bold flex items-center gap-2">
          <Shield className="w-8 h-8" />
          Admin Dashboard
        </h1>
        <p className="text-muted-foreground mt-1">
          System overview and quick access to management tools
        </p>
      </div>

      {/* Stats Cards */}
      <div className="grid md:grid-cols-5 gap-4">
        <Card>
          <CardHeader className="pb-3">
            <CardDescription>Total Requests</CardDescription>
            <CardTitle className="text-3xl">{stats.totalRequests}</CardTitle>
          </CardHeader>
        </Card>
        <Card>
          <CardHeader className="pb-3">
            <CardDescription>Pending</CardDescription>
            <CardTitle className="text-3xl text-amber-600">{stats.pendingRequests}</CardTitle>
          </CardHeader>
        </Card>
        <Card>
          <CardHeader className="pb-3">
            <CardDescription>Filaments</CardDescription>
            <CardTitle className="text-3xl">{stats.totalFilaments}</CardTitle>
          </CardHeader>
        </Card>
        <Card>
          <CardHeader className="pb-3">
            <CardDescription>Total Users</CardDescription>
            <CardTitle className="text-3xl">{stats.totalUsers}</CardTitle>
          </CardHeader>
        </Card>
        <Card>
          <CardHeader className="pb-3">
            <CardDescription>Guests</CardDescription>
            <CardTitle className="text-3xl">{stats.guestCount}</CardTitle>
          </CardHeader>
        </Card>
      </div>

      {/* Printer Status */}
      {printerStatus && (
        <PrinterStatusCard status={printerStatus} />
      )}

      {/* Live Stream Card */}
      <Card>
        <CardHeader>
          <div className="flex items-center justify-between">
            <div>
              <CardTitle className="flex items-center gap-2">
                <Camera className="w-5 h-5" />
                Live Camera Stream
              </CardTitle>
              <CardDescription className="mt-1">
                {streamStats ? (
                  streamStats.isEnabled ? (
                    streamStats.isActive ? (
                      <span className="flex items-center gap-1 text-green-600">
                        <span className="w-2 h-2 bg-green-600 rounded-full animate-pulse" />
                        Live
                      </span>
                    ) : (
                      'Offline'
                    )
                  ) : (
                    'Disabled'
                  )
                ) : (
                  'Loading...'
                )}
              </CardDescription>
            </div>
            <Button variant="outline" size="sm" onClick={() => window.location.href = '/live-view'}>
              View Stream
            </Button>
          </div>
        </CardHeader>
        <CardContent>
          <div className="flex items-center gap-2 text-sm text-muted-foreground">
            <Users className="w-4 h-4" />
            <span>{streamStats?.activeViewers || 0} active viewers</span>
          </div>
        </CardContent>
      </Card>

      {/* Management Cards */}
      <div className="space-y-4">
        <h2 className="text-xl font-semibold">Management Tools</h2>

        <div className="grid md:grid-cols-2 gap-4">
          {/* Print Requests Card */}
          <Link to="/admin/requests">
            <Card className="h-full hover:shadow-lg transition-shadow cursor-pointer">
              <CardHeader>
                <div className="flex items-center justify-between">
                  <div className="flex items-center gap-3">
                    <PrinterIcon className="w-8 h-8 text-blue-500" />
                    <div>
                      <CardTitle>Print Requests</CardTitle>
                      <CardDescription>Manage all print requests</CardDescription>
                    </div>
                  </div>
                  <ArrowRight className="w-5 h-5 text-muted-foreground" />
                </div>
              </CardHeader>
              <CardContent>
                <div className="text-sm text-muted-foreground">
                  {stats.pendingRequests} pending • {stats.totalRequests} total
                </div>
              </CardContent>
            </Card>
          </Link>

          {/* Filaments Card */}
          <Link to="/admin/filaments">
            <Card className="h-full hover:shadow-lg transition-shadow cursor-pointer">
              <CardHeader>
                <div className="flex items-center justify-between">
                  <div className="flex items-center gap-3">
                    <Package className="w-8 h-8 text-purple-500" />
                    <div>
                      <CardTitle>Filaments</CardTitle>
                      <CardDescription>Manage inventory</CardDescription>
                    </div>
                  </div>
                  <ArrowRight className="w-5 h-5 text-muted-foreground" />
                </div>
              </CardHeader>
              <CardContent>
                <div className="text-sm text-muted-foreground">
                  {stats.totalFilaments} filaments in stock
                </div>
              </CardContent>
            </Card>
          </Link>

          {/* Filament Requests Card */}
          <Link to="/admin/filament-requests">
            <Card className="h-full hover:shadow-lg transition-shadow cursor-pointer">
              <CardHeader>
                <div className="flex items-center justify-between">
                  <div className="flex items-center gap-3">
                    <Package className="w-8 h-8 text-amber-500" />
                    <div>
                      <CardTitle>Filament Requests</CardTitle>
                      <CardDescription>Review requests</CardDescription>
                    </div>
                  </div>
                  <ArrowRight className="w-5 h-5 text-muted-foreground" />
                </div>
              </CardHeader>
              <CardContent>
                <div className="text-sm text-muted-foreground">
                  User requests for new filaments
                </div>
              </CardContent>
            </Card>
          </Link>

          {/* Users & Guests Card */}
          <Link to="/admin/users">
            <Card className="h-full hover:shadow-lg transition-shadow cursor-pointer">
              <CardHeader>
                <div className="flex items-center justify-between">
                  <div className="flex items-center gap-3">
                    <Users className="w-8 h-8 text-green-500" />
                    <div>
                      <CardTitle>Users & Guests</CardTitle>
                      <CardDescription>Manage users</CardDescription>
                    </div>
                  </div>
                  <ArrowRight className="w-5 h-5 text-muted-foreground" />
                </div>
              </CardHeader>
              <CardContent>
                <div className="text-sm text-muted-foreground">
                  {stats.totalUsers} total • {stats.guestCount} guests
                </div>
              </CardContent>
            </Card>
          </Link>
        </div>
      </div>
    </div>
  );
};
