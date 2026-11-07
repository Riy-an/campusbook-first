using CampusBook.Facade.Common;
using CampusBook.Facade.ViewModels.Profile;

namespace CampusBook.Facade.Interface;

public interface IStudentProfileFacade
{
    Task<ProfileDetailsViewModel?> GetMyProfileAsync(int userId);
    Task<ProfileEditViewModel?> GetProfileForEditAsync(int userId);
    Task<ServiceResult> UpdateProfileAsync(int userId, ProfileEditViewModel model);
    Task<PublicProfileViewModel?> GetPublicProfileAsync(int targetUserId);
}

