using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace DonutMS.Core.MVVM;

public abstract class BaseViewModel : ObservableObject
{
    protected readonly ILogger Logger;
    private bool _isLoading;
    private string? _errorMessage;

    protected BaseViewModel(ILogger? logger)
    {
        Logger = logger ?? NullLogger.Instance;
    }

    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    protected virtual void ClearError() => ErrorMessage = null;

    protected void LogInfo(string message) => Logger.LogInformation(message);
    protected void LogDebug(string message) => Logger.LogDebug(message);
    protected void LogWarning(string message) => Logger.LogWarning(message);
    protected void LogError(string message) => Logger.LogError(message);
    
    protected virtual void SetError(string message)
    {
        ErrorMessage = message;
        Logger.LogError(message);
    }
}
