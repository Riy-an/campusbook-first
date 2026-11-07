# CampusBook — University Book Exchange System (Phase-1 Student Module)

> **Target Framework:** .NET 10.0 (`net10.0`)  
> **Architecture Pattern:** N-Tier Layered Architecture with Facade Pattern  
> **Database Platform:** Microsoft SQL Server 2025 Express (`Server=.\\SQLEXPRESS;Database=CampusBookDb`)  
> **Authentication:** ASP.NET Core Secure Cookie Authentication with PBKDF2 Password Hashing

---

## 1. Project Overview

**CampusBook** is a university-only web application designed for academic textbook donation, affordable student resale, and book-for-book exchanges. It empowers verified university students to share textbooks they no longer need and acquire required course materials safely on campus.

### Scope of Phase-1: Student Module
Phase-1 delivers the complete student experience across five core functional areas:
1. **Student Registration, Authentication, and Verification:** Full registration workflow, unique Student ID and email enforcement, PBKDF2 password hashing via `IPasswordHasher<User>`, email confirmation token workflow with development delivery, secure cookie session management, and anti-forgery protection.
2. **Student Profile Management:** Viewing and editing permitted profile fields, academic affiliation (department, batch, semester), contact phone, biography, safe profile picture uploads, and a privacy-preserving public profile view for fellow students.
3. **Book Listing Management:** Publishing books for **Donation** (enforced $0 price), **Sale** (non-negative price), or **Exchange** (trade for other books). Includes condition ratings (New, Good, Fair, Poor), category and department categorization, cover image uploads, ownership verification, and optimistic concurrency locking.
4. **Book Browsing, Search, Filtering, and Details:** Database-backed catalogue queries, full-text searches (title, author, ISBN), multi-facet filtering (category, department, condition, exchange type, price range), sorting (newest, lowest price, highest price), pagination with state preservation, and book detail inspection.
5. **Book Request & Exchange Management:** Full request lifecycle (Pending &rarr; Accepted &rarr; Completed / Rejected / Cancelled). Enforces reservation on acceptance, prevents double reservation, validates offered books in trades, and requires mutual handover confirmation from both the book owner and requesting student before declaring an exchange completed.
6. **Student Dashboard & Navigation:** Quick metric cards backed by real database counts (active listings, reserved books, pending incoming, pending sent, completed exchanges), quick action navigation, and recent activity streams.

---

## 2. Architecture & Project Structure

The application adheres strictly to the architectural conventions documented in `detail.md`: an **N-Tier Layered Architecture** with the **Facade Pattern** mediating between Presentation and Data Persistence.

```
CampusBook.sln
├── src/
│   ├── CampusBook.Entities/          # Domain POCO entities and business enums
│   │   ├── Book.cs                   # Academic book listing entity
│   │   ├── BookRequest.cs            # Exchange request & handover tracking entity
│   │   ├── BookCategory.cs           # Subject category lookup entity
│   │   ├── Department.cs             # Academic department lookup entity
│   │   ├── Role.cs                   # Authorization roles (Student, Admin)
│   │   ├── StudentProfile.cs         # 1:1 user profile extension entity
│   │   ├── User.cs                   # Student identity & credentials entity
│   │   └── Enums...                  # BookCondition, ExchangeType, BookStatus, RequestStatus
│   │
│   ├── CampusBook.Framework/         # Common cross-cutting layer
│   │   ├── UrlRewrite.cs             # Centralized compile-time route string constants
│   │   └── Extensions.cs             # Date, price, and string formatting extensions
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
│   │   │   └── ILookupRepository.cs
│   │   ├── Implementation/           # Concrete repository classes
│   │   │   ├── Repository.cs         # Generic EF Core base repository
│   │   │   ├── UserRepository.cs
│   │   │   ├── StudentProfileRepository.cs
│   │   │   ├── BookRepository.cs     # Database-level filtering, search, and pagination
│   │   │   ├── BookRequestRepository.cs
│   │   │   └── LookupRepository.cs
│   │   └── Migrations/               # EF Core database migrations
│   │       └── 20261107091607_InitialCampusBookSchema.cs
│   │
│   ├── CampusBook.Facade/            # Application business logic & orchestration
│   │   ├── DependencyInjection.cs    # AddCampusBookFacade extension method
│   │   ├── Common/ServiceResult.cs   # Standardized operation result type
│   │   ├── Services/                 # Infrastructure services
│   │   │   ├── IEmailSender.cs       # Email delivery contract
│   │   │   ├── DevelopmentEmailSender.cs # Dev email dispatcher & link capture
│   │   │   ├── IFileStorageService.cs # Image upload validation & storage
│   │   │   └── FileStorageService.cs
│   │   ├── ViewModels/               # Dedicated DTOs preventing overposting
│   │   │   ├── Account/              # Register, Login, Reset Password models
│   │   │   ├── Books/                # Book create, edit, filter, details models
│   │   │   ├── Profile/              # My profile, edit profile, public profile models
│   │   │   ├── Requests/             # Request creation, sent, received, details models
│   │   │   └── Dashboard/            # Real-time metrics dashboard model
│   │   ├── Interface/                # Facade contracts
│   │   │   ├── IUserFacade.cs
│   │   │   ├── IStudentProfileFacade.cs
│   │   │   ├── IBookFacade.cs
│   │   │   ├── IBookRequestFacade.cs
│   │   │   └── IDashboardFacade.cs
│   │   └── Implementation/           # Concrete facade implementations
│   │       ├── UserFacade.cs         # Registration, hashing, email verification
│   │       ├── StudentProfileFacade.cs # Sanitized profile updates, photo storage
│   │       ├── BookFacade.cs         # Listing validation, pricing rules, catalogue queries
│   │       ├── BookRequestFacade.cs  # Request state machine & mutual handover
│   │       └── DashboardFacade.cs    # Database aggregate counters
│   │
│   └── CampusBook.Web.UI/            # Presentation layer (ASP.NET Core MVC)
│       ├── Controllers/              # Thin MVC controllers
│       │   ├── AccountController.cs  # Sign-in, sign-up, email confirmation, password reset
│       │   ├── ProfileController.cs  # Profile view, edit, public view
│       │   ├── BooksController.cs    # Catalogue, book details, CRUD
│       │   ├── RequestsController.cs # Request exchange, accept, reject, handover
│       │   ├── DashboardController.cs# Student workspace dashboard
│       │   └── HomeController.cs     # Welcome landing, error, not found
│       ├── Views/                    # Razor Views with Bootstrap 5.3 & TagHelpers
│       ├── wwwroot/                  # Static CSS, JS, Bootstrap, and upload folders
│       └── Program.cs                # Startup pipeline & cookie authentication
│
└── tests/
    └── CampusBook.Tests/             # Automated test suite (xUnit, InMemory EF Core, Moq)
        ├── AuthenticationTests.cs    # 6 tests: registration, duplicates, domain, hashing
        ├── ProfileTests.cs           # 3 tests: profile retrieval, anti-overposting, privacy
        ├── BookListingTests.cs       # 4 tests: pricing rules, ownership security, reservations
        ├── BookCatalogueTests.cs     # 4 tests: status filters, ISBN search, sorting
        ├── BookRequestWorkflowTests.cs # 7 tests: self-request, trades, reservations, handover
        └── ControllerSecurityTests.cs # 14 tests: [Authorize] and [ValidateAntiForgeryToken]
