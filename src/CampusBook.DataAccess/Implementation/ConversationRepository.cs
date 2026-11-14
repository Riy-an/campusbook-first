using CampusBook.DataAccess.Interface;
using CampusBook.Entities;
using Microsoft.EntityFrameworkCore;

namespace CampusBook.DataAccess.Implementation;

public class ConversationRepository : Repository<Conversation>, IConversationRepository
{
    public ConversationRepository(ApplicationDbContext dbContext) : base(dbContext)
    {
    }

    public async Task<Conversation?> GetByIdWithMessagesAsync(int conversationId)
    {
        return await _dbContext.Conversations
            .Include(c => c.Request)
                .ThenInclude(r => r.Book)
            .Include(c => c.Request)
                .ThenInclude(r => r.Requester)
                    .ThenInclude(u => u.StudentProfile)
            .Include(c => c.Request)
                .ThenInclude(r => r.Book.Owner)
                    .ThenInclude(u => u.StudentProfile)
            .Include(c => c.Messages.OrderBy(m => m.SentAt))
                .ThenInclude(m => m.Sender)
            .FirstOrDefaultAsync(c => c.ConversationId == conversationId);
    }

    public async Task<Conversation?> GetByRequestIdAsync(int requestId)
    {
        return await _dbContext.Conversations
            .Include(c => c.Request)
                .ThenInclude(r => r.Book)
            .Include(c => c.Request)
                .ThenInclude(r => r.Requester)
            .Include(c => c.Request)
                .ThenInclude(r => r.Book.Owner)
            .Include(c => c.Messages.OrderBy(m => m.SentAt))
                .ThenInclude(m => m.Sender)
            .FirstOrDefaultAsync(c => c.RequestId == requestId);
    }

    public async Task<IEnumerable<Conversation>> GetUserConversationsAsync(int userId)
    {
        return await _dbContext.Conversations
            .Include(c => c.Request)
                .ThenInclude(r => r.Book)
            .Include(c => c.Request)
                .ThenInclude(r => r.Requester)
                    .ThenInclude(u => u.StudentProfile)
            .Include(c => c.Request)
                .ThenInclude(r => r.Book.Owner)
                    .ThenInclude(u => u.StudentProfile)
            .Include(c => c.Messages)
            .Where(c => c.Request.RequesterId == userId || c.Request.Book.OwnerId == userId)
            .OrderByDescending(c => c.LastActivityAt)
            .ToListAsync();
    }

    public async Task AddMessageAsync(Message message)
    {
        await _dbContext.Messages.AddAsync(message);
        var conversation = await _dbContext.Conversations.FindAsync(message.ConversationId);
        if (conversation != null)
        {
            conversation.LastActivityAt = message.SentAt;
        }
        await _dbContext.SaveChangesAsync();
    }

    public async Task MarkMessagesAsReadAsync(int conversationId, int currentUserId)
    {
        var unread = await _dbContext.Messages
            .Where(m => m.ConversationId == conversationId && m.SenderId != currentUserId && !m.IsRead)
            .ToListAsync();

        if (unread.Any())
        {
            foreach (var msg in unread)
            {
                msg.IsRead = true;
            }
            await _dbContext.SaveChangesAsync();
        }
    }

    public async Task<int> GetUnreadMessageCountAsync(int userId)
    {
        return await _dbContext.Messages
            .CountAsync(m => m.SenderId != userId &&
                             !m.IsRead &&
                             (m.Conversation.Request.RequesterId == userId || m.Conversation.Request.Book.OwnerId == userId));
    }
}
