using CampusBook.DataAccess.Interface;
using CampusBook.Entities;
using Microsoft.EntityFrameworkCore;

namespace CampusBook.DataAccess.Implementation;

public class AuditLogRepository : Repository<AuditLog>, IAuditLogRepository
{
    public AuditLogRepository(ApplicationDbContext dbContext) : base(dbContext)
    {
    }

    public async Task LogAsync(AuditLog log)
    {
        ArgumentNullException.ThrowIfNull(log);
        log.TimestampUtc = DateTime.UtcNow;
        await _dbContext.AuditLogs.AddAsync(log);
        await _dbContext.SaveChangesAsync();
    }

    public async Task<(IEnumerable<AuditLog> Items, int TotalCount)> GetLogsPagedAsync(
        string? action,
        int? adminUserId,
        DateTime? fromDate,
        DateTime? toDate,
        int pageIndex,
        int pageSize)
    {
        var query = _dbContext.AuditLogs
            .Include(a => a.AdminUser)
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(action))
        {
            query = query.Where(a => a.Action.Contains(action));
        }

        if (adminUserId.HasValue)
        {
            query = query.Where(a => a.AdminUserId == adminUserId.Value);
        }

        if (fromDate.HasValue)
        {
            query = query.Where(a => a.TimestampUtc >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(a => a.TimestampUtc <= toDate.Value);
        }

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(a => a.TimestampUtc)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }
}
