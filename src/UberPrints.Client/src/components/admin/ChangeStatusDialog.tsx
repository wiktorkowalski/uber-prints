import { useState, useEffect } from 'react';
import { api } from '../../lib/api';
import { PrintRequestDto, RequestStatusEnum } from '../../types/api';
import { useToast } from '../../hooks/use-toast';
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from '../ui/dialog';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '../ui/select';
import { Textarea } from '../ui/textarea';
import { Label } from '../ui/label';
import { Button } from '../ui/button';
import { Loader2 } from 'lucide-react';
import { getStatusLabel } from '../../lib/utils';

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

interface ChangeStatusDialogProps {
  request: PrintRequestDto | null;
  open: boolean;
  onOpenChange: (open: boolean) => void;
  onSuccess: () => void;
}

export const ChangeStatusDialog = ({ request, open, onOpenChange, onSuccess }: ChangeStatusDialogProps) => {
  const { toast } = useToast();
  const [newStatus, setNewStatus] = useState<RequestStatusEnum | null>(null);
  const [adminNotes, setAdminNotes] = useState('');
  const [submitting, setSubmitting] = useState(false);

  useEffect(() => {
    if (open && request) {
      setNewStatus(request.currentStatus);
      setAdminNotes('');
    }
  }, [open, request]);

  const handleSubmit = async () => {
    if (!request || newStatus === null) return;

    try {
      setSubmitting(true);
      await api.changeRequestStatus(request.id, {
        status: newStatus,
        adminNotes: adminNotes || undefined,
      });

      toast({
        title: "Status updated",
        description: "Request status has been updated successfully.",
        variant: "success",
      });

      onSuccess();
      onOpenChange(false);
    } catch (err: any) {
      console.error('Error updating status:', err);
      toast({
        title: "Failed to update status",
        description: err.response?.data?.message || 'Failed to update status',
        variant: "destructive",
      });
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Change Request Status</DialogTitle>
          <DialogDescription>
            Update the status for request by {request?.requesterName}
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
            onClick={() => onOpenChange(false)}
            disabled={submitting}
          >
            Cancel
          </Button>
          <Button onClick={handleSubmit} disabled={submitting || newStatus === null}>
            {submitting && <Loader2 className="w-4 h-4 mr-2 animate-spin" />}
            Update Status
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
};
