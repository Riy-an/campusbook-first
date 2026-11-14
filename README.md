# CampusBook — University Book Exchange System (Phase-1 & Phase-2)

> **Target Framework:** .NET 10.0 (`net10.0`)  
> **Architecture Pattern:** N-Tier Layered Architecture with Facade Pattern  
> **Database Platform:** Microsoft SQL Server 2025 Express (`Server=.\\SQLEXPRESS;Database=CampusBookDb`)  
> **Authentication:** ASP.NET Core Secure Cookie Authentication with PBKDF2 Password Hashing  
> **Currency Format:** Bangladeshi Taka (`৳`)

---

## 1. Project Overview

**CampusBook** is a university-focused web application designed for academic textbook exchange and student resale. It empowers verified university students to share textbooks they no longer need and acquire required course materials safely on campus.

### Scope of Phase-1 & Phase-2

#### Phase-1 Foundation
- **Student Profile Management:** Viewing and editing permitted profile fields, academic affiliation (department, batch, semester), contact phone with Bangladeshi number placeholders, biography, safe profile picture uploads, university ID card photo upload with a strict maximum limit of 3 changes per student, and a privacy-preserving public profile view.
- **Book Listing Management:** Publishing books for **Sale** (non-negative price in ৳) or **Exchange** (book-for-book trade). Includes condition ratings (New, Good, Fair, Poor), category and department categorization, cover image uploads, ownership verification, and optimistic concurrency locking.
- **Book Browsing, Search, Filtering, and Details:** Database-backed catalogue queries, full-text searches (title, author, ISBN), multi-facet filtering (category, department, condition, exchange type, price range), sorting (newest, lowest price, highest price), pagination, and book detail inspection.
- **Book Request & Exchange Management:** Full request lifecycle (Pending &rarr; Accepted &rarr; Completed / Rejected / Cancelled). Enforces reservation on acceptance, prevents double reservation, validates offered books in trades, and requires mutual handover confirmation from both the book owner and requesting student before declaring an exchange completed.
- **Student Dashboard & Navigation:** Quick metric cards backed by real database counts (active listings, reserved books, pending incoming, pending sent, completed exchanges, wishlist, wanted books, unread notifications, unread messages).

#### Phase-2 Enhancements & Architectural Changes
1. **Modified Registration & Immediate Login (Change One):**
   - Students can register with any valid email address (e.g., Gmail, Outlook, Yahoo) without university domain restrictions.
   - Separate mandatory fields for **Login/Personal Email** and **University Email** (`UniversityEmail`).
   - Required **University ID Card** photo or scan upload (`StudentIdCard`), securely validated (JPG/PNG/WebP, max 5MB) and stored privately on disk.
   - Email verification requirement removed: immediate login upon registration with zero confirmation gating.
2. **Elimination of Free Donation (Change Two):**
   - Book listings strictly support **Sale** and **Exchange**.
   - Donation removed from all active UI forms, search filters, detail cards, and dashboards.
   - Backend validation rejects Donation on creation and edit while safely preserving historical records.
   - All prices formatted with Bangladeshi Taka symbol (`৳`).
3. **Structured Exchange Offer Form (Change Three):**
   - When requesting a book listed for Exchange, the requester must fill a comprehensive structured offer: Title, Author, Category, Condition, and Description (required), plus ISBN, Edition, Year, Department, CourseCode, Notes, and optional cover image.
   - Optional prefill from requester's active listings.
   - Persisted as a dedicated `ExchangeOffer` entity linked 1:1 with `BookRequest`.
   - Owner inspects complete offer before deciding to accept or reject.
   - Acceptance reserves both books and auto-creates a communication channel.
4. **Wishlist Feature:**
   - Dedicated `WishlistItem` entity with unique compound index `(UserId, BookId)`.
   - One-click AJAX toggle button on book cards and book details.
   - "My Wishlist" view with status badges, direct request triggers, and removal controls.
   - Prevents students from wishlisting their own books.
5. **Wanted-Book Posts & Automated Matching:**
   - Students can post requests for books not currently in the catalogue (`WantedBook` entity).
   - Lifecycle management: `Active`, `Fulfilled`, `Closed`.
   - Multi-facet browsing and searching for student wanted posts.
   - Automated matching engine: when a new book is published, owners of matching wanted posts receive real-time in-app notifications.
