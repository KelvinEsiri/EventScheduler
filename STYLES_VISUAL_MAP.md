# 🗺️ Styles Organization - Visual Map

## 🎯 Import Flow

```
App.razor
    │
    └─> styles/main.css
            │
            ├─> Core Styles
            │   ├─> app.css (Global: typography, forms, validation)
            │   ├─> auth.css (Login, Register, Password strength)
            │   ├─> layout.css (Base layout, navbar, sidebar)
            │   ├─> layout-enhancements.css (Enhanced navbar/footer)
            │   ├─> calendar.css (FullCalendar, modals, events)
            │   └─> events.css (Event cards, badges, filters)
            │
            ├─> Component Styles
            │   ├─> toast-notification.css
            │   ├─> reconnection-modal.css
            │   ├─> connection-indicator.css
            │   ├─> loading-spinner.css
            │   └─> blazor-error-ui.css
            │
            └─> Page Styles
                ├─> home.css
                ├─> logout.css
                ├─> calendar-view.css
                ├─> calendar-list.css
                └─> public-events.css
```

## 📋 Style Responsibilities

### 🌍 Global Styles (app.css)
```
Base Typography → Form Controls → Validation States → Blazor Error Boundary
```

### 🔐 Authentication (auth.css)
```
Auth Container → Auth Card → Form Inputs → Password Strength → Login/Register
```

### 📐 Layout (layout.css + layout-enhancements.css)
```
Page Layout → Sidebar → Navbar → Top Row → Footer → Buttons → Navigation
```

### 📅 Calendar (calendar.css)
```
Calendar Container → Header → FullCalendar → Day Cells → Events → Modals → Forms
```

### 📝 Events (events.css)
```
Event Container → Event Cards → Badges → Filters → Event Grid → Empty States
```

## 🧩 Component Style Map

```
┌─────────────────────────────────────────────────────────────────┐
│  ToastNotification.razor                                        │
│  Uses: components/toast-notification.css                        │
│  • Toast container positioning                                  │
│  • Toast variants (success, error, warning, info)              │
│  • Slide-in animations                                          │
└─────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│  App.razor (Reconnection Modal)                                 │
│  Uses: components/reconnection-modal.css                        │
│  • Modal overlay                                                │
│  • Loading spinner                                              │
│  • Connection states                                            │
└─────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│  MainLayout.razor (Error UI)                                    │
│  Uses: components/blazor-error-ui.css                          │
│  • Error banner                                                 │
│  • Reload button                                                │
│  • Dismiss button                                               │
└─────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│  Multiple Pages (Connection Status)                             │
│  Uses: components/connection-indicator.css                      │
│  • Connected/Disconnected states                                │
│  • Pulse animations                                             │
└─────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│  Multiple Pages (Loading)                                       │
│  Uses: components/loading-spinner.css                          │
│  • Loading container                                            │
│  • Spinner animations                                           │
│  • Loading text                                                 │
└─────────────────────────────────────────────────────────────────┘
```

## 🗂️ Page Style Map

```
┌─────────────────────────────────────────────────────────────────┐
│  Home.razor                                                      │
│  Uses: pages/home.css                                           │
│  • Hero section                                                 │
│  • Feature cards                                                │
│  • Action buttons                                               │
│  • Stats section                                                │
└─────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│  Logout.razor                                                    │
│  Uses: pages/logout.css                                         │
│  • Logout container                                             │
│  • Logout card                                                  │
│  • Logout icon                                                  │
└─────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│  CalendarView.razor                                             │
│  Uses: pages/calendar-view.css                                  │
│       + components/connection-indicator.css                     │
│       + components/loading-spinner.css                          │
│  • Calendar card                                                │
│  • Calendar body                                                │
└─────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│  CalendarList.razor                                             │
│  Uses: pages/calendar-list.css (minimal)                        │
│  Note: Most styles still inline (working great!)                │
│  • Page background                                              │
│  • Empty state                                                  │
│  • Event grid                                                   │
└─────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│  PublicEvents.razor                                             │
│  Uses: pages/public-events.css (minimal)                        │
│  Note: Most styles still inline (working great!)                │
│  • View toggle                                                  │
│  • Filter section                                               │
│  • Calendar card                                                │
└─────────────────────────────────────────────────────────────────┘
```

