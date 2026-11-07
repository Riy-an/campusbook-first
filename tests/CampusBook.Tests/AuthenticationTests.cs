using CampusBook.DataAccess;
using CampusBook.DataAccess.Implementation;
using CampusBook.Entities;
using CampusBook.Facade.Implementation;
using CampusBook.Facade.Services;
using CampusBook.Facade.ViewModels.Account;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace CampusBook.Tests;

public class AuthenticationTests
{
    private (UserFacade facade, ApplicationDbContext context) CreateFacade(
        string dbName,
        Dictionary<string, string?>? configValues = null,
        Mock<IFileStorageService>? fileStorageMock = null)
    {
        var context = TestDbContextFactory.Create(dbName);
        var userRepo = new UserRepository(context);
        var passwordHasher = new PasswordHasher<User>();
        var emailSenderMock = new Mock<IEmailSender>();
        var storageMock = fileStorageMock ?? new Mock<IFileStorageService>();
        var logger = NullLogger<UserFacade>.Instance;

        var inMemorySettings = configValues ?? new Dictionary<string, string?>();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        var facade = new UserFacade(userRepo, passwordHasher, emailSenderMock.Object, configuration, storageMock.Object, logger);
        return (facade, context);
    }

    [Fact]
    public async Task RegisterAsync_WithValidStudentData_SucceedsAndHashesPassword()
    {
        // Arrange
        var (facade, context) = CreateFacade(Guid.NewGuid().ToString());
        var model = new RegisterViewModel
        {
            FullName = "Alice Johnson",
            StudentId = "STU-1001",
            Email = "alice@university.edu",
            Password = "SecurePassword123!",
            ConfirmPassword = "SecurePassword123!",
            Department = "Computer Science",
            Batch = "2026"
        };

        // Act
        var result = await facade.RegisterAsync(model, "https://localhost:7155");

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal("alice@university.edu", result.Data.Email);
        Assert.Equal("STU-1001", result.Data.StudentId);
        Assert.False(result.Data.IsEmailConfirmed); // Must require confirmation!
        Assert.NotEqual("SecurePassword123!", result.Data.PasswordHash); // Never plain text!
        Assert.Equal(1, result.Data.RoleId); // Role must be Student!
    }