6. **In-App Notification Center:**
   - `Notification` entity linked to user with read status tracking.
   - Navbar bell with live unread counter badge.
   - Notification events: Request Received, Request Accepted, Request Rejected, Request Cancelled, Book Unavailable, Handover Completed, Wanted Book Match, and New Chat Message.
   - Individual "Mark as Read" and bulk "Mark All as Read" capabilities.
7. **Direct Student Messaging (Conversation & Messages):**
   - 1:1 `Conversation` auto-provisioned upon request acceptance.
   - Secure sanitized chat (`Message` entity) preventing XSS/script injection.
   - Transitions to read-only mode once the exchange handover is completed.
8. **Exchange History Ledger:**
   - Dedicated exchange history view with summary metrics: Total Sold, Total Purchased, Total Exchanged.
   - Filters by transaction role: Sold, Purchased, Exchanged.
   - Preserves complete snapshot of offered trade books and timestamps.

---

## 2. Architecture & Project Structure

The application adheres strictly to an **N-Tier Layered Architecture** with the **Facade Pattern** mediating between Presentation and Data Persistence:

```
CampusBook.sln
├── src/
│   ├── CampusBook.Entities/          # Domain POCO entities and business enums
│   │   ├── Book.cs                   # Academic book listing entity
│   │   ├── BookRequest.cs            # Exchange request & handover tracking entity
│   │   ├── ExchangeOffer.cs          # Structured book-for-book trade offer entity (Phase-2)
│   │   ├── WishlistItem.cs           # User book wishlist entity (Phase-2)
│   │   ├── WantedBook.cs             # Wanted book request post entity (Phase-2)
│   │   ├── Notification.cs           # In-app student notification entity (Phase-2)
│   │   ├── Conversation.cs           # 1:1 request chat thread entity (Phase-2)
│   │   ├── Message.cs                # Sanitized conversation message entity (Phase-2)
│   │   ├── BookCategory.cs           # Subject category lookup entity
│   │   ├── Department.cs             # Academic department lookup entity
│   │   ├── Role.cs                   # Authorization roles (Student, Admin)
│   │   ├── StudentProfile.cs         # 1:1 user profile extension entity
│   │   ├── User.cs                   # Student identity & credentials entity
│   │   └── Enums...                  # BookCondition, ExchangeType, BookStatus, RequestStatus, WantedBookStatus, NotificationType
│   │
│   ├── CampusBook.Framework/         # Common cross-cutting layer
│   │   ├── UrlRewrite.cs             # Centralized compile-time route string constants
│   │   └── Extensions.cs             # Date, price (৳), and string formatting extensions
│   │
│   ├── CampusBook.DataAccess/        # Persistence layer (EF Core 10.0)
│   │   ├── ApplicationDbContext.cs   # DbContext with Fluent API & SQL Server cascade rules
│   │   ├── DesignTimeDbContextFactory.cs # EF CLI migration factory
│   │   ├── Interface/                # Repository contracts
│   │   │   ├── IRepository.cs        # Generic base repository contract
│   │   │   ├── IUserRepository.cs
│   │   │   ├── IStudentProfileRepository.cs
│   │   │   ├── IBookRepository.cs
│   │   │   ├── IBookRequestRepository.cs
│   │   │   ├── ILookupRepository.cs
│   │   │   ├── IWishlistRepository.cs
│   │   │   ├── IWantedBookRepository.cs
│   │   │   ├── INotificationRepository.cs
│   │   │   └── IConversationRepository.cs
│   │   ├── Implementation/           # Concrete repository classes
│   │   │   ├── Repository.cs
│   │   │   ├── UserRepository.cs
│   │   │   ├── StudentProfileRepository.cs
│   │   │   ├── BookRepository.cs     # Database-level filtering, search, and pagination
│   │   │   ├── BookRequestRepository.cs
│   │   │   ├── LookupRepository.cs
│   │   │   ├── WishlistRepository.cs
│   │   │   ├── WantedBookRepository.cs
│   │   │   ├── NotificationRepository.cs
│   │   │   └── ConversationRepository.cs
│   │   └── Migrations/               # EF Core database migrations
│   │       ├── 20261107100229_Shahed.cs
│   │       ├── 20261107160203_Campusbook.cs
│   │       ├── 20261113193632_Phase2Features.cs
│   │       └── 20261114111904_AddIdCardChangeLimitToStudentProfile.cs
│   │
│   ├── CampusBook.Facade/            # Application business logic & orchestration
│   │   ├── DependencyInjection.cs    # AddCampusBookFacade extension method
│   │   ├── Common/ServiceResult.cs   # Standardized operation result type
│   │   ├── Services/                 # Infrastructure services
│   │   │   ├── IEmailSender.cs
│   │   │   ├── DevelopmentEmailSender.cs
│   │   │   ├── IFileStorageService.cs
│   │   │   └── FileStorageService.cs
│   │   ├── ViewModels/               # Dedicated DTOs preventing overposting
│   │   │   ├── Account/              # Register, Login, Reset Password models
│   │   │   ├── Books/                # Book create, edit, filter, details models
│   │   │   ├── Profile/              # My profile, edit profile, public profile models
│   │   │   ├── Requests/             # Request creation, sent, received, offer details models
│   │   │   ├── Wishlist/             # Wishlist items & page models
│   │   │   ├── WantedBooks/          # Wanted book CRUD & filter models
│   │   │   ├── Notifications/        # In-app notifications models
│   │   │   ├── Messages/             # Conversation summary & message models
│   │   │   ├── History/              # Completed exchange transaction models
│   │   │   └── Dashboard/            # Real-time metrics dashboard model
│   │   ├── Interface/                # Facade contracts
│   │   │   ├── IUserFacade.cs
│   │   │   ├── IStudentProfileFacade.cs
│   │   │   ├── IBookFacade.cs
│   │   │   ├── IBookRequestFacade.cs
│   │   │   ├── IDashboardFacade.cs
│   │   │   ├── IWishlistFacade.cs
│   │   │   ├── IWantedBookFacade.cs
│   │   │   ├── INotificationFacade.cs
│   │   │   ├── IConversationFacade.cs
│   │   │   └── IExchangeHistoryFacade.cs
│   │   └── Implementation/           # Concrete facade implementations
│   │       ├── UserFacade.cs
│   │       ├── StudentProfileFacade.cs
│   │       ├── BookFacade.cs
│   │       ├── BookRequestFacade.cs
│   │       ├── DashboardFacade.cs
│   │       ├── WishlistFacade.cs
│   │       ├── WantedBookFacade.cs
│   │       ├── NotificationFacade.cs
│   │       ├── ConversationFacade.cs
│   │       └── ExchangeHistoryFacade.cs
│   │
│   └── CampusBook.Web.UI/            # Presentation layer (ASP.NET Core MVC)
│       ├── Controllers/              # Thin MVC controllers
│       │   ├── AccountController.cs
│       │   ├── ProfileController.cs
│       │   ├── BooksController.cs
│       │   ├── RequestsController.cs
│       │   ├── DashboardController.cs
│       │   ├── WishlistController.cs
│       │   ├── WantedBooksController.cs
│       │   ├── NotificationsController.cs
│       │   ├── MessagesController.cs
│       │   ├── HistoryController.cs
│       │   └── HomeController.cs
│       ├── Views/                    # Razor Views with Bootstrap 5.3 & TagHelpers
│       ├── wwwroot/                  # Static CSS, JS, Bootstrap, and upload folders
│       └── Program.cs                # Startup pipeline & cookie authentication
│
└── tests/
    └── CampusBook.Tests/             # Automated test suite (xUnit, InMemory EF Core, Moq)
        ├── AuthenticationTests.cs    # Registration, personal/university emails, ID card, hashing, Taka formatting
        ├── ProfileTests.cs           # Profile retrieval, anti-overposting, public privacy
        ├── BookListingTests.cs       # Sale/Exchange rules, rejection of Donation, ownership security, reservations
        ├── BookCatalogueTests.cs     # Available status filters, ISBN search, price sorting, donation exclusion
        ├── BookRequestWorkflowTests.cs # Structured offer validation, trades, reservations, handover, chat provision
        ├── ControllerSecurityTests.cs # [Authorize] and [ValidateAntiForgeryToken] attribute verification
        ├── WishlistTests.cs          # Add, toggle, remove, self-book rejection, user isolation
        ├── WantedBookTests.cs        # Create, edit, close, matching listing notifications
        ├── NotificationTests.cs      # Creation, unread count, individual mark-read, mark-all-read
        ├── MessagingTests.cs         # Authorized chat, unauthorized rejection, XSS sanitization, read-only on complete
        └── ExchangeHistoryTests.cs   # Completed ledger queries, sold/purchased/exchanged categorization, offer snapshots
```

