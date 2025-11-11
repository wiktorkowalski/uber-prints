import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { api } from '../lib/api';
import { PrintRequestDto, RequestStatusEnum } from '../types/api';
import { useAuth } from '../contexts/AuthContext';
import { Button } from '../components/ui/button';
import { Badge } from '../components/ui/badge';
import { Skeleton } from '../components/ui/skeleton';
import { HoverCard, HoverCardContent, HoverCardTrigger } from '../components/ui/hover-card';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '../components/ui/tabs';
import { getStatusLabel, getStatusColor, formatRelativeTime, sanitizeUrl } from '../lib/utils';
import { ExternalLink, Package, User } from 'lucide-react';
import { ModelThumbnail } from '../components/ModelThumbnail';

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
        <div className="flex justify-between items-center">
          <Skeleton className="h-10 w-64" />
          <Skeleton className="h-10 w-32" />
        </div>
        <div className="grid gap-4">
          {[1, 2, 3].map((i) => (
            <div key={i} className="border rounded-lg p-6 space-y-4">
              <div className="flex justify-between items-start">
                <div className="space-y-2 flex-1">
                  <Skeleton className="h-6 w-48" />
                  <Skeleton className="h-4 w-32" />
                </div>
                <Skeleton className="h-6 w-20" />
              </div>
              <div className="space-y-2">
                <Skeleton className="h-4 w-full" />
                <Skeleton className="h-4 w-3/4" />
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
        {filteredRequests.map((request) => (
          <div
            key={request.id}
            className="border rounded-lg p-6 hover:border-primary transition-colors"
          >
            <div className="flex gap-6">
              {/* Model Thumbnail */}
              <div className="flex-shrink-0">
                <ModelThumbnail modelUrl={request.modelUrl} size={128} />
              </div>

              {/* Request Content */}
              <div className="flex-1 min-w-0">
                <div className="flex justify-between items-start mb-4">
                  <div className="flex-1">
                    <div className="flex items-center gap-2 mb-1">
                      <HoverCard>
                        <HoverCardTrigger asChild>
                          <Link to={`/requests/${request.id}`}>
                            <h3 className="text-xl font-semibold hover:text-primary transition-colors">
                              {request.requesterName}
                            </h3>
                          </Link>
                        </HoverCardTrigger>
                        <HoverCardContent className="w-80">
                          <div className="space-y-2">
                            <h4 className="text-sm font-semibold">Request Details</h4>
                            <div className="text-sm space-y-1">
                              <p><span className="text-muted-foreground">ID:</span> {request.id.slice(0, 8)}...</p>
                              <p><span className="text-muted-foreground">Requester:</span> {request.requesterName}</p>
                              {request.filamentName && (
                                <p><span className="text-muted-foreground">Filament:</span> {request.filamentName}</p>
                              )}
                              <p><span className="text-muted-foreground">Delivery:</span> {request.requestDelivery ? 'Yes' : 'No'}</p>
                              {request.notes && (
                                <p className="pt-1"><span className="text-muted-foreground">Notes:</span><br/>{request.notes.slice(0, 100)}{request.notes.length > 100 ? '...' : ''}</p>
                              )}
                            </div>
                          </div>
                        </HoverCardContent>
                      </HoverCard>
                      {user && request.userId === user.id && (
                        <Badge variant="secondary" className="flex items-center gap-1">
                          <User className="w-3 h-3" />
                          Your request
                        </Badge>
                      )}
                    </div>
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
                  <Link to={`/requests/${request.id}`} className="block">
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
                      <p className="text-muted-foreground line-clamp-2 mt-2">
                        {request.notes}
                      </p>
                    )}
                  </Link>

                  <div className="flex items-center text-muted-foreground text-sm mt-3 pt-3 border-t">
                    <ExternalLink className="w-4 h-4 mr-2" />
                    <a
                      href={sanitizeUrl(request.modelUrl)}
                      target="_blank"
                      rel="noopener noreferrer"
                      className="hover:text-primary truncate"
                      onClick={(e) => e.stopPropagation()}
                    >
                      {request.modelUrl}
                    </a>
                  </div>
                </div>
              </div>
            </div>
          </div>
        ))}
      </div>
    );
  };

  return (
    <div className="space-y-6">
      <div className="flex justify-between items-center">
        <h1 className="text-3xl font-bold">All Print Requests</h1>
        <Link to="/requests/new">
          <Button>
            <Package className="w-4 h-4 mr-2" />
            New Request
          </Button>
        </Link>
      </div>

      {requests.length === 0 ? (
        <div className="text-center py-12 border rounded-lg">
          <Package className="w-12 h-12 mx-auto mb-4 text-muted-foreground" />
          <h3 className="text-lg font-semibold mb-2">No requests yet</h3>
          <p className="text-muted-foreground mb-4">
            Be the first to submit a 3D printing request!
          </p>
          <Link to="/requests/new">
            <Button>Submit Request</Button>
          </Link>
        </div>
      ) : (
        <Tabs defaultValue="all" className="space-y-4">
          <TabsList>
            <TabsTrigger value="all">All ({requests.length})</TabsTrigger>
            <TabsTrigger value="pending">Pending ({pendingCount})</TabsTrigger>
            {user && <TabsTrigger value="mine">My Requests ({myRequestsCount})</TabsTrigger>}
          </TabsList>

          <TabsContent value="all">
            {renderRequestsList(getRequestsByStatus('all'))}
          </TabsContent>

          <TabsContent value="pending">
            {renderRequestsList(getRequestsByStatus(RequestStatusEnum.Pending))}
          </TabsContent>

          {user && (
            <TabsContent value="mine">
              {renderRequestsList(getRequestsByStatus('mine'))}
            </TabsContent>
          )}
        </Tabs>
      )}
    </div>
  );
};