## 🎨 Color Palette Reference

### Primary Colors (from CSS variables)
```css
--primary-gradient: linear-gradient(135deg, #667eea 0%, #764ba2 100%)
--primary-color: #667eea
--primary-dark: #5568d3
--secondary-color: #764ba2
```

### Status Colors
```
Success:  #48bb78 / #10b981
Error:    #f56565 / #ef4444
Warning:  #ed8936 / #f59e0b
Info:     #4299e1 / #3b82f6
```

### Neutral Colors
```
Background: #f8fafc
Border:     #e2e8f0
Text:       #1e293b (primary), #64748b (secondary)
```

## 📏 Spacing System

```
XS:  0.25rem (4px)
SM:  0.5rem  (8px)
MD:  1rem    (16px)
LG:  1.5rem  (24px)
XL:  2rem    (32px)
2XL: 3rem    (48px)
```

## 🔤 Typography Scale

```
Hero:    clamp(2.75rem, 6.5vw, 4.25rem)
H1:      1.5rem - 2rem
H2:      1.25rem - 1.5rem
H3:      1.125rem
Body:    0.875rem - 1rem
Small:   0.75rem - 0.8125rem
```

## 🎭 Animation Reference

### Defined Animations
```
fadeIn       → Modal overlays
slideIn      → Toast notifications
slideUp      → Modal content
spin         → Loading spinners
pulse        → Connection indicators
float        → Header icons
```

## 🔧 Breakpoints

```css
Mobile:     < 576px
Tablet:     576px - 768px
Desktop:    768px - 1200px
Large:      > 1200px
```

### Media Queries
```css
@media (max-width: 576px)   /* Mobile */
@media (max-width: 768px)   /* Tablet */
@media (max-width: 1200px)  /* Desktop */
@media (min-width: 1200px)  /* Large Desktop */
```

## 🌲 Complete File Tree

```
EventScheduler/
├── EventScheduler.Web/
│   └── wwwroot/
│       ├── styles/                          ← NEW!
│       │   ├── main.css                     ← Import this
│       │   ├── README.md                    ← Documentation
│       │   ├── app.css
│       │   ├── auth.css
│       │   ├── layout.css
│       │   ├── layout-enhancements.css
│       │   ├── calendar.css
│       │   ├── events.css
│       │   ├── components/
│       │   │   ├── toast-notification.css
│       │   │   ├── reconnection-modal.css
│       │   │   ├── connection-indicator.css
│       │   │   ├── loading-spinner.css
│       │   │   └── blazor-error-ui.css
│       │   └── pages/
│       │       ├── home.css
│       │       ├── logout.css
│       │       ├── calendar-view.css
│       │       ├── calendar-list.css
│       │       └── public-events.css
│       ├── css/                             ← OLD (can remove)
│       └── app.css                          ← OLD (can remove)
├── STYLES_ORGANIZATION_COMPLETE.md          ← Main summary
├── STYLES_MIGRATION_SUMMARY.md              ← Technical details
└── STYLES_QUICK_REFERENCE.md                ← Quick lookup
```

## 📖 Documentation Index

| Document | Purpose | Audience |
|----------|---------|----------|
| `styles/README.md` | Comprehensive guide | All developers |
| `STYLES_ORGANIZATION_COMPLETE.md` | Overview & summary | Team leads |
| `STYLES_MIGRATION_SUMMARY.md` | Technical details | Developers |
| `STYLES_QUICK_REFERENCE.md` | Quick lookup | Everyone |
| `STYLES_VISUAL_MAP.md` | This file | Visual learners |

---

**Quick Navigation**: [README](wwwroot/styles/README.md) • [Complete](STYLES_ORGANIZATION_COMPLETE.md) • [Migration](STYLES_MIGRATION_SUMMARY.md) • [Quick Ref](STYLES_QUICK_REFERENCE.md)
