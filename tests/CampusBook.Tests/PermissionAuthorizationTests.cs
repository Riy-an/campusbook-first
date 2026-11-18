using System.Reflection;
using System.Security.Claims;
using CampusBook.DataAccess;
using CampusBook.DataAccess.Implementation;
using CampusBook.Entities;
using CampusBook.Facade.Implementation;
using CampusBook.Facade.Interface;
using CampusBook.Facade.Services;
using CampusBook.Facade.ViewModels.Account;
using CampusBook.Framework;
using CampusBook.Web.UI.Authorization;
using CampusBook.Web.UI.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace CampusBook.Tests;

public class PermissionAuthorizationTests
{
    private static IMemoryCache CreateCache()
    {
        return new MemoryCache(new MemoryCacheOptions());
    }

    private static IFormFile CreateMockIdCard()
    {
        var formFileMock = new Mock<IFormFile>();
        formFileMock.Setup(f => f.Length).Returns(1024);
        formFileMock.Setup(f => f.FileName).Returns("idcard.jpg");
        return formFileMock.Object;
    }

    [Fact]
    public void SeedData_ContainsDefaultGroupsAndPermissions()
    {
        using var context = TestDbContextFactory.Create(nameof(SeedData_ContainsDefaultGroupsAndPermissions));

        var groups = context.PermissionGroups.ToList();
        var permissions = context.Permissions.ToList();
        var maps = context.PermissionGroupMaps.ToList();

        Assert.Contains(groups, g => g.GroupName == Permissions.Groups.Student && g.PermissionGroupId == 1);
        Assert.Contains(groups, g => g.GroupName == Permissions.Groups.Admin && g.PermissionGroupId == 2);
        Assert.Contains(groups, g => g.GroupName == Permissions.Groups.Moderator && g.PermissionGroupId == 3);

        Assert.True(permissions.Count >= 29, $"Expected at least 29 permissions, found {permissions.Count}");
        Assert.NotEmpty(maps);
    }

