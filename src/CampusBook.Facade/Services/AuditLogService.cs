using CampusBook.DataAccess.Interface;
using CampusBook.Entities;
using CampusBook.Facade.ViewModels.Admin;
using Microsoft.Extensions.Logging;

namespace CampusBook.Facade.Services;

public class AuditLogService : IAuditLogService
{
    private readonly IAuditLogRepository _auditLogRepo;
    private readonly ILogger<AuditLogService> _logger;

    public AuditLogService(IAuditLogRepository auditLogRepo, ILogger<AuditLogService> logger)
    {
        _auditLogRepo = auditLogRepo;
        _logger = logger;
    }

    public async Task LogAsync(
        int? adminUserId,
        string action,
        string targetEntityType,
        string? targetId,
        string details,
        string? ipAddress = null,
        bool isSuccess = true)
    {
        try
        {
            var log = new AuditLog
            {
                AdminUserId = adminUserId,
                Action = action,
                TargetEntityType = targetEntityType,
                TargetId = targetId,
                Details = details,
                IpAddress = ipAddress,
                TimestampUtc = DateTime.UtcNow,
                IsSuccess = isSuccess
            };

            await _auditLogRepo.LogAsync(log);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write audit log for action {Action} on {TargetType}:{TargetId}", action, targetEntityType, targetId);
        }
    }

    public async Task<AuditLogListViewModel> GetLogsPagedAsync(
        string? action,
        int? adminUserId,
        DateTime? fromDate,
        DateTime? toDate,
        int pageIndex,
        int pageSize)
    {
        var (items, totalCount) = await _auditLogRepo.GetLogsPagedAsync(action, adminUserId, fromDate, toDate, pageIndex, pageSize);

        return new AuditLogListViewModel
        {
            Logs = items,
            TotalCount = totalCount,
            PageIndex = pageIndex,
            PageSize = pageSize,
            Action = action,
            AdminUserId = adminUserId,
            FromDate = fromDate,
            ToDate = toDate
        };
    }
}
