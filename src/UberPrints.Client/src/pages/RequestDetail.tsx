import { useEffect, useState } from 'react';
import { useParams, useNavigate, useLocation, Link } from 'react-router-dom';
import { api } from '../lib/api';
import { PrintRequestDto } from '../types/api';
import { useAuth } from '../contexts/AuthContext';
import { useToast } from '../hooks/use-toast';
import { Button } from '../components/ui/button';
import { Skeleton } from '../components/ui/skeleton';
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
import { formatShortDate, formatShortDateTime, sanitizeUrl, getChangeFieldLabel } from '../lib/utils';
import { getRequestStage } from '../lib/requestStage';
import { StageBar } from '../components/StageBar';
import { StatusBadge } from '../components/StatusBadge';
import { ModelThumbnail } from '../components/ModelThumbnail';
import { Ticket, TicketDivider, TicketRow } from '../components/Ticket';
import { ArrowLeft, Copy, ExternalLink, Loader2, Package, Trash2, Edit2, Video } from 'lucide-react';
import { EditRequestDialog } from '../components/admin/EditRequestDialog';
import { ChangeStatusDialog } from '../components/admin/ChangeStatusDialog';

export const RequestDetail = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const location = useLocation();
  const { user, isAuthenticated } = useAuth();
  const { toast } = useToast();
  const [request, setRequest] = useState<PrintRequestDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [deleting, setDeleting] = useState(false);
  const [deleteDialogOpen, setDeleteDialogOpen] = useState(false);
  const [statusDialogRequest, setStatusDialogRequest] = useState<PrintRequestDto | null>(null);
  const [editDialogRequest, setEditDialogRequest] = useState<PrintRequestDto | null>(null);

  useEffect(() => {
    if (id) {
      loadRequest();
    }
  }, [id]);

  const loadRequest = async () => {
    if (!id) return;

    try {
      setLoading(true);
      const data = await api.getRequest(id);
      setRequest(data);
    } catch (err: any) {
      // Tracking a private request from a non-owner session: use the track response
      const trackedRequest = (location.state as { request?: PrintRequestDto } | null)?.request;
      if (err.response?.status === 404 && trackedRequest?.id === id) {
        setRequest(trackedRequest);
        return;
      }
      console.error('Error loading request:', err);
      setError(err.response?.status === 404 ? 'Request not found' : 'Failed to load request');
    } finally {
      setLoading(false);
    }
  };

  const handleDelete = async () => {
    if (!request) return;

    try {
      setDeleting(true);
      await api.deleteRequest(request.id);
      toast({
        title: "Request deleted",
        description: "Your print request has been deleted successfully.",
        variant: "success",
      });
      navigate('/dashboard');
    } catch (err: any) {
      console.error('Error deleting request:', err);
      toast({
        title: "Failed to delete request",
        description: err.response?.data?.message || 'Failed to delete request',
        variant: "destructive",
      });
    } finally {
      setDeleting(false);
      setDeleteDialogOpen(false);
    }
  };

  const handleDialogSuccess = async () => {
    await loadRequest();
  };

  const canDelete = isAuthenticated && user && request?.userId === user.id;

  const handleCopyToken = () => {
    if (!request?.guestTrackingToken) return;
    navigator.clipboard.writeText(request.guestTrackingToken);
    toast({
      title: "Copied!",
      description: "Tracking token copied to clipboard",
      variant: "success",
    });
  };

  if (loading) {
    return (
      <div className="space-y-6">
        <div className="space-y-2">
          <Skeleton className="h-4 w-20" />
          <Skeleton className="h-10 w-72" />
          <Skeleton className="h-4 w-48" />
        </div>
        <Skeleton className="h-20 w-full rounded-lg" />
        <div className="grid gap-6 lg:grid-cols-[minmax(0,1.2fr)_minmax(0,1fr)]">
          <Skeleton className="h-72 w-full rounded-lg" />
          <div className="space-y-6">
            <Skeleton className="h-44 w-full rounded-lg" />
            <Skeleton className="h-32 w-full rounded-lg" />
          </div>
        </div>
      </div>
    );
  }

  if (error || !request) {
    return (
      <div className="text-center py-12">
        <Package className="w-12 h-12 mx-auto mb-4 text-muted-foreground" />
        <h2 className="text-2xl font-heading font-bold mb-2">{error || 'Request not found'}</h2>
        <p className="text-muted-foreground mb-6">
          The request you're looking for doesn't exist or has been removed.
        </p>
        <Link to="/requests">
          <Button>
            <ArrowLeft className="w-4 h-4 mr-2" />
            Back to Requests
          </Button>
        </Link>
      </div>
    );
  }

  const isOwner = !!user && request.userId === user.id;
  const stage = getRequestStage(request.currentStatus);
  const isPrinting = stage === 'printing';
  // Set by TrackRequest after a successful token lookup.
  const cameFromTracking = (location.state as { tracked?: boolean } | null)?.tracked === true;
  const showTicket = isOwner || cameFromTracking;
  const statusHistory = [...request.statusHistory].sort(
    (a, b) => new Date(a.timestamp).getTime() - new Date(b.timestamp).getTime()
  );

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="space-y-1.5">
        <Link to="/requests" className="text-sm font-semibold text-primary hover:underline">
          Requests
        </Link>
        <div className="flex flex-wrap items-baseline gap-x-4 gap-y-1">
          <h1 className="font-heading text-3xl font-extrabold tracking-tight lg:text-4xl break-words min-w-0">
            {request.requesterName}
          </h1>
          <span className="font-heading text-xl font-medium text-muted-foreground tabular-nums">
            #{request.id.slice(0, 8)}
          </span>
        </div>
        <p className="text-muted-foreground">
          Requested on {formatShortDate(request.createdAt)}
          {isOwner && <span className="font-semibold text-primary"> · Your request</span>}
        </p>
        {isOwner && !isAuthenticated && (
          <p className="text-sm text-muted-foreground">
            You're viewing this request as a guest. Sign in with Discord to access it from any device and get notifications.
          </p>
        )}
      </div>

      {/* Stage */}
      <section className="rounded-lg border border-border bg-card px-6 py-5">
        <StageBar status={request.currentStatus} />
      </section>

      <div className="grid items-start gap-6 lg:grid-cols-[minmax(0,1.2fr)_minmax(0,1fr)]">
        {/* Left column */}
        <div className="space-y-6">
          <section className="flex items-center justify-center overflow-hidden rounded-lg border border-border bg-muted/50 p-6">
            <ModelThumbnail modelUrl={request.modelUrl} size={280} className="max-w-full overflow-hidden rounded-md" />
          </section>

          <section className="space-y-2 rounded-lg border border-border bg-card px-6 py-5">
            <h2 className="font-heading text-lg font-bold">Notes</h2>
            {request.notes ? (
              <p className="max-w-[62ch] whitespace-pre-wrap leading-relaxed">{request.notes}</p>
            ) : (
              <p className="text-muted-foreground">No notes.</p>
            )}
          </section>
        </div>

        {/* Right column */}
        <div className="space-y-6">
          {showTicket && (
            <Ticket number={`#${request.id.slice(0, 8)}`} stamp={stage}>
              <div className="text-xs font-medium text-muted-foreground">Your ticket</div>
              <TicketDivider />
              <TicketRow label="Requester">{request.requesterName}</TicketRow>
              <TicketRow label="Created">{formatShortDate(request.createdAt)}</TicketRow>
              <TicketRow label="Filament">{request.filamentName || 'Not specified'}</TicketRow>
              {request.guestTrackingToken && (
                <>
                  <TicketDivider />
                  <div className="flex items-center justify-between gap-3">
                    <div className="min-w-0">
                      <div className="text-muted-foreground">Tracking</div>
                      <div className="break-all font-medium tracking-wider">{request.guestTrackingToken}</div>
                    </div>
                    <Button
                      variant="outline"
                      size="icon"
                      className="flex-shrink-0"
                      onClick={handleCopyToken}
                      aria-label="Copy tracking token"
                    >
                      <Copy className="h-4 w-4" />
                    </Button>
                  </div>
                </>
              )}
            </Ticket>
          )}

          <section className="rounded-lg border border-border bg-card px-6 py-5">
            <dl className="grid grid-cols-[110px_minmax(0,1fr)] gap-x-4 gap-y-3 text-sm">
              <dt className="text-muted-foreground">Model</dt>
              <dd className="min-w-0">
                <a
                  href={sanitizeUrl(request.modelUrl)}
                  target="_blank"
                  rel="noopener noreferrer"
                  className="inline-flex max-w-full items-center gap-1.5 font-medium text-primary hover:underline"
                >
                  <span className="truncate">{request.modelUrl.replace(/^https?:\/\/(www\.)?/, '')}</span>
                  <ExternalLink className="h-3.5 w-3.5 flex-shrink-0" />
                </a>
              </dd>
              <dt className="text-muted-foreground">Filament</dt>
              <dd>{request.filamentName || 'Not specified'}</dd>
              <dt className="text-muted-foreground">Delivery</dt>
              <dd>{request.requestDelivery ? 'Yes, delivery requested' : 'No, pickup only'}</dd>
              <dt className="text-muted-foreground">Visibility</dt>
              <dd>{request.isPublic ? 'Public' : 'Private'}</dd>
              {request.guestTrackingToken && (
                <>
                  <dt className="text-muted-foreground">Tracking token</dt>
                  <dd className="min-w-0 space-y-1">
                    <code className="block break-all rounded bg-muted px-2 py-1 text-xs">
                      {request.guestTrackingToken}
                    </code>
                    <p className="text-xs text-muted-foreground">
                      {isAuthenticated
                        ? "Share this token with others to let them track this request without logging in"
                        : "Save this token to track your request later without logging in"
                      }
                    </p>
                  </dd>
                </>
              )}
            </dl>
          </section>

          <section className="space-y-3 rounded-lg border border-border bg-card px-6 py-5">
            <h2 className="font-heading text-lg font-bold">History</h2>
            {statusHistory.length === 0 ? (
              <p className="text-sm text-muted-foreground">No status changes yet</p>
            ) : (
              <ol className="max-h-[400px] space-y-3 overflow-y-auto">
                {statusHistory.map((history) => (
                  <li key={history.id} className="grid grid-cols-[120px_minmax(0,1fr)] gap-3 text-sm">
                    <span className="text-muted-foreground tabular-nums">{formatShortDateTime(history.timestamp)}</span>
                    <div className="min-w-0 space-y-1">
                      <StatusBadge status={history.status} />
                      {history.changedByUsername && (
                        <p className="text-muted-foreground">by {history.changedByUsername}</p>
                      )}
                      {history.adminNotes && (
                        <p className="rounded-md bg-muted p-2">{history.adminNotes}</p>
                      )}
                    </div>
                  </li>
                ))}
              </ol>
            )}
          </section>

          {request.changes && request.changes.length > 0 && (
            <section className="space-y-3 rounded-lg border border-border bg-card px-6 py-5">
              <h2 className="font-heading text-lg font-bold">Changes</h2>
              <ol className="max-h-[400px] space-y-3 overflow-y-auto">
                {request.changes.map((change) => (
                  <li key={change.id} className="grid grid-cols-[120px_minmax(0,1fr)] gap-3 text-sm">
                    <span className="text-muted-foreground tabular-nums">{formatShortDateTime(change.changedAt)}</span>
                    <div className="min-w-0 space-y-0.5">
                      <p className="font-semibold">{getChangeFieldLabel(change.fieldName)}</p>
                      <p className="break-words">
                        <span className="text-muted-foreground line-through">{change.oldValue || '(empty)'}</span>
                        {' → '}
                        <span className="font-medium">{change.newValue || '(empty)'}</span>
                      </p>
                      {change.changedByUsername && (
                        <p className="text-muted-foreground">by {change.changedByUsername}</p>
                      )}
                    </div>
                  </li>
                ))}
              </ol>
            </section>
          )}

          {/* Actions */}
          <div className="grid grid-cols-1 gap-2.5 sm:grid-cols-2">
            {isPrinting && (
              <Button asChild>
                <Link to="/live-view">
                  <Video className="w-4 h-4 mr-2" />
                  Watch live camera
                </Link>
              </Button>
            )}
            {request.guestTrackingToken && (
              <Button variant="outline" className="bg-card" onClick={handleCopyToken}>
                <Copy className="w-4 h-4 mr-2" />
                Copy tracking token
              </Button>
            )}
            {user?.isAdmin && (
              <>
                <Button
                  variant="outline"
                  className="bg-card"
                  onClick={() => setEditDialogRequest(request)}
                >
                  <Edit2 className="w-4 h-4 mr-2" />
                  Edit request
                </Button>
                <Button
                  variant="outline"
                  className="bg-card"
                  onClick={() => setStatusDialogRequest(request)}
                >
                  Change status
                </Button>
              </>
            )}
            {canDelete && (
              <>
                <Button
                  variant="outline"
                  className="bg-card"
                  onClick={() => navigate(`/requests/${request.id}/edit`)}
                >
                  <Edit2 className="w-4 h-4 mr-2" />
                  Edit
                </Button>
                <Button
                  variant="destructive"
                  onClick={() => setDeleteDialogOpen(true)}
                  disabled={deleting}
                >
                  <Trash2 className="w-4 h-4 mr-2" />
                  Delete
                </Button>
              </>
            )}
          </div>
        </div>
      </div>

      {/* Admin Dialogs */}
      <EditRequestDialog
        request={editDialogRequest}
        open={editDialogRequest !== null}
        onOpenChange={(open) => !open && setEditDialogRequest(null)}
        onSuccess={handleDialogSuccess}
      />

      <ChangeStatusDialog
        request={statusDialogRequest}
        open={statusDialogRequest !== null}
        onOpenChange={(open) => !open && setStatusDialogRequest(null)}
        onSuccess={handleDialogSuccess}
      />

      {/* Delete Confirmation Dialog */}
      <AlertDialog open={deleteDialogOpen} onOpenChange={setDeleteDialogOpen}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Delete request</AlertDialogTitle>
            <AlertDialogDescription>
              Are you sure you want to delete this print request? This action cannot be undone.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel disabled={deleting}>Cancel</AlertDialogCancel>
            <AlertDialogAction
              onClick={handleDelete}
              disabled={deleting}
              className="bg-destructive text-destructive-foreground hover:bg-destructive/90"
            >
              {deleting ? (
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
    </div>
  );
};