    [Fact]
    public async Task StudentGroup_HasExpectedPermissions_AndLacksAdminPermissions()
    {
        using var context = TestDbContextFactory.Create(nameof(StudentGroup_HasExpectedPermissions_AndLacksAdminPermissions));
        var repo = new PermissionRepository(context);

        var studentGroup = await repo.GetGroupByNameAsync(Permissions.Groups.Student);
        Assert.NotNull(studentGroup);

        var groupMaps = await repo.GetGroupMapsAsync(studentGroup.PermissionGroupId);
        var permNames = groupMaps.Select(gm => gm.Permission.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

        // Student grants
        Assert.Contains(Permissions.Books.View, permNames);
        Assert.Contains(Permissions.Books.Create, permNames);
        Assert.Contains(Permissions.Books.EditOwn, permNames);
        Assert.Contains(Permissions.Books.DeleteOwn, permNames);
        Assert.Contains(Permissions.Requests.Create, permNames);
        Assert.Contains(Permissions.Requests.ViewOwn, permNames);
        Assert.Contains(Permissions.Wishlist.ManageOwn, permNames);
        Assert.Contains(Permissions.Messaging.Send, permNames);

        // Student lacks administrative & elevated moderation permissions
        Assert.DoesNotContain(Permissions.Admin.UsersManage, permNames);
        Assert.DoesNotContain(Permissions.Admin.PermissionsManage, permNames);
        Assert.DoesNotContain(Permissions.Books.EditAny, permNames);
        Assert.DoesNotContain(Permissions.Books.DeleteAny, permNames);
    }

    [Fact]
    public async Task ModeratorGroup_HasElevatedModeration_AndLacksAdminPermissions()
    {
        using var context = TestDbContextFactory.Create(nameof(ModeratorGroup_HasElevatedModeration_AndLacksAdminPermissions));
        var repo = new PermissionRepository(context);

        var modGroup = await repo.GetGroupByNameAsync(Permissions.Groups.Moderator);
        Assert.NotNull(modGroup);

        var groupMaps = await repo.GetGroupMapsAsync(modGroup.PermissionGroupId);
        var permNames = groupMaps.Select(gm => gm.Permission.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

        // Moderator has elevated content moderation
        Assert.Contains(Permissions.Books.EditAny, permNames);
        Assert.Contains(Permissions.Books.DeleteAny, permNames);
        Assert.Contains(Permissions.WantedBooks.ManageAny, permNames);

        // Moderator does NOT have admin system permissions
        Assert.DoesNotContain(Permissions.Admin.UsersManage, permNames);
        Assert.DoesNotContain(Permissions.Admin.PermissionsManage, permNames);
    }

    [Fact]
    public async Task AdminGroup_HasFullPermissions()
    {
        using var context = TestDbContextFactory.Create(nameof(AdminGroup_HasFullPermissions));
        var repo = new PermissionRepository(context);

        var adminGroup = await repo.GetGroupByNameAsync(Permissions.Groups.Admin);
        Assert.NotNull(adminGroup);

        var groupMaps = await repo.GetGroupMapsAsync(adminGroup.PermissionGroupId);
        var permNames = groupMaps.Select(gm => gm.Permission.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.Contains(Permissions.Admin.UsersView, permNames);
        Assert.Contains(Permissions.Admin.UsersManage, permNames);
        Assert.Contains(Permissions.Admin.PermissionsView, permNames);
        Assert.Contains(Permissions.Admin.PermissionsManage, permNames);
        Assert.Contains(Permissions.Books.EditAny, permNames);
        Assert.Contains(Permissions.Books.DeleteAny, permNames);
    }

    [Fact]
    public async Task ExistingUser_WithoutExplicitUserPermissionRow_InheritsRolePermissions()
    {
        using var context = TestDbContextFactory.Create(nameof(ExistingUser_WithoutExplicitUserPermissionRow_InheritsRolePermissions));
        var repo = new PermissionRepository(context);

        // Seed legacy user with RoleId = 1 (Student), without any UserPermission records
        var user = new User
        {
            UserId = 100,
            FullName = "Legacy Student",
            StudentId = "LEGACY-001",
            Email = "legacy@aust.edu",
            PasswordHash = "hash",
            IsActive = true,
            RoleId = 1
        };
        await context.Users.AddAsync(user);
        await context.SaveChangesAsync();

        var permissions = await repo.GetUserEffectivePermissionsAsync(user.UserId);

        Assert.Contains(Permissions.Books.Create, permissions);
        Assert.Contains(Permissions.Requests.Create, permissions);
        Assert.DoesNotContain(Permissions.Admin.UsersManage, permissions);
    }

    [Fact]
    public async Task InactiveUser_FailsClosed_ReturnsEmptyPermissions()
    {
        using var context = TestDbContextFactory.Create(nameof(InactiveUser_FailsClosed_ReturnsEmptyPermissions));
        var repo = new PermissionRepository(context);

        var user = new User
        {
            UserId = 101,
            FullName = "Banned Admin",
            StudentId = "BANNED-001",
            Email = "banned@aust.edu",
            PasswordHash = "hash",
            IsActive = false, // Deactivated
            RoleId = 2 // Admin
        };
        await context.Users.AddAsync(user);
        await context.SaveChangesAsync();

        var permissions = await repo.GetUserEffectivePermissionsAsync(user.UserId);

        Assert.Empty(permissions);
        var hasAccess = await repo.HasPermissionAsync(user.UserId, Permissions.Books.View);
        Assert.False(hasAccess);
    }

    [Fact]
    public async Task DirectPermissionGrant_OverridesGroupPermissions()
    {
        using var context = TestDbContextFactory.Create(nameof(DirectPermissionGrant_OverridesGroupPermissions));
        var repo = new PermissionRepository(context);

        var user = new User
        {
            UserId = 102,
            FullName = "Student with Grant",
            StudentId = "GRANT-001",
            Email = "grant@aust.edu",
            PasswordHash = "hash",
            IsActive = true,
            RoleId = 1 // Student
        };
        await context.Users.AddAsync(user);
        await context.SaveChangesAsync();

        // Initially lacks Books.EditAny
        var initialPerms = await repo.GetUserEffectivePermissionsAsync(user.UserId);
        Assert.DoesNotContain(Permissions.Books.EditAny, initialPerms);

        // Find Books.EditAny permission ID
        var editAnyPerm = await context.Permissions.FirstAsync(p => p.Name == Permissions.Books.EditAny);

        // Grant Books.EditAny directly to this user
        await repo.SetUserDirectPermissionAsync(user.UserId, editAnyPerm.PermissionId, isGranted: true);

        var updatedPerms = await repo.GetUserEffectivePermissionsAsync(user.UserId);
        Assert.Contains(Permissions.Books.EditAny, updatedPerms);
    }

    [Fact]
    public async Task DirectPermissionDeny_OverridesGroupGrant()
    {
        using var context = TestDbContextFactory.Create(nameof(DirectPermissionDeny_OverridesGroupGrant));
        var repo = new PermissionRepository(context);

        var user = new User
        {
            UserId = 103,
            FullName = "Student with Restriction",
            StudentId = "RESTRICT-001",
            Email = "restrict@aust.edu",
            PasswordHash = "hash",
            IsActive = true,
            RoleId = 1 // Student
        };
        await context.Users.AddAsync(user);
        await context.SaveChangesAsync();

        // Initially student has Books.Create
        var initialPerms = await repo.GetUserEffectivePermissionsAsync(user.UserId);
        Assert.Contains(Permissions.Books.Create, initialPerms);

        // Direct deny for Books.Create
        var createPerm = await context.Permissions.FirstAsync(p => p.Name == Permissions.Books.Create);
        await repo.SetUserDirectPermissionAsync(user.UserId, createPerm.PermissionId, isGranted: false);

        var updatedPerms = await repo.GetUserEffectivePermissionsAsync(user.UserId);
        Assert.DoesNotContain(Permissions.Books.Create, updatedPerms);
    }

    [Fact]
    public async Task PermissionService_CachesPermissions_AndInvalidatesUponChange()
    {
        using var context = TestDbContextFactory.Create(nameof(PermissionService_CachesPermissions_AndInvalidatesUponChange));
        var repo = new PermissionRepository(context);
        var cache = CreateCache();
        var service = new PermissionService(repo, cache, NullLogger<PermissionService>.Instance);

        var user = new User
        {
            UserId = 104,
            FullName = "Cache Test User",
            StudentId = "CACHE-001",
            Email = "cache@aust.edu",
            PasswordHash = "hash",
            IsActive = true,
            RoleId = 1
        };
        await context.Users.AddAsync(user);
        await context.SaveChangesAsync();

        // 1st call populates cache
        var perms1 = await service.GetUserPermissionsAsync(user.UserId);
        Assert.Contains(Permissions.Books.Create, perms1);

        // Assign Moderator group via service
        await service.AssignGroupToUserAsync(user.UserId, Permissions.Groups.Moderator);

        // Service automatically invalidates cache; next read has elevated permissions
        var perms2 = await service.GetUserPermissionsAsync(user.UserId);
        Assert.Contains(Permissions.Books.EditAny, perms2);
    }

    [Fact]
    public async Task Registration_AutomaticallyAssignsStudentPermissionGroup()
    {
        using var context = TestDbContextFactory.Create(nameof(Registration_AutomaticallyAssignsStudentPermissionGroup));
        var userRepo = new UserRepository(context);
        var passwordHasher = new PasswordHasher<User>();
        var emailSenderMock = new Mock<IEmailSender>();
        var storageMock = new Mock<IFileStorageService>();
        string? error;
        storageMock.Setup(s => s.IsValidImage(It.IsAny<IFormFile>(), out error)).Returns(true);
        storageMock.Setup(s => s.SaveFileAsync(It.IsAny<IFormFile>(), It.IsAny<string>()))
            .ReturnsAsync("/uploads/idcards/mock_idcard.jpg");

        var config = new ConfigurationBuilder().Build();

        var facade = new UserFacade(
            userRepo,
            passwordHasher,
            emailSenderMock.Object,
            config,
            storageMock.Object,
            NullLogger<UserFacade>.Instance);

        var registerModel = new RegisterViewModel
        {
            FullName = "New Register Student",
            StudentId = "2026-REG-001",
            Email = "newstudent@gmail.com",
            UniversityEmail = "newstudent@aust.edu",
            StudentIdCard = CreateMockIdCard(),
            Password = "SecurePassword123!",
            ConfirmPassword = "SecurePassword123!",
            Department = "Computer Science",
            Batch = "2026"
        };

        var result = await facade.RegisterAsync(registerModel, "https://localhost:7155");
        Assert.True(result.Success);

        var createdUser = await context.Users.FirstAsync(u => u.Email == "newstudent@gmail.com");
        Assert.Equal(1, createdUser.RoleId);

        // Verify UserPermission record was created for PermissionGroupId = 1
        var userGroup = await context.UserPermissions
            .FirstOrDefaultAsync(up => up.UserId == createdUser.UserId && up.PermissionGroupId == 1);
        Assert.NotNull(userGroup);
        Assert.True(userGroup.IsGranted);
    }

    [Fact]
    public async Task PermissionAuthorizationHandler_Succeeds_WhenUserHasPermission()
    {
        var permServiceMock = new Mock<IPermissionService>();
        permServiceMock
            .Setup(s => s.HasPermissionAsync(50, Permissions.Books.Create))
            .ReturnsAsync(true);

        var handler = new PermissionAuthorizationHandler(permServiceMock.Object, NullLogger<PermissionAuthorizationHandler>.Instance);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "50"),
            new Claim(ClaimTypes.Name, "test@aust.edu")
        };
        var identity = new ClaimsIdentity(claims, "Cookie");
        var principal = new ClaimsPrincipal(identity);

        var requirement = new PermissionRequirement(Permissions.Books.Create);
        var context = new AuthorizationHandlerContext([requirement], principal, null);

        await handler.HandleAsync(context);

        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public async Task PermissionAuthorizationHandler_Fails_WhenUserLacksPermission()
    {
        var permServiceMock = new Mock<IPermissionService>();
        permServiceMock
            .Setup(s => s.HasPermissionAsync(50, Permissions.Admin.UsersManage))
            .ReturnsAsync(false);

        var handler = new PermissionAuthorizationHandler(permServiceMock.Object, NullLogger<PermissionAuthorizationHandler>.Instance);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "50"),
            new Claim(ClaimTypes.Name, "test@aust.edu")
        };
        var identity = new ClaimsIdentity(claims, "Cookie");
        var principal = new ClaimsPrincipal(identity);

        var requirement = new PermissionRequirement(Permissions.Admin.UsersManage);
        var context = new AuthorizationHandlerContext([requirement], principal, null);

        await handler.HandleAsync(context);

        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public async Task PermissionAuthorizationHandler_Fails_WhenUnauthenticated()
    {
        var permServiceMock = new Mock<IPermissionService>();
        var handler = new PermissionAuthorizationHandler(permServiceMock.Object, NullLogger<PermissionAuthorizationHandler>.Instance);

        var principal = new ClaimsPrincipal(new ClaimsIdentity()); // Unauthenticated
        var requirement = new PermissionRequirement(Permissions.Books.Create);
        var context = new AuthorizationHandlerContext([requirement], principal, null);

        await handler.HandleAsync(context);

        Assert.False(context.HasSucceeded);
        permServiceMock.Verify(s => s.HasPermissionAsync(It.IsAny<int>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task PermissionAuthorizationHandler_Fails_WhenUserIdClaimMissing()
    {
        var permServiceMock = new Mock<IPermissionService>();
        var handler = new PermissionAuthorizationHandler(permServiceMock.Object, NullLogger<PermissionAuthorizationHandler>.Instance);

        var claims = new[]
        {
            new Claim(ClaimTypes.Name, "test@aust.edu")
            // NameIdentifier is omitted
        };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "Cookie"));
        var requirement = new PermissionRequirement(Permissions.Books.Create);
        var context = new AuthorizationHandlerContext([requirement], principal, null);

        await handler.HandleAsync(context);

        Assert.False(context.HasSucceeded);
        permServiceMock.Verify(s => s.HasPermissionAsync(It.IsAny<int>(), It.IsAny<string>()), Times.Never);
    }

    [Theory]
    [InlineData(typeof(BooksController), "Create", Permissions.Books.Create)]
    [InlineData(typeof(BooksController), "MyBooks", Permissions.Books.View)]
    [InlineData(typeof(BooksController), "Edit", Permissions.Books.EditOwn)]
    [InlineData(typeof(BooksController), "Delete", Permissions.Books.DeleteOwn)]
    [InlineData(typeof(RequestsController), "Sent", Permissions.Requests.ViewOwn)]
    [InlineData(typeof(RequestsController), "Received", Permissions.Requests.ViewOwn)]
    [InlineData(typeof(RequestsController), "Create", Permissions.Requests.Create)]
    [InlineData(typeof(RequestsController), "Accept", Permissions.Requests.ManageOwn)]
    [InlineData(typeof(RequestsController), "Reject", Permissions.Requests.ManageOwn)]
    [InlineData(typeof(RequestsController), "Cancel", Permissions.Requests.ManageOwn)]
    [InlineData(typeof(RequestsController), "ConfirmHandover", Permissions.Requests.ManageOwn)]
    [InlineData(typeof(WishlistController), "Index", Permissions.Wishlist.ViewOwn)]
    [InlineData(typeof(WishlistController), "Toggle", Permissions.Wishlist.ManageOwn)]
    [InlineData(typeof(WantedBooksController), "MyPosts", Permissions.WantedBooks.View)]
    [InlineData(typeof(WantedBooksController), "Create", Permissions.WantedBooks.Create)]
    [InlineData(typeof(WantedBooksController), "Edit", Permissions.WantedBooks.EditOwn)]
    [InlineData(typeof(WantedBooksController), "Close", Permissions.WantedBooks.DeleteOwn)]
    [InlineData(typeof(MessagesController), "Index", Permissions.Messaging.ViewOwn)]
    [InlineData(typeof(MessagesController), "Send", Permissions.Messaging.Send)]
    [InlineData(typeof(NotificationsController), "Index", Permissions.Notifications.ViewOwn)]
    [InlineData(typeof(NotificationsController), "MarkAsRead", Permissions.Notifications.ManageOwn)]
    [InlineData(typeof(HistoryController), "Index", Permissions.History.ViewOwn)]
    [InlineData(typeof(ProfileController), "Index", Permissions.Profile.ViewOwn)]
    [InlineData(typeof(ProfileController), "Edit", Permissions.Profile.EditOwn)]
    public void ControllerActions_AreDecoratedWithExpectedPermission(Type controllerType, string actionName, string expectedPermission)
    {
        var methods = controllerType
            .GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .Where(m => m.Name == actionName)
            .ToList();

        Assert.NotEmpty(methods);

        foreach (var method in methods)
        {
            var permAttr = method.GetCustomAttribute<HasPermissionAttribute>();
            Assert.NotNull(permAttr);
            Assert.Equal(expectedPermission, permAttr.Permission);
            Assert.Equal(expectedPermission, permAttr.Policy);
        }
    }
}
