import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { api } from '../lib/api';
import { PrintRequestDto, RequestStatusEnum } from '../types/api';
import { useToast } from '../hooks/use-toast';
import { Button } from '../components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '../components/ui/card';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '../components/ui/tabs';
import { Badge } from '../components/ui/badge';
import { Skeleton } from '../components/ui/skeleton';
import { getStatusLabel, getStatusColor, formatRelativeTime, sanitizeUrl } from '../lib/utils';
import { Package, Loader2, ExternalLink, Edit2, Shield } from 'lucide-react';
import { EditRequestDialog } from '../components/admin/EditRequestDialog';
import { ChangeStatusDialog } from '../components/admin/ChangeStatusDialog';

export const AdminRequests = () => {
  const { toast } = useToast();
  const [requests, setRequests] = useState<PrintRequestDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  // Request dialogs
  const [statusDialogRequest, setStatusDialogRequest] = useState<PrintRequestDto | null>(null);
  const [editDialogRequest, setEditDialogRequest] = useState<PrintRequestDto | null>(null);

  useEffect(() => {
    loadRequests();
  }, []);

  const loadRequests = async () => {
    try {
      setLoading(true);
      const data = await api.getAdminRequests();
      setRequests(data);
    } catch (err) {
      console.error('Error loading requests:', err);
      setError('Failed to load requests');
    } finally {
      setLoading(false);
    }
  };

  const handleDialogSuccess = async () => {
    await loadRequests();
  };

  const getRequestsByStatus = (status: RequestStatusEnum) => {
    return requests.filter(r => r.currentStatus === status);
  };

  const pendingCount = getRequestsByStatus(RequestStatusEnum.Pending).length;
  const activeCount = requests.filter(r =>
    [RequestStatusEnum.Accepted, RequestStatusEnum.Paused, RequestStatusEnum.OnHold,
    RequestStatusEnum.WaitingForMaterials, RequestStatusEnum.Delivering,
    RequestStatusEnum.WaitingForPickup].includes(r.currentStatus)
  ).length;
  const completedCount = getRequestsByStatus(RequestStatusEnum.Completed).length;

  if (loading) {
    return (
      <div className="space-y-6">
        <div>
          <Skeleton className="h-10 w-64" />
          <Skeleton className="h-4 w-96 mt-1" />
        </div>
        <div className="grid md:grid-cols-4 gap-4">
          {[1, 2, 3, 4].map((i) => (
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
                <div key={i} className="border rounded-lg p-6 space-y-4">
                  <div className="flex justify-between items-start">
                    <div className="space-y-2 flex-1">
                      <Skeleton className="h-6 w-48" />
                      <Skeleton className="h-4 w-32" />
                    </div>
                    <div className="flex gap-2">
                      <Skeleton className="h-9 w-24" />
                      <Skeleton className="h-9 w-32" />
                    </div>
                  </div>
                  <Skeleton className="h-4 w-full" />
                  <Skeleton className="h-4 w-3/4" />
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
          Manage Print Requests
        </h1>
        <p className="text-muted-foreground mt-1">
          View and manage all print requests
        </p>
      </div>

      {/* Stats Cards */}
      <div className="grid md:grid-cols-4 gap-4">
        <Card>
          <CardHeader className="pb-3">
            <CardDescription>Total Requests</CardDescription>
            <CardTitle className="text-3xl">{requests.length}</CardTitle>
          </CardHeader>
        </Card>
        <Card>
          <CardHeader className="pb-3">
            <CardDescription>Pending</CardDescription>
            <CardTitle className="text-3xl">{pendingCount}</CardTitle>
          </CardHeader>
        </Card>
        <Card>
          <CardHeader className="pb-3">
            <CardDescription>Active</CardDescription>
            <CardTitle className="text-3xl">{activeCount}</CardTitle>
          </CardHeader>
        </Card>
        <Card>
          <CardHeader className="pb-3">
            <CardDescription>Completed</CardDescription>
            <CardTitle className="text-3xl">{completedCount}</CardTitle>
          </CardHeader>
        </Card>
      </div>

      {/* Tabs */}
      <Tabs defaultValue="all" className="space-y-4">
        <TabsList>
          <TabsTrigger value="all">All Requests</TabsTrigger>
          <TabsTrigger value="pending">Pending ({pendingCount})</TabsTrigger>
          <TabsTrigger value="active">Active ({activeCount})</TabsTrigger>
          <TabsTrigger value="completed">Completed</TabsTrigger>
        </TabsList>

        <TabsContent value="all" className="space-y-4">
          <RequestsTable
            requests={requests}
            onStatusChange={setStatusDialogRequest}
            onEdit={setEditDialogRequest}
            error={error}
            onRetry={loadRequests}
          />
        </TabsContent>

        <TabsContent value="pending" className="space-y-4">
          <RequestsTable
            requests={getRequestsByStatus(RequestStatusEnum.Pending)}
            onStatusChange={setStatusDialogRequest}
            onEdit={setEditDialogRequest}
            error={error}
            onRetry={loadRequests}
          />
        </TabsContent>

        <TabsContent value="active" className="space-y-4">
          <RequestsTable
            requests={requests.filter(r =>
              [RequestStatusEnum.Accepted, RequestStatusEnum.Paused, RequestStatusEnum.OnHold,
              RequestStatusEnum.WaitingForMaterials, RequestStatusEnum.Delivering,
              RequestStatusEnum.WaitingForPickup].includes(r.currentStatus)
            )}
            onStatusChange={setStatusDialogRequest}
            onEdit={setEditDialogRequest}
            error={error}
            onRetry={loadRequests}
          />
        </TabsContent>

        <TabsContent value="completed" className="space-y-4">
          <RequestsTable
            requests={getRequestsByStatus(RequestStatusEnum.Completed)}
            onStatusChange={setStatusDialogRequest}
            onEdit={setEditDialogRequest}
            error={error}
            onRetry={loadRequests}
          />
        </TabsContent>
      </Tabs>

      {/* Request Management Dialogs */}
      <ChangeStatusDialog
        request={statusDialogRequest}
        open={statusDialogRequest !== null}
        onOpenChange={(open) => !open && setStatusDialogRequest(null)}
        onSuccess={handleDialogSuccess}
      />

      <EditRequestDialog
        request={editDialogRequest}
        open={editDialogRequest !== null}
        onOpenChange={(open) => !open && setEditDialogRequest(null)}
        onSuccess={handleDialogSuccess}
      />
    </div>
  );
};

interface RequestsTableProps {
  requests: PrintRequestDto[];
  onStatusChange: (request: PrintRequestDto) => void;
  onEdit: (request: PrintRequestDto) => void;
  error: string | null;
  onRetry: () => void;
}

const RequestsTable = ({ requests, onStatusChange, onEdit, error, onRetry }: RequestsTableProps) => {
  if (error) {
    return (
      <Card>
        <CardContent className="pt-6">
          <div className="text-center py-8">
            <p className="text-red-600 mb-4">{error}</p>
            <Button onClick={onRetry}>Try Again</Button>
          </div>
        </CardContent>
      </Card>
    );
  }

  if (requests.length === 0) {
    return (
      <Card>
        <CardContent className="pt-6">
          <div className="text-center py-12">
            <Package className="w-12 h-12 mx-auto mb-4 text-muted-foreground" />
            <h3 className="text-lg font-semibold mb-2">No requests found</h3>
            <p className="text-muted-foreground">
              No requests match this filter
            </p>
          </div>
        </CardContent>
      </Card>
    );
  }

  return (
    <div className="space-y-4">
      {requests.map((request) => (
        <Card key={request.id}>
          <CardContent className="pt-6">
            <div className="flex justify-between items-start mb-4">
              <div className="flex-1">
                <div className="flex items-center gap-3 mb-2">
                  <Link
                    to={`/request/${request.id}`}
                    className="text-lg font-semibold hover:text-primary"
                  >
                    {request.requesterName}
                  </Link>
                  <Badge className={getStatusColor(request.currentStatus)}>
                    {getStatusLabel(request.currentStatus)}
                  </Badge>
                </div>
                <p className="text-sm text-muted-foreground">
                  {formatRelativeTime(request.createdAt)}
                </p>
              </div>
              <div className="flex gap-2">
                <Button
                  variant="outline"
                  size="sm"
                  onClick={() => onEdit(request)}
                >
                  <Edit2 className="w-4 h-4 mr-2" />
                  Edit
                </Button>
                <Button
                  variant="outline"
                  size="sm"
                  onClick={() => onStatusChange(request)}
                >
                  Change Status
                </Button>
              </div>
            </div>

            <div className="space-y-2 text-sm">
              <div className="flex items-center text-muted-foreground">
                <ExternalLink className="w-4 h-4 mr-2 flex-shrink-0" />
                <a
                  href={sanitizeUrl(request.modelUrl)}
                  target="_blank"
                  rel="noopener noreferrer"
                  className="hover:text-primary truncate"
                >
                  {request.modelUrl}
                </a>
              </div>
              <div className="text-muted-foreground">
                Filament: <span className="font-medium">{request.filamentName || 'Not specified'}</span>
              </div>
              {request.requestDelivery && (
                <div className="text-muted-foreground">🚚 Delivery requested</div>
              )}
              {request.notes && (
                <p className="text-muted-foreground mt-2 p-3 bg-muted rounded-lg">
                  {request.notes}
                </p>
              )}
            </div>
          </CardContent>
        </Card>
      ))}
    </div>
  );
};
