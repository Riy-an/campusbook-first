using System.Text.RegularExpressions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CampusBook.Facade.Services;

public class DevelopmentEmailSender : IEmailSender
{
    private readonly ILogger<DevelopmentEmailSender> _logger;
    private readonly IHostEnvironment _env;

    public string? LastGeneratedConfirmationLink { get; private set; }
    public string? LastGeneratedResetLink { get; private set; }

    public DevelopmentEmailSender(ILogger<DevelopmentEmailSender> logger, IHostEnvironment env)
    {
        _logger = logger;
        _env = env;
    }

    public Task SendEmailAsync(string toEmail, string subject, string message)
    {
        _logger.LogInformation("================ EMAIL DISPATCH (Development) ================");
        _logger.LogInformation("To: {ToEmail}", toEmail);
        _logger.LogInformation("Subject: {Subject}", subject);
        _logger.LogInformation("Message Content:\n{Message}", message);
        _logger.LogInformation("==============================================================");

        // In Development environment only, capture confirmation and password reset links for testing convenience
        if (_env.IsDevelopment())
        {
            var confirmMatch = Regex.Match(message, @"(https?://[^\s""'>]+/confirm-email\?[^\s""'>]+)");
            if (confirmMatch.Success)
            {
                LastGeneratedConfirmationLink = confirmMatch.Groups[1].Value;
            }

            var resetMatch = Regex.Match(message, @"(https?://[^\s""'>]+/reset-password\?[^\s""'>]+)");
            if (resetMatch.Success)
            {
                LastGeneratedResetLink = resetMatch.Groups[1].Value;
            }
        }

        return Task.CompletedTask;
    }
}

