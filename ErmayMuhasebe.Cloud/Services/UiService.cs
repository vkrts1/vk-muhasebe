using System;
using System.Threading.Tasks;

namespace ErmayMuhasebe.Cloud.Services
{
    public class UiService
    {
        public event Func<Task<bool>>? OnEscapePressed;

        public class ContextMenuItem
        {
            public string? Text { get; set; }
            public string? Icon { get; set; }
            public Func<Task>? Action { get; set; }
            public bool IsDivider { get; set; }
            public bool IsDanger { get; set; }
        }

        public event Action<double, double, List<ContextMenuItem>>? OnShowContextMenu;
        public event Action? OnHideContextMenu;

        public void ShowContextMenu(double x, double y, List<ContextMenuItem> items)
        {
            OnShowContextMenu?.Invoke(x, y, items);
        }

        public void HideContextMenu()
        {
            OnHideContextMenu?.Invoke();
        }

        public event Action? OnThemeChanged;
        public void NotifyThemeChanged() => OnThemeChanged?.Invoke();

        public event Action? OnBarSettingsChanged;
        public void NotifyBarSettingsChanged() => OnBarSettingsChanged?.Invoke();

        public async Task<bool> NotifyEscapePressedAsync()
        {
            // Close context menu first if open
            OnHideContextMenu?.Invoke();

            if (OnEscapePressed != null)
            {
                var delegates = OnEscapePressed.GetInvocationList();
                // Reverse iterate so latest (usually dialogs) handle it first
                for (int i = delegates.Length - 1; i >= 0; i--)
                {
                    if (delegates[i] is Func<Task<bool>> func)
                    {
                        var handled = await func();
                        if (handled) return true;
                    }
                }
            }
            return false;
        }
    }
}
