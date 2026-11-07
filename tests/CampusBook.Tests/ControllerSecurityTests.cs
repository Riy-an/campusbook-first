using System.Reflection;
using CampusBook.Web.UI.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace CampusBook.Tests;

public class ControllerSecurityTests
{
    [Fact]
    public void DashboardController_HasAuthorizeAttribute()
    {
        var authAttr = typeof(DashboardController).GetCustomAttribute<AuthorizeAttribute>();
        Assert.NotNull(authAttr);
    }

    [Fact]
    public void ProfileController_HasAuthorizeAttribute()
    {
        var authAttr = typeof(ProfileController).GetCustomAttribute<AuthorizeAttribute>();
        Assert.NotNull(authAttr);
    }

    [Fact]
    public void RequestsController_HasAuthorizeAttribute()
    {
        var authAttr = typeof(RequestsController).GetCustomAttribute<AuthorizeAttribute>();
        Assert.NotNull(authAttr);
    }

    [Theory]
    [InlineData("Create")]
    [InlineData("Edit")]
    [InlineData("MyBooks")]
    [InlineData("Delete")]
    public void BooksController_ProtectedActions_HaveAuthorizeAttribute(string actionName)
    {
        var method = typeof(BooksController)
            .GetMethods()
            .FirstOrDefault(m => m.Name == actionName);

        Assert.NotNull(method);
        var authAttr = method.GetCustomAttribute<AuthorizeAttribute>();
        Assert.NotNull(authAttr);
    }

    [Theory]
    [InlineData(typeof(AccountController), "Login")]
    [InlineData(typeof(AccountController), "Register")]
    [InlineData(typeof(AccountController), "ResetPassword")]
    [InlineData(typeof(ProfileController), "Edit")]
    [InlineData(typeof(BooksController), "Create")]
    [InlineData(typeof(BooksController), "Edit")]
    [InlineData(typeof(RequestsController), "Create")]
    public void SensitivePostActions_HaveValidateAntiForgeryTokenAttribute(Type controllerType, string actionName)
    {
        var postMethods = controllerType
            .GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .Where(m => m.Name == actionName && m.GetCustomAttribute<HttpPostAttribute>() != null)
            .ToList();

        Assert.NotEmpty(postMethods);
        foreach (var method in postMethods)
        {
            var antiForgery = method.GetCustomAttribute<ValidateAntiForgeryTokenAttribute>();
            Assert.NotNull(antiForgery);
        }
    }
}

