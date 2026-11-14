using CampusBook.DataAccess;
using CampusBook.DataAccess.Implementation;
using CampusBook.Entities;
using CampusBook.Facade.Implementation;
using CampusBook.Facade.Services;
using CampusBook.Facade.ViewModels.Profile;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace CampusBook.Tests;

public class ProfileTests
{
    private static IFormFile CreateMockFile(string fileName)
    {
        var mock = new Mock<IFormFile>();
        mock.Setup(f => f.Length).Returns(2048);
        mock.Setup(f => f.FileName).Returns(fileName);
        return mock.Object;
    }

    private (StudentProfileFacade facade, ApplicationDbContext context, User student) SetupProfileEnvironment(string dbName)
    {
        var context = TestDbContextFactory.Create(dbName);
        var userRepo = new UserRepository(context);
        var profileRepo = new StudentProfileRepository(context);
        var bookRepo = new BookRepository(context);
        var requestRepo = new BookRequestRepository(context);
        var fileStorageMock = new Mock<IFileStorageService>();

        string? error;
        fileStorageMock.Setup(s => s.IsValidImage(It.IsAny<IFormFile>(), out error)).Returns(true);
        fileStorageMock.Setup(s => s.SaveFileAsync(It.IsAny<IFormFile>(), It.IsAny<string>()))
            .ReturnsAsync("/uploads/idcards/updated_card.jpg");

        var logger = NullLogger<StudentProfileFacade>.Instance;

        var student = new User
        {
            UserId = 1,
            FullName = "Grace Hopper",
            StudentId = "STU-1008",
            Email = "grace@university.edu",
            PasswordHash = "hashed_pw",
            RoleId = 1,
            IsEmailConfirmed = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            StudentProfile = new StudentProfile
            {
                UserId = 1,
                Department = "Computer Science",
                Batch = "Fall 2026",
                PhoneNumber = "+1 555-0199",
                Bio = "Loving compilers and programming languages."
            }
        };

        context.Users.Add(student);
        context.SaveChanges();

        var facade = new StudentProfileFacade(userRepo, profileRepo, bookRepo, requestRepo, fileStorageMock.Object, logger);
        return (facade, context, student);
    }

    [Fact]
    public async Task GetMyProfileAsync_ReturnsFullProfileData()
    {
        // Arrange
        var (facade, _, student) = SetupProfileEnvironment(Guid.NewGuid().ToString());

        // Act
        var profile = await facade.GetMyProfileAsync(student.UserId);

        // Assert
        Assert.NotNull(profile);
        Assert.Equal("Grace Hopper", profile.FullName);
        Assert.Equal("STU-1008", profile.StudentId);
        Assert.Equal("grace@university.edu", profile.Email);
        Assert.Equal("Computer Science", profile.Department);
        Assert.Equal("+1 555-0199", profile.PhoneNumber);
    }

    [Fact]
    public async Task UpdateProfileAsync_OnlyModifiesPermittedFields_AndProtectsSensitiveFields()
    {
        // Arrange
        var (facade, context, student) = SetupProfileEnvironment(Guid.NewGuid().ToString());

        var editModel = new ProfileEditViewModel
        {
            FullName = "Grace M. Hopper",
            Department = "Software Engineering",
            Batch = "Spring 2027",
            Semester = "6th Semester",
            PhoneNumber = "+1 555-9999",
            Bio = "Pioneered the first compiler."
        };

        // Act
        var result = await facade.UpdateProfileAsync(student.UserId, editModel);

        // Assert
        Assert.True(result.Success);

        // Verify database state directly
        var updatedUser = await context.Users.FindAsync(student.UserId);
        Assert.NotNull(updatedUser);
        Assert.Equal("Grace M. Hopper", updatedUser.FullName);
        Assert.Equal("Software Engineering", updatedUser.StudentProfile?.Department);
        Assert.Equal("6th Semester", updatedUser.StudentProfile?.Semester);

        // Verify protected security-sensitive fields WERE NOT OVERWRITTEN!
        Assert.Equal("STU-1008", updatedUser.StudentId);
        Assert.Equal("grace@university.edu", updatedUser.Email);
        Assert.Equal(1, updatedUser.RoleId);
        Assert.True(updatedUser.IsEmailConfirmed);
    }

