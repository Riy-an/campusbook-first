using CampusBook.Entities;

namespace CampusBook.DataAccess.Interface;

public interface IAuditLogRepository
{
    Task LogAsync(AuditLog log);
    Task<(IEnumerable<AuditLog> Items, int TotalCount)> GetLogsPagedAsync(
        string? action,
        int? adminUserId,
        DateTime? fromDate,
        DateTime? toDate,
        int pageIndex,
        int pageSize);
}
