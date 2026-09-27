import { FilamentRequestStatusEnum } from '../types/api';
import { cn } from '../lib/utils';
import { RequestStage, STAGE_BG_CLASSES, STAGE_TEXT_CLASSES } from '../lib/requestStage';

// Reuse the print request stage colors so both status badges share one palette.
const FILAMENT_STATUS_STAGES: Record<FilamentRequestStatusEnum, RequestStage> = {
  [FilamentRequestStatusEnum.Pending]: 'waiting',
  [FilamentRequestStatusEnum.Approved]: 'printing',
  [FilamentRequestStatusEnum.Ordered]: 'pickup',
  [FilamentRequestStatusEnum.Received]: 'done',
  [FilamentRequestStatusEnum.Rejected]: 'rejected',
};

interface FilamentRequestStatusBadgeProps {
  status: FilamentRequestStatusEnum;
  className?: string;
}

export function FilamentRequestStatusBadge({ status, className }: FilamentRequestStatusBadgeProps) {
  const stage = FILAMENT_STATUS_STAGES[status] ?? 'waiting';

  return (
    <span
      className={cn(
        'inline-flex items-center gap-1.5 text-sm font-semibold whitespace-nowrap',
        STAGE_TEXT_CLASSES[stage],
        className
      )}
    >
      <span aria-hidden="true" className={cn('h-[9px] w-[9px] rounded-full', STAGE_BG_CLASSES[stage])} />
      {status}
    </span>
  );
}
