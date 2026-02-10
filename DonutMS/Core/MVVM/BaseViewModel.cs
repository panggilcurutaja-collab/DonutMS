using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;

namespace DonutMS.Core.MVVM;

public abstract class BaseViewModel : ObservableObject
{
    protected readonly ILogger<BaseViewModel> Logger;
    private bool _isLoading;
    private string? _errorMessage;

    protected BaseViewModel(ILogger<BaseViewModel> logger)
    {
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
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

    protected virtual void SetError(string message)
    {
        ErrorMessage = message;
        Logger.LogError(message);
    }

    protected virtual void LogInfo(string message) => Logger.LogInformation(message);
    protected virtual void LogWarning(string message) => Logger.LogWarning(message);
    protected virtual void LogError(string message) => Logger.LogError(message);
}
