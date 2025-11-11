import { useEffect, useState, useCallback } from 'react';
import { VideoPlayer } from '../components/VideoPlayer';
import { api } from '../lib/api';
import { useAuth } from '../contexts/AuthContext';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '../components/ui/card';
import { Alert, AlertDescription } from '../components/ui/alert';
import { Badge } from '../components/ui/badge';
import { Button } from '../components/ui/button';
import { Separator } from '../components/ui/separator';
import { AlertCircle, Users, Clock, Power, Camera, Database, Trash2, Scissors, Settings, RotateCw } from 'lucide-react';
import { useToast } from '../hooks/use-toast';
import { Input } from '../components/ui/input';
import { Label } from '../components/ui/label';
import { PrinterStatusCard } from '../components/PrinterStatusCard';
import { PrinterStatusDto } from '../types/api';

interface StreamStatus {
  isEnabled: boolean;
  isActive: boolean;
  viewerCount: number;
  uptime: string | null;
  lastError: string | null;
}

interface BufferDiagnostics {
  bufferSizeBytes: number;
  bufferSizeMB: number;
  tsFileCount: number;
  m3u8FileCount: number;
  totalFileCount: number;
  isStreamActive: boolean;
  outputPath: string;
  bufferDurationMinutes: number;
  error?: string;
}