    [Fact]
    public async Task GetPublicProfileAsync_HidesPrivateContactDetails()
    {
        // Arrange
        var (facade, _, student) = SetupProfileEnvironment(Guid.NewGuid().ToString());

        // Act
        var publicProfile = await facade.GetPublicProfileAsync(student.UserId);

        // Assert
        Assert.NotNull(publicProfile);
        Assert.Equal("Grace Hopper", publicProfile.FullName);
        Assert.Equal("Computer Science", publicProfile.Department);
        Assert.Equal("Fall 2026", publicProfile.Batch);
        Assert.Equal("Loving compilers and programming languages.", publicProfile.Bio);
        // Notice PublicProfileViewModel does NOT even have Email or PhoneNumber properties,
        // strictly enforcing privacy at the DTO contract boundary!
    }

    [Fact]
    public async Task UpdateProfileAsync_WhenChangingIdCardUnderLimit_SucceedsAndIncrementsCount()
    {
        // Arrange
        var (facade, context, student) = SetupProfileEnvironment(Guid.NewGuid().ToString());
        var editModel = new ProfileEditViewModel
        {
            FullName = student.FullName,
            Department = "Computer Science",
            Batch = "Fall 2026",
            StudentIdCard = CreateMockFile("new_card.jpg")
        };

        // Act
        var result = await facade.UpdateProfileAsync(student.UserId, editModel);

        // Assert
        Assert.True(result.Success);
        var refreshed = await context.Users.FindAsync(student.UserId);
        Assert.NotNull(refreshed?.StudentProfile);
        Assert.Equal(1, refreshed.StudentProfile.IdCardChangeCount);
        Assert.Equal("/uploads/idcards/updated_card.jpg", refreshed.StudentProfile.StudentIdCardUrl);
    }

    [Fact]
    public async Task UpdateProfileAsync_WhenChangingIdCardReachesLimit_RejectsSubsequentChanges()
    {
        // Arrange - User already changed ID card 3 times
        var (facade, context, student) = SetupProfileEnvironment(Guid.NewGuid().ToString());
        student.StudentProfile!.IdCardChangeCount = 3;
        await context.SaveChangesAsync();

        var editModel = new ProfileEditViewModel
        {
            FullName = student.FullName,
            Department = "Computer Science",
            Batch = "Fall 2026",
            StudentIdCard = CreateMockFile("attempt_four.jpg")
        };

        // Act - Attempt 4th change
        var result = await facade.UpdateProfileAsync(student.UserId, editModel);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("maximum limit of 3 ID card photo changes", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);

        var refreshed = await context.Users.FindAsync(student.UserId);
        Assert.Equal(3, refreshed!.StudentProfile!.IdCardChangeCount);
    }

    [Fact]
    public async Task UpdateProfileAsync_WithoutChangingIdCard_DoesNotIncrementCount()
    {
        // Arrange
        var (facade, context, student) = SetupProfileEnvironment(Guid.NewGuid().ToString());
        student.StudentProfile!.IdCardChangeCount = 2;
        await context.SaveChangesAsync();

        var editModel = new ProfileEditViewModel
        {
            FullName = "Grace Brewster Hopper",
            Department = "Computer Science",
            Batch = "Fall 2026",
            StudentIdCard = null // No ID card file uploaded
        };

        // Act
        var result = await facade.UpdateProfileAsync(student.UserId, editModel);

        // Assert
        Assert.True(result.Success);
        var refreshed = await context.Users.FindAsync(student.UserId);
        Assert.Equal(2, refreshed!.StudentProfile!.IdCardChangeCount); // Remains 2!
    }
}
