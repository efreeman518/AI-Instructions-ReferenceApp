using MudBlazor;

namespace TaskFlow.Blazor.Services;

/// <summary>
/// Scoped singleton shared across layout and pages. Holds cross-page state,
/// tracks in-flight API calls, and wraps calls so the layout can show a
/// global progress indicator without each page reinventing that plumbing.
/// Pages receive it via @inject rather than cascading parameters.
/// </summary>
public class FloatService
{
    private readonly ISnackbar _snackbar;
    private int _pendingRequests;

    /// <summary>Initializes float service with required dependencies and default state.</summary>
    public FloatService(ISnackbar snackbar)
    {
        _snackbar = snackbar;
    }

    // Layout-driven UI state
    public string ModuleName { get; set; } = string.Empty;
    public bool RequestIsActive => _pendingRequests > 0;

    // Cross-page signals. A page (e.g. TaskItem) raises TaskItemsChanged so
    // TaskList or Dashboard can refresh without being coupled to the sender.
    public event Action? TaskItemsChanged;
    public event Action? CategoriesChanged;
    public event Action? TagsChanged;

    /// <summary>Provides the notify task items changed operation for float service.</summary>
    public void NotifyTaskItemsChanged() => TaskItemsChanged?.Invoke();
    /// <summary>Provides the notify categories changed operation for float service.</summary>
    public void NotifyCategoriesChanged() => CategoriesChanged?.Invoke();
    /// <summary>Provides the notify tags changed operation for float service.</summary>
    public void NotifyTagsChanged() => TagsChanged?.Invoke();

    // The layout subscribes so AppBar repaints whenever RequestIsActive flips.
    public Action? StateHasChanged { get; set; }

    /// <summary>Provides the execute with progress operation for float service.</summary>
    public async Task<T?> ExecuteWithProgressAsync<T>(Func<Task<T>> call, string? errorMessage = null)
    {
        try
        {
            Interlocked.Increment(ref _pendingRequests);
            StateHasChanged?.Invoke();
            return await call();
        }
        catch (OperationCanceledException)
        {
            // Circuit/request cancelled (navigation, tab close, Playwright teardown).
            return default;
        }
        catch (Microsoft.JSInterop.JSDisconnectedException)
        {
            // Client disconnected while awaiting a JS-backed UI update.
            return default;
        }
        catch (Exception ex)
        {
            _snackbar.Add(errorMessage ?? ex.Message, Severity.Error);
            return default;
        }
        finally
        {
            Interlocked.Decrement(ref _pendingRequests);
            StateHasChanged?.Invoke();
        }
    }

    /// <summary>Provides the execute with progress operation for float service.</summary>
    public async Task<bool> ExecuteWithProgressAsync(Func<Task> call, string? errorMessage = null)
    {
        try
        {
            Interlocked.Increment(ref _pendingRequests);
            StateHasChanged?.Invoke();
            await call();
            return true;
        }
        catch (OperationCanceledException)
        {
            // Circuit/request cancelled (navigation, tab close, Playwright teardown).
            return false;
        }
        catch (Microsoft.JSInterop.JSDisconnectedException)
        {
            // Client disconnected while awaiting a JS-backed UI update.
            return false;
        }
        catch (Exception ex)
        {
            _snackbar.Add(errorMessage ?? ex.Message, Severity.Error);
            return false;
        }
        finally
        {
            Interlocked.Decrement(ref _pendingRequests);
            StateHasChanged?.Invoke();
        }
    }

    /// <summary>Provides the show snack operation for float service.</summary>
    public void ShowSnack(string message, Severity severity = Severity.Normal)
        => _snackbar.Add(message, severity);
}
