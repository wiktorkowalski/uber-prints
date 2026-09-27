import { Link } from 'react-router-dom';
import { PrintRequestDto } from '../types/api';
import { formatCompactTime, getModelHost } from '../lib/utils';
import { ModelThumbnail } from './ModelThumbnail';
import { StatusBadge } from './StatusBadge';
import { Skeleton } from './ui/skeleton';

// Desktop column template: thumbnail, model and requester, filament, created, stage.
const ROW_GRID = 'md:grid md:grid-cols-[56px_minmax(0,2fr)_minmax(0,1.3fr)_130px_170px] md:gap-4';

export function RequestListHeader() {
  return (
    <div className={`hidden items-center border-b border-border bg-muted/50 px-4 py-3 text-[13px] font-medium text-muted-foreground ${ROW_GRID}`}>
      <span />
      <span>Model and requester</span>
      <span>Filament</span>
      <span>Created</span>
      <span>Stage</span>
    </div>
  );
}

interface RequestRowProps {
  request: PrintRequestDto;
  /** Shows the "yours" tag next to the requester name. */
  isOwn?: boolean;
}

export function RequestRow({ request, isOwn = false }: RequestRowProps) {
  return (
    <Link
      to={`/requests/${request.id}`}
      data-testid="request-row"
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
            {isOwn && (
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
  );
}

export function RequestRowsSkeleton({ rows = 5 }: { rows?: number }) {
  return (
    <div className="rounded-lg border border-border bg-card">
      {Array.from({ length: rows }, (_, i) => (
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
  );
}
