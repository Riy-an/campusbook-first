namespace CampusBook.Facade.Services;

public interface IEmailSender
{
    Task SendEmailAsync(string toEmail, string subject, string message);
    string? LastGeneratedConfirmationLink { get; }
    string? LastGeneratedResetLink { get; }
}

