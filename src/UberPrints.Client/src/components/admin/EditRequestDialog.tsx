import { useState, useEffect } from 'react';
import { api } from '../../lib/api';
import { PrintRequestDto, FilamentDto } from '../../types/api';
import { useToast } from '../../hooks/use-toast';
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from '../ui/dialog';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '../ui/select';
import { Textarea } from '../ui/textarea';
import { Label } from '../ui/label';
import { Input } from '../ui/input';
import { Button } from '../ui/button';
import { Loader2 } from 'lucide-react';

interface EditRequestDialogProps {
  request: PrintRequestDto | null;
  open: boolean;
  onOpenChange: (open: boolean) => void;
  onSuccess: () => void;
}

export const EditRequestDialog = ({ request, open, onOpenChange, onSuccess }: EditRequestDialogProps) => {
  const { toast } = useToast();
  const [filaments, setFilaments] = useState<FilamentDto[]>([]);
  const [filamentsLoading, setFilamentsLoading] = useState(false);
  const [editFormData, setEditFormData] = useState<{
    requesterName: string;
    modelUrl: string;
    notes: string;
    requestDelivery: boolean;
    isPublic: boolean;
    filamentId?: string;
  }>({
    requesterName: '',
    modelUrl: '',
    notes: '',
    requestDelivery: false,
    isPublic: true,
    filamentId: undefined,
  });
  const [submitting, setSubmitting] = useState(false);

  useEffect(() => {
    if (open && request) {
      setEditFormData({
        requesterName: request.requesterName,
        modelUrl: request.modelUrl,
        notes: request.notes || '',
        requestDelivery: request.requestDelivery,
        isPublic: request.isPublic,
        filamentId: request.filamentId,
      });
      loadFilaments();
    }
  }, [open, request]);

  const loadFilaments = async () => {
    try {
      setFilamentsLoading(true);
      const data = await api.getFilaments();
      setFilaments(data);
    } catch (err) {
      console.error('Error loading filaments:', err);
      toast({
        title: "Failed to load filaments",
        description: "Could not load filament options",
        variant: "destructive",
      });
    } finally {
      setFilamentsLoading(false);
    }
  };

  const handleSubmit = async () => {
    if (!request) return;

    try {
      setSubmitting(true);
      await api.updateAdminRequest(request.id, editFormData);

      toast({
        title: "Request updated",
        description: "Print request has been updated successfully.",
        variant: "success",
      });

      onSuccess();
      onOpenChange(false);
    } catch (err: any) {
      console.error('Error updating request:', err);
      toast({
        title: "Failed to update request",
        description: err.response?.data?.message || 'Failed to update request',
        variant: "destructive",
      });
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-2xl">
        <DialogHeader>
          <DialogTitle>Edit Print Request</DialogTitle>
          <DialogDescription>
            Update request details for {request?.requesterName}
          </DialogDescription>
        </DialogHeader>

        <div className="space-y-4 py-4">
          <div className="space-y-2">
            <Label>Requester Name</Label>
            <Input
              value={editFormData.requesterName}
              onChange={(e) => setEditFormData({ ...editFormData, requesterName: e.target.value })}
              placeholder="John Doe"
            />
          </div>

          <div className="space-y-2">
            <Label>Model URL</Label>
            <Input
              value={editFormData.modelUrl}
              onChange={(e) => setEditFormData({ ...editFormData, modelUrl: e.target.value })}
              placeholder="https://www.thingiverse.com/thing:123456"
              type="url"
            />
          </div>

          <div className="space-y-2">
            <Label>Filament (Optional)</Label>
            {filamentsLoading ? (
              <div className="text-sm text-muted-foreground">Loading filaments...</div>
            ) : (
              <Select
                value={editFormData.filamentId || 'none'}
                onValueChange={(value) => setEditFormData({ ...editFormData, filamentId: value === 'none' ? undefined : value })}
              >
                <SelectTrigger>
                  <SelectValue placeholder="Select filament (optional)" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="none">No filament selected</SelectItem>
                  {filaments.map((filament) => (
                    <SelectItem key={filament.id} value={filament.id}>
                      {filament.name} - {filament.material} ({filament.colour}) - {filament.stockAmount}{filament.stockUnit}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            )}
          </div>

          <div className="space-y-2">
            <Label>Notes</Label>
            <Textarea
              value={editFormData.notes}
              onChange={(e) => setEditFormData({ ...editFormData, notes: e.target.value })}
              placeholder="Any special instructions..."
              rows={3}
            />
          </div>

          <div className="flex items-center space-x-2">
            <input
              type="checkbox"
              id="requestDelivery"
              checked={editFormData.requestDelivery}
              onChange={(e) => setEditFormData({ ...editFormData, requestDelivery: e.target.checked })}
              className="rounded"
            />
            <Label htmlFor="requestDelivery">Request Delivery</Label>
          </div>

          <div className="flex items-center space-x-2">
            <input
              type="checkbox"
              id="isPublic"
              checked={editFormData.isPublic}
              onChange={(e) => setEditFormData({ ...editFormData, isPublic: e.target.checked })}
              className="rounded"
            />
            <Label htmlFor="isPublic">Make request public</Label>
          </div>
        </div>

        <DialogFooter>
          <Button
            variant="outline"
            onClick={() => onOpenChange(false)}
            disabled={submitting}
          >
            Cancel
          </Button>
          <Button onClick={handleSubmit} disabled={submitting || filamentsLoading}>
            {submitting && <Loader2 className="w-4 h-4 mr-2 animate-spin" />}
            Save Changes
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
};
