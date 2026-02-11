using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using DonutMS.Data.Entities;
using DonutMS.Data.Repositories;
using DonutMS.Core.Utils;
using DonutMS.Models;
using DonutMS.Models.DTOs;

namespace DonutMS.Services;

public interface IAuthService
{
    UserContext CurrentUser { get; set; }
    Task<UserContext?> LoginAsync(string username, string password);
    Task LogoutAsync();
    Task<IEnumerable<UserDto>> GetAllUsersAsync(bool includeInactive = false);
    Task<UserDto> CreateUserAsync(CreateUserDto dto);
    Task<UserDto> UpdateUserAsync(int id, UpdateUserDto dto, string? newPassword = null);
    Task<bool> ToggleUserActiveAsync(int id, bool isActive);
    Task<bool> ResetPasswordAsync(int id, string newPassword);
}

public class AuthService : IAuthService
{
    private readonly IRepository<User> _userRepository;
    private readonly IEncryptionService _encryptionService;
    private readonly IAuditService _auditService;
    private readonly ILogger<AuthService> _logger;

    public UserContext CurrentUser { get; set; } = new()
    {
        IsAuthenticated = false
    };

    public AuthService(
        IRepository<User> userRepository,
        IEncryptionService encryptionService,
        IAuditService auditService,
        ILogger<AuthService> logger)
    {
        _userRepository = userRepository;
        _encryptionService = encryptionService;
        _auditService = auditService;
        _logger = logger;
    }

    public async Task<UserContext?> LoginAsync(string username, string password)
    {
        var user = await _userRepository.AsQueryable()
            .FirstOrDefaultAsync(u => u.Username.ToLower() == username.ToLower());

        if (user == null || !user.IsActive)
            return null;

        if (!PasswordHasher.VerifyHash(password, user.PasswordHash, user.PasswordSalt))
            return null;

        user.LastLogin = DateTime.UtcNow;
        await _userRepository.UpdateAsync(user);
        await _userRepository.SaveChangesAsync();

        CurrentUser = new UserContext
        {
            UserId = user.Id,
            Username = user.Username,
            FullName = user.FullName ?? user.Username,
            Role = ParseRole(user.Role),
            IsAuthenticated = true,
            LoginTime = DateTime.Now
        };

        await _auditService.LogAsync("User", user.Id, "Login", user.Id.ToString(), user.Username);
        _logger.LogInformation("User {User} logged in", user.Username);
        return CurrentUser;
    }

    public async Task LogoutAsync()
    {
        var userId = CurrentUser.UserId;
        var username = CurrentUser.Username;

        CurrentUser = new UserContext { IsAuthenticated = false };
        await _auditService.LogAsync("User", userId, "Logout", userId.ToString(), username);
    }

    public async Task<IEnumerable<UserDto>> GetAllUsersAsync(bool includeInactive = false)
    {
        var users = await _userRepository.AsQueryable()
            .Where(u => includeInactive || u.IsActive)
            .OrderBy(u => u.Username)
            .ToListAsync();

        return users.Select(MapUser).ToList();
    }

    public async Task<UserDto> CreateUserAsync(CreateUserDto dto)
    {
        var existing = await _userRepository.AsQueryable()
            .FirstOrDefaultAsync(u => u.Username.ToLower() == dto.Username.ToLower());
        if (existing != null)
            throw new InvalidOperationException($"Username '{dto.Username}' already exists");

        var (hash, salt) = PasswordHasher.CreateHash(dto.Password);

        var user = new User
        {
            Username = dto.Username.Trim(),
            Email = _encryptionService.Protect(dto.Email),
            FullName = dto.FullName?.Trim(),
            PasswordHash = hash,
            PasswordSalt = salt,
            Role = string.IsNullOrWhiteSpace(dto.Role) ? "Operator" : dto.Role,
            IsActive = dto.IsActive,
            Notes = _encryptionService.Protect(dto.Notes)
        };

        await _userRepository.AddAsync(user);
        await _userRepository.SaveChangesAsync();

        await _auditService.LogAsync("User", user.Id, "Create", CurrentUser.UserId.ToString(), CurrentUser.Username, null, user.Username);
        _logger.LogInformation("User created: {User}", user.Username);
        return MapUser(user);
    }

    public async Task<UserDto> UpdateUserAsync(int id, UpdateUserDto dto, string? newPassword = null)
    {
        var user = await _userRepository.GetByIdAsync(id);
        if (user == null)
            throw new KeyNotFoundException($"User with ID {id} not found");

        var oldSnapshot = MapUser(user);

        if (!string.IsNullOrWhiteSpace(dto.Email))
            user.Email = _encryptionService.Protect(dto.Email);
        if (!string.IsNullOrWhiteSpace(dto.FullName))
            user.FullName = dto.FullName;
        if (!string.IsNullOrWhiteSpace(dto.Role))
            user.Role = dto.Role;
        if (dto.IsActive.HasValue)
            user.IsActive = dto.IsActive.Value;
        if (dto.Notes != null)
            user.Notes = _encryptionService.Protect(dto.Notes);

        if (!string.IsNullOrWhiteSpace(newPassword))
        {
            var (hash, salt) = PasswordHasher.CreateHash(newPassword);
            user.PasswordHash = hash;
            user.PasswordSalt = salt;
        }

        await _userRepository.UpdateAsync(user);
        await _userRepository.SaveChangesAsync();

        var newSnapshot = MapUser(user);
        await _auditService.LogAsync("User", user.Id, "Update", CurrentUser.UserId.ToString(), CurrentUser.Username,
            oldSnapshot.Username, newSnapshot.Username);

        return MapUser(user);
    }

    public async Task<bool> ToggleUserActiveAsync(int id, bool isActive)
    {
        var user = await _userRepository.GetByIdAsync(id);
        if (user == null)
            return false;

        user.IsActive = isActive;
        await _userRepository.UpdateAsync(user);
        await _userRepository.SaveChangesAsync();

        await _auditService.LogAsync("User", user.Id, isActive ? "Activate" : "Deactivate", CurrentUser.UserId.ToString(), CurrentUser.Username);
        return true;
    }

    public async Task<bool> ResetPasswordAsync(int id, string newPassword)
    {
        var user = await _userRepository.GetByIdAsync(id);
        if (user == null)
            return false;

        var (hash, salt) = PasswordHasher.CreateHash(newPassword);
        user.PasswordHash = hash;
        user.PasswordSalt = salt;

        await _userRepository.UpdateAsync(user);
        await _userRepository.SaveChangesAsync();

        await _auditService.LogAsync("User", user.Id, "ResetPassword", CurrentUser.UserId.ToString(), CurrentUser.Username);
        return true;
    }

    private UserDto MapUser(User user)
    {
        return new UserDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = _encryptionService.Unprotect(user.Email),
            FullName = user.FullName,
            Role = user.Role,
            IsActive = user.IsActive,
            LastLogin = user.LastLogin,
            Notes = _encryptionService.Unprotect(user.Notes)
        };
    }

    private static UserRole ParseRole(string? role)
    {
        if (!string.IsNullOrWhiteSpace(role) &&
            Enum.TryParse<UserRole>(role, true, out var parsed))
        {
            return parsed;
        }

        return UserRole.Operator;
    }
}
