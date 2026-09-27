import { useEffect, useMemo, useState } from 'react';
import { Link } from 'react-router-dom';
import { api } from '../lib/api';
import { PrintRequestDto } from '../types/api';
import { useAuth } from '../contexts/AuthContext';
import { Button } from '../components/ui/button';
import { Input } from '../components/ui/input';
import { Skeleton } from '../components/ui/skeleton';
import { Tabs, TabsList, TabsTrigger } from '../components/ui/tabs';
import { formatCompactTime, getModelHost } from '../lib/utils';
import { Package, Search } from 'lucide-react';
import { ModelThumbnail } from '../components/ModelThumbnail';
import { StatusBadge } from '../components/StatusBadge';
import { PageHeader } from '../components/PageHeader';
import { getRequestStage, MAIN_STAGES, MainRequestStage, STAGE_LABELS } from '../lib/requestStage';

type RequestFilter = MainRequestStage | 'all' | 'mine';

// Desktop column template: thumbnail, model and requester, filament, created, stage.
const ROW_GRID = 'md:grid md:grid-cols-[56px_minmax(0,2fr)_minmax(0,1.3fr)_130px_170px] md:gap-4';

export const RequestList = () => {
  const { user } = useAuth();
  const [requests, setRequests] = useState<PrintRequestDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [filter, setFilter] = useState<RequestFilter>('all');
  const [query, setQuery] = useState('');

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

  const getFilteredRequests = (f: RequestFilter) => {
    if (f === 'all') {
      return requests;
    }
    if (f === 'mine') {
      return user ? requests.filter(r => r.userId === user.id) : [];
    }
    return requests.filter(r => getRequestStage(r.currentStatus) === f);
  };

  const visibleRequests = useMemo(() => {
    const byFilter = getFilteredRequests(filter);
    const q = query.trim().toLowerCase();
    if (!q) return byFilter;
    return byFilter.filter(r =>
      [r.requesterName, r.notes, r.filamentName, r.modelUrl].some(v => v?.toLowerCase().includes(q))
    );
    // getFilteredRequests only reads requests and user.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [requests, user, filter, query]);

  const newRequestButton = (
    <Link to="/requests/new">
      <Button>
        <Package className="w-4 h-4 mr-2" />
        New request
      </Button>
    </Link>
  );

  if (loading) {
    return (
      <div className="space-y-6">
        <Skeleton className="h-10 w-48" />
        <Skeleton className="h-10 w-full max-w-xl" />
        <div className="rounded-lg border border-border bg-card">
          {[1, 2, 3, 4, 5].map((i) => (
            <div key={i} className="flex items-center gap-4 border-b border-border px-4 py-3 last:border-b-0">
              <Skeleton className="h-14 w-14 flex-shrink-0 rounded-md" />
              <div className="flex-1 space-y-2">
                <Skeleton className="h-4 w-40" />
                <Skeleton className="h-3 w-28" />
              </div>
              <Skeleton className="h-4 w-20" />
            </div>
          ))}
        </div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="text-center py-12">
        <p className="text-destructive mb-4">{error}</p>
        <Button onClick={loadRequests}>Try Again</Button>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <PageHeader title="Requests" actions={newRequestButton} className="mb-0" />

      {requests.length === 0 ? (
        <div className="rounded-lg border border-border bg-card py-16 text-center">
          <Package className="w-10 h-10 mx-auto mb-4 text-muted-foreground" />
          <h3 className="text-xl font-heading font-bold mb-2">No requests yet</h3>
          <p className="text-muted-foreground mb-6 max-w-md mx-auto">
            Be the first to submit a 3D printing request.
          </p>
          {newRequestButton}
        </div>
      ) : (
        <>
          <div className="flex flex-col gap-3 lg:flex-row lg:items-center lg:justify-between">
            <Tabs value={filter} onValueChange={(v) => setFilter(v as RequestFilter)}>
              <TabsList className="h-auto flex-wrap justify-start">
                <TabsTrigger value="all" className="gap-1.5">
                  All <span className="text-xs opacity-70 tabular-nums">{requests.length}</span>
                </TabsTrigger>
                {MAIN_STAGES.map((stage) => (
                  <TabsTrigger key={stage} value={stage} className="gap-1.5">
                    {STAGE_LABELS[stage]}{' '}
                    <span className="text-xs opacity-70 tabular-nums">{getFilteredRequests(stage).length}</span>
                  </TabsTrigger>
                ))}
                {user && (
                  <TabsTrigger value="mine" className="gap-1.5">
                    Mine <span className="text-xs opacity-70 tabular-nums">{getFilteredRequests('mine').length}</span>
                  </TabsTrigger>
                )}
              </TabsList>
            </Tabs>
            <div className="relative w-full lg:w-64">
              <Search aria-hidden="true" className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
              <Input
                type="search"
                aria-label="Search requests"
                placeholder="Search requests"
                value={query}
                onChange={(e) => setQuery(e.target.value)}
                className="bg-card pl-9"
              />
            </div>
          </div>

          <section className="overflow-hidden rounded-lg border border-border bg-card">
            <div className={`hidden items-center border-b border-border bg-muted/50 px-4 py-3 text-xs font-semibold uppercase tracking-wide text-muted-foreground ${ROW_GRID}`}>
              <span />
              <span>Model and requester</span>
              <span>Filament</span>
              <span>Created</span>
              <span>Stage</span>
            </div>

            {visibleRequests.length === 0 ? (
              <div className="py-12 text-center">
                <h3 className="text-lg font-heading font-bold mb-1">No requests found</h3>
                <p className="text-muted-foreground">No requests match this filter.</p>
              </div>
            ) : (
              visibleRequests.map((request) => (
                <Link
                  key={request.id}
                  to={`/requests/${request.id}`}
                  className={`flex items-center gap-3 border-b border-border px-4 py-2.5 transition-colors last:border-b-0 hover:bg-muted/50 focus-visible:bg-muted/50 focus-visible:outline-none ${ROW_GRID}`}
                >
                  <ModelThumbnail
                    modelUrl={request.modelUrl}
                    size={56}
                    showPlatform={false}
                    className="flex-shrink-0 overflow-hidden rounded-md"
                  />
                  <div className="min-w-0 flex-1">
                    <div className="flex items-center justify-between gap-3">
                      <div className="truncate font-heading text-base font-bold">
                        {request.requesterName}
                        {user && request.userId === user.id && (
                          <span className="ml-2 text-xs font-semibold text-primary">yours</span>
                        )}
                      </div>
                      {/* Mobile: badge sits on the title line so the meta line gets the full width. */}
                      <StatusBadge status={request.currentStatus} className="flex-shrink-0 md:hidden" />
                    </div>
                    <div className="truncate text-sm text-muted-foreground">
                      <span className="tabular-nums">#{request.id.slice(0, 8)}</span>
                      {' · '}
                      {getModelHost(request.modelUrl)}
                      {request.requestDelivery && ' · delivery'}
                      {/* Filament and date columns are hidden on mobile, so fold them into the meta line. */}
                      <span className="md:hidden">
                        {request.filamentName && ` · ${request.filamentName}`}
                        {` · ${formatCompactTime(request.createdAt)}`}
                      </span>
                    </div>
                  </div>
                  <span className="hidden truncate text-sm md:block">
                    {request.filamentName ?? <span className="text-muted-foreground">Not picked</span>}
                  </span>
                  <span className="hidden truncate text-sm text-muted-foreground md:block">
                    {formatCompactTime(request.createdAt)}
                  </span>
                  <StatusBadge status={request.currentStatus} className="hidden md:inline-flex" />
                </Link>
              ))
            )}
          </section>
        </>
      )}
    </div>
  );
};
