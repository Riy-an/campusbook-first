using CampusBook.Facade.ViewModels.Dashboard;

namespace CampusBook.Facade.Interface;

public interface IDashboardFacade
{
    Task<DashboardViewModel> GetDashboardDataAsync(int userId);
}

