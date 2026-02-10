namespace DonutMS.Services;

public class PlaceholderService : IPlaceholderService
{
    public Task<bool> HealthCheckAsync()
    {
        return Task.FromResult(true);
    }
}
