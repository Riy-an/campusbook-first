using CampusBook.Entities;

namespace CampusBook.DataAccess.Interface;

public interface IConversationRepository : IRepository<Conversation>
{
    Task<Conversation?> GetByIdWithMessagesAsync(int conversationId);
    Task<Conversation?> GetByRequestIdAsync(int requestId);
    Task<IEnumerable<Conversation>> GetUserConversationsAsync(int userId);
    Task AddMessageAsync(Message message);
    Task MarkMessagesAsReadAsync(int conversationId, int currentUserId);
    Task<int> GetUnreadMessageCountAsync(int userId);
}
