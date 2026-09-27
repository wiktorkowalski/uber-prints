import { RequestStatusEnum } from '../types/api';
import { cn, getStatusLabel } from '../lib/utils';
import { getRequestStage, STAGE_BG_CLASSES, STAGE_TEXT_CLASSES } from '../lib/requestStage';

interface StatusBadgeProps {
  status: RequestStatusEnum;
  className?: string;
  /** Kept for API compatibility; the badge renders a dot instead of an icon. */
  showIcon?: boolean;
  size?: 'sm' | 'md' | 'lg';
}

export function StatusBadge({ status, className }: StatusBadgeProps) {
  const stage = getRequestStage(status);

  return (
    <span
      className={cn(
        'inline-flex items-center gap-1.5 text-sm font-semibold whitespace-nowrap',
        STAGE_TEXT_CLASSES[stage],
        className
      )}
    >
      <span aria-hidden="true" className={cn('h-[9px] w-[9px] rounded-full', STAGE_BG_CLASSES[stage])} />
      {getStatusLabel(status)}
    </span>
  );
}
