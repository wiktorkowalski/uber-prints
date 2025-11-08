/**
 * Utility functions for extracting thumbnail images from 3D model URLs
 * Supports Printables, Thingiverse, and MakerWorld
 */

export interface ModelThumbnail {
  url: string | null;
  platform: 'printables' | 'thingiverse' | 'makerworld' | 'unknown';
}

/**
 * Identifies the platform from a 3D model link
 * Returns platform info without fetching the thumbnail
 */
export function identifyModelPlatform(modelUrl: string): ModelThumbnail {
  try {
    const url = new URL(modelUrl);
    const hostname = url.hostname.toLowerCase();

    if (hostname.includes('printables.com')) {
      return { url: null, platform: 'printables' };
    }

    if (hostname.includes('thingiverse.com')) {
      return { url: null, platform: 'thingiverse' };
    }

    if (hostname.includes('makerworld.com')) {
      return { url: null, platform: 'makerworld' };
    }

    return { url: null, platform: 'unknown' };
  } catch (error) {
    console.error('Error identifying platform:', error);
    return { url: null, platform: 'unknown' };
  }
}

/**
 * Extracts model ID from Printables URL
 * Example: https://www.printables.com/model/1098891-octopus-wine-bottle-holder -> 1098891
 */
function extractPrintablesId(url: URL): string | null {
  const match = url.pathname.match(/\/model\/(\d+)/);
  return match ? match[1] : null;
}

/**
 * Extracts thing ID from Thingiverse URL
 * Example: https://www.thingiverse.com/thing:3495390 -> 3495390
 */
function extractThingiverseId(url: URL): string | null {
  const match = url.pathname.match(/\/thing:(\d+)/);
  return match ? match[1] : null;
}

/**
 * Extracts model ID from MakerWorld URL
 * Example: https://makerworld.com/en/models/67544 -> 67544
 */
function extractMakerWorldId(url: URL): string | null {
  const match = url.pathname.match(/\/models\/(\d+)/);
  return match ? match[1] : null;
}

/**
 * Fetches thumbnail URL via backend API to avoid CORS issues
 */
export async function fetchModelThumbnail(modelUrl: string): Promise<string | null> {
  try {
    console.log('Fetching thumbnail for:', modelUrl);

    const response = await fetch(`/api/thumbnail?modelUrl=${encodeURIComponent(modelUrl)}`);

    if (!response.ok) {
      console.warn('Backend thumbnail fetch failed:', response.status);
      return null;
    }

    const data = await response.json();

    if (data.thumbnailUrl) {
      console.log('Found thumbnail:', data.thumbnailUrl);
      return data.thumbnailUrl;
    }

    console.log('No thumbnail found');
    return null;
  } catch (error) {
    console.error('Error fetching thumbnail:', error);
    return null;
  }
}

/**
 * Get platform display name from URL
 */
export function getModelPlatformName(modelUrl: string): string {
  try {
    const url = new URL(modelUrl);
    const hostname = url.hostname.toLowerCase();

    if (hostname.includes('printables.com')) return 'Printables';
    if (hostname.includes('thingiverse.com')) return 'Thingiverse';
    if (hostname.includes('makerworld.com')) return 'MakerWorld';

    return 'External Site';
  } catch {
    return 'External Site';
  }
}
