using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using DonutMS.Core.MVVM;
using DonutMS.Models;
using DonutMS.Models.DTOs;
using DonutMS.Services;

namespace DonutMS.ViewModels;

public partial class RegisterViewModel : BaseViewModel
{
    private readonly IAuthService _authService;

    public event EventHandler? CloseRequested;

    [ObservableProperty]
    private string username = string.Empty;

    [ObservableProperty]
    private string fullName = string.Empty;

    [ObservableProperty]
    private string email = string.Empty;

    [ObservableProperty]
    private string notes = string.Empty;

    [ObservableProperty]
    private ObservableCollection<string> availableRoles = new();

    [ObservableProperty]
    private string selectedRole = "Operator";

    [ObservableProperty]
    private string passwordInput = string.Empty;

    [ObservableProperty]
    private string confirmPasswordInput = string.Empty;

    [ObservableProperty]
    private bool isPasswordVisible;

    [ObservableProperty]
    private bool isConfirmPasswordVisible;

    [ObservableProperty]
    private string statusMessage = string.Empty;

    public RegisterViewModel(IAuthService authService, ILogger<RegisterViewModel> logger) : base(logger)
    {
        _authService = authService;
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        try
        {
            var anyUsers = (await _authService.GetAllUsersAsync(true)).Any();
            InitializeRoles(anyUsers);
        }
        catch (Exception ex)
        {
            SetError($"Unable to load roles: {ex.Message}");
            InitializeRoles(anyUsersExist: true);
        }
    }

    [RelayCommand]
    public async Task RegisterAsync()
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

            if (PasswordInput.Length < 6)
            {
                SetError("Password must be at least 6 characters");
                return;
            }

            if (!string.Equals(PasswordInput, ConfirmPasswordInput, StringComparison.Ordinal))
            {
                SetError("Password confirmation does not match");
                return;
            }

            if (!CanRegisterRole(SelectedRole))
            {
                SetError("You do not have permission to assign this role");
                return;
            }

            IsLoading = true;

            var dto = new CreateUserDto
            {
                Username = Username.Trim(),
                FullName = string.IsNullOrWhiteSpace(FullName) ? null : FullName.Trim(),
                Email = string.IsNullOrWhiteSpace(Email) ? null : Email.Trim(),
                Role = SelectedRole,
                Notes = string.IsNullOrWhiteSpace(Notes) ? null : Notes.Trim(),
                Password = PasswordInput,
                IsActive = true
            };

            await _authService.CreateUserAsync(dto);

            StatusMessage = $"User '{dto.Username}' registered successfully";
            ClearForm();
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            SetError($"Registration failed: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public void Cancel()
    {
        ClearForm();
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    private void InitializeRoles(bool anyUsersExist)
    {
        var roles = Enum.GetNames(typeof(UserRole)).ToList();
        if (!anyUsersExist)
        {
            AvailableRoles = new ObservableCollection<string>(roles);
        }
        else if (_authService.CurrentUser.IsAuthenticated && _authService.CurrentUser.Role.HasFlag(UserRole.Admin))
        {
            AvailableRoles = new ObservableCollection<string>(roles);
        }
        else
        {
            AvailableRoles = new ObservableCollection<string>(new[] { "Operator", "Kasir" });
        }

        SelectedRole = AvailableRoles.Contains("Admin") ? "Admin" : AvailableRoles.FirstOrDefault() ?? "Operator";
    }

    private bool CanRegisterRole(string? role)
    {
        if (string.IsNullOrWhiteSpace(role))
            return false;

        if (_authService.CurrentUser.IsAuthenticated && _authService.CurrentUser.Role.HasFlag(UserRole.Admin))
            return true;

        return role is "Operator" or "Kasir";
    }

    private void ClearForm()
    {
        Username = string.Empty;
        FullName = string.Empty;
        Email = string.Empty;
        Notes = string.Empty;
        PasswordInput = string.Empty;
        ConfirmPasswordInput = string.Empty;
    }
}
