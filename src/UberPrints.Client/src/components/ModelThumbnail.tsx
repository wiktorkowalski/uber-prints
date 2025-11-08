import { useEffect, useState } from 'react';
import { fetchModelThumbnail, identifyModelPlatform } from '../lib/modelThumbnails';
import { PrinterPlaceholder } from './PrinterPlaceholder';
import { Badge } from './ui/badge';

interface ModelThumbnailProps {
  modelUrl: string;
  size?: number;
  className?: string;
}

/**
 * Displays a thumbnail image for a 3D model URL
 * Fetches thumbnail from supported platforms or shows placeholder
 */
export function ModelThumbnail({ modelUrl, size = 128, className = '' }: ModelThumbnailProps) {
  const [thumbnailUrl, setThumbnailUrl] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(false);
  const platform = identifyModelPlatform(modelUrl);

  useEffect(() => {
    let isMounted = true;

    const loadThumbnail = async () => {
      setLoading(true);
      setError(false);

      try {
        const url = await fetchModelThumbnail(modelUrl);
        if (isMounted) {
          setThumbnailUrl(url);
          setLoading(false);
        }
      } catch (err) {
        console.error('Failed to load thumbnail:', err);
        if (isMounted) {
          setError(true);
          setLoading(false);
        }
      }
    };

    loadThumbnail();

    return () => {
      isMounted = false;
    };
  }, [modelUrl]);

  const getPlatformLabel = () => {
    switch (platform.platform) {
      case 'printables':
        return 'Printables';
      case 'thingiverse':
        return 'Thingiverse';
      case 'makerworld':
        return 'MakerWorld';
      default:
        return 'External';
    }
  };

  const getPlatformVariant = () => {
    switch (platform.platform) {
      case 'printables':
        return 'default' as const;
      case 'thingiverse':
      case 'makerworld':
        return 'secondary' as const;
      default:
        return 'outline' as const;
    }
  };

  return (
    <div className={`relative ${className}`} style={{ width: size, height: size }}>
      {loading || error || !thumbnailUrl ? (
        <PrinterPlaceholder size={size} className="rounded-lg" />
      ) : (
        <img
          src={thumbnailUrl}
          alt="3D model thumbnail"
          className="w-full h-full object-cover rounded-lg"
          onError={() => setError(true)}
        />
      )}

      {/* Platform badge */}
      <div className="absolute bottom-1 right-1">
        <Badge variant={getPlatformVariant()} className="text-xs px-1.5 py-0.5">
          {getPlatformLabel()}
        </Badge>
      </div>
    </div>
  );
}
