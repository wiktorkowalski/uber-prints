# UberPrints Design System

## Design Concept: "Precision Craft"

A modern, technical aesthetic that combines precision engineering with maker culture warmth.

---

## Color Palette

### Light Mode
- **Background**: `220 15% 98%` - Warm neutral with subtle grid aesthetic
- **Foreground**: `220 20% 15%` - Deep slate for text
- **Primary**: `220 30% 25%` - Deep indigo/slate (technical precision)
- **Accent**: `15 85% 62%` - Vibrant coral (3D printer energy, heat)
- **Card**: `0 0% 100%` - Pure white with subtle elevation

### Dark Mode
- **Background**: `220 25% 8%` - Deep slate with warmth
- **Foreground**: `220 15% 95%` - Light slate for text
- **Primary**: `220 40% 75%` - Lighter indigo for contrast
- **Accent**: `15 85% 58%` - Slightly adjusted coral for dark backgrounds

### Status Colors
All status colors use CSS variables for theme-aware rendering:
- **Pending**: Yellow (`--status-pending`, `--status-pending-bg`)
- **Accepted**: Green (`--status-accepted`, `--status-accepted-bg`)
- **Rejected**: Red (`--status-rejected`, `--status-rejected-bg`)
- **OnHold**: Gray (`--status-onhold`, `--status-onhold-bg`)
- **Paused**: Purple (`--status-paused`, `--status-paused-bg`)
- **Waiting**: Blue (`--status-waiting`, `--status-waiting-bg`)
- **Delivering**: Coral/Accent (`--status-delivering`, `--status-delivering-bg`)
- **Completed**: Green (`--status-completed`, `--status-completed-bg`)

---

## Typography

### Fonts
- **Headings**: `Sora` - Geometric, modern, technical feel
  - Weight: 600 (semibold)
  - Letter spacing: -0.02em (tighter for modern look)
- **Body**: `Plus Jakarta Sans` - Humanist, approachable, clear
  - Weight: 400 (regular), 500 (medium), 600 (semibold)
- **Monospace**: `SF Mono`, `Monaco`, `Inconsolata`, `Fira Code`
  - Use for: IDs, technical data, timestamps

### Scale
- **h1**: `text-3xl lg:text-4xl` (30px/36px → 36px/42px)
- **h2**: `text-2xl lg:text-3xl` (24px/30px → 30px/36px)
- **h3**: `text-xl lg:text-2xl` (20px/24px → 24px/30px)
- **h4**: `text-lg lg:text-xl` (18px/22px → 20px/24px)
- **Body**: `text-base` (16px/24px)
- **Small**: `text-sm` (14px/20px)
- **Extra small**: `text-xs` (12px/16px)

---

## Spacing System

### Standard Spacing
- **Tight**: `space-y-4` / `gap-4` (16px) - Dense content
- **Normal**: `space-y-6` / `gap-6` (24px) - Default spacing
- **Loose**: `space-y-8` / `gap-8` (32px) - Breathing room
- **Extra loose**: `space-y-12` / `gap-12` (48px) - Section separation

### Icon Sizes
- **Inline**: `w-3 h-3` / `w-3.5 h-3.5` (12px/14px) - Within badges, small buttons
- **Default**: `w-4 h-4` / `w-5 h-5` (16px/20px) - Standard buttons, nav items
- **Emphasis**: `w-6 h-6` / `w-7 h-7` (24px/28px) - Feature cards, highlighted actions
- **Hero**: `w-8 h-8` and larger (32px+) - Empty states, hero sections

---

## Components

### StatusBadge
**Location**: `/src/components/StatusBadge.tsx`

Centralized status badge component with icons and theme-aware colors.

```tsx
import { StatusBadge } from '../components/StatusBadge';

<StatusBadge
  status={RequestStatusEnum.Pending}
  showIcon={true}
  size="md"  // sm | md | lg
/>
```

**Features**:
- Uses CSS variable status colors (automatically adapts to light/dark mode)
- Optional icons for better visual scanning
- Three sizes: sm, md (default), lg
- Includes status label mapping

---

### PageHeader
**Location**: `/src/components/PageHeader.tsx`

Standardized page header with title, description, breadcrumbs, and actions.

```tsx
import { PageHeader } from '../components/PageHeader';

<PageHeader
  title="Print Requests"
  description="Browse and track all 3D printing requests"
  breadcrumbs={[
    { label: 'Home', href: '/' },
    { label: 'Requests' }
  ]}
  actions={
    <Button>New Request</Button>
  }
/>
```

**Features**:
- Animated entrance (slide-up with staggered delays)
- Breadcrumb navigation with hover states
- Flexible action slot for buttons/controls
- Consistent spacing and typography

