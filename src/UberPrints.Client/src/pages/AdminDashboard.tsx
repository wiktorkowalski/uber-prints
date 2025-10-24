import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { api } from '../lib/api';
import { PrintRequestDto, RequestStatusEnum, FilamentDto, CreateFilamentDto, UpdateFilamentDto, FilamentRequestDto, FilamentRequestStatusEnum, ChangeFilamentRequestStatusDto } from '../types/api';
import { useToast } from '../hooks/use-toast';
import { Button } from '../components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '../components/ui/card';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '../components/ui/tabs';
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from '../components/ui/dialog';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '../components/ui/select';
import { Textarea } from '../components/ui/textarea';
import { Label } from '../components/ui/label';
import { Input } from '../components/ui/input';
import { Badge } from '../components/ui/badge';
import { Skeleton } from '../components/ui/skeleton';
import { Progress } from '../components/ui/progress';
import { Collapsible, CollapsibleContent, CollapsibleTrigger } from '../components/ui/collapsible';
import { ChevronDown } from 'lucide-react';
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from '../components/ui/alert-dialog';
import { getStatusLabel, getStatusColor, formatRelativeTime, sanitizeUrl } from '../lib/utils';
import { Shield, Package, Loader2, ExternalLink, Edit2, Plus, Trash2, AlertCircle, CheckCircle2, Camera, Users } from 'lucide-react';
import { EditRequestDialog } from '../components/admin/EditRequestDialog';
import { ChangeStatusDialog } from '../components/admin/ChangeStatusDialog';

const ALL_FILAMENT_REQUEST_STATUS_VALUES: FilamentRequestStatusEnum[] = [
  FilamentRequestStatusEnum.Pending,
  FilamentRequestStatusEnum.Approved,
  FilamentRequestStatusEnum.Rejected,
  FilamentRequestStatusEnum.Ordered,
  FilamentRequestStatusEnum.Received,
];

const getFilamentRequestStatusLabel = (status: FilamentRequestStatusEnum) => {
  return FilamentRequestStatusEnum[status];
};

const getFilamentRequestStatusColor = (status: FilamentRequestStatusEnum) => {
  switch (status) {
    case FilamentRequestStatusEnum.Pending:
      return 'bg-yellow-500';
    case FilamentRequestStatusEnum.Approved:
      return 'bg-green-500';
    case FilamentRequestStatusEnum.Rejected:
      return 'bg-red-500';
    case FilamentRequestStatusEnum.Ordered:
      return 'bg-blue-500';
    case FilamentRequestStatusEnum.Received:
      return 'bg-purple-500';
    default:
      return 'bg-gray-500';
  }
};

