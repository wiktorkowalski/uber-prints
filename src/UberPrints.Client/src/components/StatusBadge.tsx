import { RequestStatusEnum } from '../types/api';
import { cn } from '../lib/utils';
import {
  Circle,
  Clock,
  CheckCircle2,
  XCircle,
  Pause,
  PackageSearch,
  Truck,
  Package,
  AlertCircle
} from 'lucide-react';

interface StatusBadgeProps {
  status: RequestStatusEnum;
  className?: string;
  showIcon?: boolean;
  size?: 'sm' | 'md' | 'lg';
}

const statusConfig: Record<RequestStatusEnum, {
  label: string;
  icon: typeof Circle;
  colorClass: string;
  bgClass: string;
}> = {
  [RequestStatusEnum.Pending]: {
    label: 'Pending',
    icon: Clock,
    colorClass: 'text-status-pending',
    bgClass: 'bg-status-pending-bg'
  },
  [RequestStatusEnum.Accepted]: {
    label: 'Accepted',
    icon: CheckCircle2,
    colorClass: 'text-status-accepted',
    bgClass: 'bg-status-accepted-bg'
  },
  [RequestStatusEnum.Rejected]: {
    label: 'Rejected',
    icon: XCircle,
    colorClass: 'text-status-rejected',
    bgClass: 'bg-status-rejected-bg'
  },
  [RequestStatusEnum.OnHold]: {
    label: 'On Hold',
    icon: AlertCircle,
    colorClass: 'text-status-onhold',
    bgClass: 'bg-status-onhold-bg'
  },
  [RequestStatusEnum.Paused]: {
    label: 'Paused',
    icon: Pause,
    colorClass: 'text-status-paused',
    bgClass: 'bg-status-paused-bg'
  },
  [RequestStatusEnum.WaitingForMaterials]: {
    label: 'Waiting for Materials',
    icon: PackageSearch,
    colorClass: 'text-status-waiting',
    bgClass: 'bg-status-waiting-bg'
  },
  [RequestStatusEnum.Delivering]: {
    label: 'Delivering',
    icon: Truck,
    colorClass: 'text-status-delivering',
    bgClass: 'bg-status-delivering-bg'
  },
  [RequestStatusEnum.WaitingForPickup]: {
    label: 'Waiting for Pickup',
    icon: Package,
    colorClass: 'text-status-waiting',
    bgClass: 'bg-status-waiting-bg'
  },
  [RequestStatusEnum.Completed]: {
    label: 'Completed',
    icon: CheckCircle2,
    colorClass: 'text-status-completed',
    bgClass: 'bg-status-completed-bg'
  },
};

const sizeClasses = {
  sm: 'px-2 py-0.5 text-xs gap-1',
  md: 'px-2.5 py-1 text-xs gap-1.5',
  lg: 'px-3 py-1.5 text-sm gap-2'
};

const iconSizes = {
  sm: 'w-3 h-3',
  md: 'w-3.5 h-3.5',
  lg: 'w-4 h-4'
};

export function StatusBadge({
  status,
  className,
  showIcon = true,
  size = 'md'
}: StatusBadgeProps) {
  const config = statusConfig[status] || {
    label: 'Unknown',
    icon: Circle,
    colorClass: 'text-muted-foreground',
    bgClass: 'bg-muted'
  };

  const Icon = config.icon;

  return (
    <span
      className={cn(
        'inline-flex items-center rounded-md font-medium transition-smooth',
        config.colorClass,
        config.bgClass,
        sizeClasses[size],
        className
      )}
    >
      {showIcon && <Icon className={iconSizes[size]} />}
      <span>{config.label}</span>
    </span>
  );
}

// Export utility functions for backwards compatibility
export function getStatusLabel(status: RequestStatusEnum): string {
  return statusConfig[status]?.label || 'Unknown';
}

export function getStatusIcon(status: RequestStatusEnum) {
  return statusConfig[status]?.icon || Circle;
}
