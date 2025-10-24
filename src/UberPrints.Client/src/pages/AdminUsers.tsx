import { useEffect, useState } from 'react';
import { api } from '../lib/api';
import { AdminUserDto } from '../types/api';
import { useToast } from '../hooks/use-toast';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '../components/ui/card';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '../components/ui/tabs';
import { Badge } from '../components/ui/badge';
import { Skeleton } from '../components/ui/skeleton';
import { formatRelativeTime } from '../lib/utils';
import { Shield, Users, Clock, PrinterIcon, Package } from 'lucide-react';

export const AdminUsers = () => {
  const { toast } = useToast();
  const [users, setUsers] = useState<AdminUserDto[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    loadUsers();
  }, []);

  const loadUsers = async () => {
    try {
      setLoading(true);
      const data = await api.getAdminUsers();
      setUsers(data);
    } catch (err) {
      console.error('Error loading users:', err);
      toast({
        title: "Failed to load users",
        description: "Could not load users and guests list",
        variant: "destructive",
      });
    } finally {
      setLoading(false);
    }
  };

  const authenticatedUsers = users.filter(u => !u.isGuest);
  const guestUsers = users.filter(u => u.isGuest);

  if (loading) {
    return (
      <div className="space-y-6">
        <div>
          <Skeleton className="h-10 w-64" />
          <Skeleton className="h-4 w-96 mt-1" />
        </div>
        <div className="grid md:grid-cols-3 gap-4">
          {[1, 2, 3].map((i) => (
            <Card key={i}>
              <CardHeader className="pb-3">
                <Skeleton className="h-4 w-32" />
                <Skeleton className="h-8 w-16" />
              </CardHeader>
            </Card>
          ))}
        </div>
        <Card>
          <CardContent className="pt-6">
            <div className="space-y-4">
              {[1, 2, 3].map((i) => (
                <div key={i} className="border rounded-lg p-4 space-y-3">
                  <div className="flex justify-between items-start">
                    <div className="space-y-2 flex-1">
                      <Skeleton className="h-6 w-48" />
                      <Skeleton className="h-4 w-64" />
                    </div>
                  </div>
                  <div className="flex gap-2">
                    <Skeleton className="h-6 w-20" />
                    <Skeleton className="h-6 w-20" />
                  </div>
                </div>
              ))}
            </div>
          </CardContent>
        </Card>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div>
        <h1 className="text-3xl font-bold flex items-center gap-2">
          <Shield className="w-8 h-8" />
          Users & Guests
        </h1>
        <p className="text-muted-foreground mt-1">
          View all authenticated users and guest sessions
        </p>
      </div>

      {/* Stats Cards */}
      <div className="grid md:grid-cols-3 gap-4">
        <Card>
          <CardHeader className="pb-3">
            <CardDescription>Total Users</CardDescription>
            <CardTitle className="text-3xl">{users.length}</CardTitle>
          </CardHeader>
        </Card>
        <Card>
          <CardHeader className="pb-3">
            <CardDescription>Authenticated Users</CardDescription>
            <CardTitle className="text-3xl">{authenticatedUsers.length}</CardTitle>
          </CardHeader>
        </Card>
        <Card>
          <CardHeader className="pb-3">
            <CardDescription>Guest Users</CardDescription>
            <CardTitle className="text-3xl">{guestUsers.length}</CardTitle>
          </CardHeader>
        </Card>
      </div>

      {/* Tabs */}
      <Tabs defaultValue="authenticated" className="space-y-4">
        <TabsList>
          <TabsTrigger value="authenticated">
            Authenticated ({authenticatedUsers.length})
          </TabsTrigger>
          <TabsTrigger value="guests">
            Guests ({guestUsers.length})
          </TabsTrigger>
        </TabsList>

        <TabsContent value="authenticated" className="space-y-4">
          <UsersTable users={authenticatedUsers} />
        </TabsContent>

        <TabsContent value="guests" className="space-y-4">
          <UsersTable users={guestUsers} />
        </TabsContent>
      </Tabs>
    </div>
  );
};

interface UsersTableProps {
  users: AdminUserDto[];
}

const UsersTable = ({ users }: UsersTableProps) => {
  if (users.length === 0) {
    return (
      <Card>
        <CardContent className="pt-6">
          <div className="text-center py-12">
            <Users className="w-12 h-12 mx-auto mb-4 text-muted-foreground" />
            <h3 className="text-lg font-semibold mb-2">No users found</h3>
            <p className="text-muted-foreground">
              No users in this category yet
            </p>
          </div>
        </CardContent>
      </Card>
    );
  }

  return (
    <div className="space-y-4">
      {users.map((user) => (
        <Card key={user.id}>
          <CardContent className="pt-6">
            <div className="flex items-start justify-between gap-4">
              <div className="flex-1 min-w-0">
                <div className="flex items-center gap-3 mb-2 flex-wrap">
                  <h3 className="text-lg font-semibold break-all">{user.username}</h3>
                  {user.isAdmin && (
                    <Badge variant="default" className="bg-purple-600">
                      Admin
                    </Badge>
                  )}
                  {user.isGuest && (
                    <Badge variant="secondary">Guest</Badge>
                  )}
                </div>

                <div className="space-y-2 text-sm text-muted-foreground">
                  {user.globalName && (
                    <div>
                      <span className="font-medium text-foreground">{user.globalName}</span>
                      <span> • Display Name</span>
                    </div>
                  )}

                  {user.discordId && (
                    <div>
                      Discord ID: <span className="font-mono text-xs">{user.discordId}</span>
                    </div>
                  )}

                  {user.guestSessionToken && (
                    <div>
                      Guest Token: <span className="font-mono text-xs">{user.guestSessionToken.substring(0, 12)}...</span>
                    </div>
                  )}

                  <div className="flex items-center gap-4 mt-3 pt-3 border-t">
                    <div className="flex items-center gap-2">
                      <Clock className="w-4 h-4" />
                      <span>Joined {formatRelativeTime(user.createdAt)}</span>
                    </div>
                  </div>
                </div>
              </div>

              <div className="flex flex-col gap-3 min-w-max">
                <div className="flex items-center gap-2 px-3 py-2 bg-muted rounded-lg">
                  <PrinterIcon className="w-4 h-4 text-muted-foreground" />
                  <div className="text-right">
                    <div className="text-sm font-semibold">{user.printRequestCount}</div>
                    <div className="text-xs text-muted-foreground">Requests</div>
                  </div>
                </div>

                <div className="flex items-center gap-2 px-3 py-2 bg-muted rounded-lg">
                  <Package className="w-4 h-4 text-muted-foreground" />
                  <div className="text-right">
                    <div className="text-sm font-semibold">{user.filamentRequestCount}</div>
                    <div className="text-xs text-muted-foreground">Filament Reqs</div>
                  </div>
                </div>
              </div>
            </div>
          </CardContent>
        </Card>
      ))}
    </div>
  );
};
