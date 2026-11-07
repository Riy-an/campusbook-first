using CampusBook.Entities;
using CampusBook.Facade.Common;
using CampusBook.Facade.ViewModels.Account;

namespace CampusBook.Facade.Interface;

public interface IUserFacade
{
    Task<ServiceResult<User>> RegisterAsync(RegisterViewModel model, string confirmationBaseUrl);
    Task<ServiceResult<User>> ValidateLoginAsync(string email, string password);
    Task<ServiceResult> ConfirmEmailAsync(string token);
    Task<ServiceResult> GeneratePasswordResetTokenAsync(string email, string resetBaseUrl);
    Task<ServiceResult> ResetPasswordAsync(ResetPasswordViewModel model);
    Task<User?> GetUserByIdAsync(int userId);
    Task<bool> IsEmailConfirmedAsync(int userId);
}

