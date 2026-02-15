namespace DonutMS.Configuration;

public sealed class ModuleSettings
{
    public List<string> Enabled { get; set; } = new();

    public bool IsEnabled(string viewName)
    {
        if (Enabled.Count == 0)
            return true;

        return Enabled.Any(m => string.Equals(m, viewName, StringComparison.OrdinalIgnoreCase));
    }
}