export const AdminDashboard = () => {
  const { toast } = useToast();
  const [requests, setRequests] = useState<PrintRequestDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  // Request dialogs
  const [statusDialogRequest, setStatusDialogRequest] = useState<PrintRequestDto | null>(null);
  const [editDialogRequest, setEditDialogRequest] = useState<PrintRequestDto | null>(null);

  // Filament management
  const [filaments, setFilaments] = useState<FilamentDto[]>([]);
  const [filamentsLoading, setFilamentsLoading] = useState(false);
  const [filamentsLoaded, setFilamentsLoaded] = useState(false);
  const [filamentDialogOpen, setFilamentDialogOpen] = useState(false);
  const [editingFilament, setEditingFilament] = useState<FilamentDto | null>(null);
  const [filamentFormData, setFilamentFormData] = useState<CreateFilamentDto>({
    name: '',
    material: '',
    brand: '',
    colour: '',
    stockAmount: 0,
    stockUnit: 'g',
    link: '',
    photoUrl: '',
    isAvailable: true,
  });
  const [filamentSubmitting, setFilamentSubmitting] = useState(false);

  // Filament request management
  const [filamentRequests, setFilamentRequests] = useState<FilamentRequestDto[]>([]);
  const [filamentRequestsLoading, setFilamentRequestsLoading] = useState(false);
  const [filamentRequestsLoaded, setFilamentRequestsLoaded] = useState(false);
  const [selectedFilamentRequest, setSelectedFilamentRequest] = useState<FilamentRequestDto | null>(null);
  const [newFilamentRequestStatus, setNewFilamentRequestStatus] = useState<FilamentRequestStatusEnum | null>(null);
  const [filamentRequestReason, setFilamentRequestReason] = useState('');
  const [selectedFilamentForRequest, setSelectedFilamentForRequest] = useState<string>('');
  const [updatingFilamentRequest, setUpdatingFilamentRequest] = useState(false);

  // Stream stats
  const [streamStats, setStreamStats] = useState<{
    isEnabled: boolean;
    isActive: boolean;
    activeViewers: number;
  } | null>(null);
  const [deleteFilamentDialogOpen, setDeleteFilamentDialogOpen] = useState(false);
  const [filamentToDelete, setFilamentToDelete] = useState<FilamentDto | null>(null);
  const [deletingFilament, setDeletingFilament] = useState(false);
  const [creatingFilamentForRequest, setCreatingFilamentForRequest] = useState<FilamentRequestDto | null>(null);

  useEffect(() => {
    loadRequests();
    loadStreamStats();
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

  const loadStreamStats = async () => {
    try {
      const data = await api.getStreamStats();
      setStreamStats(data);
    } catch (err) {
      console.error('Error loading stream stats:', err);
      // Don't show error toast for stream stats, it's not critical
    }
  };

  const handleDialogSuccess = async () => {
    await loadRequests();
  };

  const getRequestsByStatus = (status: RequestStatusEnum) => {
    return requests.filter(r => r.currentStatus === status);
  };

  // Filament management functions
  const loadFilaments = async () => {
    if (filamentsLoaded) return; // Prevent duplicate loads

    try {
      setFilamentsLoading(true);
      const data = await api.getFilaments();
      setFilaments(data);
      setFilamentsLoaded(true);
    } catch (err) {
      toast({
        title: "Failed to load filaments",
        description: "Could not load filament inventory",
        variant: "destructive",
      });
    } finally {
      setFilamentsLoading(false);
    }
  };

  const openCreateFilamentDialog = () => {
    setEditingFilament(null);
    setCreatingFilamentForRequest(null); // Clear any request tracking
    setFilamentFormData({
      name: '',
      material: '',
      brand: '',
      colour: '',
      stockAmount: 0,
      stockUnit: 'g',
      link: '',
      photoUrl: '',
      isAvailable: true,
    });
    setFilamentDialogOpen(true);
  };

  const openEditFilamentDialog = (filament: FilamentDto) => {
    setEditingFilament(filament);
    setCreatingFilamentForRequest(null); // Clear any request tracking
    setFilamentFormData({
      name: filament.name,
      material: filament.material,
      brand: filament.brand,
      colour: filament.colour,
      stockAmount: filament.stockAmount,
      stockUnit: filament.stockUnit,
      link: filament.link || '',
      photoUrl: filament.photoUrl || '',
      isAvailable: filament.isAvailable,
    });
    setFilamentDialogOpen(true);
  };

  const handleFilamentSubmit = async () => {
    try {
      setFilamentSubmitting(true);
      if (editingFilament) {
        await api.updateFilament(editingFilament.id, filamentFormData as UpdateFilamentDto);
        toast({
          title: "Filament updated",
          description: "Filament has been updated successfully",
          variant: "success",
        });
      } else {
        const newFilament = await api.createFilament(filamentFormData);
        toast({
          title: "Filament created",
          description: "New filament has been added to inventory",
          variant: "success",
        });

        // If this filament was created from a filament request, automatically link and approve it
        if (creatingFilamentForRequest) {
          try {
            const statusData: ChangeFilamentRequestStatusDto = {
              status: FilamentRequestStatusEnum.Approved,
              filamentId: newFilament.id,
              reason: 'Filament added to inventory',
            };
            await api.changeFilamentRequestStatus(creatingFilamentForRequest.id, statusData);

            toast({
              title: "Filament request approved",
              description: "The filament request has been linked to the new filament and approved.",
              variant: "success",
            });

            // Reload filament requests
            setFilamentRequestsLoaded(false);
            await loadFilamentRequests();

            // Clear the tracking state
            setCreatingFilamentForRequest(null);
          } catch (err: any) {
            console.error('Error auto-approving filament request:', err);
            toast({
              title: "Filament created but not linked",
              description: "The filament was created but could not be automatically linked to the request.",
              variant: "destructive",
            });
          }
        }
      }
      setFilamentDialogOpen(false);
      await loadFilaments();
    } catch (err: any) {
      console.error('Error saving filament:', err);
      toast({
        title: "Failed to save filament",
        description: err.response?.data?.message || 'Could not save filament',
        variant: "destructive",
      });
    } finally {
      setFilamentSubmitting(false);
    }
  };

  // Filament request management functions
  const loadFilamentRequests = async () => {
    if (filamentRequestsLoaded) return;

    try {
      setFilamentRequestsLoading(true);
      const data = await api.getAdminFilamentRequests();
      setFilamentRequests(data);
      setFilamentRequestsLoaded(true);
    } catch (err) {
      toast({
        title: "Failed to load filament requests",
        description: "Could not load filament requests",
        variant: "destructive",
      });
    } finally {
      setFilamentRequestsLoading(false);
    }
  };

  const openFilamentRequestStatusDialog = async (request: FilamentRequestDto) => {
    setSelectedFilamentRequest(request);
    setNewFilamentRequestStatus(request.currentStatus);
    setFilamentRequestReason('');
    setSelectedFilamentForRequest(request.filamentId || '');
    // Load filaments if not already loaded (needed for linking)
    if (!filamentsLoaded && !filamentsLoading) {
      await loadFilaments();
    }
  };

  const createFilamentFromRequest = (request: FilamentRequestDto) => {
    // Pre-fill filament form with request data
    const filamentName = `${request.brand} ${request.material} ${request.colour}`;
    setFilamentFormData({
      name: filamentName,
      material: request.material,
      brand: request.brand,
      colour: request.colour,
      stockAmount: 0,
      stockUnit: 'g',
      link: request.link || '',
      photoUrl: '',
      isAvailable: true,
    });
    setEditingFilament(null);
    setCreatingFilamentForRequest(request); // Track that we're creating from a request
    setFilamentDialogOpen(true);
    // Close the status dialog
    setSelectedFilamentRequest(null);
  };

  const handleFilamentRequestStatusChange = async () => {
    if (!selectedFilamentRequest || newFilamentRequestStatus === null) return;

    try {
      setUpdatingFilamentRequest(true);
      const statusData: ChangeFilamentRequestStatusDto = {
        status: newFilamentRequestStatus,
        reason: filamentRequestReason || undefined,
      };

      // If approving and a filament is selected, include it
      if (newFilamentRequestStatus === FilamentRequestStatusEnum.Approved && selectedFilamentForRequest) {
        statusData.filamentId = selectedFilamentForRequest;
      }

      await api.changeFilamentRequestStatus(selectedFilamentRequest.id, statusData);

      // Reload filament requests
      setFilamentRequestsLoaded(false);
      await loadFilamentRequests();

      toast({
        title: "Status updated",
        description: "Filament request status has been updated successfully.",
        variant: "success",
      });

      // Close dialog and reset
      setSelectedFilamentRequest(null);
      setNewFilamentRequestStatus(null);
      setFilamentRequestReason('');
      setSelectedFilamentForRequest('');
    } catch (err: any) {
      console.error('Error updating filament request status:', err);
      toast({
        title: "Failed to update status",
        description: err.response?.data?.message || 'Failed to update status',
        variant: "destructive",
      });
    } finally {
      setUpdatingFilamentRequest(false);
    }
  };

  const handleDeleteFilament = async () => {
    if (!filamentToDelete) return;

    try {
      setDeletingFilament(true);
      await api.deleteFilament(filamentToDelete.id);
      toast({
        title: "Filament deleted",
        description: "Filament has been removed from inventory",
        variant: "success",
      });
      await loadFilaments();
    } catch (err: any) {
      console.error('Error deleting filament:', err);
      toast({
        title: "Failed to delete filament",
        description: err.response?.data?.message || 'Could not delete filament',
        variant: "destructive",
      });
    } finally {
      setDeletingFilament(false);
      setDeleteFilamentDialogOpen(false);
      setFilamentToDelete(null);
    }
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

      {/* Tabs */}
      <Tabs defaultValue="all" className="space-y-4" onValueChange={(value) => {
        if (value === 'filaments' && !filamentsLoaded && !filamentsLoading) {
          loadFilaments();
        }
        if (value === 'filament-requests' && !filamentRequestsLoaded && !filamentRequestsLoading) {
          loadFilamentRequests();
        }
      }}>
        <TabsList>
          <TabsTrigger value="all">All Requests</TabsTrigger>
          <TabsTrigger value="pending">Pending ({pendingCount})</TabsTrigger>
          <TabsTrigger value="active">Active ({activeCount})</TabsTrigger>
          <TabsTrigger value="completed">Completed</TabsTrigger>
          <TabsTrigger value="filaments">Filaments</TabsTrigger>
          <TabsTrigger value="filament-requests">Filament Requests</TabsTrigger>
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

        <TabsContent value="filaments" className="space-y-4">
          <Card>
            <CardHeader>
              <div className="flex items-center justify-between">
                <div>
                  <CardTitle>Filament Management</CardTitle>
                  <CardDescription>Manage filament inventory and stock levels</CardDescription>
                </div>
                <Button onClick={openCreateFilamentDialog}>
                  <Plus className="w-4 h-4 mr-2" />
                  Add Filament
                </Button>
              </div>
            </CardHeader>
            <CardContent>
              {filamentsLoading ? (
                <div className="space-y-4">
                  {[1, 2, 3].map((i) => (
                    <div key={i} className="border rounded-lg p-4 flex items-start gap-4">
                      <Skeleton className="w-20 h-20 rounded" />
                      <div className="flex-1 space-y-2">
                        <Skeleton className="h-6 w-48" />
                        <Skeleton className="h-4 w-64" />
                      </div>
                      <div className="flex gap-2">
                        <Skeleton className="h-9 w-20" />
                        <Skeleton className="h-9 w-9" />
                      </div>
                    </div>
                  ))}
                </div>
              ) : filaments.length === 0 ? (
                <div className="text-center py-12">
                  <Package className="w-12 h-12 mx-auto mb-4 text-muted-foreground" />
                  <h3 className="text-lg font-semibold mb-2">No filaments yet</h3>
                  <p className="text-muted-foreground mb-4">
                    Add filaments to your inventory to start accepting print requests
                  </p>
                  <Button onClick={openCreateFilamentDialog}>
                    <Plus className="w-4 h-4 mr-2" />
                    Add First Filament
                  </Button>
                </div>
              ) : (
                <div className="grid gap-4">
                  {filaments.map((filament) => (
                    <div
                      key={filament.id}
                      className="border rounded-lg p-4 flex items-start gap-4"
                    >
                      {filament.photoUrl && (
                        <img
                          src={filament.photoUrl}
                          alt={filament.name}
                          className="w-20 h-20 object-cover rounded"
                        />
                      )}
                      <div className="flex-1">
                        <div className="flex items-start justify-between mb-2">
                          <div className="flex-1">
                            <div className="flex items-center gap-2">
                              <h3 className="text-lg font-semibold">{filament.name}</h3>
                              {!filament.isAvailable && (
                                <Badge variant="secondary" className="text-xs">
                                  Hidden
                                </Badge>
                              )}
                            </div>
                            <p className="text-sm text-muted-foreground">
                              {filament.brand} • {filament.material} • {filament.colour}
                            </p>
                          </div>
                          <div className="min-w-[140px]">
                            {filament.stockAmount <= 0 ? (
                              <Badge variant="destructive" className="flex items-center gap-1">
                                <AlertCircle className="w-3 h-3" />
                                Out of Stock
                              </Badge>
                            ) : (
                              <div className="space-y-1">
                                <div className="flex justify-between text-xs">
                                  <span className="text-muted-foreground">Stock</span>
                                  <span className="font-medium">{filament.stockAmount} {filament.stockUnit}</span>
                                </div>
                                <Progress
                                  value={Math.min((filament.stockAmount / 1000) * 100, 100)}
                                  className="h-2"
                                />
                              </div>
                            )}
                          </div>
                        </div>
                        {filament.link && (
                          <a
                            href={filament.link}
                            target="_blank"
                            rel="noopener noreferrer"
                            className="text-sm text-primary hover:underline flex items-center gap-1"
                          >
                            <ExternalLink className="w-3 h-3" />
                            Product Link
                          </a>
                        )}
                      </div>
                      <div className="flex gap-2">
                        <Button
                          variant="outline"
                          size="sm"
                          onClick={() => openEditFilamentDialog(filament)}
                        >
                          <Edit2 className="w-4 h-4 mr-2" />
                          Edit
                        </Button>
                        <Button
                          variant="destructive"
                          size="sm"
                          onClick={() => {
                            setFilamentToDelete(filament);
                            setDeleteFilamentDialogOpen(true);
                          }}
                        >
                          <Trash2 className="w-4 h-4" />
                        </Button>
                      </div>
                    </div>
                  ))}
                </div>
              )}
            </CardContent>
          </Card>
        </TabsContent>

        <TabsContent value="filament-requests" className="space-y-4">
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center gap-2">
                <Package className="w-5 h-5" />
                Filament Requests
              </CardTitle>
              <CardDescription>
                Review and manage user filament requests
              </CardDescription>
            </CardHeader>
            <CardContent>
              {filamentRequestsLoading ? (
                <div className="space-y-4">
                  {[1, 2].map((i) => (
                    <div key={i} className="border rounded-lg p-4 space-y-3">
                      <div className="flex justify-between items-start">
                        <div className="space-y-2 flex-1">
                          <Skeleton className="h-6 w-64" />
                          <Skeleton className="h-4 w-48" />
                        </div>
                        <Skeleton className="h-6 w-20" />
                      </div>
                      <Skeleton className="h-4 w-full" />
                    </div>
                  ))}
                </div>
              ) : filamentRequests.length === 0 ? (
                <div className="text-center py-8 text-muted-foreground">
                  No filament requests yet
                </div>
              ) : (
                <div className="space-y-4">
                  {filamentRequests.map((request) => (
                    <div
                      key={request.id}
                      className="border rounded-lg p-4 flex items-start justify-between gap-4"
                    >
                      <div className="flex-1">
                        <div className="flex items-start justify-between mb-2">
                          <div>
                            <h3 className="text-lg font-semibold">
                              {request.brand} - {request.material} ({request.colour})
                            </h3>
                            <p className="text-sm text-muted-foreground">
                              Requested by {request.requesterName} • {formatRelativeTime(request.createdAt)}
                            </p>
                          </div>
                          <Badge className={getFilamentRequestStatusColor(request.currentStatus)}>
                            {getFilamentRequestStatusLabel(request.currentStatus)}
                          </Badge>
                        </div>
                        {request.link && (
                          <a
                            href={request.link}
                            target="_blank"
                            rel="noopener noreferrer"
                            className="text-sm text-primary hover:underline flex items-center gap-1 mb-2"
                          >
                            <ExternalLink className="w-3 h-3" />
                            Product Link
                          </a>
                        )}
                        {request.notes && (
                          <p className="text-sm text-muted-foreground mb-2">{request.notes}</p>
                        )}
                        {request.filamentId && request.filamentName && (
                          <div className="flex items-center gap-2 mt-2 p-2 bg-green-50 dark:bg-green-950 border border-green-200 dark:border-green-800 rounded">
                            <CheckCircle2 className="w-4 h-4 text-green-600 dark:text-green-400" />
                            <p className="text-sm text-green-700 dark:text-green-300 font-medium">
                              In Stock: {request.filamentName}
                            </p>
                          </div>
                        )}
                        {request.statusHistory.length > 1 && (
                          <Collapsible className="mt-2">
                            <CollapsibleTrigger className="flex items-center gap-2 text-sm cursor-pointer text-muted-foreground hover:text-foreground">
                              <ChevronDown className="w-4 h-4 transition-transform ui-state-open:rotate-180" />
                              Status History ({request.statusHistory.length})
                            </CollapsibleTrigger>
                            <CollapsibleContent>
                              <div className="mt-2 space-y-1 pl-4 border-l-2">
                                {request.statusHistory.map((history) => (
                                  <div key={history.id} className="text-sm">
                                    <Badge variant="outline" className="mr-2">
                                      {getFilamentRequestStatusLabel(history.status)}
                                    </Badge>
                                    {history.changedByUsername && (
                                      <span className="text-muted-foreground">
                                        by {history.changedByUsername}
                                      </span>
                                    )}
                                    {' • '}
                                    <span className="text-muted-foreground">
                                      {formatRelativeTime(history.createdAt)}
                                    </span>
                                    {history.reason && (
                                      <p className="text-muted-foreground italic mt-1">
                                        {history.reason}
                                      </p>
                                    )}
                                  </div>
                                ))}
                              </div>
                            </CollapsibleContent>
                          </Collapsible>
                        )}
                      </div>
                      <div className="flex flex-col gap-2">
                        {request.currentStatus === FilamentRequestStatusEnum.Pending && !request.filamentId && (
                          <Button
                            onClick={() => createFilamentFromRequest(request)}
                            className="w-full"
                          >
                            <Plus className="w-4 h-4 mr-2" />
                            Add to Stock
                          </Button>
                        )}
                        {request.filamentId && (
                          <Button
                            variant="outline"
                            size="sm"
                            onClick={() => {
                              const filament = filaments.find(f => f.id === request.filamentId);
                              if (filament) {
                                openEditFilamentDialog(filament);
                              }
                            }}
                            disabled={!filaments.find(f => f.id === request.filamentId)}
                          >
                            <Package className="w-4 h-4 mr-2" />
                            View in Stock
                          </Button>
                        )}
                        <Button
                          variant="outline"
                          size="sm"
                          onClick={() => openFilamentRequestStatusDialog(request)}
                        >
                          <Edit2 className="w-4 h-4 mr-2" />
                          Change Status
                        </Button>
                      </div>
                    </div>
                  ))}
                </div>
              )}
            </CardContent>
          </Card>
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

      {/* Filament Create/Edit Dialog */}
      <Dialog open={filamentDialogOpen} onOpenChange={setFilamentDialogOpen}>
        <DialogContent className="max-w-2xl max-h-[90vh] overflow-y-auto">
          <DialogHeader>
            <DialogTitle>{editingFilament ? 'Edit Filament' : 'Add New Filament'}</DialogTitle>
            <DialogDescription>
              {editingFilament
                ? 'Update filament details and stock levels'
                : 'Add a new filament to your inventory'}
            </DialogDescription>
          </DialogHeader>

          <div className="space-y-4 py-4">
            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-2">
                <Label htmlFor="name">Name *</Label>
                <Input
                  id="name"
                  value={filamentFormData.name}
                  onChange={(e) => setFilamentFormData({ ...filamentFormData, name: e.target.value })}
                  placeholder="e.g., PLA Black 1kg"
                />
              </div>

              <div className="space-y-2">
                <Label htmlFor="material">Material *</Label>
                <Input
                  id="material"
                  value={filamentFormData.material}
                  onChange={(e) => setFilamentFormData({ ...filamentFormData, material: e.target.value })}
                  placeholder="e.g., PLA, PETG, ABS"
                />
              </div>
            </div>

            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-2">
                <Label htmlFor="brand">Brand *</Label>
                <Input
                  id="brand"
                  value={filamentFormData.brand}
                  onChange={(e) => setFilamentFormData({ ...filamentFormData, brand: e.target.value })}
                  placeholder="e.g., Prusa, eSun"
                />
              </div>

              <div className="space-y-2">
                <Label htmlFor="colour">Colour *</Label>
                <Input
                  id="colour"
                  value={filamentFormData.colour}
                  onChange={(e) => setFilamentFormData({ ...filamentFormData, colour: e.target.value })}
                  placeholder="e.g., Black, Red, White"
                />
              </div>
            </div>

            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-2">
                <Label htmlFor="stockAmount">Stock Amount *</Label>
                <Input
                  id="stockAmount"
                  type="number"
                  min="0"
                  value={filamentFormData.stockAmount}
                  onChange={(e) => setFilamentFormData({ ...filamentFormData, stockAmount: parseFloat(e.target.value) || 0 })}
                  placeholder="0"
                />
              </div>

              <div className="space-y-2">
                <Label htmlFor="stockUnit">Stock Unit *</Label>
                <Input
                  id="stockUnit"
                  value={filamentFormData.stockUnit}
                  onChange={(e) => setFilamentFormData({ ...filamentFormData, stockUnit: e.target.value })}
                  placeholder="g, kg, m"
                />
              </div>
            </div>

            <div className="space-y-2">
              <Label htmlFor="link">Product Link (Optional)</Label>
              <Input
                id="link"
                type="url"
                value={filamentFormData.link}
                onChange={(e) => setFilamentFormData({ ...filamentFormData, link: e.target.value })}
                placeholder="https://..."
              />
            </div>

            <div className="space-y-2">
              <Label htmlFor="photoUrl">Photo URL (Optional)</Label>
              <Input
                id="photoUrl"
                type="url"
                value={filamentFormData.photoUrl}
                onChange={(e) => setFilamentFormData({ ...filamentFormData, photoUrl: e.target.value })}
                placeholder="https://..."
              />
            </div>
          </div>

          <div className="flex items-center space-x-2 pt-2">
            <input
              type="checkbox"
              id="isAvailable"
              checked={filamentFormData.isAvailable ?? true}
              onChange={(e) => setFilamentFormData({ ...filamentFormData, isAvailable: e.target.checked })}
              className="w-4 h-4 rounded border-gray-300 text-primary focus:ring-primary"
            />
            <Label htmlFor="isAvailable" className="text-sm font-medium cursor-pointer">
              Available for selection in print requests
            </Label>
          </div>
          <p className="text-xs text-muted-foreground">
            When enabled, users can select this filament when creating print requests. Disable to hide from selection (e.g., for discontinued or special-use filaments).
          </p>

          <DialogFooter>
            <Button
              variant="outline"
              onClick={() => setFilamentDialogOpen(false)}
              disabled={filamentSubmitting}
            >
              Cancel
            </Button>
            <Button
              onClick={handleFilamentSubmit}
              disabled={
                filamentSubmitting ||
                !filamentFormData.name ||
                !filamentFormData.material ||
                !filamentFormData.brand ||
                !filamentFormData.colour ||
                !filamentFormData.stockUnit
              }
            >
              {filamentSubmitting && <Loader2 className="w-4 h-4 mr-2 animate-spin" />}
              {editingFilament ? 'Update Filament' : 'Add Filament'}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Delete Filament Confirmation Dialog */}
      <AlertDialog open={deleteFilamentDialogOpen} onOpenChange={setDeleteFilamentDialogOpen}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Delete Filament</AlertDialogTitle>
            <AlertDialogDescription>
              Are you sure you want to delete {filamentToDelete?.name}? This action cannot be undone.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel disabled={deletingFilament}>Cancel</AlertDialogCancel>
            <AlertDialogAction
              onClick={handleDeleteFilament}
              disabled={deletingFilament}
              className="bg-destructive text-destructive-foreground hover:bg-destructive/90"
            >
              {deletingFilament ? (
                <>
                  <Loader2 className="w-4 h-4 mr-2 animate-spin" />
                  Deleting...
                </>
              ) : (
                'Delete'
              )}
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>

      {/* Filament Request Status Change Dialog */}
      <Dialog open={selectedFilamentRequest !== null} onOpenChange={(open) => !open && setSelectedFilamentRequest(null)}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Change Filament Request Status</DialogTitle>
            <DialogDescription>
              Update the status for filament request: {selectedFilamentRequest?.brand} - {selectedFilamentRequest?.material} ({selectedFilamentRequest?.colour})
            </DialogDescription>
          </DialogHeader>

          <div className="space-y-4 py-4">
            <div className="space-y-2">
              <Label>New Status</Label>
              <Select
                value={newFilamentRequestStatus?.toString()}
                onValueChange={(value) => setNewFilamentRequestStatus(parseInt(value) as FilamentRequestStatusEnum)}
              >
                <SelectTrigger>
                  <SelectValue placeholder="Select status" />
                </SelectTrigger>
                <SelectContent>
                  {ALL_FILAMENT_REQUEST_STATUS_VALUES.map((status) => (
                    <SelectItem key={status} value={status.toString()}>
                      {getFilamentRequestStatusLabel(status)}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>

            {newFilamentRequestStatus === FilamentRequestStatusEnum.Approved && (
              <div className="space-y-3">
                <div className="space-y-2">
                  <Label>Link to Existing Filament (Optional)</Label>
                  <Select
                    value={selectedFilamentForRequest}
                    onValueChange={setSelectedFilamentForRequest}
                  >
                    <SelectTrigger>
                      <SelectValue placeholder="Select existing filament or add new to stock" />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="">None - Add new to stock</SelectItem>
                      {filaments.map((filament) => (
                        <SelectItem key={filament.id} value={filament.id}>
                          {filament.name} ({filament.brand})
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
                </div>
                <div className="flex items-center gap-2">
                  <div className="h-px bg-border flex-1" />
                  <span className="text-xs text-muted-foreground">OR</span>
                  <div className="h-px bg-border flex-1" />
                </div>
                <Button
                  type="button"
                  variant="secondary"
                  className="w-full"
                  onClick={() => selectedFilamentRequest && createFilamentFromRequest(selectedFilamentRequest)}
                >
                  <Plus className="w-4 h-4 mr-2" />
                  Add New to Stock
                </Button>
                <p className="text-xs text-muted-foreground">
                  Link to an existing filament or add a new one to your inventory.
                </p>
              </div>
            )}

            <div className="space-y-2">
              <Label>Reason / Notes (Optional)</Label>
              <Textarea
                value={filamentRequestReason}
                onChange={(e) => setFilamentRequestReason(e.target.value)}
                placeholder="Add any notes for the requester..."
                rows={3}
              />
            </div>
          </div>

          <DialogFooter>
            <Button
              variant="outline"
              onClick={() => setSelectedFilamentRequest(null)}
              disabled={updatingFilamentRequest}
            >
              Cancel
            </Button>
            <Button onClick={handleFilamentRequestStatusChange} disabled={updatingFilamentRequest || newFilamentRequestStatus === null}>
              {updatingFilamentRequest && <Loader2 className="w-4 h-4 mr-2 animate-spin" />}
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
