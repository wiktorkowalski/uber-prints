import { Loader2 } from 'lucide-react';
import { cn } from '../../lib/utils';

interface LoadingSpinnerProps {
  size?: 'sm' | 'md' | 'lg';
  message?: string;
  className?: string;
}

export const LoadingSpinner = ({ size = 'md', message, className }: LoadingSpinnerProps) => {
  const sizeClasses = {
    sm: 'w-6 h-6',
    md: 'w-12 h-12',
    lg: 'w-16 h-16',
  };

  return (
    <div className={cn("flex items-center justify-center py-12", className)}>
      <div className="text-center">
        <Loader2 className={cn(sizeClasses[size], "animate-spin text-primary mx-auto mb-4")} />
        {message && <p className="text-muted-foreground">{message}</p>}
      </div>
    </div>
  );
};
