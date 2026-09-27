import { RequestStatusEnum } from '../types/api';
import { cn, getStatusLabel } from '../lib/utils';
import {
  getRequestStage,
  MAIN_STAGES,
  STAGE_BG_CLASSES,
  STAGE_LABELS,
  STAGE_TEXT_CLASSES,
} from '../lib/requestStage';

interface StageBarProps {
  status: RequestStatusEnum;
  className?: string;
}

export function StageBar({ status, className }: StageBarProps) {
  const stage = getRequestStage(status);
  const statusLabel = getStatusLabel(status);

  if (stage === 'rejected') {
    return (
      <div className={cn('space-y-1', className)} aria-label="Rejected">
        <p className={cn('flex items-center gap-2 font-semibold', STAGE_TEXT_CLASSES.rejected)}>
          <span aria-hidden="true" className={cn('h-[9px] w-[9px] rounded-full', STAGE_BG_CLASSES.rejected)} />
          Rejected
        </p>
        <p className="text-sm text-muted-foreground">This request was rejected.</p>
      </div>
    );
  }

  const currentIndex = MAIN_STAGES.indexOf(stage);

  return (
    <div
      role="group"
      aria-label={`Stage ${currentIndex + 1} of ${MAIN_STAGES.length}: ${STAGE_LABELS[stage]}`}
      className={cn('space-y-2', className)}
    >
      <div className="grid grid-cols-4 gap-1.5" aria-hidden="true">
        {MAIN_STAGES.map((s, i) => (
          <div
            key={s}
            className={cn('h-2 rounded', i <= currentIndex ? STAGE_BG_CLASSES[stage] : 'bg-border')}
          />
        ))}
      </div>
      <div className="grid grid-cols-4 gap-1.5 text-xs sm:text-sm">
        {MAIN_STAGES.map((s, i) => {
          const isCurrent = i === currentIndex;
          const showStatus = isCurrent && statusLabel !== STAGE_LABELS[s];
          return (
            <span
              key={s}
              aria-current={isCurrent ? 'step' : undefined}
              className={isCurrent ? 'font-semibold text-foreground' : 'text-muted-foreground'}
            >
              {STAGE_LABELS[s]}
              {showStatus && ` · ${statusLabel}`}
            </span>
          );
        })}
      </div>
    </div>
  );
}
