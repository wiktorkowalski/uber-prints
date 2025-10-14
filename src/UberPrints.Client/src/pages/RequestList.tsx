import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { api } from '../lib/api';
import { PrintRequestDto } from '../types/api';
import { Button } from '../components/ui/button';
import { LoadingSpinner } from '../components/ui/loading-spinner';
import { getStatusLabel, getStatusColor, formatRelativeTime } from '../lib/utils';
import { ExternalLink, Package } from 'lucide-react';

export const RequestList = () => {
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

  if (loading) {
    return <LoadingSpinner message="Loading requests..." />;
  }

  if (error) {
    return (
      <div className="text-center py-12">
        <p className="text-red-600 mb-4">{error}</p>
        <Button onClick={loadRequests}>Try Again</Button>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div className="flex justify-between items-center">
        <h1 className="text-3xl font-bold">All Print Requests</h1>
        <Link to="/request/new">
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
          <Link to="/request/new">
            <Button>Submit Request</Button>
          </Link>
        </div>
      ) : (
        <div className="grid gap-4">
          {requests.map((request) => (
            <div
              key={request.id}
              className="border rounded-lg p-6 hover:border-primary transition-colors"
            >
              <Link to={`/request/${request.id}`} className="block">
                <div className="flex justify-between items-start mb-4">
                  <div>
                    <h3 className="text-xl font-semibold mb-1">
                      {request.requesterName}
                    </h3>
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
                </div>
              </Link>

              <div className="flex items-center text-muted-foreground text-sm mt-3 pt-3 border-t">
                <ExternalLink className="w-4 h-4 mr-2" />
                <a
                  href={request.modelUrl}
                  target="_blank"
                  rel="noopener noreferrer"
                  className="hover:text-primary truncate"
                >
                  {request.modelUrl}
                </a>
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
};
