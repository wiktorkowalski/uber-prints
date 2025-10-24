import { useEffect, useState, useCallback } from 'react';
import { VideoPlayer } from '../components/VideoPlayer';
import { api } from '../lib/api';
import { useAuth } from '../contexts/AuthContext';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '../components/ui/card';
import { Alert, AlertDescription } from '../components/ui/alert';
import { Badge } from '../components/ui/badge';
import { Button } from '../components/ui/button';
import { Separator } from '../components/ui/separator';
import { AlertCircle, Users, Clock, Power, Camera } from 'lucide-react';
import { useToast } from '../hooks/use-toast';

interface StreamStatus {
  isEnabled: boolean;
  isActive: boolean;
  viewerCount: number;
  uptime: string | null;
  lastError: string | null;
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

  // Join stream on mount, leave on unmount
  useEffect(() => {
    joinStream();
    return () => {
      leaveStream();
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
      } catch (err) {
        console.error('Failed to send heartbeat:', err);
      }
    }, 10000);

    return () => clearInterval(interval);
  }, [hasJoined, viewerId, fetchStatus]);

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
          <Button
            onClick={handleToggleStreaming}
            disabled={isTogglingStream}
            variant={status?.isEnabled ? 'outline' : 'default'}
          >
            <Power className="w-4 h-4 mr-2" />
            {status?.isEnabled ? 'Disable Stream' : 'Enable Stream'}
          </Button>
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
