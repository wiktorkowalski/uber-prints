import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { api } from '../lib/api';
import { PrintRequestDto, RequestStatusEnum } from '../types/api';
import { useAuth } from '../contexts/AuthContext';
import { Button } from '../components/ui/button';
import { Badge } from '../components/ui/badge';
import { Skeleton } from '../components/ui/skeleton';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '../components/ui/tabs';
import { formatRelativeTime } from '../lib/utils';
import { ExternalLink, Package, User, Truck } from 'lucide-react';
import { ModelThumbnail } from '../components/ModelThumbnail';
import { StatusBadge } from '../components/StatusBadge';
import { PageHeader } from '../components/PageHeader';

export const RequestList = () => {
  const { user } = useAuth();
  const [requests, setRequests] = useState<PrintRequestDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    loadRequests();
  }, []);

  const loadRequests = async () => {
    try {
      setLoading(true);
      const data = await api.getRequests();
      setRequests(data);
    } catch (err) {
      console.error('Error loading requests:', err);
      setError('Failed to load requests');
    } finally {
      setLoading(false);
    }
  };

  const getRequestsByStatus = (status: RequestStatusEnum | 'all' | 'mine') => {
    if (status === 'all') {
      return requests;
    }
    if (status === 'mine') {
      return user ? requests.filter(r => r.userId === user.id) : [];
    }
    return requests.filter(r => r.currentStatus === status);
  };

  const pendingCount = getRequestsByStatus(RequestStatusEnum.Pending).length;
  const myRequestsCount = user ? getRequestsByStatus('mine').length : 0;

  if (loading) {
    return (
      <div className="space-y-6">
        <Skeleton className="h-24 w-full rounded-lg" />
        <Skeleton className="h-12 w-full rounded-lg" />
        <div className="grid gap-4">
          {[1, 2, 3].map((i) => (
            <div key={i} className="card-enhanced p-6 space-y-4 animate-pulse">
              <div className="flex gap-6">
                <Skeleton className="h-32 w-32 rounded-lg flex-shrink-0" />
                <div className="flex-1 space-y-3">
                  <div className="flex justify-between items-start">
                    <div className="space-y-2 flex-1">
                      <Skeleton className="h-6 w-48" />
                      <Skeleton className="h-4 w-32" />
                    </div>
                    <Skeleton className="h-7 w-24 rounded-full" />
                  </div>
                  <div className="space-y-2">
                    <Skeleton className="h-4 w-full" />
                    <Skeleton className="h-4 w-3/4" />
                  </div>
                </div>
              </div>
            </div>
          ))}
        </div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="text-center py-12">
        <p className="text-red-600 mb-4">{error}</p>
        <Button onClick={loadRequests}>Try Again</Button>
      </div>
    );
  }

  const renderRequestsList = (filteredRequests: PrintRequestDto[]) => {
    if (filteredRequests.length === 0) {
      return (
        <div className="text-center py-12 border rounded-lg">
          <Package className="w-12 h-12 mx-auto mb-4 text-muted-foreground" />
          <h3 className="text-lg font-semibold mb-2">No requests found</h3>
          <p className="text-muted-foreground mb-4">
            No requests match this filter
          </p>
        </div>
      );
    }

    return (
      <div className="grid gap-4">
        {filteredRequests.map((request, index) => (
          <Link
            key={request.id}
            to={`/requests/${request.id}`}
            className="block"
          >
            <div
              className="group card-interactive p-6 animate-fade-in"
              style={{ animationDelay: `${index * 50}ms` }}
            >
              <div className="flex gap-6">
                {/* Model Thumbnail */}
                <div className="flex-shrink-0 overflow-hidden rounded-lg">
                  <ModelThumbnail modelUrl={request.modelUrl} size={144} />
                </div>

                {/* Request Content */}
                <div className="flex-1 min-w-0">
                  <div className="flex justify-between items-start mb-3">
                    <div className="flex-1 min-w-0">
                      <div className="flex items-center gap-2 mb-2 flex-wrap">
                        <h3 className="text-xl font-heading font-semibold group-hover:text-primary transition-colors">
                          {request.requesterName}
                        </h3>
                        {user && request.userId === user.id && (
                          <Badge variant="secondary" className="flex items-center gap-1 text-xs">
                            <User className="w-3 h-3" />
                            Your request
                          </Badge>
                        )}
                      </div>
                      <p className="text-sm text-muted-foreground font-mono">
                        {formatRelativeTime(request.createdAt)}
                      </p>
                    </div>
                    <StatusBadge status={request.currentStatus} />
                  </div>

                  <div className="space-y-3">
                    {request.filamentName && (
                      <div className="flex items-center gap-2 text-sm">
                        <div className="w-1.5 h-1.5 rounded-full bg-primary" />
                        <span className="text-muted-foreground">
                          Filament: <span className="font-medium text-foreground">{request.filamentName}</span>
                        </span>
                      </div>
                    )}
                    {request.requestDelivery && (
                      <div className="flex items-center gap-2 text-sm text-muted-foreground">
                        <Truck className="w-4 h-4 text-primary" />
                        <span>Delivery requested</span>
                      </div>
                    )}
                    {request.notes && (
                      <p className="text-sm text-muted-foreground line-clamp-2 leading-relaxed">
                        {request.notes}
                      </p>
                    )}

                    <div className="flex items-center text-muted-foreground text-xs pt-2 mt-2 border-t border-border/50">
                      <ExternalLink className="w-3.5 h-3.5 mr-1.5 flex-shrink-0" />
                      <span className="truncate font-mono">{request.modelUrl}</span>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          </Link>
        ))}
      </div>
    );
  };

  return (
    <div className="space-y-6">
      <PageHeader
        title="Print Requests"
        description="Browse and track all 3D printing requests"
        breadcrumbs={[
          { label: 'Home', href: '/' },
          { label: 'Requests' }
        ]}
        actions={
          <Link to="/requests/new">
            <Button className="transition-all hover:scale-105 shadow-md">
              <Package className="w-4 h-4 mr-2" />
              New Request
            </Button>
          </Link>
        }
      />

      {requests.length === 0 ? (
        <div className="text-center py-16 card-enhanced">
          <div className="w-16 h-16 mx-auto mb-4 bg-primary/10 rounded-full flex items-center justify-center">
            <Package className="w-8 h-8 text-primary" />
          </div>
          <h3 className="text-xl font-heading font-semibold mb-2">No requests yet</h3>
          <p className="text-muted-foreground mb-6 max-w-md mx-auto">
            Be the first to submit a 3D printing request and bring your ideas to life!
          </p>
          <Link to="/requests/new">
            <Button size="lg">
              <Package className="w-5 h-5 mr-2" />
              Submit Your First Request
            </Button>
          </Link>
        </div>
      ) : (
        <Tabs defaultValue="all" className="space-y-4">
          <TabsList className="grid w-full max-w-md grid-cols-3">
            <TabsTrigger value="all" className="gap-1.5">
              All <span className="text-xs opacity-70">({requests.length})</span>
            </TabsTrigger>
            <TabsTrigger value="pending" className="gap-1.5">
              Pending <span className="text-xs opacity-70">({pendingCount})</span>
            </TabsTrigger>
            {user && (
              <TabsTrigger value="mine" className="gap-1.5">
                Mine <span className="text-xs opacity-70">({myRequestsCount})</span>
              </TabsTrigger>
            )}
          </TabsList>

          <TabsContent value="all" className="mt-6">
            {renderRequestsList(getRequestsByStatus('all'))}
          </TabsContent>

          <TabsContent value="pending" className="mt-6">
            {renderRequestsList(getRequestsByStatus(RequestStatusEnum.Pending))}
          </TabsContent>

          {user && (
            <TabsContent value="mine" className="mt-6">
              {renderRequestsList(getRequestsByStatus('mine'))}
            </TabsContent>
          )}
        </Tabs>
      )}
    </div>
  );
};
