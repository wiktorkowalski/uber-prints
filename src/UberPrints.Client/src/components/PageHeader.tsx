import { ReactNode } from 'react';
import { cn } from '../lib/utils';
import { ChevronRight } from 'lucide-react';
import { Link } from 'react-router-dom';

interface BreadcrumbItem {
  label: string;
  href?: string;
}

interface PageHeaderProps {
  title: string;
  description?: string;
  breadcrumbs?: BreadcrumbItem[];
  actions?: ReactNode;
  className?: string;
}

export function PageHeader({
  title,
  description,
  breadcrumbs,
  actions,
  className
}: PageHeaderProps) {
  return (
    <div className={cn('page-header', className)}>
      {breadcrumbs && breadcrumbs.length > 0 && (
        <nav className="flex items-center space-x-1 text-sm text-muted-foreground mb-3 animate-fade-in">
          {breadcrumbs.map((crumb, index) => (
            <div key={index} className="flex items-center">
              {index > 0 && <ChevronRight className="w-4 h-4 mx-1" />}
              {crumb.href ? (
                <Link
                  to={crumb.href}
                  className="hover:text-foreground transition-smooth"
                >
                  {crumb.label}
                </Link>
              ) : (
                <span className="text-foreground font-medium">{crumb.label}</span>
              )}
            </div>
          ))}
        </nav>
      )}

      <div className="flex items-start justify-between gap-4">
        <div className="space-y-2 min-w-0 flex-1">
          <h1 className="page-title animate-slide-up">{title}</h1>
          {description && (
            <p className="page-description animate-slide-up" style={{ animationDelay: '50ms' }}>
              {description}
            </p>
          )}
        </div>

        {actions && (
          <div className="flex items-center gap-2 animate-slide-up" style={{ animationDelay: '100ms' }}>
            {actions}
          </div>
        )}
      </div>
    </div>
  );
}
