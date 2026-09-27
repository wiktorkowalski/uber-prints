import { useCallback, useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { Video } from 'lucide-react';
import { api } from '../lib/api';
import { PrinterStateEnum, PrinterStatusDto, PrintRequestDto } from '../types/api';
import { Button } from '../components/ui/button';
import { Skeleton } from '../components/ui/skeleton';
import { StatusBadge } from '../components/StatusBadge';
import { LayerStack } from '../components/LayerStack';
import { getRequestStage, RequestStage } from '../lib/requestStage';
import { cn, getModelHost } from '../lib/utils';

const POLL_INTERVAL_MS = 10000;
const QUEUE_SIZE = 5;

// Queue order: what is on the printer first, then what waits for it, then what waits for pickup.
const QUEUE_STAGE_ORDER: Partial<Record<RequestStage, number>> = { printing: 0, waiting: 1, pickup: 2 };

type StreamStatus = Awaited<ReturnType<typeof api.getStreamStatus>>;

const STATE_TEXT_CLASSES: Partial<Record<PrinterStateEnum, string>> = {
  [PrinterStateEnum.Printing]: 'text-stage-printing',
  [PrinterStateEnum.Paused]: 'text-stage-waiting',
  [PrinterStateEnum.Attention]: 'text-stage-waiting',
  [PrinterStateEnum.Error]: 'text-stage-rejected',
  [PrinterStateEnum.Stopped]: 'text-stage-rejected',
};

function formatDuration(seconds: number): string {
  const hours = Math.floor(seconds / 3600);
  const minutes = Math.floor((seconds % 3600) / 60);
  return hours > 0 ? `${hours} h ${minutes} min` : `${minutes} min`;
}

function formatTemp(value?: number): string {
  return value == null ? '--' : `${Math.round(value)} °C`;
}

export const Home = () => {
  const [printer, setPrinter] = useState<PrinterStatusDto | null>(null);
  const [printerLoading, setPrinterLoading] = useState(true);
  const [stream, setStream] = useState<StreamStatus | null>(null);
  const [queue, setQueue] = useState<PrintRequestDto[]>([]);
  const [queueLoading, setQueueLoading] = useState(true);
  const [queueError, setQueueError] = useState(false);

  const fetchPrinterStatus = useCallback(async () => {
    try {
      setPrinter(await api.getPrinterStatus());
    } catch (err) {
      console.error('Failed to fetch printer status:', err);
      setPrinter(null);
    } finally {
      setPrinterLoading(false);
    }
  }, []);

  const fetchStreamStatus = useCallback(async () => {
    try {
      setStream(await api.getStreamStatus());
    } catch (err) {
      console.error('Failed to fetch stream status:', err);
      setStream(null);
    }
  }, []);

  useEffect(() => {
    fetchPrinterStatus();
    fetchStreamStatus();
    const interval = setInterval(() => {
      fetchPrinterStatus();
      fetchStreamStatus();
    }, POLL_INTERVAL_MS);
    return () => clearInterval(interval);
  }, [fetchPrinterStatus, fetchStreamStatus]);

  useEffect(() => {
    const loadQueue = async () => {
      try {
        const requests = await api.getRequests();
        setQueue(
          requests
            .filter(r => QUEUE_STAGE_ORDER[getRequestStage(r.currentStatus)] !== undefined)
            .sort((a, b) =>
              QUEUE_STAGE_ORDER[getRequestStage(a.currentStatus)]! - QUEUE_STAGE_ORDER[getRequestStage(b.currentStatus)]!
              || new Date(a.createdAt).getTime() - new Date(b.createdAt).getTime()
            )
            .slice(0, QUEUE_SIZE)
        );
      } catch (err) {
        console.error('Failed to load requests:', err);
        setQueueError(true);
      } finally {
        setQueueLoading(false);
      }
    };
    loadQueue();
  }, []);

  return (
    <div className="space-y-6">
      {/* Printer panel */}
      <section className="grid overflow-hidden rounded-lg border border-border bg-card md:grid-cols-[minmax(0,1.35fr)_minmax(0,1fr)]">
        <CameraPreview stream={stream} />
        <div className="p-6 lg:p-7">
          {printerLoading ? (
            <div className="space-y-4">
              <Skeleton className="h-6 w-40" />
              <Skeleton className="h-36 w-full" />
              <Skeleton className="h-16 w-full" />
            </div>
          ) : (
            <PrinterSummary printer={printer} />
          )}
        </div>
      </section>

      <div className="grid items-start gap-6 md:grid-cols-[minmax(0,1.35fr)_minmax(0,1fr)]">
        {/* Up next */}
        <section className="overflow-hidden rounded-lg border border-border bg-card">
          <div className="flex items-baseline justify-between border-b border-border px-5 py-4">
            <h2 className="font-heading text-lg font-bold">Up next</h2>
            <Link to="/requests" className="text-sm font-semibold text-primary hover:underline">
              All requests
            </Link>
          </div>
          {queueLoading ? (
            <div className="space-y-3 p-5">
              {[1, 2, 3].map(i => <Skeleton key={i} className="h-10 w-full" />)}
            </div>
          ) : queueError ? (
            <p className="px-5 py-6 text-sm text-destructive">Failed to load requests.</p>
          ) : queue.length === 0 ? (
            <p className="px-5 py-6 text-sm text-muted-foreground">Nothing in the queue.</p>
          ) : (
            <ol>
              {queue.map((request, index) => (
                <li key={request.id} className="border-b border-border last:border-b-0">
                  <Link
                    to={`/requests/${request.id}`}
                    className="grid grid-cols-[28px_minmax(0,1fr)_auto] items-center gap-3.5 px-5 py-3 transition-colors hover:bg-muted/50"
                  >
                    <span className="font-heading font-bold text-muted-foreground tabular-nums">{index + 1}</span>
                    <div className="min-w-0">
                      <div className="truncate font-semibold">{request.requesterName}</div>
                      <div className="truncate text-sm text-muted-foreground">
                        {getModelHost(request.modelUrl)}
                        {` · ${request.filamentName ?? 'no filament picked'}`}
                        {request.requestDelivery && ' · delivery'}
                      </div>
                    </div>
                    <StatusBadge status={request.currentStatus} />
                  </Link>
                </li>
              ))}
            </ol>
          )}
        </section>

        {/* Request a print */}
        <section className="space-y-3 rounded-lg border border-border bg-card p-6">
          <h2 className="font-heading text-lg font-bold">Request a print</h2>
          <p className="text-muted-foreground">
            Paste a model link, pick a filament, and we print it. No account needed.
          </p>
          <Button asChild className="w-full">
            <Link to="/requests/new">New request</Link>
          </Button>
        </section>
      </div>
    </div>
  );
};

function CameraPreview({ stream }: { stream: StreamStatus | null }) {
  const isLive = !!stream?.isActive;

  // Fixed dark ground in both themes: this box stands in for a video frame.
  return (
    <div className="relative flex aspect-video items-center justify-center bg-zinc-950 text-zinc-400">
      {isLive && (
        <span className="absolute left-4 top-4 rounded bg-stage-rejected px-2.5 py-1 text-xs font-semibold text-white">
          LIVE
        </span>
      )}
      <Button asChild variant="secondary">
        <Link to="/live-view">
          <Video className="mr-2 h-4 w-4" />
          Watch live
        </Link>
      </Button>
      {isLive && stream.viewerCount > 0 && (
        <span className="absolute bottom-3.5 right-4 text-sm text-zinc-300 tabular-nums">
          {stream.viewerCount} watching
        </span>
      )}
    </div>
  );
}

function PrinterSummary({ printer }: { printer: PrinterStatusDto | null }) {
  if (!printer) {
    return (
      <div className="space-y-2">
        <h2 className="font-heading text-lg font-bold">Printer</h2>
        <p className="text-muted-foreground">Printer status is not available right now.</p>
      </div>
    );
  }

  const offline = !printer.isAvailable;
  const isActive = !offline && (
    printer.currentState === PrinterStateEnum.Printing || printer.currentState === PrinterStateEnum.Paused
  );
  const progress = printer.printProgress ?? 0;
  const stateLabel = offline ? 'Offline' : printer.currentState;
  const stateClass = offline ? 'text-muted-foreground' : STATE_TEXT_CLASSES[printer.currentState] ?? 'text-muted-foreground';

  return (
    <div className="flex h-full flex-col gap-5">
      <div className="flex items-center justify-between gap-3">
        <h2 className="min-w-0 truncate font-heading text-lg font-bold">{printer.name}</h2>
        <span className={cn('inline-flex items-center gap-1.5 whitespace-nowrap text-sm font-semibold', stateClass)}>
          <span aria-hidden="true" className="h-[9px] w-[9px] rounded-full bg-current" />
          {stateLabel}
        </span>
      </div>

      {isActive ? (
        <div className="flex items-end gap-5">
          <LayerStack progress={progress} className="h-[150px] w-[60px] flex-shrink-0" />
          <div className="min-w-0 space-y-1.5">
            <div className="font-heading text-5xl font-extrabold leading-none tabular-nums">
              {printer.axisZ != null ? `${printer.axisZ.toFixed(1)} mm` : `${Math.round(progress)}%`}
            </div>
            <div className="text-muted-foreground tabular-nums">
              {printer.axisZ != null && <>{Math.round(progress)}% printed<br /></>}
              {printer.timeRemaining ? `${formatDuration(printer.timeRemaining)} left` : 'Time left unknown'}
            </div>
          </div>
        </div>
      ) : (
        <p className="text-muted-foreground">{offline ? 'Printer is offline' : 'Printer is idle'}</p>
      )}

      <div className="grid grid-cols-2 gap-2.5">
        <TempBox label="Nozzle" value={printer.nozzleTemperature} />
        <TempBox label="Bed" value={printer.bedTemperature} />
      </div>

      {isActive && printer.currentFileName && (
        <p className="truncate text-sm text-muted-foreground">
          Now printing: <span className="font-medium text-foreground">{printer.currentFileName}</span>
        </p>
      )}
    </div>
  );
}

function TempBox({ label, value }: { label: string; value?: number }) {
  return (
    <div className="rounded-md border border-border px-3 py-2.5 text-sm text-muted-foreground">
      {label}
      <div className="font-heading text-2xl font-bold text-foreground tabular-nums">{formatTemp(value)}</div>
    </div>
  );
}
