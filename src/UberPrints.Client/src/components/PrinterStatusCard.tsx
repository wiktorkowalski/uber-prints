import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { PrintProgress } from './PrintProgress';
import { TemperatureDisplay } from './TemperatureDisplay';
import { PrinterStateEnum, PrinterStatusDto } from '@/types/api';
import { Printer, Flame, Box, Wind, Gauge, Droplet, Move3D } from 'lucide-react';

interface PrinterStatusCardProps {
  status: PrinterStatusDto;
}

export function PrinterStatusCard({ status }: PrinterStatusCardProps) {
  const getStateBadgeVariant = (state: PrinterStateEnum): 'default' | 'secondary' | 'destructive' | 'outline' => {
    switch (state) {
      case PrinterStateEnum.Printing:
        return 'default';
      case PrinterStateEnum.Idle:
      case PrinterStateEnum.Ready:
        return 'secondary';
      case PrinterStateEnum.Error:
      case PrinterStateEnum.Stopped:
        return 'destructive';
      default:
        return 'outline';
    }
  };

  const isPrinting = status.currentState === PrinterStateEnum.Printing;
  const isPaused = status.currentState === PrinterStateEnum.Paused;

  return (
    <Card>
      <CardHeader>
        <CardTitle className="flex items-center justify-between">
          <div className="flex items-center gap-2">
            <Printer className="h-5 w-5" />
            <span>{status.name}</span>
          </div>
          <Badge variant={getStateBadgeVariant(status.currentState)}>
            {status.currentState}
          </Badge>
        </CardTitle>
        {status.location && (
          <p className="text-sm text-muted-foreground">{status.location}</p>
        )}
      </CardHeader>

      <CardContent className="space-y-4">
        {/* Progress */}
        {(isPrinting || isPaused) && (
          <PrintProgress
            progress={status.printProgress}
            timeRemaining={status.timeRemaining}
            timePrinting={status.timePrinting}
            fileName={status.currentFileName}
          />
        )}

        {/* Temperatures */}
        <div className="grid grid-cols-2 gap-4">
          <TemperatureDisplay
            label="Nozzle"
            current={status.nozzleTemperature}
            target={status.nozzleTargetTemperature}
            icon={<Flame className="h-4 w-4 text-orange-500" />}
          />
          <TemperatureDisplay
            label="Bed"
            current={status.bedTemperature}
            target={status.bedTargetTemperature}
            icon={<Box className="h-4 w-4 text-blue-500" />}
          />
        </div>

        {/* Additional Stats */}
        <div className="grid grid-cols-2 gap-3 pt-2 border-t">
          {/* Flow & Speed */}
          {status.flowRate != null && (
            <div className="flex items-center gap-2">
              <Droplet className="h-4 w-4 text-cyan-500" />
              <div>
                <div className="text-xs text-muted-foreground">Flow</div>
                <div className="text-sm font-semibold">{status.flowRate}%</div>
              </div>
            </div>
          )}
          {status.speedRate != null && (
            <div className="flex items-center gap-2">
              <Gauge className="h-4 w-4 text-purple-500" />
              <div>
                <div className="text-xs text-muted-foreground">Speed</div>
                <div className="text-sm font-semibold">{status.speedRate}%</div>
              </div>
            </div>
          )}

          {/* Fans */}
          {status.fanPrint != null && (
            <div className="flex items-center gap-2">
              <Wind className="h-4 w-4 text-blue-400" />
              <div>
                <div className="text-xs text-muted-foreground">Print Fan</div>
                <div className="text-sm font-semibold">{status.fanPrint} RPM</div>
              </div>
            </div>
          )}
          {status.fanHotend != null && (
            <div className="flex items-center gap-2">
              <Wind className="h-4 w-4 text-orange-400" />
              <div>
                <div className="text-xs text-muted-foreground">Hotend Fan</div>
                <div className="text-sm font-semibold">{status.fanHotend} RPM</div>
              </div>
            </div>
          )}

          {/* Position */}
          {status.axisX != null && (
            <div className="flex items-center gap-2">
              <Move3D className="h-4 w-4 text-red-500" />
              <div>
                <div className="text-xs text-muted-foreground">X Position</div>
                <div className="text-sm font-semibold">{status.axisX.toFixed(2)} mm</div>
              </div>
            </div>
          )}
          {status.axisY != null && (
            <div className="flex items-center gap-2">
              <Move3D className="h-4 w-4 text-yellow-500" />
              <div>
                <div className="text-xs text-muted-foreground">Y Position</div>
                <div className="text-sm font-semibold">{status.axisY.toFixed(2)} mm</div>
              </div>
            </div>
          )}
          {status.axisZ != null && (
            <div className="flex items-center gap-2">
              <Move3D className="h-4 w-4 text-green-500" />
              <div>
                <div className="text-xs text-muted-foreground">Z Height</div>
                <div className="text-sm font-semibold">{status.axisZ.toFixed(2)} mm</div>
              </div>
            </div>
          )}
        </div>

        {/* Last update */}
        {status.lastStatusUpdate && (
          <p className="text-xs text-muted-foreground">
            Last update: {new Date(status.lastStatusUpdate).toLocaleTimeString()}
          </p>
        )}

      </CardContent>
    </Card>
  );
}
