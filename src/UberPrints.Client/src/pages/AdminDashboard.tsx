import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { api } from '../lib/api';
import { PrintRequestDto, RequestStatusEnum } from '../types/api';
import { useToast } from '../hooks/use-toast';
import { Button } from '../components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '../components/ui/card';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '../components/ui/tabs';
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from '../components/ui/dialog';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '../components/ui/select';
import { Textarea } from '../components/ui/textarea';
import { Label } from '../components/ui/label';
import { Badge } from '../components/ui/badge';
import { LoadingSpinner } from '../components/ui/loading-spinner';
import { getStatusLabel, getStatusColor, formatRelativeTime } from '../lib/utils';
import { Shield, Package, Loader2, ExternalLink, Edit2 } from 'lucide-react';

// Define all status values explicitly for type safety
const ALL_STATUS_VALUES: RequestStatusEnum[] = [
  RequestStatusEnum.Pending,
  RequestStatusEnum.Accepted,
  RequestStatusEnum.Rejected,
  RequestStatusEnum.OnHold,
  RequestStatusEnum.Paused,
  RequestStatusEnum.WaitingForMaterials,
  RequestStatusEnum.Delivering,
  RequestStatusEnum.WaitingForPickup,
  RequestStatusEnum.Completed,
];

export const AdminDashboard = () => {
  const { toast } = useToast();
  const [requests, setRequests] = useState<PrintRequestDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  // Status change dialog
  const [selectedRequest, setSelectedRequest] = useState<PrintRequestDto | null>(null);
  const [newStatus, setNewStatus] = useState<RequestStatusEnum | null>(null);
  const [adminNotes, setAdminNotes] = useState('');
  const [updating, setUpdating] = useState(false);

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

  const handleStatusChange = async () => {
    if (!selectedRequest || newStatus === null) return;

    try {
      setUpdating(true);
      await api.changeRequestStatus(selectedRequest.id, {
        status: newStatus,
        adminNotes: adminNotes || undefined,
      });

      // Reload requests
      await loadRequests();

      toast({
        title: "Status updated",
        description: "Request status has been updated successfully.",
        variant: "success",
      });

      // Close dialog and reset
      setSelectedRequest(null);
      setNewStatus(null);
      setAdminNotes('');
    } catch (err: any) {
      console.error('Error updating status:', err);
      toast({
        title: "Failed to update status",
        description: err.response?.data?.message || 'Failed to update status',
        variant: "destructive",
      });
    } finally {
      setUpdating(false);
    }
  };

  const openStatusDialog = (request: PrintRequestDto) => {
    setSelectedRequest(request);
    setNewStatus(request.currentStatus);
    setAdminNotes('');
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
    return <LoadingSpinner message="Loading admin panel..." />;
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
          Manage all print requests and system settings
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
          <TabsTrigger value="filaments">Filaments</TabsTrigger>
        </TabsList>

        <TabsContent value="all" className="space-y-4">
          <RequestsTable
            requests={requests}
            onStatusChange={openStatusDialog}
            error={error}
            onRetry={loadRequests}
          />
        </TabsContent>

        <TabsContent value="pending" className="space-y-4">
          <RequestsTable
            requests={getRequestsByStatus(RequestStatusEnum.Pending)}
            onStatusChange={openStatusDialog}
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
            onStatusChange={openStatusDialog}
            error={error}
            onRetry={loadRequests}
          />
        </TabsContent>

        <TabsContent value="completed" className="space-y-4">
          <RequestsTable
            requests={getRequestsByStatus(RequestStatusEnum.Completed)}
            onStatusChange={openStatusDialog}
            error={error}
            onRetry={loadRequests}
          />
        </TabsContent>

        <TabsContent value="filaments" className="space-y-4">
          <Card>
            <CardHeader>
              <CardTitle>Filament Management</CardTitle>
              <CardDescription>Manage filament inventory (Coming Soon)</CardDescription>
            </CardHeader>
            <CardContent>
              <p className="text-muted-foreground text-center py-8">
                Filament management interface will be available soon
              </p>
            </CardContent>
          </Card>
        </TabsContent>
      </Tabs>

      {/* Status Change Dialog */}
      <Dialog open={selectedRequest !== null} onOpenChange={(open) => !open && setSelectedRequest(null)}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Change Request Status</DialogTitle>
            <DialogDescription>
              Update the status for request by {selectedRequest?.requesterName}
            </DialogDescription>
          </DialogHeader>

          <div className="space-y-4 py-4">
            <div className="space-y-2">
              <Label>New Status</Label>
              <Select
                value={newStatus?.toString()}
                onValueChange={(value) => setNewStatus(parseInt(value) as RequestStatusEnum)}
              >
                <SelectTrigger>
                  <SelectValue placeholder="Select status" />
                </SelectTrigger>
                <SelectContent>
                  {ALL_STATUS_VALUES.map((status) => (
                    <SelectItem key={status} value={status.toString()}>
                      {getStatusLabel(status)}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>

            <div className="space-y-2">
              <Label>Admin Notes (Optional)</Label>
              <Textarea
                value={adminNotes}
                onChange={(e) => setAdminNotes(e.target.value)}
                placeholder="Add any notes for the user..."
                rows={3}
              />
            </div>
          </div>

          <DialogFooter>
            <Button
              variant="outline"
              onClick={() => setSelectedRequest(null)}
              disabled={updating}
            >
              Cancel
            </Button>
            <Button onClick={handleStatusChange} disabled={updating || newStatus === null}>
              {updating && <Loader2 className="w-4 h-4 mr-2 animate-spin" />}
              Update Status
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
};

interface RequestsTableProps {
  requests: PrintRequestDto[];
  onStatusChange: (request: PrintRequestDto) => void;
  error: string | null;
  onRetry: () => void;
}

const RequestsTable = ({ requests, onStatusChange, error, onRetry }: RequestsTableProps) => {
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
              <Button
                variant="outline"
                size="sm"
                onClick={() => onStatusChange(request)}
              >
                <Edit2 className="w-4 h-4 mr-2" />
                Change Status
              </Button>
            </div>

            <div className="space-y-2 text-sm">
              <div className="flex items-center text-muted-foreground">
                <ExternalLink className="w-4 h-4 mr-2 flex-shrink-0" />
                <a
                  href={request.modelUrl}
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
