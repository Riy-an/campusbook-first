namespace CampusBook.Entities;

public enum NotificationType
{
    RequestReceived = 1,
    RequestAccepted = 2,
    RequestRejected = 3,
    RequestCancelled = 4,
    BookUnavailable = 5,
    ExchangeCompleted = 6,
    WantedBookMatch = 7,
    General = 8
}
