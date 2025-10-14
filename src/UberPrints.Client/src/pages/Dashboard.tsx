import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { api } from '../lib/api';
import { PrintRequestDto, RequestStatusEnum } from '../types/api';
import { useAuth } from '../contexts/AuthContext';
import { Button } from '../components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '../components/ui/card';
import { LoadingSpinner } from '../components/ui/loading-spinner';
import { getStatusLabel, getStatusColor, formatRelativeTime } from '../lib/utils';
import { Package, Plus, ExternalLink, User, AlertCircle } from 'lucide-react';

export const Dashboard = () => {
  const { user, isAuthenticated } = useAuth();
  const [requests, setRequests] = useState<PrintRequestDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    loadMyRequests();
  }, [user]);

  const loadMyRequests = async () => {
    try {
      setLoading(true);
      const allRequests = await api.getRequests();
      // Filter to only show user's requests (both authenticated and guest)
      const myRequests = allRequests.filter(r => r.userId === user?.id);
      setRequests(myRequests);
    } catch (err) {
      console.error('Error loading requests:', err);
      setError('Failed to load your requests');
    } finally {
      setLoading(false);
    }
  };

  if (loading) {
    return <LoadingSpinner message="Loading your requests..." />;
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold flex items-center gap-2">
            <User className="w-8 h-8" />
            My Dashboard
          </h1>
          <p className="text-muted-foreground mt-1">
            {isAuthenticated
              ? `Welcome back, ${user?.username}!`
              : 'Your print requests from this browser'}
          </p>
        </div>
        <Link to="/request/new">
          <Button>
            <Plus className="w-4 h-4 mr-2" />
            New Request
          </Button>
        </Link>
      </div>

      {/* Guest User Notice */}
      {!isAuthenticated && (
        <Card className="bg-blue-50 dark:bg-blue-950 border-blue-200 dark:border-blue-800">
          <CardContent className="pt-6">
            <div className="flex items-start gap-3">
              <AlertCircle className="w-5 h-5 text-blue-600 dark:text-blue-400 flex-shrink-0 mt-0.5" />
              <div>
                <h3 className="font-medium text-blue-900 dark:text-blue-100 mb-1">
                  You're using a guest session
                </h3>
                <p className="text-sm text-blue-700 dark:text-blue-300 mb-3">
                  Your requests are saved to this browser only. Sign in with Discord to access your requests from any device and get notifications about status updates.
                </p>
                <Button
                  size="sm"
                  onClick={() => window.location.href = api.getDiscordLoginUrl()}
                >
                  Sign in with Discord
                </Button>
              </div>
            </div>
          </CardContent>
        </Card>
      )}

      {/* Stats Cards */}
      <div className="grid md:grid-cols-3 gap-4">
        <Card>
          <CardHeader className="pb-3">
            <CardDescription>Total Requests</CardDescription>
            <CardTitle className="text-3xl">{requests.length}</CardTitle>
          </CardHeader>
        </Card>
        <Card>
          <CardHeader className="pb-3">
            <CardDescription>Pending/In Progress</CardDescription>
            <CardTitle className="text-3xl">
              {requests.filter(r => ![RequestStatusEnum.Completed, RequestStatusEnum.Rejected].includes(r.currentStatus)).length}
            </CardTitle>
          </CardHeader>
        </Card>
        <Card>
          <CardHeader className="pb-3">
            <CardDescription>Completed</CardDescription>
            <CardTitle className="text-3xl">
              {requests.filter(r => r.currentStatus === RequestStatusEnum.Completed).length}
            </CardTitle>
          </CardHeader>
        </Card>
      </div>

      {/* Requests List */}
      <Card>
        <CardHeader>
          <CardTitle>Your Requests</CardTitle>
          <CardDescription>
            All your 3D printing requests in one place
          </CardDescription>
        </CardHeader>
        <CardContent>
          {error ? (
            <div className="text-center py-8">
              <p className="text-red-600 mb-4">{error}</p>
              <Button onClick={loadMyRequests}>Try Again</Button>
            </div>
          ) : requests.length === 0 ? (
            <div className="text-center py-12">
              <Package className="w-12 h-12 mx-auto mb-4 text-muted-foreground" />
              <h3 className="text-lg font-semibold mb-2">No requests yet</h3>
              <p className="text-muted-foreground mb-4">
                Start by submitting your first 3D printing request!
              </p>
              <Link to="/request/new">
                <Button>
                  <Plus className="w-4 h-4 mr-2" />
                  Submit Request
                </Button>
              </Link>
            </div>
          ) : (
            <div className="space-y-4">
              {requests.map((request) => (
                <Link
                  key={request.id}
                  to={`/request/${request.id}`}
                  className="block border rounded-lg p-4 hover:border-primary transition-colors"
                >
                  <div className="flex justify-between items-start mb-3">
                    <div>
                      <h3 className="font-semibold">{request.requesterName}</h3>
                      <p className="text-sm text-muted-foreground">
                        {formatRelativeTime(request.createdAt)}
                      </p>
                    </div>
                    <span
                      className={`px-3 py-1 rounded-full text-xs font-medium ${getStatusColor(
                        request.currentStatus
                      )}`}
                    >
                      {getStatusLabel(request.currentStatus)}
                    </span>
                  </div>

                  <div className="space-y-2 text-sm">
                    <div className="flex items-center text-muted-foreground">
                      <ExternalLink className="w-4 h-4 mr-2 flex-shrink-0" />
                      <span className="truncate">{request.modelUrl}</span>
                    </div>
                    {request.filamentName && (
                      <div className="text-muted-foreground">
                        Filament: <span className="font-medium">{request.filamentName}</span>
                      </div>
                    )}
                    {request.requestDelivery && (
                      <div className="text-muted-foreground">
                        🚚 Delivery requested
                      </div>
                    )}
                    {request.notes && (
                      <p className="text-muted-foreground line-clamp-1 mt-2">
                        {request.notes}
                      </p>
                    )}
                  </div>
                </Link>
              ))}
            </div>
          )}
        </CardContent>
      </Card>
    </div>
  );
};
