namespace DonutMS.Models;

/// <summary>
/// Represents a navigation menu item with role-based visibility.
/// </summary>
public class MenuItemModel
{
    public string Label { get; set; } = string.Empty;
    public string ViewName { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public UserRole RequiredRoles { get; set; } = UserRole.None;
    public List<MenuItemModel> SubItems { get; set; } = new();
    public int Order { get; set; }

    public bool IsVisibleForRole(UserRole userRole)
    {
        if (RequiredRoles == UserRole.None)
            return true;

        return (userRole & RequiredRoles) != 0;
    }
}
