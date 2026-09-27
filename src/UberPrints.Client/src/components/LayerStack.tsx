import { cn } from '../lib/utils';

interface LayerStackProps {
  /** Progress in percent, 0-100. */
  progress: number;
  bars?: number;
  className?: string;
}

/**
 * Vertical stack of thin bars, filled bottom-up in proportion to progress.
 * The top filled bar is the layer being printed now.
 */
export function LayerStack({ progress, bars = 16, className }: LayerStackProps) {
  const clamped = Math.min(100, Math.max(0, progress));
  const filled = Math.round((clamped / 100) * bars);

  return (
    <div
      role="img"
      aria-label={`${Math.round(clamped)}% printed`}
      className={cn('flex flex-col-reverse gap-0.5', className)}
    >
      {Array.from({ length: bars }, (_, i) => (
        <span
          key={i}
          className={cn(
            'block flex-1 rounded-[1px]',
            i < filled - 1 ? 'bg-primary' : i === filled - 1 ? 'bg-foreground' : 'bg-border'
          )}
        />
      ))}
    </div>
  );
}
