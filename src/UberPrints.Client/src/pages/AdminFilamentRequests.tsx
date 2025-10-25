import { useEffect, useState } from 'react';
import { api } from '../lib/api';
import { FilamentRequestDto, FilamentRequestStatusEnum, FilamentDto, CreateFilamentDto, ChangeFilamentRequestStatusDto } from '../types/api';
import { useToast } from '../hooks/use-toast';
import { Button } from '../components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '../components/ui/card';
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from '../components/ui/dialog';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '../components/ui/select';
import { Textarea } from '../components/ui/textarea';
import { Label } from '../components/ui/label';
import { Input } from '../components/ui/input';
import { Badge } from '../components/ui/badge';
import { Skeleton } from '../components/ui/skeleton';
import { Collapsible, CollapsibleContent, CollapsibleTrigger } from '../components/ui/collapsible';
import { ChevronDown, Package, Loader2, ExternalLink, Edit2, Plus, CheckCircle2, Shield } from 'lucide-react';
import { formatRelativeTime } from '../lib/utils';

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

export const AdminFilamentRequests = () => {
  const { toast } = useToast();

  const [filamentRequests, setFilamentRequests] = useState<FilamentRequestDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [filaments, setFilaments] = useState<FilamentDto[]>([]);
  const [selectedFilamentRequest, setSelectedFilamentRequest] = useState<FilamentRequestDto | null>(null);
  const [newFilamentRequestStatus, setNewFilamentRequestStatus] = useState<FilamentRequestStatusEnum | null>(null);
  const [filamentRequestReason, setFilamentRequestReason] = useState('');
  const [selectedFilamentForRequest, setSelectedFilamentForRequest] = useState<string>('');
  const [updatingFilamentRequest, setUpdatingFilamentRequest] = useState(false);
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
  const [filamentDialogOpen, setFilamentDialogOpen] = useState(false);
  const [creatingFilamentForRequest, setCreatingFilamentForRequest] = useState<FilamentRequestDto | null>(null);
  const [filamentSubmitting, setFilamentSubmitting] = useState(false);

  useEffect(() => {
    loadFilamentRequests();
  }, []);

  const loadFilamentRequests = async () => {
    try {
      setLoading(true);
      const data = await api.getAdminFilamentRequests();
      setFilamentRequests(data);
    } catch (err) {
      toast({
        title: "Failed to load filament requests",
        description: "Could not load filament requests",
        variant: "destructive",
      });
    } finally {
      setLoading(false);
    }
  };

  const loadFilaments = async () => {
    try {
      const data = await api.getFilaments();
      setFilaments(data);
    } catch (err) {
      toast({
        title: "Failed to load filaments",
        description: "Could not load filament inventory",
        variant: "destructive",
      });
    }
  };

  const openFilamentRequestStatusDialog = async (request: FilamentRequestDto) => {
    setSelectedFilamentRequest(request);
    setNewFilamentRequestStatus(request.currentStatus);
    setFilamentRequestReason('');
    setSelectedFilamentForRequest(request.filamentId || '');
    // Load filaments if needed
    if (filaments.length === 0) {
      await loadFilaments();
    }
  };

  const createFilamentFromRequest = (request: FilamentRequestDto) => {
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
    setCreatingFilamentForRequest(request);
    setFilamentDialogOpen(true);
    setSelectedFilamentRequest(null);
  };

  const handleFilamentSubmit = async () => {
    try {
      setFilamentSubmitting(true);
      const newFilament = await api.createFilament(filamentFormData);
      toast({
        title: "Filament created",
        description: "New filament has been added to inventory",
        variant: "success",
      });

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

          await loadFilamentRequests();
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
      setFilamentDialogOpen(false);
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

  const handleFilamentRequestStatusChange = async () => {
    if (!selectedFilamentRequest || newFilamentRequestStatus === null) return;

    try {
      setUpdatingFilamentRequest(true);
      const statusData: ChangeFilamentRequestStatusDto = {
        status: newFilamentRequestStatus,
        reason: filamentRequestReason || undefined,
      };

      if (newFilamentRequestStatus === FilamentRequestStatusEnum.Approved && selectedFilamentForRequest) {
        statusData.filamentId = selectedFilamentForRequest;
      }

      await api.changeFilamentRequestStatus(selectedFilamentRequest.id, statusData);

      toast({
        title: "Status updated",
        description: "Filament request status has been updated successfully.",
        variant: "success",
      });

      setSelectedFilamentRequest(null);
      setNewFilamentRequestStatus(null);
      setFilamentRequestReason('');
      setSelectedFilamentForRequest('');

      await loadFilamentRequests();
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

  if (loading) {
    return (
      <div className="space-y-6">
        <div>
          <Skeleton className="h-10 w-64" />
          <Skeleton className="h-4 w-96 mt-1" />
        </div>
        <Card>
          <CardContent className="pt-6">
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
          Filament Requests
        </h1>
        <p className="text-muted-foreground mt-1">
          Review and manage user filament requests
        </p>
      </div>

      {/* Requests Card */}
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
          {filamentRequests.length === 0 ? (
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
                        disabled
                        className="w-full"
                      >
                        <Package className="w-4 h-4 mr-2" />
                        Linked
                      </Button>
                    )}
                    <Button
                      variant="outline"
                      size="sm"
                      onClick={() => openFilamentRequestStatusDialog(request)}
                      className="w-full"
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

      {/* Status Change Dialog */}
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

      {/* Create Filament Dialog */}
      <Dialog open={filamentDialogOpen} onOpenChange={setFilamentDialogOpen}>
        <DialogContent className="max-w-2xl max-h-[90vh] overflow-y-auto">
          <DialogHeader>
            <DialogTitle>Add New Filament</DialogTitle>
            <DialogDescription>
              Add a new filament to your inventory
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
              Add Filament
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
};
