using AutoMapper;
using Microsoft.Extensions.Logging;
using DonutMS.Data.Entities;
using DonutMS.Data.Repositories;
using DonutMS.Models.DTOs;

namespace DonutMS.Services;

public interface IAuditService
{
    Task LogAsync(
        string entityName,
        int entityId,
        string action,
        string? userId = null,
        string? userName = null,
        string? oldValues = null,
        string? newValues = null,
        string? remarks = null,
        string? ipAddress = null);

    Task<IEnumerable<AuditLogDto>> GetByDateRangeAsync(DateTime fromDate, DateTime toDate);
    Task<IEnumerable<AuditLogDto>> GetByUserAsync(string userId);
    Task<IEnumerable<AuditLogDto>> GetByEntityAsync(string entityName, int entityId);
}

public class AuditService : IAuditService
{
    private readonly IAuditRepository _auditRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<AuditService> _logger;

    public AuditService(
        IAuditRepository auditRepository,
        IMapper mapper,
        ILogger<AuditService> logger)
    {
        _auditRepository = auditRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task LogAsync(
        string entityName,
        int entityId,
        string action,
        string? userId = null,
        string? userName = null,
        string? oldValues = null,
        string? newValues = null,
        string? remarks = null,
        string? ipAddress = null)
    {
        var log = new AuditLog
        {
            EntityName = entityName,
            EntityId = entityId,
            Action = action,
            UserId = string.IsNullOrWhiteSpace(userId) ? "system" : userId,
            UserName = string.IsNullOrWhiteSpace(userName) ? "system" : userName,
            AuditDate = DateTime.UtcNow,
            OldValues = oldValues,
            NewValues = newValues,
            Remarks = remarks,
            IPAddress = ipAddress
        };

        await _auditRepository.AddAsync(log);
        await _auditRepository.SaveChangesAsync();
        _logger.LogInformation("Audit logged: {Entity} {Action} {EntityId}", entityName, action, entityId);
    }

    public async Task<IEnumerable<AuditLogDto>> GetByDateRangeAsync(DateTime fromDate, DateTime toDate)
    {
        var logs = await _auditRepository.GetAuditsByDateRangeAsync(fromDate, toDate);
        return _mapper.Map<IEnumerable<AuditLogDto>>(logs);
    }

    public async Task<IEnumerable<AuditLogDto>> GetByUserAsync(string userId)
    {
        var logs = await _auditRepository.GetAuditsByUserAsync(userId);
        return _mapper.Map<IEnumerable<AuditLogDto>>(logs);
    }

    public async Task<IEnumerable<AuditLogDto>> GetByEntityAsync(string entityName, int entityId)
    {
        var logs = await _auditRepository.GetAuditLogsAsync(entityName, entityId);
        return _mapper.Map<IEnumerable<AuditLogDto>>(logs);
    }
}
