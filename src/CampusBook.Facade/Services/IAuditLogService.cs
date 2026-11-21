using CampusBook.Entities;
using CampusBook.Facade.ViewModels.Admin;

namespace CampusBook.Facade.Services;

public interface IAuditLogService
{
    Task LogAsync(int? adminUserId, string action, string targetEntityType, string? targetId, string details, string? ipAddress = null, bool isSuccess = true);
    Task<AuditLogListViewModel> GetLogsPagedAsync(string? action, int? adminUserId, DateTime? fromDate, DateTime? toDate, int pageIndex, int pageSize);
}
