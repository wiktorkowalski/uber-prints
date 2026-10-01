import { useEffect, useRef, useState } from 'react';
import { Loader2 } from 'lucide-react';
import videojs from 'video.js';
import 'video.js/dist/video-js.css';
import { useLatestRef } from '../hooks/use-latest-ref';
import type Player from 'video.js/dist/types/player';

interface VideoPlayerProps {
  streamUrl: string;
  onError?: (error: Error) => void;
  onReady?: () => void;
}

/** Placeholder for the player area while the camera stream starts. */
export function CameraStarting() {
  return (
    <div className="flex h-full w-full flex-col items-center justify-center gap-3 bg-muted text-muted-foreground">
      <Loader2 className="h-8 w-8 animate-spin text-primary" />
      <p className="font-heading font-bold text-foreground">Starting camera…</p>
      <p className="text-sm">This usually takes about 10 seconds.</p>
    </div>
  );
}

/**
 * HLS Video Player component using Video.js
 */
export function VideoPlayer({ streamUrl, onError, onReady }: VideoPlayerProps) {
  const videoRef = useRef<HTMLVideoElement>(null);
  const playerRef = useRef<Player | null>(null);
  // True once the first frame is decoded; until then the starting overlay covers the player.
  const [hasFrame, setHasFrame] = useState(false);
  // The init effect runs once; retries read the current URL instead of the one at init.
  const streamUrlRef = useLatestRef(streamUrl);

  useEffect(() => {
    // Make sure Video.js player is only initialized once
    if (!playerRef.current && videoRef.current) {
      const videoElement = videoRef.current;

      const player = videojs(videoElement, {
        controls: true,
        // Browsers only allow autoplay without a click when the video is muted.
        autoplay: 'muted',
        muted: true,
        preload: 'auto',
        fluid: true,
        liveui: true,
        responsive: true,
        html5: {
          vhs: {
            // Video.js HLS settings optimized for live streaming
            enableLowInitialPlaylist: true,
            smoothQualityChange: true,
            overrideNative: true,
            // Handle live edge better
            useBandwidthFromLocalStorage: true,
            // Reduce buffering for lower latency
            experimentalBufferBasedABR: true,
          },
          nativeAudioTracks: false,
          nativeVideoTracks: false,
        },
        // Live stream tracker settings
        liveTracker: {
          trackingThreshold: 0,
          liveTolerance: 15,
        },
      });

      playerRef.current = player;

      // Set up event handlers
      player.ready(() => {
        console.log('Video.js player is ready');
        onReady?.();
      });

      player.one('loadeddata', () => setHasFrame(true));

      let initialLoadAttempts = 0;
      const maxInitialAttempts = 3;

      player.on('error', () => {
        const error = player.error();
        console.error('Video.js error:', error);

        // Filter out non-critical errors common in live streaming
        if (error) {
          const errorCode = error.code;
          const errorMessage = error.message || 'Video playback error';

          // Error codes to ignore or handle differently:
          // 2 = MEDIA_ERR_NETWORK (often temporary during live streaming)
          // 4 = MEDIA_ERR_SRC_NOT_SUPPORTED (stream might be initializing)
          if (errorCode === 2) {
            console.warn('Network error during streaming (may be temporary):', errorMessage);
            // Don't call onError for temporary network issues, but drop the
            // starting overlay so the player's own error UI and controls show.
            setHasFrame(true);
            return;
          }

          if (errorCode === 4 && initialLoadAttempts < maxInitialAttempts) {
            console.warn(`Stream not ready yet (attempt ${initialLoadAttempts + 1}/${maxInitialAttempts}), retrying in 2 seconds...`);
            initialLoadAttempts++;

            // Retry loading the stream after a delay
            setTimeout(() => {
              if (player && !player.isDisposed()) {
                // Reset player and reload source
                player.src({
                  src: streamUrlRef.current,
                  type: 'application/x-mpegURL',
                });
                player.load();
              }
            }, 2000);
            return;
          }

          // For persistent errors, report them
          if (errorCode === 4 && initialLoadAttempts >= maxInitialAttempts) {
            console.error('Stream failed to load after multiple attempts');
          }
          onError?.(new Error(errorMessage));
        }
      });

      // Handle warning events that don't stop playback
      player.on('warning', (event: Event) => {
        console.warn('Video.js warning:', event);
      });

      // Suppress common live streaming console errors
      const originalConsoleError = console.error;
      console.error = (...args: unknown[]) => {
        const errorMsg = args[0]?.toString() || '';
        // Suppress the common duration error for live streams
        if (errorMsg.includes('duration') || errorMsg.includes('seekable')) {
          return;
        }
        originalConsoleError.apply(console, args);
      };
    }
  }, [onError, onReady, streamUrlRef]);

  // Update source when streamUrl changes
  useEffect(() => {
    const player = playerRef.current;
    if (player) {
      player.src({
        src: streamUrl,
        type: 'application/x-mpegURL', // HLS MIME type
      });
    }
  }, [streamUrl]);

  // Dispose the Video.js player when the component unmounts
  useEffect(() => {
    const player = playerRef.current;

    return () => {
      if (player && !player.isDisposed()) {
        player.dispose();
        playerRef.current = null;
      }
    };
  }, []);

  return (
    <div className="relative">
      <div data-vjs-player>
        <video
          ref={videoRef}
          className="video-js vjs-big-play-centered vjs-fluid"
          muted
          autoPlay
          playsInline
        />
      </div>
      {!hasFrame && (
        <div className="absolute inset-0">
          <CameraStarting />
        </div>
      )}
    </div>
  );
}