---

## Utility Classes

### Animations
- `animate-snap-in` - Bouncy scale + slide entrance
- `animate-fade-in` - Simple fade in
- `animate-slide-up` - Slide from bottom with fade
- `animate-pulse-soft` - Gentle pulse animation

**Usage**: Chain with `style={{ animationDelay: '100ms' }}` for staggered animations.

### Backgrounds
- `bg-grid-pattern` - Subtle grid (24px × 24px)
- `bg-grid-pattern-fine` - Finer grid (12px × 12px)
- Use with opacity for subtle technical aesthetic

### Transitions
- `transition-smooth` - Standard 150ms ease
- `transition-bounce` - Bouncy cubic-bezier transition
- `hover:scale-105` - Subtle grow on hover

### Cards
- `card-enhanced` - Standard card with border and shadow
- `card-interactive` - Clickable card with hover effects
- `card-hover` - Standalone hover effect utility

### Focus
- `focus-ring` - Enhanced focus indicator (2px ring with offset)

---

## Patterns

### Hero Sections
```tsx
<section className="relative overflow-hidden bg-gradient-to-br from-background via-background to-primary/5 py-20">
  <div className="absolute inset-0 bg-grid-pattern opacity-40" />
  {/* Accent blur circles */}
  <div className="absolute top-20 right-20 w-72 h-72 bg-accent/10 rounded-full blur-3xl" />
  <div className="relative">
    {/* Content */}
  </div>
</section>
```

### Feature Cards
```tsx
<div className="group card-interactive p-8 overflow-hidden">
  <div className="absolute top-0 right-0 w-24 h-24 bg-accent/10 rounded-full blur-2xl transition-all group-hover:w-32 group-hover:h-32" />
  <div className="relative">
    <div className="w-14 h-14 bg-gradient-to-br from-accent/20 to-accent/10 rounded-xl flex items-center justify-center transition-transform group-hover:scale-110 group-hover:rotate-3">
      <Icon className="w-7 h-7 text-accent" />
    </div>
    {/* Content */}
  </div>
</div>
```

### Staggered List Animations
```tsx
{items.map((item, index) => (
  <div
    key={item.id}
    className="animate-fade-in"
    style={{ animationDelay: `${index * 50}ms` }}
  >
    {/* Content */}
  </div>
))}
```

---

## Best Practices

### Color Usage
- **Primary**: Use for headings, important UI elements, borders
- **Accent**: Use sparingly for CTAs, highlights, active states
- **Avoid**: Don't overuse accent color - it loses impact

### Typography
- Use `font-heading` class for headings to apply Sora font
- Use `font-mono` for technical data (IDs, URLs, timestamps)
- Default body text uses Plus Jakarta Sans automatically

### Spacing
- Maintain consistent spacing scale throughout
- Use `space-y-*` for vertical rhythm
- Use `gap-*` for flex/grid gaps

### Animations
- Keep animations purposeful and subtle
- Use staggered animations for lists (50-100ms delay per item)
- Prefer CSS transitions for hover states (150-200ms)
- Reserve bouncy animations for high-impact moments

### Accessibility
- All interactive elements have `hover:`, `focus:`, and `active:` states
- Use `focus-ring` utility for consistent focus indicators
- Status badges include icons for non-color-dependent recognition
- Ensure color contrast meets WCAG AA standards

---

## Migration Guide

### Replacing Old Status Badges
**Before**:
```tsx
<span className={getStatusColor(status)}>
  {getStatusLabel(status)}
</span>
```

**After**:
```tsx
<StatusBadge status={status} />
```

### Replacing Page Headers
**Before**:
```tsx
<div className="flex justify-between items-center">
  <h1 className="text-3xl font-bold">Page Title</h1>
  <Button>Action</Button>
</div>
```

**After**:
```tsx
<PageHeader
  title="Page Title"
  description="Optional description"
  actions={<Button>Action</Button>}
/>
```

### Adding Animations
Simply add animation classes to existing elements:
```tsx
<div className="card-enhanced animate-fade-in">
  {/* Existing content */}
</div>
```

For staggered lists, use index-based delays:
```tsx
style={{ animationDelay: `${index * 50}ms` }}
```

---

## Resources

- **Tailwind Config**: `/tailwind.config.js`
- **CSS Variables**: `/src/index.css`
- **Components**: `/src/components/`
- **Google Fonts**: Sora, Plus Jakarta Sans (loaded in index.css)

---

## Future Enhancements

- [ ] Add dark mode toggle component
- [ ] Create loading skeleton variants
- [ ] Add toast notification system styling
- [ ] Create form field component variants
- [ ] Add data visualization components (charts, gauges)
- [ ] Create print progress timeline component
