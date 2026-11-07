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
```

---

## 3. Database Design & Entity-Relationship (ER) Diagram

The persistence schema targets **Microsoft SQL Server 2025 Express** and resolves multiple cascade paths (preventing SQL Server Error 1785) by applying `DeleteBehavior.Restrict` across intersecting parent-child relationships.

```mermaid
erDiagram
    Roles ||--o{ Users : "assigns (Restrict)"
    Users ||--o| StudentProfiles : "extends (Cascade 1:1)"
    Departments ||--o{ Books : "classifies (Restrict)"
    BookCategories ||--o{ Books : "categorizes (Restrict)"
    Users ||--o{ Books : "owns (Restrict)"
    Books ||--o{ BookRequests : "requested_in (Restrict)"
    Users ||--o{ BookRequests : "submits (Restrict)"
    Books ||--o{ BookRequests : "offered_as_trade (Restrict)"

    Roles {
        int RoleId PK
        string RoleName UK
    }

    Users {
        int UserId PK
        string FullName
        string StudentId UK
        string Email UK
        string PasswordHash
        int RoleId FK
        bool IsEmailConfirmed
        string EmailConfirmationToken
        DateTime EmailConfirmationTokenExpiry
        string PasswordResetToken
        DateTime PasswordResetTokenExpiry
        bool IsActive
        DateTime CreatedAt
    }

    StudentProfiles {
        int UserId PK_FK
        string Department
        string Batch
        string Semester
        string PhoneNumber
        string ProfilePictureUrl
        string Bio
        DateTime UpdatedAt
    }

    Departments {
        int DepartmentId PK
        string Name
        string Code UK
    }

    BookCategories {
        int CategoryId PK
        string Name UK
        string Description
    }

    Books {
        int BookId PK
        string Title
        string Author
        string Isbn
        string Edition
        string CourseCode
        int CategoryId FK
        int DepartmentId FK
        int Condition
        int ExchangeType
        decimal Price
        string Description
        string ImageUrl
        int OwnerId FK
        int Status
        DateTime CreatedAt
        DateTime UpdatedAt
        byte[] RowVersion
    }

    BookRequests {
        int RequestId PK
        int BookId FK
        int RequesterId FK
        int OfferedBookId FK_NULL
        string RequestMessage
        int Status
        bool OwnerConfirmed
        DateTime OwnerConfirmedAt
        bool RequesterConfirmed
        DateTime RequesterConfirmedAt
        DateTime CreatedAt
        DateTime UpdatedAt
    }
```

---

## 4. Key Business Rules & Workflows

### 4.1 Authentication & Email Verification
- Students register with Full Name, unique Student ID, unique University Email, Department, and Batch.
- Passwords are encrypted with PBKDF2 (`IPasswordHasher<User>`). Plain-text passwords are never stored or compared.
- An email confirmation token is generated upon registration. Protected student operations (publishing listings and submitting exchange requests) strictly require an email-confirmed account (`IsEmailConfirmed == true`).
- In Development mode, the generated confirmation link is output to the logs and displayed on the confirmation screen for rapid testing without external SMTP credentials.

### 4.2 Book Listing Lifecycle & Pricing
- **Donation:** Enforces a `$0.00` price.
- **Sale:** Enforces non-negative pricing (`Price >= 0`).
- **Exchange:** Trade-based listing (`Price = 0.00`).
- Listings are immediately active (`BookStatus.Available`).
- When a request is accepted, the book transitions to `BookStatus.Reserved`.
- When an exchange completes, the book transitions to `BookStatus.Completed` (permanently unavailable in catalogue).
- Books in the `Reserved` state cannot be edited or deactivated.
- Only the book's owner may edit or deactivate the listing.

### 4.3 Book Request & Mutual Handover Lifecycle
```
[Pending] ──(Owner Rejects)───────> [Rejected]
   │
   ├──(Requester Cancels)─────────> [Cancelled]
   │
   └──(Owner Accepts)─────────────> [Accepted] (Book status -> Reserved)
                                       │
                                       ├──(Owner Confirms Handover)
                                       │       +
                                       ├──(Requester Confirms Receipt)
                                       │
                                       ▼
                                  [Completed] (Book status -> Completed)
```
- A student cannot request their own book.
- Duplicate active requests for the same book by the same student are prevented.
- When requesting an **Exchange** listing, the requester must select one of their own available book listings as the offered trade book.
- When an exchange request is accepted, **both** the requested book and the offered trade book are placed in `Reserved` status.
- Conflicting pending requests for the same book are automatically closed/rejected upon acceptance to guarantee concurrency safety.
- Handover requires explicit confirmation from **both** the book owner and the requesting student. Once both have confirmed, the request moves to `Completed` and both books are marked `Completed`.

---

## 5. SQL Server Configuration & Database Setup

### 5.1 Local SQL Server 2025 Express Instance Setup
1. Ensure Microsoft SQL Server 2025 Express is installed and running on your local workstation.
2. The default connection string configured in `src/CampusBook.Web.UI/appsettings.json` is:
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Server=.\\SQLEXPRESS;Database=CampusBookDb;Trusted_Connection=True;TrustServerCertificate=True;"
   }
   ```
3. If your local instance has a different name (e.g., `localhost` or `(localdb)\\MSSQLLocalDB`), modify the `Server` property in `appsettings.json` or override via environment variable `ConnectionStrings__DefaultConnection`.

### 5.2 Applying Entity Framework Core Migrations
To create the database and apply the initial schema with all 7 tables, indexes, constraints, and seed data, open PowerShell at the repository root:

```powershell
dotnet ef database update --project src/CampusBook.DataAccess --startup-project src/CampusBook.Web.UI
```

*Alternatively, using Visual Studio Package Manager Console:*
```powershell
Update-Database -Project CampusBook.DataAccess -StartupProject CampusBook.Web.UI
```

This creates the `CampusBookDb` database and populates:
- Initial Roles: `Student` (Id: 1), `Admin` (Id: 2).
- Academic Departments: Computer Science (CSE), Electrical Engineering (EEE), Business Administration (BBA), Economics (ECON), English (ENG), Mathematics (MNS).
- Subject Categories: Computer Science & IT, Mathematics & Statistics, Engineering, Business & Economics, Natural Sciences, Humanities, Literature & Languages.

### 5.3 Troubleshooting Database Connection Failures
- **Error: "A network-related or instance-specific error has occurred while establishing a connection to SQL Server"**:
  1. Open Windows `Services.msc` and verify that the `SQL Server (SQLEXPRESS)` service is in the **Running** state.
  2. Open SQL Server Configuration Manager &rarr; SQL Server Network Configuration &rarr; Protocols for SQLEXPRESS, and verify that **TCP/IP** and **Named Pipes** are **Enabled**.
  3. Ensure SQL Server Browser service is running if connecting to a named instance over TCP/IP.
- **Error: "Cannot open database CampusBookDb requested by the login"**:
  1. Run `dotnet ef database update` to ensure the database and tables are provisioned.

---

## 6. How to Run the Application

### 6.1 Using the .NET CLI
```powershell
dotnet run --project src/CampusBook.Web.UI
```

### 6.2 Default Endpoints
- **HTTPS:** `https://localhost:7155`
- **HTTP:** `http://localhost:5105`
- **Catalogue:** `https://localhost:7155/books`
- **Sign In:** `https://localhost:7155/login`
- **Register:** `https://localhost:7155/register`

---

## 7. Automated Test Suite & Verification Results

The test project `tests/CampusBook.Tests` contains **38 automated unit and integration tests** executing against EF Core InMemory storage.

### 7.1 Running the Automated Tests
```powershell
dotnet test tests/CampusBook.Tests/CampusBook.Tests.csproj
```

### 7.2 Verified Test Scenarios Matrix

| Category | Test Name | Scenario Verified | Result |
| :--- | :--- | :--- | :---: |
| **Auth** | `RegisterAsync_WithValidStudentData_SucceedsAndHashesPassword` | Student registration succeeds; password hashed via PBKDF2; Student role assigned | **PASSED** |
| **Auth** | `RegisterAsync_WithDuplicateEmail_IsRejected` | Registration with an existing email is rejected | **PASSED** |
| **Auth** | `RegisterAsync_WithDuplicateStudentId_IsRejected` | Registration with an existing Student ID is rejected | **PASSED** |
| **Auth** | `RegisterAsync_WithRestrictedEmailDomain_EnforcesDomain` | Configurable university domain check rejects non-institutional emails | **PASSED** |
| **Auth** | `ValidateLoginAsync_WhenEmailNotConfirmed_BlocksLogin` | Unverified email accounts cannot log in | **PASSED** |
| **Auth** | `ConfirmEmailAsync_WithValidToken_EnablesAccountLogin` | Valid confirmation token confirms account and permits login | **PASSED** |
| **Profile**| `GetMyProfileAsync_ReturnsFullProfileData` | Authenticated student can view their complete profile and book counts | **PASSED** |
| **Profile**| `UpdateProfileAsync_OnlyModifiesPermittedFields_AndProtectsSensitiveFields` | Anti-overposting test: StudentId, Email, Role, and Confirmation status cannot be altered | **PASSED** |
| **Profile**| `GetPublicProfileAsync_HidesPrivateContactDetails` | Public student profile conceals phone number and email | **PASSED** |
| **Book** | `CreateBookAsync_DonationType_ForcesPriceToZero` | Donation listing strictly sets price to $0.00 | **PASSED** |
| **Book** | `CreateBookAsync_SaleType_WithNegativePrice_IsRejected` | Sale listing with negative price is rejected | **PASSED** |
| **Book** | `UpdateBookAsync_ByNonOwner_IsRejected` | Modifying another student's listing is rejected with unauthorized error | **PASSED** |
| **Book** | `UpdateBookAsync_WhenBookIsReserved_IsRejected` | Listings in Reserved state cannot be modified | **PASSED** |
| **Catalog**| `GetCatalogueAsync_OnlyReturnsAvailableBooks` | Reserved, Inactive, and Completed books are excluded from the public catalogue | **PASSED** |
| **Catalog**| `GetCatalogueAsync_SearchByIsbn_ReturnsExactMatch` | Full-text ISBN search retrieves the matching book | **PASSED** |
| **Catalog**| `GetCatalogueAsync_FilterByCategory_ReturnsOnlyMatchingCategory` | Multi-facet filtering by category retrieves matching books | **PASSED** |
| **Catalog**| `GetCatalogueAsync_SortByPriceAscending_SortsCorrectly` | Price ascending sorting orders free and low-cost books first | **PASSED** |
| **Request**| `CreateRequestAsync_WhenRequestingOwnBook_IsRejected` | Student cannot request their own book | **PASSED** |
| **Request**| `CreateRequestAsync_DuplicateActiveRequest_IsRejected` | Submitting duplicate active requests for the same book is rejected | **PASSED** |
| **Request**| `CreateRequestAsync_BookForBookExchange_WithoutOfferedBook_IsRejected` | Trade request requires selecting an owned available trade book | **PASSED** |
| **Request**| `AcceptRequestAsync_ReservesBook_AndInvalidatesOtherPendingRequests` | Accepting request reserves book and closes conflicting pending requests | **PASSED** |
| **Request**| `AcceptTradeRequest_ReservesBothRequestedAndOfferedBooks` | Accepting a trade request reserves both the requested and offered books | **PASSED** |
| **Request**| `CancelRequestAsync_WhenAccepted_RestoresAvailability` | Cancelling an accepted request restores books to Available status | **PASSED** |
| **Request**| `ConfirmHandoverAsync_RequiresBothParties_ToCompleteExchange` | Both parties must confirm handover before request and books are marked Completed | **PASSED** |
| **Security**| `DashboardController_HasAuthorizeAttribute` | Protected dashboard requires authentication | **PASSED** |
| **Security**| `ProfileController_HasAuthorizeAttribute` | Profile management requires authentication | **PASSED** |
| **Security**| `RequestsController_HasAuthorizeAttribute` | Request management requires authentication | **PASSED** |
| **Security**| `BooksController_ProtectedActions_HaveAuthorizeAttribute` (4 tests) | Create, Edit, MyBooks, and Delete actions require authentication | **PASSED** |
| **Security**| `SensitivePostActions_HaveValidateAntiForgeryTokenAttribute` (7 tests) | All sensitive form POST handlers enforce CSRF anti-forgery token validation | **PASSED** |

**Summary: 38 Passed, 0 Failed, 0 Skipped.**