    [Fact]
    public async Task RegisterAsync_WithDuplicateEmail_IsRejected()
    {
        // Arrange
        var (facade, context) = CreateFacade(Guid.NewGuid().ToString());
        var model1 = new RegisterViewModel
        {
            FullName = "Bob Smith",
            StudentId = "STU-1002",
            Email = "bob@university.edu",
            Password = "Password123!",
            ConfirmPassword = "Password123!",
            Department = "Physics",
            Batch = "2026"
        };
        await facade.RegisterAsync(model1, "https://localhost:7155");

        // Act - Attempt to register same email with different student ID
        var model2 = new RegisterViewModel
        {
            FullName = "Robert Smith",
            StudentId = "STU-1003",
            Email = "bob@university.edu",
            Password = "Password123!",
            ConfirmPassword = "Password123!",
            Department = "Physics",
            Batch = "2026"
        };
        var result = await facade.RegisterAsync(model2, "https://localhost:7155");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("email address already exists", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RegisterAsync_WithDuplicateStudentId_IsRejected()
    {
        // Arrange
        var (facade, context) = CreateFacade(Guid.NewGuid().ToString());
        var model1 = new RegisterViewModel
        {
            FullName = "Charlie Brown",
            StudentId = "STU-1004",
            Email = "charlie@university.edu",
            Password = "Password123!",
            ConfirmPassword = "Password123!",
            Department = "Mathematics",
            Batch = "2026"
        };
        await facade.RegisterAsync(model1, "https://localhost:7155");

        // Act - Attempt to register different email with existing Student ID
        var model2 = new RegisterViewModel
        {
            FullName = "Charles Brown",
            StudentId = "STU-1004",
            Email = "charles.brown@university.edu",
            Password = "Password123!",
            ConfirmPassword = "Password123!",
            Department = "Mathematics",
            Batch = "2026"
        };
        var result = await facade.RegisterAsync(model2, "https://localhost:7155");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Student ID already exists", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RegisterAsync_WithRestrictedEmailDomain_EnforcesDomain()
    {
        // Arrange
        var config = new Dictionary<string, string?>
        {
            { "Authentication:AllowedEmailDomain", "campus.edu" }
        };
        var (facade, _) = CreateFacade(Guid.NewGuid().ToString(), config);

        var model = new RegisterViewModel
        {
            FullName = "David Lee",
            StudentId = "STU-1005",
            Email = "david@gmail.com", // Outside allowed domain!
            Password = "Password123!",
            ConfirmPassword = "Password123!",
            Department = "Business",
            Batch = "2026"
        };

        // Act
        var result = await facade.RegisterAsync(model, "https://localhost:7155");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("@campus.edu", result.ErrorMessage);
    }

    [Fact]
    public async Task ValidateLoginAsync_WhenEmailNotConfirmed_BlocksLogin()
    {
        // Arrange
        var (facade, _) = CreateFacade(Guid.NewGuid().ToString());
        var model = new RegisterViewModel
        {
            FullName = "Emma Watson",
            StudentId = "STU-1006",
            Email = "emma@university.edu",
            Password = "Password123!",
            ConfirmPassword = "Password123!",
            Department = "Arts",
            Batch = "2026"
        };
        await facade.RegisterAsync(model, "https://localhost:7155");

        // Act - Try logging in before email confirmation
        var loginResult = await facade.ValidateLoginAsync("emma@university.edu", "Password123!");

        // Assert
        Assert.False(loginResult.Success);
        Assert.Contains("not been confirmed", loginResult.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ConfirmEmailAsync_WithValidToken_EnablesAccountLogin()
    {
        // Arrange
        var (facade, context) = CreateFacade(Guid.NewGuid().ToString());
        var model = new RegisterViewModel
        {
            FullName = "Frank Castle",
            StudentId = "STU-1007",
            Email = "frank@university.edu",
            Password = "Password123!",
            ConfirmPassword = "Password123!",
            Department = "Engineering",
            Batch = "2026"
        };
        var regResult = await facade.RegisterAsync(model, "https://localhost:7155");
        var token = regResult.Data!.EmailConfirmationToken!;

        // Act - Confirm email
        var confirmResult = await facade.ConfirmEmailAsync(token);

        // Assert
        Assert.True(confirmResult.Success);

        // Act - Now login
        var loginResult = await facade.ValidateLoginAsync("frank@university.edu", "Password123!");
        Assert.True(loginResult.Success);
        Assert.NotNull(loginResult.Data);
        Assert.True(loginResult.Data.IsEmailConfirmed);
    }

    [Fact]
    public async Task RegisterAsync_WithBangladeshiPhoneNumberAndIdCard_SavesCorrectly()
    {
        // Arrange
        var fileStorageMock = new Mock<IFileStorageService>();
        var formFileMock = new Mock<Microsoft.AspNetCore.Http.IFormFile>();
        formFileMock.Setup(f => f.Length).Returns(1024);
        string? error;
        fileStorageMock.Setup(s => s.IsValidImage(It.IsAny<Microsoft.AspNetCore.Http.IFormFile>(), out error))
            .Returns(true);
        fileStorageMock.Setup(s => s.SaveFileAsync(It.IsAny<Microsoft.AspNetCore.Http.IFormFile>(), "uploads/idcards"))
            .ReturnsAsync("/uploads/idcards/student_card.jpg");

        var (facade, context) = CreateFacade(Guid.NewGuid().ToString(), fileStorageMock: fileStorageMock);

        var model = new RegisterViewModel
        {
            FullName = "Sadia Islam",
            StudentId = "STU-2001",
            Email = "sadia@university.edu",
            PhoneNumber = "01712-345678",
            StudentIdCard = formFileMock.Object,
            Password = "Password123!",
            ConfirmPassword = "Password123!",
            Department = "Computer Science and Engineering",
            Batch = "Fall 2026"
        };

        // Act
        var result = await facade.RegisterAsync(model, "https://localhost:7155");

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.NotNull(result.Data.StudentProfile);
        Assert.Equal("01712-345678", result.Data.StudentProfile.PhoneNumber);
        Assert.Equal("/uploads/idcards/student_card.jpg", result.Data.StudentProfile.StudentIdCardUrl);
    }

    [Fact]
    public void ToPriceText_FormatsBangladeshiTakaCorrectly()
    {
        // Free / Donation
        Assert.Equal("Free / Donation", CampusBook.Framework.Extensions.ToPriceText(0m));
        Assert.Equal("Free / Donation", CampusBook.Framework.Extensions.ToPriceText(-5m));

        // Integer Taka amounts
        Assert.Equal("৳250", CampusBook.Framework.Extensions.ToPriceText(250m));
        Assert.Equal("৳1,500", CampusBook.Framework.Extensions.ToPriceText(1500m));

        // Fractional Taka amounts
        Assert.Equal("৳120.50", CampusBook.Framework.Extensions.ToPriceText(120.50m));
    }
}
