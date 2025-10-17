# 🎨 CSS Folder Organization

This folder contains all CSS stylesheets for the Event Scheduler application, organized in a modular and maintainable structure.

## 📁 Folder Structure

```
css/
├── main.css                          # Master stylesheet (imports all others)
├── app.css                          # Global application styles
├── auth.css                         # Authentication pages styles
├── layout.css                       # Layout & navigation styles
├── layout-enhancements.css          # Enhanced layout styles from MainLayout
├── calendar.css                     # FullCalendar & calendar view styles
├── events.css                       # Events list & management styles
├── components/                      # Component-specific styles
│   ├── toast-notification.css       # Toast notification component
│   ├── reconnection-modal.css       # Blazor reconnection modal
│   ├── connection-indicator.css     # Real-time connection indicator
│   ├── loading-spinner.css          # Loading states & spinners
│   └── blazor-error-ui.css         # Blazor error UI
└── pages/                           # Page-specific styles
    ├── home.css                     # Home page
    ├── logout.css                   # Logout page
    ├── calendar-view.css            # Calendar view page
    ├── calendar-list.css            # Calendar list page
    └── public-events.css            # Public events page
```

## 📝 File Descriptions

### Core Styles

- **`main.css`**: Master stylesheet that imports all other CSS files. Reference this single file in your application.
- **`app.css`**: Global application styles including typography, form controls, validation, and error boundaries.
- **`auth.css`**: Authentication-related styles for login, register, and password strength indicators.
- **`layout.css`**: Base layout, navigation menu, sidebar, top row, and button styles.
- **`layout-enhancements.css`**: Enhanced navbar styles extracted from MainLayout.razor.

### Feature Styles

- **`calendar.css`**: Complete calendar view styles including FullCalendar customization, modals, forms, and responsive design.
- **`events.css`**: Event cards, filters, grids, badges, and event management UI.

### Component Styles

All reusable component styles extracted from inline `<style>` blocks:

- **`toast-notification.css`**: Toast notification positioning, animations, and variants (success, error, warning, info).
- **`reconnection-modal.css`**: Blazor SignalR reconnection modal with loading spinner.
- **`connection-indicator.css`**: Real-time connection status indicator with pulse animations.
- **`loading-spinner.css`**: Loading states and spinner animations used across pages.
- **`blazor-error-ui.css`**: Blazor error boundary UI styling.

### Page-Specific Styles

Styles that are unique to individual pages:

- **`home.css`**: Home page hero section, feature cards, action buttons, and stats.
- **`logout.css`**: Logout page loading card and animations.
- **`calendar-view.css`**: Calendar view page-specific card and layout styles.
- **`calendar-list.css`**: Calendar list page background, empty states, and event grids.
- **`public-events.css`**: Public events page view toggle and filter section.

## 🔧 Usage

### Method 1: Import Main Stylesheet (Recommended)

Add this single reference to your `App.razor` or `_Host.cshtml`:

```html
<link rel="stylesheet" href="styles/main.css" />
```

### Method 2: Import Individual Stylesheets

If you need fine-grained control, import only what you need:

```html
<!-- Core -->
<link rel="stylesheet" href="styles/app.css" />
<link rel="stylesheet" href="styles/layout.css" />

<!-- Features -->
<link rel="stylesheet" href="styles/calendar.css" />

<!-- Components -->
<link rel="stylesheet" href="styles/components/toast-notification.css" />
```

## 🎯 Benefits of This Organization

1. **Modularity**: Each file has a single responsibility
2. **Maintainability**: Easy to find and update specific styles
3. **Performance**: Import only what you need
4. **Scalability**: Easy to add new styles without cluttering
5. **Team Collaboration**: Multiple developers can work on different files
6. **Debugging**: Easier to track down style issues
7. **Documentation**: Self-documenting structure

## 🔄 Migration Notes

### Removed Inline Styles From:

- ✅ `App.razor` → `components/reconnection-modal.css`
- ✅ `ToastNotification.razor` → `components/toast-notification.css`
- ✅ `Home.razor` → `pages/home.css`
- ✅ `Logout.razor` → `pages/logout.css`
- ✅ `MainLayout.razor` → `layout-enhancements.css` & `components/blazor-error-ui.css`
- ✅ `CalendarView.razor` → `components/connection-indicator.css` & `pages/calendar-view.css`
- ✅ `CalendarList.razor` → `pages/calendar-list.css`
- ✅ `PublicEvents.razor` → `pages/public-events.css`

### Legacy CSS Folder

The old `wwwroot/css/` folder can be safely removed after migrating references.

## 📋 Naming Conventions

- **Files**: `kebab-case.css`
- **Classes**: Use existing conventions (BEM-like in some areas, utility-based in others)
- **IDs**: Minimal use, primarily for FullCalendar and Blazor system elements
- **Variables**: CSS custom properties in `:root` (see `calendar.css`)

## 🚀 Best Practices

1. **Don't use `!important`** unless absolutely necessary (overriding third-party styles)
2. **Prefer classes over IDs** for styling
3. **Use CSS custom properties** for theme values
4. **Keep selectors shallow** (1-3 levels deep max)
5. **Comment sections clearly** with emoji headers for quick scanning
6. **Test responsive breakpoints** at 576px, 768px, 992px, 1200px
7. **Use relative units** (rem, em, %) over pixels when appropriate
8. **Maintain accessibility** with proper focus states and contrast ratios

## 🐛 Troubleshooting

### Styles Not Loading?

1. Check browser console for 404 errors
2. Verify the CSS file path is correct
3. Clear browser cache (Ctrl+Shift+R)
4. Check if CSS file is included in build output

### Styles Conflicting?

1. Check CSS specificity
2. Verify import order in `main.css`
3. Look for duplicate class names
4. Use browser DevTools to inspect computed styles

### Performance Issues?

1. Minimize nested selectors
2. Avoid universal selectors (`*`)
3. Consider lazy-loading non-critical styles
4. Combine and minify for production

## 📚 Related Documentation

- [PROJECT_STRUCTURE.md](../../PROJECT_STRUCTURE.md) - Overall project organization
- [CALENDARVIEW_IMPLEMENTATION_COMPLETE.md](../../CALENDARVIEW_IMPLEMENTATION_COMPLETE.md) - Calendar styling details
- [BUTTON_LAYOUT_STANDARDIZATION.md](../../docs/BUTTON_LAYOUT_STANDARDIZATION.md) - Button styling standards

## 🔮 Future Enhancements

- [ ] Add CSS minification for production builds
- [ ] Implement CSS modules for component isolation
- [ ] Add CSS linting with Stylelint
- [ ] Create a design tokens system
- [ ] Add dark mode support
- [ ] Implement CSS-in-JS if needed for dynamic theming

---

**Last Updated**: October 17, 2025  
**Maintained By**: Development Team
