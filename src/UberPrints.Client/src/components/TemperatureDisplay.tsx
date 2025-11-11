import { Thermometer } from 'lucide-react';

interface TemperatureDisplayProps {
  label: string;
  current?: number;
  target?: number;
  icon?: React.ReactNode;
}

export function TemperatureDisplay({ label, current, target, icon }: TemperatureDisplayProps) {
  const formatTemp = (temp?: number) => {
    return temp !== undefined ? `${Math.round(temp)}°C` : '--°C';
  };

  const isHeating = target !== undefined && current !== undefined && current < target - 2;
  const isAtTarget = target !== undefined && current !== undefined && Math.abs(current - target) <= 2;

  return (
    <div className="flex items-center gap-2">
      {icon || <Thermometer className="h-4 w-4 text-muted-foreground" />}

      <div className="flex-1">
        <div className="text-xs text-muted-foreground">{label}</div>
        <div className="flex items-baseline gap-2">
          <span className={`text-lg font-semibold ${
            isHeating ? 'text-orange-500' :
            isAtTarget ? 'text-green-500' :
            'text-foreground'
          }`}>
            {formatTemp(current)}
          </span>
          {target !== undefined && target > 0 && (
            <span className="text-sm text-muted-foreground">
              / {formatTemp(target)}
            </span>
          )}
        </div>
      </div>
    </div>
  );
}
