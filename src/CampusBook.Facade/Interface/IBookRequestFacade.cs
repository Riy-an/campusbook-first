using CampusBook.Facade.Common;
using CampusBook.Facade.ViewModels.Requests;

namespace CampusBook.Facade.Interface;

public interface IBookRequestFacade
{
    Task<CreateRequestViewModel?> GetCreateRequestViewModelAsync(int bookId, int requesterId);
    Task<ServiceResult<int>> CreateRequestAsync(int requesterId, CreateRequestViewModel model);
    Task<IEnumerable<SentRequestViewModel>> GetSentRequestsAsync(int requesterId);
    Task<IEnumerable<ReceivedRequestViewModel>> GetReceivedRequestsAsync(int ownerId);
    Task<RequestDetailsViewModel?> GetRequestDetailsAsync(int requestId, int currentUserId);
    Task<ServiceResult> CancelRequestAsync(int requestId, int requesterId);
    Task<ServiceResult> AcceptRequestAsync(int requestId, int ownerId);
    Task<ServiceResult> RejectRequestAsync(int requestId, int ownerId);
    Task<ServiceResult> ConfirmHandoverAsync(int requestId, int currentUserId);
}

