using CampusBook.DataAccess;
using CampusBook.DataAccess.Implementation;
using CampusBook.Entities;
using CampusBook.Facade.Implementation;
using CampusBook.Facade.Services;
using CampusBook.Facade.ViewModels.Requests;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace CampusBook.Tests;

public class BookRequestWorkflowTests
{
    private (BookRequestFacade facade, ApplicationDbContext context, User owner, User requester, Book donationBook, Book exchangeBook, Book requesterTradeBook)
        SetupExchangeEnvironment(string dbName)
    {
        var context = TestDbContextFactory.Create(dbName);
        var requestRepo = new BookRequestRepository(context);
        var bookRepo = new BookRepository(context);
        var userRepo = new UserRepository(context);
        var lookupRepo = new LookupRepository(context);
        var fileStorageMock = new Mock<IFileStorageService>();
        var bookFacadeLogger = NullLogger<BookFacade>.Instance;
        var requestFacadeLogger = NullLogger<BookRequestFacade>.Instance;

        var bookFacade = new BookFacade(bookRepo, requestRepo, lookupRepo, fileStorageMock.Object, bookFacadeLogger);

        var owner = new User
        {
            UserId = 1,
            FullName = "Owner Student",
            StudentId = "STU-4001",
            Email = "owner@university.edu",
            PasswordHash = "hash",
            RoleId = 1,
            IsEmailConfirmed = true,
            IsActive = true
        };

        var requester = new User
        {
            UserId = 2,
            FullName = "Requester Student",
            StudentId = "STU-4002",
            Email = "requester@university.edu",
            PasswordHash = "hash",
            RoleId = 1,
            IsEmailConfirmed = true,
            IsActive = true
        };

        context.Users.AddRange(owner, requester);

        var donationBook = new Book
        {
            BookId = 1,
            Title = "Compilers: Principles, Techniques, and Tools",
            Author = "Aho, Lam, Sethi, Ullman",
            CategoryId = 1,
            Condition = BookCondition.Good,
            ExchangeType = ExchangeType.Donation,
            Price = 0.00m,
            Description = "Dragon book for donation.",
            OwnerId = owner.UserId,
            Status = BookStatus.Available
        };

        var exchangeBook = new Book
        {
            BookId = 2,
            Title = "Computer Networks",
            Author = "Andrew Tanenbaum",
            CategoryId = 1,
            Condition = BookCondition.Good,
            ExchangeType = ExchangeType.Exchange,
            Price = 0.00m,
            Description = "Looking to exchange for another CSE book.",
            OwnerId = owner.UserId,
            Status = BookStatus.Available
        };

        var requesterTradeBook = new Book
        {
            BookId = 3,
            Title = "Operating Systems Three Easy Pieces",
            Author = "Arpaci-Dusseau",
            CategoryId = 1,
            Condition = BookCondition.New,
            ExchangeType = ExchangeType.Exchange,
            Price = 0.00m,
            Description = "Available for trade.",
            OwnerId = requester.UserId,
            Status = BookStatus.Available
        };

        context.Books.AddRange(donationBook, exchangeBook, requesterTradeBook);
        context.SaveChanges();

        var facade = new BookRequestFacade(requestRepo, bookRepo, userRepo, bookFacade, requestFacadeLogger);
        return (facade, context, owner, requester, donationBook, exchangeBook, requesterTradeBook);
    }