export const LiveView = () => {
  const { user } = useAuth();
  const { toast } = useToast();
  const [status, setStatus] = useState<StreamStatus | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [hasJoined, setHasJoined] = useState(false);
  const [viewerId, setViewerId] = useState<string | null>(null);
  const [isTogglingStream, setIsTogglingStream] = useState(false);
  const [isStreamReady, setIsStreamReady] = useState(false);
  const [bufferDiagnostics, setBufferDiagnostics] = useState<BufferDiagnostics | null>(null);
  const [isLoadingBuffer, setIsLoadingBuffer] = useState(false);
  const [isResettingBuffer, setIsResettingBuffer] = useState(false);
  const [isTrimmingBuffer, setIsTrimmingBuffer] = useState(false);
  const [bufferDurationInput, setBufferDurationInput] = useState<string>('30');
  const [isUpdatingConfig, setIsUpdatingConfig] = useState(false);
  const [isRestarting, setIsRestarting] = useState(false);
  const [printerStatus, setPrinterStatus] = useState<PrinterStatusDto | null>(null);

  const isAdmin = user?.isAdmin ?? false;
  const streamUrl = '/stream/playlist.m3u8';

  // Fetch stream status
  const fetchStatus = useCallback(async () => {
    try {
      const data = await api.getStreamStatus();
      setStatus(data);
      setError(null);
    } catch (err) {
      console.error('Failed to fetch stream status:', err);
      setError('Failed to load stream status');
    }
  }, []);

  // Fetch printer status
  const fetchPrinterStatus = useCallback(async () => {
    try {
      const data = await api.getPrinterStatus();
      setPrinterStatus(data);
    } catch (err) {
      console.error('Failed to fetch printer status:', err);
    }
  }, []);

  // Join stream (called on mount)
  const joinStream = useCallback(async () => {
    try {
      const response = await api.joinStream();

      if (!response.success) {
        setError(response.message || 'Failed to join stream');
        setIsLoading(false);
        return;
      }

      // Store viewer ID for heartbeat and leaving
      if (response.viewerId) {
        setViewerId(response.viewerId);
      }

      setHasJoined(true);
      await fetchStatus();
      setIsLoading(false);
    } catch (err) {
      console.error('Failed to join stream:', err);
      setError('Failed to connect to stream');
      setIsLoading(false);
    }
  }, [fetchStatus]);

  // Leave stream (called on unmount)
  const leaveStream = useCallback(async () => {
    if (!hasJoined || !viewerId) return;

    try {
      await api.leaveStream(viewerId);
    } catch (err) {
      console.error('Failed to leave stream:', err);
    }
  }, [hasJoined, viewerId]);

  // Fetch buffer diagnostics (admin only)
  const fetchBufferDiagnostics = useCallback(async () => {
    if (!isAdmin) return;

    setIsLoadingBuffer(true);
    try {
      const data = await api.getBufferDiagnostics();
      setBufferDiagnostics(data);
      // Update input field with current config
      setBufferDurationInput(data.bufferDurationMinutes.toString());
    } catch (err) {
      console.error('Failed to fetch buffer diagnostics:', err);
      toast({
        title: 'Error',
        description: 'Failed to load buffer diagnostics',
        variant: 'destructive',
      });
    } finally {
      setIsLoadingBuffer(false);
    }
  }, [isAdmin, toast]);

  // Toggle streaming (admin only)
  const handleToggleStreaming = async () => {
    setIsTogglingStream(true);
    try {
      const response = await api.toggleStreaming();
      toast({
        title: response.isEnabled ? 'Streaming Enabled' : 'Streaming Disabled',
        description: response.message,
      });
      await fetchStatus();
    } catch (err) {
      toast({
        title: 'Error',
        description: 'Failed to toggle streaming',
        variant: 'destructive',
      });
    } finally {
      setIsTogglingStream(false);
    }
  };

  // Reset buffer (admin only)
  const handleResetBuffer = async () => {
    setIsResettingBuffer(true);
    try {
      const response = await api.resetBuffer();
      toast({
        title: 'Buffer Reset',
        description: response.message,
      });
      await fetchBufferDiagnostics();
    } catch (err) {
      toast({
        title: 'Error',
        description: 'Failed to reset buffer',
        variant: 'destructive',
      });
    } finally {
      setIsResettingBuffer(false);
    }
  };

  // Trim buffer (admin only)
  const handleTrimBuffer = async () => {
    setIsTrimmingBuffer(true);
    try {
      const durationMinutes = bufferDiagnostics?.bufferDurationMinutes || 30;
      const response = await api.trimBuffer(durationMinutes);
      toast({
        title: 'Buffer Trimmed',
        description: `Deleted ${response.deletedCount} files (${(response.deletedSize / (1024 * 1024)).toFixed(2)} MB)`,
      });
      await fetchBufferDiagnostics();
    } catch (err) {
      toast({
        title: 'Error',
        description: 'Failed to trim buffer',
        variant: 'destructive',
      });
    } finally {
      setIsTrimmingBuffer(false);
    }
  };

  // Update buffer configuration (admin only)
  const handleUpdateBufferConfig = async () => {
    const durationMinutes = parseInt(bufferDurationInput);

    if (isNaN(durationMinutes) || durationMinutes < 5 || durationMinutes > 240) {
      toast({
        title: 'Invalid Duration',
        description: 'Buffer duration must be between 5 and 240 minutes',
        variant: 'destructive',
      });
      return;
    }

    setIsUpdatingConfig(true);
    try {
      const response = await api.updateBufferConfig(durationMinutes);
      toast({
        title: 'Configuration Updated',
        description: response.message,
        variant: response.requiresRestart ? 'default' : 'default',
      });
      await fetchBufferDiagnostics();
    } catch (err) {
      toast({
        title: 'Error',
        description: 'Failed to update buffer configuration',
        variant: 'destructive',
      });
    } finally {
      setIsUpdatingConfig(false);
    }
  };

  // Restart stream (admin only)
  const handleRestartStream = async () => {
    setIsRestarting(true);
    try {
      const response = await api.restartStreaming();
      toast({
        title: response.success ? 'Stream Restarted' : 'Restart Failed',
        description: response.message,
        variant: response.success ? 'default' : 'destructive',
      });
      await fetchStatus();
      if (isAdmin) {
        await fetchBufferDiagnostics();
      }
    } catch (err) {
      toast({
        title: 'Error',
        description: 'Failed to restart stream',
        variant: 'destructive',
      });
    } finally {
      setIsRestarting(false);
    }
  };

  // Join stream on mount, leave on unmount
  useEffect(() => {
    joinStream();
    fetchPrinterStatus();

    // Poll printer status every 10 seconds
    const printerInterval = setInterval(fetchPrinterStatus, 10000);

    return () => {
      leaveStream();
      clearInterval(printerInterval);
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []); // Empty deps array ensures this only runs once on mount/unmount

  // Check if playlist is ready
  const checkPlaylistReady = useCallback(async (): Promise<boolean> => {
    try {
      const response = await fetch(streamUrl, { method: 'HEAD' });
      return response.ok;
    } catch {
      return false;
    }
  }, [streamUrl]);

  // Poll for playlist availability when stream becomes active
  useEffect(() => {
    if (!status?.isActive || isStreamReady) return;

    let attempts = 0;
    const maxAttempts = 10;

    const pollInterval = setInterval(async () => {
      attempts++;
      const ready = await checkPlaylistReady();

      if (ready) {
        console.log('Stream playlist is ready');
        setIsStreamReady(true);
        clearInterval(pollInterval);
      } else if (attempts >= maxAttempts) {
        console.warn('Stream playlist not ready after maximum attempts');
        clearInterval(pollInterval);
        // Still set ready to true to show the player (it has retry logic)
        setIsStreamReady(true);
      }
    }, 1000); // Check every second

    return () => clearInterval(pollInterval);
  }, [status?.isActive, isStreamReady, checkPlaylistReady]);

  // Reset stream ready state when stream stops
  useEffect(() => {
    if (!status?.isActive) {
      setIsStreamReady(false);
    }
  }, [status?.isActive]);

  // Send heartbeat every 10 seconds and poll status
  useEffect(() => {
    if (!hasJoined || !viewerId) return;

    const interval = setInterval(async () => {
      try {
        // Send heartbeat to keep session alive
        await api.sendHeartbeat(viewerId);
        // Also update status
        await fetchStatus();
        // Update buffer diagnostics for admin
        if (isAdmin) {
          await fetchBufferDiagnostics();
        }
      } catch (err) {
        console.error('Failed to send heartbeat:', err);
      }
    }, 10000);

    return () => clearInterval(interval);
  }, [hasJoined, viewerId, fetchStatus, isAdmin, fetchBufferDiagnostics]);

  // Initial buffer diagnostics fetch for admin
  useEffect(() => {
    if (isAdmin && hasJoined) {
      fetchBufferDiagnostics();
    }
  }, [isAdmin, hasJoined, fetchBufferDiagnostics]);

  const formatUptime = (uptime: string | null) => {
    if (!uptime) return 'N/A';

    try {
      const match = uptime.match(/(\d+):(\d+):(\d+)/);
      if (match) {
        const [, hours, minutes, seconds] = match;
        return `${hours}h ${minutes}m ${seconds}s`;
      }
      return uptime;
    } catch {
      return uptime;
    }
  };

  if (isLoading) {
    return (
      <div className="container mx-auto px-4 py-8">
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <Camera className="w-6 h-6" />
              Printer Live View
            </CardTitle>
          </CardHeader>
          <CardContent>
            <div className="flex items-center justify-center h-64">
              <p className="text-muted-foreground">Loading stream...</p>
            </div>
          </CardContent>
        </Card>
      </div>
    );
  }

  return (
    <div className="container mx-auto px-4 py-8 space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold flex items-center gap-2">
            <Camera className="w-8 h-8" />
            Printer Live View
          </h1>
          <p className="text-muted-foreground mt-1">
            Watch the 3D printer in action
          </p>
        </div>

        {isAdmin && (
          <div className="flex gap-2">
            <Button
              onClick={handleRestartStream}
              disabled={isRestarting || !status?.isEnabled || isTogglingStream}
              variant="outline"
              title="Restart the stream (applies new buffer configuration)"
            >
              <RotateCw className={`w-4 h-4 mr-2 ${isRestarting ? 'animate-spin' : ''}`} />
              {isRestarting ? 'Restarting...' : 'Restart'}
            </Button>
            <Button
              onClick={handleToggleStreaming}
              disabled={isTogglingStream || isRestarting}
              variant={status?.isEnabled ? 'outline' : 'default'}
            >
              <Power className="w-4 h-4 mr-2" />
              {status?.isEnabled ? 'Disable Stream' : 'Enable Stream'}
            </Button>
          </div>
        )}
      </div>

      <Separator />

      {/* Status Bar */}
      {status && (
        <div className="flex flex-wrap gap-4">
          <div className="flex items-center gap-2">
            <span className="text-sm font-medium">Status:</span>
            <Badge variant={status.isEnabled && status.isActive ? 'default' : 'secondary'}>
              {!status.isEnabled ? 'Disabled' : status.isActive ? 'Live' : 'Offline'}
            </Badge>
          </div>

          <div className="flex items-center gap-2">
            <Users className="w-4 h-4 text-muted-foreground" />
            <span className="text-sm">
              {status.viewerCount} {status.viewerCount === 1 ? 'viewer' : 'viewers'}
            </span>
          </div>

          {status.isActive && status.uptime && (
            <div className="flex items-center gap-2">
              <Clock className="w-4 h-4 text-muted-foreground" />
              <span className="text-sm">Uptime: {formatUptime(status.uptime)}</span>
            </div>
          )}
        </div>
      )}

      {/* Error/Disabled Messages */}
      {error && (
        <Alert variant="destructive">
          <AlertCircle className="h-4 w-4" />
          <AlertDescription>{error}</AlertDescription>
        </Alert>
      )}

      {status && !status.isEnabled && (
        <Alert>
          <AlertCircle className="h-4 w-4" />
          <AlertDescription>
            The live stream is currently disabled by an administrator.
          </AlertDescription>
        </Alert>
      )}

      {status && status.isEnabled && status.isActive && !isStreamReady && !error && (
        <Alert>
          <AlertCircle className="h-4 w-4" />
          <AlertDescription>
            Stream is starting, waiting for video feed... This usually takes a few seconds.
          </AlertDescription>
        </Alert>
      )}

      {status && status.isEnabled && !status.isActive && !error && (
        <Alert>
          <AlertCircle className="h-4 w-4" />
          <AlertDescription>
            The camera is currently offline. The stream will start automatically when the printer is available.
          </AlertDescription>
        </Alert>
      )}

      {status?.lastError && (
        <Alert variant="destructive">
          <AlertCircle className="h-4 w-4" />
          <AlertDescription>Stream error: {status.lastError}</AlertDescription>
        </Alert>
      )}

      {/* Video Player */}
      {status?.isEnabled && status?.isActive && isStreamReady && !error && (
        <>
          <Card>
            <CardHeader>
              <CardTitle>Live Feed</CardTitle>
              <CardDescription>
                Real-time view from the 3D printer camera
              </CardDescription>
            </CardHeader>
            <CardContent>
              <div className="aspect-video bg-black rounded-lg overflow-hidden">
                <VideoPlayer
                  streamUrl={streamUrl}
                  onError={(err) => {
                    console.error('Video player error:', err);
                    setError('Failed to load video stream. Please try refreshing the page.');
                  }}
                  onReady={() => {
                    console.log('Video player ready');
                  }}
                />
              </div>
              <p className="text-sm text-muted-foreground mt-4">
                The video may take a few seconds to load. If you experience issues, try refreshing the page.
              </p>
            </CardContent>
          </Card>

          {/* Printer Status */}
          {printerStatus && (
            <PrinterStatusCard status={printerStatus} />
          )}
        </>
      )}

      {/* Admin Debug Section */}
      {isAdmin && (
        <Card className="border-amber-500/50 bg-amber-50/5">
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <Database className="w-5 h-5" />
              Buffer Diagnostics (Admin Only)
            </CardTitle>
            <CardDescription>
              Monitor and manage the streaming buffer
            </CardDescription>
          </CardHeader>
          <CardContent className="space-y-4">
            {isLoadingBuffer && !bufferDiagnostics ? (
              <p className="text-sm text-muted-foreground">Loading diagnostics...</p>
            ) : bufferDiagnostics?.error ? (
              <Alert variant="destructive">
                <AlertCircle className="h-4 w-4" />
                <AlertDescription>{bufferDiagnostics.error}</AlertDescription>
              </Alert>
            ) : bufferDiagnostics ? (
              <>
                <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
                  <div className="space-y-1">
                    <p className="text-sm font-medium text-muted-foreground">Buffer Size</p>
                    <p className="text-2xl font-bold">
                      {bufferDiagnostics.bufferSizeMB.toFixed(2)} MB
                    </p>
                    <p className="text-xs text-muted-foreground">
                      {bufferDiagnostics.bufferSizeBytes.toLocaleString()} bytes
                    </p>
                  </div>
                  <div className="space-y-1">
                    <p className="text-sm font-medium text-muted-foreground">Video Segments</p>
                    <p className="text-2xl font-bold">{bufferDiagnostics.tsFileCount}</p>
                    <p className="text-xs text-muted-foreground">.ts files</p>
                  </div>
                  <div className="space-y-1">
                    <p className="text-sm font-medium text-muted-foreground">Playlists</p>
                    <p className="text-2xl font-bold">{bufferDiagnostics.m3u8FileCount}</p>
                    <p className="text-xs text-muted-foreground">.m3u8 files</p>
                  </div>
                  <div className="space-y-1">
                    <p className="text-sm font-medium text-muted-foreground">Total Files</p>
                    <p className="text-2xl font-bold">{bufferDiagnostics.totalFileCount}</p>
                    <p className="text-xs text-muted-foreground">
                      {bufferDiagnostics.isStreamActive ? 'Stream active' : 'Stream inactive'}
                    </p>
                  </div>
                </div>

                {bufferDiagnostics.bufferSizeMB > 1000 && (
                  <Alert variant="destructive">
                    <AlertCircle className="h-4 w-4" />
                    <AlertDescription>
                      Warning: Buffer size exceeds 1 GB. Consider trimming or resetting the buffer.
                    </AlertDescription>
                  </Alert>
                )}

                <Separator />

                {/* Buffer Duration Configuration */}
                <div className="space-y-3">
                  <div className="flex items-center gap-2">
                    <Settings className="w-4 h-4 text-muted-foreground" />
                    <h4 className="text-sm font-semibold">Buffer Duration Configuration</h4>
                  </div>
                  <div className="flex items-end gap-2">
                    <div className="flex-1 max-w-xs space-y-2">
                      <Label htmlFor="buffer-duration" className="text-sm">
                        Buffer Duration (minutes)
                      </Label>
                      <Input
                        id="buffer-duration"
                        type="number"
                        min="5"
                        max="240"
                        value={bufferDurationInput}
                        onChange={(e) => setBufferDurationInput(e.target.value)}
                        placeholder="30"
                        className="w-full"
                      />
                      <p className="text-xs text-muted-foreground">
                        Range: 5-240 minutes. Current: {bufferDiagnostics.bufferDurationMinutes} min
                      </p>
                    </div>
                    <Button
                      onClick={handleUpdateBufferConfig}
                      disabled={isUpdatingConfig || bufferDurationInput === bufferDiagnostics.bufferDurationMinutes.toString()}
                      size="sm"
                    >
                      {isUpdatingConfig ? 'Updating...' : 'Update Config'}
                    </Button>
                  </div>
                  {bufferDiagnostics.isStreamActive && (
                    <Alert>
                      <AlertCircle className="h-4 w-4" />
                      <AlertDescription>
                        Stream is currently active. Changes will take effect after restarting the stream.
                      </AlertDescription>
                    </Alert>
                  )}
                </div>

                <Separator />

                {/* Buffer Management Actions */}
                <div className="space-y-3">
                  <h4 className="text-sm font-semibold">Buffer Management</h4>
                  <div className="flex flex-wrap gap-2">
                    <Button
                      onClick={handleTrimBuffer}
                      disabled={isTrimmingBuffer || isResettingBuffer}
                      variant="outline"
                      size="sm"
                    >
                      <Scissors className="w-4 h-4 mr-2" />
                      {isTrimmingBuffer ? 'Trimming...' : `Trim to ${bufferDiagnostics.bufferDurationMinutes} Min`}
                    </Button>
                    <Button
                      onClick={handleResetBuffer}
                      disabled={isTrimmingBuffer || isResettingBuffer}
                      variant="outline"
                      size="sm"
                    >
                      <Trash2 className="w-4 h-4 mr-2" />
                      {isResettingBuffer ? 'Resetting...' : 'Reset Buffer'}
                    </Button>
                    <Button
                      onClick={fetchBufferDiagnostics}
                      disabled={isLoadingBuffer}
                      variant="ghost"
                      size="sm"
                    >
                      {isLoadingBuffer ? 'Refreshing...' : 'Refresh'}
                    </Button>
                  </div>
                  <div className="text-xs text-muted-foreground">
                    <p>Output path: {bufferDiagnostics.outputPath}</p>
                    <p className="mt-1">
                      <strong>Trim:</strong> Removes segments older than configured duration. <strong>Reset:</strong> Removes all segments.
                    </p>
                  </div>
                </div>
              </>
            ) : null}
          </CardContent>
        </Card>
      )}

      {/* Info Card */}
      <Card>
        <CardHeader>
          <CardTitle>About the Live View</CardTitle>
        </CardHeader>
        <CardContent className="space-y-2 text-sm text-muted-foreground">
          <p>
            This page shows a live view of the 3D printer in action. The stream starts automatically when
            someone is viewing and stops when no one is watching to save bandwidth.
          </p>
          <p>
            Expected latency: 5-15 seconds. The video quality adjusts automatically based on your connection.
          </p>
          <p>
            You can rewind up to 30 minutes using the video timeline to see what happened earlier in the print.
          </p>
        </CardContent>
      </Card>
    </div>
  );
};
