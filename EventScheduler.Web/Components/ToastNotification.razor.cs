using Microsoft.AspNetCore.Components;

namespace EventScheduler.Web.Components;

public partial class ToastNotification : IDisposable
{
    [Parameter]
    public string? title { get; set; }
    
    [Parameter]
    public string? message { get; set; }
    
    [Parameter]
    public ToastType type { get; set; } = ToastType.Info;
    
    [Parameter]
    public string position { get; set; } = "top-right";
    
    [Parameter]
    public int duration { get; set; } = 5000;
    
    [Parameter]
    public EventCallback OnClose { get; set; }
    
    private bool isVisible = false;
    private System.Threading.Timer? timer;

    public enum ToastType
    {
        Success,
        Error,
        Warning,
        Info
    }

    protected override void OnInitialized()
    {
        Show();
    }

    public void Show()
    {
        isVisible = true;
        StateHasChanged();

        if (duration > 0)
        {
            timer = new System.Threading.Timer(_ =>
            {
                InvokeAsync(Close);
            }, null, duration, Timeout.Infinite);
        }
    }

    private async Task Close()
    {
        isVisible = false;
        timer?.Dispose();
        StateHasChanged();
        
        if (OnClose.HasDelegate)
        {
            await OnClose.InvokeAsync();
        }
    }

    private string GetToastClass()
    {
        return type switch
        {
            ToastType.Success => "success",
            ToastType.Error => "error",
            ToastType.Warning => "warning",
            ToastType.Info => "info",
            _ => "info"
        };
    }

    private string GetIconClass()
    {
        return type switch
        {
            ToastType.Success => "check-circle-fill",
            ToastType.Error => "exclamation-circle-fill",
            ToastType.Warning => "exclamation-triangle-fill",
            ToastType.Info => "info-circle-fill",
            _ => "info-circle-fill"
        };
    }

    public void Dispose()
    {
        timer?.Dispose();
    }
}
