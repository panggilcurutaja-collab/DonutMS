using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using DonutMS.Core.MVVM;
using DonutMS.Models;
using DonutMS.Models.DTOs;
using DonutMS.Services;

namespace DonutMS.ViewModels;

public partial class SecurityViewModel : BaseViewModel
{
    private readonly IAuditService _auditService;
    private readonly IAuthService _authService;
    private readonly IBackupService _backupService;

    [ObservableProperty]
    private ObservableCollection<AuditLogDto> auditLogs = new();

    [ObservableProperty]
    private DateTime auditFromDate = DateTime.Today.AddDays(-30);

    [ObservableProperty]
    private DateTime auditToDate = DateTime.Today;

    [ObservableProperty]
    private string auditSearchText = string.Empty;

    [ObservableProperty]
    private ObservableCollection<UserDto> users = new();

    [ObservableProperty]
    private ObservableCollection<string> availableRoles = new(Enum.GetNames(typeof(UserRole)));

    [ObservableProperty]
    private UserDto? selectedUser;

    [ObservableProperty]
    private UserDto? editingUser;

    [ObservableProperty]
    private bool isUserEditorOpen;

    [ObservableProperty]
    private string userDialogTitle = "Add User";

    [ObservableProperty]
    private string passwordInput = string.Empty;

    [ObservableProperty]
    private string confirmPasswordInput = string.Empty;

    [ObservableProperty]
    private bool includeInactiveUsers;

    [ObservableProperty]
    private string backupFolder = string.Empty;

    [ObservableProperty]
    private string? lastBackupPath;

    [ObservableProperty]
    private string statusMessage = string.Empty;

    public SecurityViewModel(
        IAuditService auditService,
        IAuthService authService,
        IBackupService backupService,
        ILogger<SecurityViewModel> logger) : base(logger)
    {
        _auditService = auditService;
        _authService = authService;
        _backupService = backupService;
        BackupFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "DonutMS",
            "Backups");
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        await LoadAuditLogsAsync();
        await LoadUsersAsync();
    }

    [RelayCommand]
    public async Task LoadAuditLogsAsync()
    {
        try
        {
            IsLoading = true;
            ClearError();

            var logs = await _auditService.GetByDateRangeAsync(AuditFromDate, AuditToDate.AddDays(1));
            if (!string.IsNullOrWhiteSpace(AuditSearchText))
            {
                logs = logs.Where(l =>
                    l.EntityName.Contains(AuditSearchText, StringComparison.OrdinalIgnoreCase) ||
                    l.Action.Contains(AuditSearchText, StringComparison.OrdinalIgnoreCase) ||
                    (l.UserName?.Contains(AuditSearchText, StringComparison.OrdinalIgnoreCase) ?? false));
            }

            AuditLogs = new ObservableCollection<AuditLogDto>(logs);
            StatusMessage = $"Loaded {AuditLogs.Count} audit log(s)";
        }
        catch (Exception ex)
        {
            SetError($"Error loading audit logs: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task LoadUsersAsync()
    {
        try
        {
            IsLoading = true;
            ClearError();

            var list = await _authService.GetAllUsersAsync(IncludeInactiveUsers);
            Users = new ObservableCollection<UserDto>(list);
            StatusMessage = $"Loaded {Users.Count} user(s)";
        }
        catch (Exception ex)
        {
            SetError($"Error loading users: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public void OpenAddUser()
    {
        ClearError();
        EditingUser = new UserDto { Role = "Operator", IsActive = true };
        UserDialogTitle = "Add User";
        PasswordInput = string.Empty;
        ConfirmPasswordInput = string.Empty;
        IsUserEditorOpen = true;
    }

    [RelayCommand]
    public void OpenEditUser(UserDto user)
    {
        if (user == null)
        {
            SetError("Select a user to edit");
            return;
        }

        EditingUser = new UserDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            FullName = user.FullName,
            Role = user.Role,
            IsActive = user.IsActive,
            Notes = user.Notes
        };
        UserDialogTitle = $"Edit User - {user.Username}";
        PasswordInput = string.Empty;
        ConfirmPasswordInput = string.Empty;
        IsUserEditorOpen = true;
    }

    [RelayCommand]
    public async Task SaveUserAsync()
    {
        try
        {
            if (EditingUser == null)
            {
                SetError("No user data to save");
                return;
            }

            if (string.IsNullOrWhiteSpace(EditingUser.Username))
            {
                SetError("Username is required");
                return;
            }

            if (EditingUser.Id == 0 && string.IsNullOrWhiteSpace(PasswordInput))
            {
                SetError("Password is required for new user");
                return;
            }

            if (!string.Equals(PasswordInput, ConfirmPasswordInput, StringComparison.Ordinal))
            {
                SetError("Password confirmation does not match");
                return;
            }

            IsLoading = true;
            ClearError();

            if (EditingUser.Id == 0)
            {
                var createDto = new CreateUserDto
                {
                    Username = EditingUser.Username,
                    Email = EditingUser.Email,
                    FullName = EditingUser.FullName,
                    Role = EditingUser.Role,
                    Password = PasswordInput,
                    Notes = EditingUser.Notes,
                    IsActive = EditingUser.IsActive
                };

                await _authService.CreateUserAsync(createDto);
                StatusMessage = $"User '{EditingUser.Username}' created";
            }
            else
            {
                var updateDto = new UpdateUserDto
                {
                    Email = EditingUser.Email,
                    FullName = EditingUser.FullName,
                    Role = EditingUser.Role,
                    IsActive = EditingUser.IsActive,
                    Notes = EditingUser.Notes
                };

                await _authService.UpdateUserAsync(EditingUser.Id, updateDto, string.IsNullOrWhiteSpace(PasswordInput) ? null : PasswordInput);
                StatusMessage = $"User '{EditingUser.Username}' updated";
            }

            IsUserEditorOpen = false;
            EditingUser = null;
            PasswordInput = string.Empty;
            ConfirmPasswordInput = string.Empty;

            await LoadUsersAsync();
        }
        catch (Exception ex)
        {
            SetError($"Error saving user: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public void CancelEdit()
    {
        IsUserEditorOpen = false;
        EditingUser = null;
        PasswordInput = string.Empty;
        ConfirmPasswordInput = string.Empty;
    }

    [RelayCommand]
    public async Task ToggleUserActiveAsync(UserDto user)
    {
        try
        {
            if (user == null)
                return;

            IsLoading = true;
            ClearError();

            await _authService.ToggleUserActiveAsync(user.Id, !user.IsActive);
            StatusMessage = user.IsActive ? $"User '{user.Username}' deactivated" : $"User '{user.Username}' activated";
            await LoadUsersAsync();
        }
        catch (Exception ex)
        {
            SetError($"Error updating user: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task CreateBackupAsync()
    {
        try
        {
            IsLoading = true;
            ClearError();

            var path = await _backupService.CreateBackupAsync(BackupFolder);
            LastBackupPath = path;
            StatusMessage = $"Backup created: {path}";
        }
        catch (Exception ex)
        {
            SetError($"Backup failed: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }
}
