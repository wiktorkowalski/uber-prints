import { useEffect, useState } from 'react';
import { useParams, useNavigate, Link } from 'react-router-dom';
import { api } from '../lib/api';
import { PrintRequestDto } from '../types/api';
import { useAuth } from '../contexts/AuthContext';
import { useToast } from '../hooks/use-toast';
import { Button } from '../components/ui/button';
import { Badge } from '../components/ui/badge';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '../components/ui/card';
import { Skeleton } from '../components/ui/skeleton';
import {
  Breadcrumb,
  BreadcrumbItem,
  BreadcrumbLink,
  BreadcrumbList,
  BreadcrumbPage,
  BreadcrumbSeparator,
} from '../components/ui/breadcrumb';
import { ScrollArea } from '../components/ui/scroll-area';
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
import { getStatusLabel, getStatusColor, formatDate, formatRelativeTime, sanitizeUrl } from '../lib/utils';
import { ArrowLeft, ExternalLink, Loader2, Package, Clock, User, Trash2, Edit2, History } from 'lucide-react';
import { EditRequestDialog } from '../components/admin/EditRequestDialog';
import { ChangeStatusDialog } from '../components/admin/ChangeStatusDialog';

export const RequestDetail = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
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

  if (loading) {
    return (
      <div className="max-w-4xl mx-auto space-y-6">
        <div className="flex items-center justify-between">
          <Skeleton className="h-10 w-24" />
          <div className="flex gap-2">
            <Skeleton className="h-9 w-32" />
            <Skeleton className="h-9 w-32" />
          </div>
        </div>
        <Card>
          <CardHeader>
            <div className="flex items-start justify-between">
              <div className="flex-1 space-y-2">
                <Skeleton className="h-8 w-48" />
                <Skeleton className="h-4 w-64" />
              </div>
              <Skeleton className="h-7 w-24" />
            </div>
          </CardHeader>
          <CardContent className="space-y-6">
            <div className="grid md:grid-cols-2 gap-6">
              <div className="space-y-2">
                <Skeleton className="h-4 w-24" />
                <Skeleton className="h-6 w-32" />
              </div>
              <div className="space-y-2">
                <Skeleton className="h-4 w-24" />
                <Skeleton className="h-6 w-48" />
              </div>
            </div>
            <div className="space-y-2">
              <Skeleton className="h-4 w-24" />
              <Skeleton className="h-6 w-full" />
            </div>
            <Skeleton className="h-24 w-full" />
          </CardContent>
        </Card>
        <Card>
          <CardHeader>
            <Skeleton className="h-6 w-48" />
            <Skeleton className="h-4 w-64" />
          </CardHeader>
          <CardContent>
            <div className="space-y-4">
              {[1, 2, 3].map((i) => (
                <div key={i} className="flex gap-4 pb-4 border-b">
                  <Skeleton className="w-2 h-2 rounded-full mt-2" />
                  <div className="flex-1 space-y-2">
                    <Skeleton className="h-6 w-24" />
                    <Skeleton className="h-4 w-32" />
                  </div>
                </div>
              ))}
            </div>
          </CardContent>
        </Card>
      </div>
    );
  }

  if (error || !request) {
    return (
      <div className="text-center py-12">
        <Package className="w-16 h-16 mx-auto mb-4 text-muted-foreground" />
        <h2 className="text-2xl font-bold mb-2">{error || 'Request not found'}</h2>
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

  return (
    <div className="max-w-4xl mx-auto space-y-6">
      {/* Breadcrumb Navigation */}
      <Breadcrumb>
        <BreadcrumbList>
          <BreadcrumbItem>
            <BreadcrumbLink href="/">Home</BreadcrumbLink>
          </BreadcrumbItem>
          <BreadcrumbSeparator />
          <BreadcrumbItem>
            <BreadcrumbLink href="/requests">Requests</BreadcrumbLink>
          </BreadcrumbItem>
          <BreadcrumbSeparator />
          <BreadcrumbItem>
            <BreadcrumbPage>Request #{request.id.slice(0, 8)}</BreadcrumbPage>
          </BreadcrumbItem>
        </BreadcrumbList>
      </Breadcrumb>

      {/* Header */}
      <div className="flex items-center justify-between">

        <div className="flex gap-2">
          {user?.isAdmin && (
            <>
              <Button
                variant="outline"
                size="sm"
                onClick={() => setEditDialogRequest(request)}
              >
                <Edit2 className="w-4 h-4 mr-2" />
                Edit Request
              </Button>
              <Button
                variant="outline"
                size="sm"
                onClick={() => setStatusDialogRequest(request)}
              >
                Change Status
              </Button>
            </>
          )}

          {canDelete && (
            <>
              <Button
                variant="outline"
                size="sm"
                onClick={() => navigate(`/requests/${request.id}/edit`)}
              >
                <Edit2 className="w-4 h-4 mr-2" />
                Edit
              </Button>
              <Button
                variant="destructive"
                size="sm"
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

      {/* Ownership Info Alert */}
      {user && request.userId === user.id && (
        <Card className="bg-blue-50 dark:bg-blue-950 border-blue-200 dark:border-blue-800">
          <CardContent className="pt-6">
            <div className="flex items-start gap-3">
              <User className="w-5 h-5 text-blue-600 dark:text-blue-400 flex-shrink-0 mt-0.5" />
              <div>
                <h3 className="font-medium text-blue-900 dark:text-blue-100 mb-1">
                  This is your request
                </h3>
                <p className="text-sm text-blue-700 dark:text-blue-300">
                  {isAuthenticated
                    ? "You can track the status of your request here. You'll be notified of any updates."
                    : "You're viewing this request as a guest. Sign in with Discord to access it from any device and get notifications."}
                </p>
              </div>
            </div>
          </CardContent>
        </Card>
      )}

      {/* Main Details Card */}
      <Card>
        <CardHeader>
          <div className="flex items-start justify-between">
            <div className="flex-1">
              <div className="flex items-center gap-2 mb-2">
                <CardTitle className="text-2xl">Request Details</CardTitle>
                {user && request.userId === user.id && (
                  <Badge variant="secondary" className="flex items-center gap-1">
                    <User className="w-3 h-3" />
                    Your request
                  </Badge>
                )}
              </div>
              <CardDescription>
                Submitted {formatRelativeTime(request.createdAt)}
              </CardDescription>
            </div>
            <span
              className={`px-3 py-1 rounded-full text-sm font-medium ${getStatusColor(
                request.currentStatus
              )}`}
            >
              {getStatusLabel(request.currentStatus)}
            </span>
          </div>
        </CardHeader>
        <CardContent className="space-y-6">
          <div className="grid md:grid-cols-2 gap-6">
            <div>
              <h3 className="text-sm font-medium text-muted-foreground mb-1">Requester</h3>
              <p className="text-lg">{request.requesterName}</p>
            </div>

            <div>
              <h3 className="text-sm font-medium text-muted-foreground mb-1">Filament</h3>
              <p className="text-lg">{request.filamentName || 'Not specified'}</p>
            </div>
          </div>

          <div>
            <h3 className="text-sm font-medium text-muted-foreground mb-2">Model URL</h3>
            <a
              href={sanitizeUrl(request.modelUrl)}
              target="_blank"
              rel="noopener noreferrer"
              className="flex items-center text-primary hover:underline break-all"
            >
              <ExternalLink className="w-4 h-4 mr-2 flex-shrink-0" />
              {request.modelUrl}
            </a>
          </div>

          {request.notes && (
            <div>
              <h3 className="text-sm font-medium text-muted-foreground mb-2">Notes</h3>
              <p className="text-sm whitespace-pre-wrap">{request.notes}</p>
            </div>
          )}

          <div className="flex items-center gap-4">
            <div className="flex items-center gap-2">
              {request.requestDelivery ? (
                <span className="text-sm">🚚 Delivery requested</span>
              ) : (
                <span className="text-sm">📦 Pickup only</span>
              )}
            </div>
          </div>

          {request.guestTrackingToken && (
            <div className="bg-muted p-4 rounded-lg">
              <div className="flex items-center justify-between mb-2">
                <h3 className="text-sm font-medium">Tracking Token</h3>
                <Button
                  variant="ghost"
                  size="sm"
                  onClick={() => {
                    navigator.clipboard.writeText(request.guestTrackingToken!);
                    toast({
                      title: "Copied!",
                      description: "Tracking token copied to clipboard",
                      variant: "success",
                    });
                  }}
                  className="h-7"
                >
                  <svg xmlns="http://www.w3.org/2000/svg" className="h-4 w-4 mr-1" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M8 16H6a2 2 0 01-2-2V6a2 2 0 012-2h8a2 2 0 012 2v2m-6 12h8a2 2 0 002-2v-8a2 2 0 00-2-2h-8a2 2 0 00-2 2v8a2 2 0 002 2z" />
                  </svg>
                  Copy
                </Button>
              </div>
              <code className="text-sm bg-background px-2 py-1 rounded block">
                {request.guestTrackingToken}
              </code>
              <p className="text-xs text-muted-foreground mt-2">
                {isAuthenticated
                  ? "Share this token with others to let them track this request without logging in"
                  : "Save this token to track your request later without logging in"
                }
              </p>
            </div>
          )}
        </CardContent>
      </Card>

      {/* Status History Card */}
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <Clock className="w-5 h-5" />
            Status History
          </CardTitle>
          <CardDescription>
            Complete timeline of status changes
          </CardDescription>
        </CardHeader>
        <CardContent>
          {request.statusHistory.length === 0 ? (
            <p className="text-sm text-muted-foreground text-center py-4">
              No status changes yet
            </p>
          ) : (
            <ScrollArea className="h-[400px] pr-4">
              <div className="space-y-4">
                {request.statusHistory
                  .sort((a, b) => new Date(b.timestamp).getTime() - new Date(a.timestamp).getTime())
                  .map((history) => (
                  <div
                    key={history.id}
                    className="flex gap-4 pb-4 border-b last:border-b-0 last:pb-0"
                  >
                    <div className="flex-shrink-0 w-2 h-2 mt-2 rounded-full bg-primary" />
                    <div className="flex-1 space-y-1">
                      <div className="flex items-center justify-between">
                        <span
                          className={`inline-block px-2 py-1 rounded text-xs font-medium ${getStatusColor(
                            history.status
                          )}`}
                        >
                          {getStatusLabel(history.status)}
                        </span>
                        <span className="text-xs text-muted-foreground">
                          {formatDate(history.timestamp)}
                        </span>
                      </div>
                      {history.changedByUsername && (
                        <p className="text-sm text-muted-foreground flex items-center gap-1">
                          <User className="w-3 h-3" />
                          Changed by {history.changedByUsername}
                        </p>
                      )}
                      {history.adminNotes && (
                        <p className="text-sm mt-2 p-3 bg-muted rounded-lg">
                          {history.adminNotes}
                        </p>
                      )}
                    </div>
                  </div>
                ))}
              </div>
            </ScrollArea>
          )}
        </CardContent>
      </Card>

      {/* Change History Card */}
      {request.changes && request.changes.length > 0 && (
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <History className="w-5 h-5" />
              Change History
            </CardTitle>
            <CardDescription>
              Track all modifications made to this request
            </CardDescription>
          </CardHeader>
          <CardContent>
            <ScrollArea className="h-[400px] pr-4">
              <div className="space-y-4">
                {request.changes.map((change) => (
                  <div
                    key={change.id}
                    className="flex gap-4 pb-4 border-b last:border-b-0 last:pb-0"
                  >
                    <div className="flex-shrink-0 w-2 h-2 mt-2 rounded-full bg-orange-500" />
                    <div className="flex-1 space-y-1">
                      <div className="flex items-center justify-between">
                        <span className="text-sm font-medium">
                          {change.fieldName}
                        </span>
                        <span className="text-xs text-muted-foreground">
                          {formatDate(change.changedAt)}
                        </span>
                      </div>
                      <div className="text-sm space-y-1">
                        <div className="flex items-start gap-2">
                          <span className="text-muted-foreground min-w-[60px]">From:</span>
                          <span className="text-red-600 dark:text-red-400 line-through">
                            {change.oldValue || '(empty)'}
                          </span>
                        </div>
                        <div className="flex items-start gap-2">
                          <span className="text-muted-foreground min-w-[60px]">To:</span>
                          <span className="text-green-600 dark:text-green-400 font-medium">
                            {change.newValue || '(empty)'}
                          </span>
                        </div>
                      </div>
                      {change.changedByUsername && (
                        <p className="text-sm text-muted-foreground flex items-center gap-1 mt-2">
                          <User className="w-3 h-3" />
                          Changed by {change.changedByUsername}
                        </p>
                      )}
                    </div>
                  </div>
                ))}
              </div>
            </ScrollArea>
          </CardContent>
        </Card>
      )}

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
            <AlertDialogTitle>Delete Request</AlertDialogTitle>
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
