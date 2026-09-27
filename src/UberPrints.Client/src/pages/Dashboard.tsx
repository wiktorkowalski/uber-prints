import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { api } from '../lib/api';
import { PrintRequestDto } from '../types/api';
import { useAuth } from '../contexts/AuthContext';
import { Button } from '../components/ui/button';
import { Skeleton } from '../components/ui/skeleton';
import { Package, Info } from 'lucide-react';
import { PageHeader } from '../components/PageHeader';
import { RequestListHeader, RequestRow, RequestRowsSkeleton } from '../components/RequestRow';

export const Dashboard = () => {
  const { user, isAuthenticated } = useAuth();
  const [requests, setRequests] = useState<PrintRequestDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const loadMyRequests = async () => {
    try {
      setLoading(true);
      setError(null);
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

  useEffect(() => {
    loadMyRequests();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [user]);

  const newRequestButton = (
    <Link to="/requests/new">
      <Button>
        <Package className="w-4 h-4 mr-2" />
        New request
      </Button>
    </Link>
  );

  if (loading) {
    return (
      <div className="space-y-6">
        <Skeleton className="h-10 w-48" />
        <RequestRowsSkeleton rows={3} />
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <PageHeader
        title="My requests"
        description={isAuthenticated ? undefined : 'Requests made from this browser'}
        actions={newRequestButton}
        className="mb-0"
      />

      {!isAuthenticated && (
        <div className="flex items-start gap-3 rounded-lg border border-border bg-card p-4">
          <Info className="w-5 h-5 text-primary flex-shrink-0 mt-0.5" />
          <div className="flex-1">
            <h3 className="font-heading font-bold mb-1">You're using a guest session</h3>
            <p className="text-sm text-muted-foreground mb-3">
              Your requests are saved to this browser only. Sign in with Discord to see them on any device and get status updates.
            </p>
            <Button
              size="sm"
              variant="outline"
              onClick={() => window.location.href = api.getDiscordLoginUrl()}
            >
              Sign in with Discord
            </Button>
          </div>
        </div>
      )}

      {error ? (
        <div className="text-center py-12">
          <p className="text-destructive mb-4">{error}</p>
          <Button onClick={loadMyRequests}>Try Again</Button>
        </div>
      ) : requests.length === 0 ? (
        <div className="rounded-lg border border-border bg-card py-16 text-center">
          <Package className="w-10 h-10 mx-auto mb-4 text-muted-foreground" />
          <h3 className="text-xl font-heading font-bold mb-2">No requests yet</h3>
          <p className="text-muted-foreground mb-6 max-w-md mx-auto">
            Your print requests show up here.
          </p>
          {newRequestButton}
        </div>
      ) : (
        <section className="overflow-hidden rounded-lg border border-border bg-card">
          <RequestListHeader />
          {requests.map((request) => (
            <RequestRow key={request.id} request={request} />
          ))}
        </section>
      )}
    </div>
  );
};
