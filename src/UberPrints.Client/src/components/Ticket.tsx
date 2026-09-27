import { ReactNode } from 'react';
import { RequestStage, STAGE_LABELS, STAGE_TEXT_CLASSES } from '../lib/requestStage';
import { cn } from '../lib/utils';

interface TicketProps {
  number?: string;
  stamp?: RequestStage;
  className?: string;
  children?: ReactNode;
}

// Echoes the thermal paper ticket the service prints for every new request.
export function Ticket({ number, stamp, className, children }: TicketProps) {
  return (
    <div className={cn('ticket space-y-2 px-5 pt-4 text-sm', className)}>
      {stamp && (
        <span className={cn('ticket-stamp', STAGE_TEXT_CLASSES[stamp])}>{STAGE_LABELS[stamp]}</span>
      )}
      {number && (
        <div className={cn('font-heading text-2xl font-extrabold leading-tight tabular-nums', stamp && 'pr-24')}>
          {number}
        </div>
      )}
      {children}
    </div>
  );
}

export function TicketRow({ label, children }: { label: string; children: ReactNode }) {
  return (
    <div className="flex items-baseline justify-between gap-4">
      <span className="flex-shrink-0 text-muted-foreground">{label}</span>
      <span className="min-w-0 break-words text-right">{children}</span>
    </div>
  );
}

export function TicketDivider() {
  return <div aria-hidden="true" className="border-t-2 border-dashed border-border" />;
}