---

## 3. Database Setup & Migrations

The database connection string is configured in `src/CampusBook.Web.UI/appsettings.json`:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=.\\SQLEXPRESS;Database=CampusBookDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
}
```

### Applying Migrations

The Phase-2 schema changes are captured in migration `20261113193632_Phase2Features.cs`. To apply all migrations to your local SQL Server:

```powershell
dotnet ef database update --project src/CampusBook.DataAccess --startup-project src/CampusBook.Web.UI
```

---

## 4. Automated Test Suite

The solution contains a comprehensive automated test suite in `tests/CampusBook.Tests/` using xUnit, Entity Framework Core InMemory provider, and Moq.

### Test Execution

Run the complete test suite from the repository root:

```powershell
dotnet test CampusBook.sln
```

### Test Suite Summary (66 Tests — 100% Passing)

| Test Suite | Total Tests | Focus Areas |
|:---|:---:|:---|
| `AuthenticationTests.cs` | 7 | Personal email registration, university email mapping, mandatory ID card upload, immediate login (no verification gating), duplicate email/student ID prevention, Taka currency formatting |
| `ProfileTests.cs` | 6 | Full profile retrieval, protection of sensitive fields during edit, privacy-preserving public profile, ID card photo update, 3-times max change limit enforcement, counter non-increment on other edits |
| `BookListingTests.cs` | 5 | Rejection of Donation type, price auto-zeroing for Exchange, non-negative price enforcement for Sale, unauthorized edit prevention, reserved book update locking |
| `BookCatalogueTests.cs` | 4 | Available book filtering, exclusion of historical Donation records, ISBN exact matching, price sorting |
| `BookRequestWorkflowTests.cs` | 7 | Self-request rejection, duplicate request prevention, structured offer validation for Exchange, competing request invalidation on accept, mutual handover completion |
| `ControllerSecurityTests.cs` | 18 | `[Authorize]` attribute enforcement on protected controllers/actions, `[ValidateAntiForgeryToken]` on state-modifying POST endpoints |
| `WishlistTests.cs` | 4 | Add to wishlist, prevent own book, toggle add/remove, user isolation |
| `WantedBookTests.cs` | 4 | Post creation, owner edit permission, closing posts, automated notification when matching book is published |
| `NotificationTests.cs` | 3 | Creation, live unread counter, single mark-as-read with user isolation, mark-all-read |
| `MessagingTests.cs` | 5 | Authorized chat between owner and requester, non-participant rejection, whitespace rejection, HTML/XSS sanitization, read-only mode upon exchange completion |
| `ExchangeHistoryTests.cs` | 2 | Completed transactions ledger, role breakdown (Sold, Purchased, Exchanged), preservation of offered book details |
| **Total** | **66** | **100% Pass Rate (0 Failures, 0 Skipped)** |

---

## 5. Running the Application

To run the web application locally:

```powershell
dotnet run --project src/CampusBook.Web.UI
```

Open your browser and navigate to:
- `https://localhost:7155` (HTTPS)
- `http://localhost:5248` (HTTP)

### Default Test Account Credentials
If seeded or registered:
- **Registration:** Any valid email address (e.g. `student@gmail.com`), University Email (`student@university.edu`), Student ID (e.g. `2026-CSE-001`), password (e.g. `Password123!`), and any sample ID card image file (PNG/JPG).
- **Immediate Login:** No email verification link required; sign in immediately using your personal email and password.
