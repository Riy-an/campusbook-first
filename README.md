# CampusBook — University Book Exchange System (Phase-1 & Phase-2)

> **Target Framework:** .NET 10.0 (`net10.0`)  
> **Architecture Pattern:** N-Tier Layered Architecture with Facade Pattern  
> **Database Platform:** Microsoft SQL Server 2025 Express (`Server=.\\SQLEXPRESS;Database=CampusBookDb`)  
> **Authentication:** ASP.NET Core Secure Cookie Authentication with PBKDF2 Password Hashing  
> **Authorization:** Database-Driven Fine-Grained Permission System with ASP.NET Core Dynamic Policies  
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
9. **Database-Driven Fine-Grained Authorization:**
   - Database tables for individual permissions, permission groups, mappings, and per-user assignments with direct overrides.
   - ASP.NET Core dynamic policy provider and server-side authorization handlers.
   - 2-minute sliding memory cache with instant cache invalidation upon privilege alteration.

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
│   │   ├── Role.cs                   # Baseline authorization roles (Student, Admin)
│   │   ├── Permission.cs             # Granular named permission entity
│   │   ├── PermissionGroup.cs        # Role/group grouping entity (Student, Admin, Moderator)
│   │   ├── PermissionGroupMap.cs     # Group-to-permission mapping entity (Composite PK)
│   │   ├── UserPermission.cs         # User group assignments & direct permission overrides
│   │   ├── StudentProfile.cs         # 1:1 user profile extension entity
│   │   ├── User.cs                   # Student identity & credentials entity
│   │   └── Enums...                  # BookCondition, ExchangeType, BookStatus, RequestStatus, WantedBookStatus, NotificationType
│   │
│   ├── CampusBook.Framework/         # Common cross-cutting layer
│   │   ├── UrlRewrite.cs             # Centralized compile-time route string constants
│   │   ├── Permissions.cs            # Compile-time permission and group constants
│   │   └── Extensions.cs             # Date, price (৳), and string formatting extensions
│   │
│   ├── CampusBook.DataAccess/        # Persistence layer (EF Core 10.0)
│   │   ├── ApplicationDbContext.cs   # DbContext with Fluent API, seed data & cascade rules
│   │   ├── DesignTimeDbContextFactory.cs # EF CLI migration factory
│   │   ├── Interface/                # Repository contracts
│   │   │   ├── IRepository.cs        # Generic base repository contract
│   │   │   ├── IUserRepository.cs
│   │   │   ├── IPermissionRepository.cs # Database permission resolution contract
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
│   │   │   ├── PermissionRepository.cs   # Effective permission evaluator with override logic
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
│   │       ├── 20261114111904_AddIdCardChangeLimitToStudentProfile.cs
│   │       └── 20261118041126_campusbook1.cs             # Consolidated full schema & fine-grained permissions
│   │
│   ├── CampusBook.Facade/            # Application business logic & orchestration
│   │   ├── DependencyInjection.cs    # AddCampusBookFacade extension method & memory cache
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
│   │   │   ├── IPermissionService.cs # Cached permission & assignment management contract
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
│   │       ├── PermissionService.cs  # Sliding cache + instant invalidation
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
│       ├── Authorization/            # ASP.NET Core dynamic authorization components
│       │   ├── PermissionRequirement.cs          # Authorization requirement
│       │   ├── PermissionAuthorizationHandler.cs # Fail-closed policy evaluation handler
│       │   ├── PermissionPolicyProvider.cs       # Dynamic "Permission:*" policy provider
│       │   └── HasPermissionAttribute.cs         # Declarative controller/action attribute
│       ├── Controllers/              # Thin MVC controllers decorated with [HasPermission(...)]
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
│       └── Program.cs                # Startup pipeline, DI registration & security policies
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
        ├── ExchangeHistoryTests.cs   # Completed ledger queries, sold/purchased/exchanged categorization, offer snapshots
        └── PermissionAuthorizationTests.cs # Seed integrity, group permissions, direct overrides, fail-close, caching
