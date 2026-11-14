using CampusBook.Facade.ViewModels.History;

namespace CampusBook.Facade.Interface;

public interface IExchangeHistoryFacade
{
    Task<ExchangeHistoryPageViewModel> GetUserExchangeHistoryAsync(int userId, HistoryRoleFilter filter = HistoryRoleFilter.All);
}
