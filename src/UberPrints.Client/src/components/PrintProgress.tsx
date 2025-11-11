import { Progress } from '@/components/ui/progress';
import { Clock, Hourglass } from 'lucide-react';

interface PrintProgressProps {
  progress?: number;
  timeRemaining?: number;
  timePrinting?: number;
  fileName?: string;
}

export function PrintProgress({ progress, timeRemaining, timePrinting, fileName }: PrintProgressProps) {
  const formatTime = (seconds?: number) => {
    if (!seconds) return '--:--';

    const hours = Math.floor(seconds / 3600);
    const minutes = Math.floor((seconds % 3600) / 60);

    if (hours > 0) {
      return `${hours}h ${minutes}m`;
    }
    return `${minutes}m`;
  };

  return (
    <div className="space-y-2">
      {fileName && (
        <p className="text-sm font-medium truncate">{fileName}</p>
      )}

      <Progress value={progress || 0} className="h-2" />

      <div className="flex justify-between text-xs text-muted-foreground">
        <div className="flex items-center gap-1">
          <Hourglass className="h-3 w-3" />
          <span>{formatTime(timePrinting)}</span>
        </div>

        <span>{progress ? `${Math.round(progress)}%` : '0%'}</span>

        <div className="flex items-center gap-1">
          <Clock className="h-3 w-3" />
          <span>{formatTime(timeRemaining)}</span>
        </div>
      </div>
    </div>
  );
}
