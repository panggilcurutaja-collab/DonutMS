using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using DonutMS.Core.MVVM;
using DonutMS.Services;

namespace DonutMS.ViewModels;

public partial class LoginViewModel : BaseViewModel
{
    private readonly IAuthService _authService;
    private readonly ICredentialStore _credentialStore;
    private readonly IWindowService _windowService;

    public event EventHandler? CloseRequested;

    [ObservableProperty]
    private string username = string.Empty;

    [ObservableProperty]
    private string passwordInput = string.Empty;

    [ObservableProperty]
    private bool isPasswordVisible;

    [ObservableProperty]
    private bool rememberMe;

    [ObservableProperty]
    private string statusMessage = string.Empty;

    public bool IsLoginSuccessful { get; private set; }

    public LoginViewModel(
        IAuthService authService,
        ICredentialStore credentialStore,
        IWindowService windowService,
        ILogger<LoginViewModel> logger) : base(logger)
    {
        _authService = authService;
        _credentialStore = credentialStore;
        _windowService = windowService;
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        try
        {
            var stored = await _credentialStore.LoadAsync();
            if (stored != null)
            {
                Username = stored.Username;
                RememberMe = true;
            }
        }
        catch (Exception ex)
        {
            LogWarning($"Failed to load stored credentials: {ex.Message}");
        }
    }

    [RelayCommand]
    public async Task LoginAsync()
    {
        try
        {
            ClearError();
            StatusMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(Username))
            {
                SetError("Username is required");
                return;
            }

            if (string.IsNullOrWhiteSpace(PasswordInput))
            {
                SetError("Password is required");
                return;
            }

            IsLoading = true;

            var user = await _authService.LoginAsync(Username.Trim(), PasswordInput);
            if (user == null)
            {
                SetError("Invalid username or password");
                return;
            }

            if (RememberMe)
            {
                await _credentialStore.SaveAsync(Username.Trim());
            }
            else
            {
                await _credentialStore.ClearAsync();
            }

            IsLoginSuccessful = true;
            StatusMessage = $"Welcome, {user.FullName}";
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            SetError($"Login failed: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public void OpenRegister()
    {
        try
        {
            ClearError();
            _windowService.ShowRegisterWindow();
        }
        catch (Exception ex)
        {
            SetError($"Unable to open register window: {ex.Message}");
        }
    }

    [RelayCommand]
    public void Cancel()
    {
        IsLoginSuccessful = false;
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }
}
