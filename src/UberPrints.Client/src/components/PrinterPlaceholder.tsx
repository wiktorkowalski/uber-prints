interface PrinterPlaceholderProps {
  className?: string;
  size?: number;
}

/**
 * Placeholder SVG icon of a 3D printer (Prusa-style)
 * Used when model thumbnail cannot be loaded
 */
export function PrinterPlaceholder({ className = '', size = 128 }: PrinterPlaceholderProps) {
  return (
    <svg
      width={size}
      height={size}
      viewBox="0 0 128 128"
      fill="none"
      xmlns="http://www.w3.org/2000/svg"
      className={className}
      aria-label="3D Printer placeholder"
    >
      {/* Background */}
      <rect width="128" height="128" rx="8" className="fill-muted" />

      {/* 3D Printer Icon */}
      <g transform="translate(24, 20)">
        {/* Top frame */}
        <rect x="8" y="0" width="64" height="8" rx="2" className="fill-muted-foreground" />
        <rect x="10" y="8" width="4" height="20" className="fill-muted-foreground" />
        <rect x="66" y="8" width="4" height="20" className="fill-muted-foreground" />

        {/* Print head */}
        <rect x="28" y="12" width="24" height="16" rx="2" className="fill-foreground/60" />
        <rect x="36" y="28" width="8" height="8" className="fill-stage-waiting" />

        {/* Middle section - print bed */}
        <rect x="4" y="36" width="72" height="4" className="fill-input" />

        {/* Build plate */}
        <rect x="8" y="40" width="64" height="24" rx="2" className="fill-border stroke-input" strokeWidth="2" />

        {/* Printed object on bed */}
        <path
          d="M 32 52 L 36 48 L 44 48 L 48 52 L 48 60 L 32 60 Z"
          className="fill-primary"
          opacity="0.6"
        />

        {/* Base/legs */}
        <rect x="8" y="64" width="8" height="20" className="fill-muted-foreground" />
        <rect x="64" y="64" width="8" height="20" className="fill-muted-foreground" />
        <rect x="4" y="84" width="72" height="4" rx="2" className="fill-foreground/60" />
      </g>
    </svg>
  );
}