```

---

## 3. Database-Driven Fine-Grained Authorization System

CampusBook incorporates a robust, enterprise-grade, database-driven authorization system that decouples fine-grained permissions from simple static roles.

### Schema Design & Entity Relationships

The schema consists of 4 specialized authorization tables configured via Fluent API with strict referential integrity, cascading rules, and unique compound indexes:

```
+------------------+         +-----------------------+         +---------------------+
|   Permissions    |         |  PermissionGroupMaps  |         |  PermissionGroups   |
+------------------+         +-----------------------+         +---------------------+
| PermissionId (PK)|<-------+| PermissionGroupId(PK) |+------->| PermissionGroupId(PK|
| Name (Unique)    |         | PermissionId (PK)     |         | GroupName (Unique)  |
| Module           |         +-----------------------+         | Description         |
| Description      |                                           | IsSystem            |
| CreatedAt        |                                           | CreatedAt           |
+------------------+                                           +---------------------+
        ^                                                                 ^
        |                       +-------------------+                     |
        |                       |  UserPermissions  |                     |
        +-----------------------| UserPermissionId  |---------------------+
                                | UserId (FK)       |
                                | PermissionGroupId | (Nullable FK)
                                | PermissionId      | (Nullable FK)
                                | IsGranted (bool)  |
                                | CreatedAt         |
                                +-------------------+
                                          |
                                          v
                                +-------------------+
                                |       Users       |
                                +-------------------+
```

1. **`Permissions`**: Individual discrete actions named hierarchically (`Module.Action`).
2. **`PermissionGroups`**: Logical groupings of permissions (`Student`, `Admin`, `Moderator`).
3. **`PermissionGroupMaps`**: Many-to-many junction between `PermissionGroups` and `Permissions` with a composite primary key `(PermissionGroupId, PermissionId)`.
4. **`UserPermissions`**: User assignment table supporting both:
   - **Group Assignments**: `PermissionGroupId` specified, granting all group permissions.
   - **Direct Overrides**: `PermissionId` specified with `IsGranted = true` (direct grant) or `IsGranted = false` (direct deny/revocation).

### Seeded System Groups

The database migration seeds three default groups:
- **`Student` (Id: 1, System: true):** Standard student accounts for book exchange, requests, wishlist, wanted posts, messaging, and profile management.
- **`Moderator` (Id: 3, System: true):** Elevated student/staff accounts with cross-user content moderation authority (`Books.EditAny`, `Books.DeleteAny`, `WantedBooks.ManageAny`).
- **`Admin` (Id: 2, System: true):** System administrators with unrestricted platform permissions including user management and authorization configuration.

### Permission Catalog (29 Granular Permissions)

| Module | Permission Name | Description | Student | Moderator | Admin |
|:---|:---|:---|:---:|:---:|:---:|
| **Books** | `Books.View` | Browse catalogue, view public listings, and view own books | ✅ | ✅ | ✅ |
| | `Books.Create` | Create and publish new book listings | ✅ | ✅ | ✅ |
| | `Books.EditOwn` | Edit own published book listings | ✅ | ✅ | ✅ |
| | `Books.DeleteOwn` | Deactivate/delete own published book listings | ✅ | ✅ | ✅ |
| | `Books.EditAny` | Edit any book listing across the platform (Moderation) | ❌ | ✅ | ✅ |
| | `Books.DeleteAny` | Deactivate or remove any book listing (Moderation) | ❌ | ✅ | ✅ |
| **Requests** | `Requests.ViewOwn` | View sent and received exchange requests | ✅ | ✅ | ✅ |
| | `Requests.Create` | Send a new book exchange or purchase request | ✅ | ✅ | ✅ |
| | `Requests.ManageOwn` | Accept, reject, cancel, or confirm handover for own requests | ✅ | ✅ | ✅ |
| | `Requests.ManageAny` | Administratively resolve or cancel any request | ❌ | ❌ | ✅ |
| **Wishlist** | `Wishlist.ViewOwn` | View personal saved book wishlist | ✅ | ✅ | ✅ |
| | `Wishlist.ManageOwn` | Add and remove books from personal wishlist | ✅ | ✅ | ✅ |
| **WantedBooks** | `WantedBooks.View` | Browse wanted-book postings and view own posts | ✅ | ✅ | ✅ |
| | `WantedBooks.Create` | Post a request for a book not yet listed | ✅ | ✅ | ✅ |
| | `WantedBooks.EditOwn` | Edit own wanted-book request postings | ✅ | ✅ | ✅ |
| | `WantedBooks.DeleteOwn` | Close or remove own wanted-book postings | ✅ | ✅ | ✅ |
| | `WantedBooks.ManageAny` | Moderate, edit, or close any wanted-book posting | ❌ | ✅ | ✅ |
| **Messaging** | `Messaging.ViewOwn` | View personal conversation threads and chat history | ✅ | ✅ | ✅ |
| | `Messaging.Send` | Send direct messages in active exchange conversations | ✅ | ✅ | ✅ |
| **Notifications** | `Notifications.ViewOwn` | View personal notification stream and unread count | ✅ | ✅ | ✅ |
| | `Notifications.ManageOwn` | Mark notifications as read or clear notification center | ✅ | ✅ | ✅ |
| **History** | `History.ViewOwn` | View personal exchange transaction history ledger | ✅ | ✅ | ✅ |
| **Profile** | `Profile.ViewOwn` | View own complete student profile and dashboard | ✅ | ✅ | ✅ |
| | `Profile.EditOwn` | Edit permitted personal profile details and ID card | ✅ | ✅ | ✅ |
| | `Profile.ViewPublic` | View public sanitized student profile | ✅ | ✅ | ✅ |
| **Admin** | `Admin.Users.View` | View all registered student accounts and profiles | ❌ | ❌ | ✅ |
| | `Admin.Users.Manage` | Activate, deactivate, or edit user accounts | ❌ | ❌ | ✅ |
| | `Admin.Permissions.View` | View authorization groups and permission matrices | ❌ | ❌ | ✅ |
| | `Admin.Permissions.Manage`| Assign groups, grant direct permissions, or revoke rights | ❌ | ❌ | ✅ |

### Evaluation Pipeline & Security Precedence

Effective permissions are resolved by `IPermissionRepository.GetUserEffectivePermissionsAsync` using a strict fail-closed pipeline:

```
[Incoming Request]
       │
       ▼
[1. User Active Check] ──(IsActive == false or not found)──> [Return Empty / Deny Access]
       │ (Active)
       ▼
[2. Collect Group IDs] (Explicit UserPermission.PermissionGroupId + Baseline User.RoleId)
       │
       ▼
[3. Gather Group Permissions] (Query PermissionGroupMaps for all collected Group IDs)
       │
       ▼
[4. Direct User Overrides]
       ├─ If UserPermission has PermissionId with IsGranted == true  ──> Add Permission
       └─ If UserPermission has PermissionId with IsGranted == false ──> Strip Permission (Explicit Deny)
       │
       ▼
[Effective Permissions Set] ──> Cached in Memory (2 min sliding)
```

1. **Active Check:** If a user account is inactive (`IsActive = false`), the system immediately fails closed and returns an empty set.
2. **Backward Compatibility:** In addition to explicit `UserPermission` rows, the evaluation automatically includes `user.RoleId`. Existing users with `RoleId = 1` retain their Student permissions immediately upon migration without requiring manual data fixes.
3. **Explicit Deny Precedence:** If a direct `UserPermission` row exists with `IsGranted = false` for a permission, it overrides and strips that permission, even if granted by multiple assigned groups.
4. **Independent Ownership Enforcement:** Action permissions (e.g., `Books.EditOwn`, `Requests.ManageOwn`) are evaluated server-side by attributes, but resource ownership is checked independently in the Facade layer. A student with `Books.EditOwn` can never edit another student's listing.

### Performance Caching & Instant Invalidation

To eliminate database roundtrips on every HTTP request:
- Permissions are cached using ASP.NET Core `IMemoryCache` with a **2-minute sliding expiration** and **5-minute absolute expiration**.
- When an administrator modifies group assignments (`AssignGroupToUserAsync`, `RemoveGroupFromUserAsync`) or direct permissions (`GrantDirectPermissionAsync`, `RevokeDirectPermissionAsync`), the service immediately invokes `InvalidateUserPermissionsCache(userId)`.
- The user's updated authorization state takes effect on the very next HTTP request without waiting for cookie expiration.

### Server-Side Policy Enforcement

Protected controller actions are annotated using `[HasPermission(...)]`:

```csharp
[HttpPost]
[Route(UrlRewrite.Books.Create)]
[HasPermission(Permissions.Books.Create)]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Create(BookCreateViewModel model)
{
    // ...
}
```

The dynamic `PermissionPolicyProvider` translates any `HasPermission("Module.Action")` attribute into a `PermissionRequirement`. The `PermissionAuthorizationHandler` queries the cached `IPermissionService`. If the requirement is not met, the handler fails closed, resulting in an immediate HTTP `403 Forbidden` (or redirect to Login for unauthenticated requests).

---

## 4. Database Setup & Migrations

The database connection string is configured in `src/CampusBook.Web.UI/appsettings.json`:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=.\\SQLEXPRESS;Database=CampusBookDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
}
```

### Applying Migrations

The complete database schema—including Phase-1, Phase-2 features, ID card change limits, and the Fine-Grained Authorization system—is managed via EF Core migrations:

```powershell
dotnet ef database update --project src/CampusBook.DataAccess --startup-project src/CampusBook.Web.UI
```

**Key Migrations:**
- `20261118041126_campusbook1.cs` — Complete consolidated database schema including Phase-1, Phase-2 features (Wishlist, WantedBooks, Notifications, Messages, ExchangeOffers, ID card limit) and fine-grained permissions (`Permissions`, `PermissionGroups`, `PermissionGroupMaps`, `UserPermissions`).

---

## 5. Automated Test Suite

The solution contains an automated test suite in `tests/CampusBook.Tests/` using xUnit, Entity Framework Core InMemory provider, and Moq.

### Test Execution

Run the complete test suite from the repository root:

```powershell
dotnet test CampusBook.sln
```

### Test Suite Summary (104 Tests — 100% Passing)

| Test Suite | Total Tests | Focus Areas |
|:---|:---:|:---|
| `AuthenticationTests.cs` | 7 | Personal email registration, university email mapping, mandatory ID card upload, immediate login, duplicate prevention, Taka currency formatting |
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
| `PermissionAuthorizationTests.cs` | 38 | Seed integrity, default group permissions, admin full access, moderator elevation, legacy role inheritance, inactive fail-closed, direct grants/denials, sliding memory cache with instant invalidation, auto-group assignment during registration, ASP.NET Core authorization handler evaluation, and controller action attribute verification |
| **Total** | **104** | **100% Pass Rate (0 Failures, 0 Skipped)** |

---

## 6. Running the Application

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
- **Default Permissions:** Every registered student account is immediately assigned the `Student` permission group, granting full textbook exchange, wishlist, wanted post, messaging, and profile capabilities.
