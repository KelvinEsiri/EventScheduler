# 🎨 Styles Organization - Quick Reference

## 📂 New Structure Overview

All styles are now in `/wwwroot/styles/` with the following organization:

```
styles/
├── main.css                 # 👈 Import this ONE file in App.razor
├── app.css                  # Global application styles
├── auth.css                 # Authentication pages
├── layout.css               # Layout & navigation
├── layout-enhancements.css  # Enhanced navbar/footer
├── calendar.css             # Calendar functionality
├── events.css               # Events lists
├── components/              # Reusable component styles
│   ├── toast-notification.css
│   ├── reconnection-modal.css
│   ├── connection-indicator.css
│   ├── loading-spinner.css
│   └── blazor-error-ui.css
└── pages/                   # Page-specific styles
    ├── home.css
    ├── logout.css
    ├── calendar-view.css
    ├── calendar-list.css
    └── public-events.css
```

## ✅ What Changed

### In App.razor
```html
<!-- OLD -->
<link rel="stylesheet" href="@Assets["app.css"]" />
<link rel="stylesheet" href="@Assets["css/calendar-view.css"]" />

<!-- NEW -->
<link rel="stylesheet" href="@Assets["styles/main.css"]" />
```

### Removed Inline Styles From:
- ✅ App.razor (reconnection modal)
- ✅ ToastNotification.razor (toast styles)
- ✅ MainLayout.razor (layout enhancements, error UI)
- ✅ Home.razor (all page styles)
- ✅ Logout.razor (all page styles)
- ✅ CalendarView.razor (connection indicator, loading)

### Files Still With Inline Styles:
- ⚠️ CalendarList.razor (extensive page-specific styles - working fine)
- ⚠️ PublicEvents.razor (view toggle & filters - working fine)

## 🔍 Find a Style

| What you need | Where it is |
|--------------|-------------|
| Button styles | `styles/layout.css` |
| Form styles | `styles/app.css` |
| Calendar | `styles/calendar.css` |
| Event cards | `styles/events.css` |
| Login/Register | `styles/auth.css` |
| Toast notifications | `styles/components/toast-notification.css` |
| Loading spinners | `styles/components/loading-spinner.css` |
| Home page | `styles/pages/home.css` |
| Error messages | `styles/components/blazor-error-ui.css` |

## 💡 Adding New Styles

### For a New Component:
1. Create file: `styles/components/my-component.css`
2. Add import to `styles/main.css`:
   ```css
   @import url('./components/my-component.css');
   ```

### For a New Page:
1. Create file: `styles/pages/my-page.css`
2. Add import to `styles/main.css`:
   ```css
   @import url('./pages/my-page.css');
   ```

### For Inline Styles (Quick Prototyping):
It's OK to keep styles inline during development. Move them to CSS files when ready.

## 🚀 Performance Tips

- ✅ Using `main.css` - One import loads everything
- ✅ Browser caches styles efficiently
- ✅ No duplicate CSS anymore
- ✅ Organized by feature/component

## 📚 Full Documentation

See `/wwwroot/styles/README.md` for complete documentation.

## 🆘 Troubleshooting

**Styles not loading?**
1. Check if `styles/main.css` is referenced in App.razor
2. Clear browser cache (Ctrl+Shift+R)
3. Check browser console for 404 errors

**Style conflicts?**
1. Check import order in `main.css`
2. Verify class names aren't duplicated
3. Use browser DevTools to inspect computed styles

**Need help?**
- Check `/wwwroot/styles/README.md`
- Check `STYLES_MIGRATION_SUMMARY.md`
- Use browser DevTools to inspect elements

---

**Quick Win**: All core styles are now organized and maintainable! 🎉
