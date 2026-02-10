namespace DonutMS.Models;

/// <summary>
/// Represents user roles in the Donut Management System.
/// </summary>
[Flags]
public enum UserRole
{
    None = 0,
    Admin = 1,
    ProduksionManager = 2,
    Operator = 4,
    Kasir = 8
}

/// <summary>
/// Represents the current authenticated user context.
/// </summary>
public class UserContext
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public DateTime LoginTime { get; set; }
    public bool IsAuthenticated { get; set; }

    public bool HasRole(UserRole role) => (Role & role) == role;
}
