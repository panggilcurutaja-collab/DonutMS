namespace DonutMS.Services;

// Service interfaces akan ditambahkan di phase-phase selanjutnya
public interface IPlaceholderService
{
    Task<bool> HealthCheckAsync();
}