    [Fact]
    public async Task CreateRequestAsync_WhenRequestingOwnBook_IsRejected()
    {
        // Arrange
        var (facade, _, owner, _, donationBook, _, _) = SetupExchangeEnvironment(Guid.NewGuid().ToString());
        var model = new CreateRequestViewModel
        {
            BookId = donationBook.BookId,
            RequestMessage = "Self request attempt"
        };

        // Act - Owner tries to request their own book
        var result = await facade.CreateRequestAsync(owner.UserId, model);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("cannot request your own book", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateRequestAsync_DuplicateActiveRequest_IsRejected()
    {
        // Arrange
        var (facade, _, _, requester, donationBook, _, _) = SetupExchangeEnvironment(Guid.NewGuid().ToString());
        var model = new CreateRequestViewModel
        {
            BookId = donationBook.BookId,
            RequestMessage = "First request"
        };
        var firstResult = await facade.CreateRequestAsync(requester.UserId, model);
        Assert.True(firstResult.Success);

        // Act - Second request for the same book by the same student
        var secondResult = await facade.CreateRequestAsync(requester.UserId, model);

        // Assert
        Assert.False(secondResult.Success);
        Assert.Contains("already have an active request", secondResult.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateRequestAsync_BookForBookExchange_WithoutOfferedBook_IsRejected()
    {
        // Arrange
        var (facade, _, _, requester, _, exchangeBook, _) = SetupExchangeEnvironment(Guid.NewGuid().ToString());
        var model = new CreateRequestViewModel
        {
            BookId = exchangeBook.BookId,
            OfferedBookId = null, // Missing offered book for exchange listing
            RequestMessage = "Trade request"
        };

        // Act
        var result = await facade.CreateRequestAsync(requester.UserId, model);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("select one of your available books", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AcceptRequestAsync_ReservesBook_AndInvalidatesOtherPendingRequests()
    {
        // Arrange
        var (facade, context, owner, requester, donationBook, _, _) = SetupExchangeEnvironment(Guid.NewGuid().ToString());

        // Create second student
        var thirdStudent = new User
        {
            UserId = 3,
            FullName = "Third Student",
            StudentId = "STU-4003",
            Email = "third@university.edu",
            PasswordHash = "hash",
            RoleId = 1,
            IsEmailConfirmed = true,
            IsActive = true
        };
        context.Users.Add(thirdStudent);
        await context.SaveChangesAsync();

        // Requester 1 submits request
        var req1Result = await facade.CreateRequestAsync(requester.UserId, new CreateRequestViewModel { BookId = donationBook.BookId });
        // Requester 2 submits request for the same book
        var req2Result = await facade.CreateRequestAsync(thirdStudent.UserId, new CreateRequestViewModel { BookId = donationBook.BookId });

        // Act - Owner accepts Requester 1's request
        var acceptResult = await facade.AcceptRequestAsync(req1Result.Data, owner.UserId);

        // Assert
        Assert.True(acceptResult.Success);

        // Verify book is now Reserved
        var freshBook = await context.Books.FindAsync(donationBook.BookId);
        Assert.Equal(BookStatus.Reserved, freshBook!.Status);

        // Verify Requester 1's request is Accepted
        var req1 = await context.BookRequests.FindAsync(req1Result.Data);
        Assert.Equal(RequestStatus.Accepted, req1!.Status);

        // Verify Requester 2's request was closed/rejected to prevent double reservation!
        var req2 = await context.BookRequests.FindAsync(req2Result.Data);
        Assert.Equal(RequestStatus.Rejected, req2!.Status);
    }

    [Fact]
    public async Task AcceptTradeRequest_ReservesBothRequestedAndOfferedBooks()
    {
        // Arrange
        var (facade, context, owner, requester, _, exchangeBook, requesterTradeBook) = SetupExchangeEnvironment(Guid.NewGuid().ToString());

        var model = new CreateRequestViewModel
        {
            BookId = exchangeBook.BookId,
            OfferedBookId = requesterTradeBook.BookId,
            RequestMessage = "Let's trade books!"
        };

        var createResult = await facade.CreateRequestAsync(requester.UserId, model);
        Assert.True(createResult.Success);

        // Act - Owner accepts trade request
        var acceptResult = await facade.AcceptRequestAsync(createResult.Data, owner.UserId);

        // Assert
        Assert.True(acceptResult.Success);

        // Verify BOTH books are Reserved
        var freshRequestedBook = await context.Books.FindAsync(exchangeBook.BookId);
        var freshOfferedBook = await context.Books.FindAsync(requesterTradeBook.BookId);
        Assert.Equal(BookStatus.Reserved, freshRequestedBook!.Status);
        Assert.Equal(BookStatus.Reserved, freshOfferedBook!.Status);
    }

    [Fact]
    public async Task CancelRequestAsync_WhenAccepted_RestoresAvailability()
    {
        // Arrange
        var (facade, context, owner, requester, donationBook, _, _) = SetupExchangeEnvironment(Guid.NewGuid().ToString());
        var createResult = await facade.CreateRequestAsync(requester.UserId, new CreateRequestViewModel { BookId = donationBook.BookId });
        await facade.AcceptRequestAsync(createResult.Data, owner.UserId);

        // Book is reserved
        var reservedBook = await context.Books.FindAsync(donationBook.BookId);
        Assert.Equal(BookStatus.Reserved, reservedBook!.Status);

        // Act - Requester cancels the request
        var cancelResult = await facade.CancelRequestAsync(createResult.Data, requester.UserId);

        // Assert
        Assert.True(cancelResult.Success);
        var restoredBook = await context.Books.FindAsync(donationBook.BookId);
        Assert.Equal(BookStatus.Available, restoredBook!.Status); // Restored!
    }

    [Fact]
    public async Task ConfirmHandoverAsync_RequiresBothParties_ToCompleteExchange()
    {
        // Arrange
        var (facade, context, owner, requester, donationBook, _, _) = SetupExchangeEnvironment(Guid.NewGuid().ToString());
        var createResult = await facade.CreateRequestAsync(requester.UserId, new CreateRequestViewModel { BookId = donationBook.BookId });
        await facade.AcceptRequestAsync(createResult.Data, owner.UserId);

        // Act 1 - Only Owner confirms handover
        var ownerConfirmResult = await facade.ConfirmHandoverAsync(createResult.Data, owner.UserId);
        Assert.True(ownerConfirmResult.Success);

        var reqAfterOwner = await context.BookRequests.FindAsync(createResult.Data);
        Assert.Equal(RequestStatus.Accepted, reqAfterOwner!.Status); // Still Accepted until requester confirms!
        Assert.True(reqAfterOwner.OwnerConfirmed);
        Assert.False(reqAfterOwner.RequesterConfirmed);

        // Act 2 - Now Requester confirms receipt
        var requesterConfirmResult = await facade.ConfirmHandoverAsync(createResult.Data, requester.UserId);
        Assert.True(requesterConfirmResult.Success);

        var finalRequest = await context.BookRequests.FindAsync(createResult.Data);
        Assert.Equal(RequestStatus.Completed, finalRequest!.Status); // Now COMPLETED!
        Assert.True(finalRequest.RequesterConfirmed);

        // Book is now Completed (no longer available in catalogue)
        var finalizedBook = await context.Books.FindAsync(donationBook.BookId);
        Assert.Equal(BookStatus.Completed, finalizedBook!.Status);
    }
}
