using CampusBook.Facade.Common;
using CampusBook.Facade.ViewModels.Messages;

namespace CampusBook.Facade.Interface;

public interface IConversationFacade
{
    Task<IEnumerable<ConversationSummaryViewModel>> GetUserConversationsAsync(int userId);
    Task<ConversationDetailsViewModel?> GetConversationDetailsAsync(int conversationId, int userId);
    Task<ServiceResult> SendMessageAsync(int conversationId, int senderId, string content);
    Task<int> GetUnreadMessageCountAsync(int userId);
}
