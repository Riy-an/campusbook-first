using CampusBook.DataAccess;
using CampusBook.DataAccess.Implementation;
using CampusBook.DataAccess.Interface;
using CampusBook.Entities;
using CampusBook.Facade.Implementation;
using CampusBook.Facade.Interface;
using CampusBook.Facade.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CampusBook.Facade;

public static class DependencyInjection
{
    public static IServiceCollection AddCampusBookFacade(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // 1. Database Context
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Server=.\\SQLEXPRESS;Database=CampusBookDb;Trusted_Connection=True;TrustServerCertificate=True;";

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(connectionString));

        // 2. Base Repositories
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IStudentProfileRepository, StudentProfileRepository>();
        services.AddScoped<IBookRepository, BookRepository>();
        services.AddScoped<IBookRequestRepository, BookRequestRepository>();
        services.AddScoped<ILookupRepository, LookupRepository>();
        services.AddScoped<IWishlistRepository, WishlistRepository>();
        services.AddScoped<IWantedBookRepository, WantedBookRepository>();
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<IConversationRepository, ConversationRepository>();

        // 3. Application Infrastructure Services
        services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
        services.AddScoped<IFileStorageService, FileStorageService>();
        services.AddSingleton<IEmailSender, DevelopmentEmailSender>();

        // 4. Business Facades
        services.AddScoped<IUserFacade, UserFacade>();
        services.AddScoped<IStudentProfileFacade, StudentProfileFacade>();
        services.AddScoped<IBookFacade, BookFacade>();
        services.AddScoped<IBookRequestFacade, BookRequestFacade>();
        services.AddScoped<IDashboardFacade, DashboardFacade>();
        services.AddScoped<IWishlistFacade, WishlistFacade>();
        services.AddScoped<IWantedBookFacade, WantedBookFacade>();
        services.AddScoped<INotificationFacade, NotificationFacade>();
        services.AddScoped<IConversationFacade, ConversationFacade>();
        services.AddScoped<IExchangeHistoryFacade, ExchangeHistoryFacade>();

        return services;
    }
}

