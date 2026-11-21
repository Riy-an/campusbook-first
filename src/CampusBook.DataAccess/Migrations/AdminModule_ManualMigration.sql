-- ============================================================
-- CampusBook: Admin Module Schema Migration SQL Script
-- Adds Admin tables, columns, indexes, and seed data
-- Safe to re-run: uses IF NOT EXISTS checks everywhere
-- ============================================================

-- ============ 1. ADD COLUMNS TO EXISTING TABLES ============

-- Users: suspension tracking
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE name = 'SuspendedAt' AND object_id = OBJECT_ID('Users'))
    ALTER TABLE [Users] ADD [SuspendedAt] datetime2 NULL;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE name = 'SuspendedByAdminId' AND object_id = OBJECT_ID('Users'))
    ALTER TABLE [Users] ADD [SuspendedByAdminId] int NULL;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE name = 'SuspensionReason' AND object_id = OBJECT_ID('Users'))
    ALTER TABLE [Users] ADD [SuspensionReason] nvarchar(max) NULL;

-- StudentProfiles: verification tracking
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE name = 'VerificationStatus' AND object_id = OBJECT_ID('StudentProfiles'))
    ALTER TABLE [StudentProfiles] ADD [VerificationStatus] int NOT NULL DEFAULT 1;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE name = 'VerificationNotes' AND object_id = OBJECT_ID('StudentProfiles'))
    ALTER TABLE [StudentProfiles] ADD [VerificationNotes] nvarchar(max) NULL;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE name = 'VerifiedAt' AND object_id = OBJECT_ID('StudentProfiles'))
    ALTER TABLE [StudentProfiles] ADD [VerifiedAt] datetime2 NULL;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE name = 'VerifiedByAdminId' AND object_id = OBJECT_ID('StudentProfiles'))
    ALTER TABLE [StudentProfiles] ADD [VerifiedByAdminId] int NULL;

-- Books: moderation tracking
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE name = 'ModeratedAt' AND object_id = OBJECT_ID('Books'))
    ALTER TABLE [Books] ADD [ModeratedAt] datetime2 NULL;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE name = 'ModeratedByAdminId' AND object_id = OBJECT_ID('Books'))
    ALTER TABLE [Books] ADD [ModeratedByAdminId] int NULL;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE name = 'ModerationReason' AND object_id = OBJECT_ID('Books'))
    ALTER TABLE [Books] ADD [ModerationReason] nvarchar(max) NULL;

-- Departments: active toggle
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE name = 'IsActive' AND object_id = OBJECT_ID('Departments'))
    ALTER TABLE [Departments] ADD [IsActive] bit NOT NULL DEFAULT 1;
GO

-- Update existing departments to active
UPDATE [Departments] SET [IsActive] = 1 WHERE [IsActive] = 0;
GO

-- BookCategories: active toggle + created timestamp
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE name = 'IsActive' AND object_id = OBJECT_ID('BookCategories'))
    ALTER TABLE [BookCategories] ADD [IsActive] bit NOT NULL DEFAULT 1;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE name = 'CreatedAt' AND object_id = OBJECT_ID('BookCategories'))
    ALTER TABLE [BookCategories] ADD [CreatedAt] datetime2 NOT NULL DEFAULT '2026-01-01T00:00:00.0000000';
GO

-- Update existing categories to active
UPDATE [BookCategories] SET [IsActive] = 1 WHERE [IsActive] = 0;
GO

-- ============ 2. CREATE NEW TABLES ============

-- AuditLogs table
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'AuditLogs')
BEGIN
    CREATE TABLE [AuditLogs] (
        [AuditLogId] int NOT NULL IDENTITY,
        [AdminUserId] int NULL,
        [Action] nvarchar(100) NOT NULL,
        [TargetEntityType] nvarchar(100) NOT NULL,
        [TargetId] nvarchar(100) NULL,
        [Details] nvarchar(4000) NOT NULL,
        [IpAddress] nvarchar(50) NULL,
        [TimestampUtc] datetime2 NOT NULL,
        [IsSuccess] bit NOT NULL,
        CONSTRAINT [PK_AuditLogs] PRIMARY KEY ([AuditLogId]),
        CONSTRAINT [FK_AuditLogs_Users_AdminUserId] FOREIGN KEY ([AdminUserId]) REFERENCES [Users] ([UserId]) ON DELETE SET NULL
    );
    CREATE INDEX [IX_AuditLogs_AdminUserId] ON [AuditLogs] ([AdminUserId]);
    CREATE INDEX [IX_AuditLogs_TimestampUtc] ON [AuditLogs] ([TimestampUtc]);
END
GO

-- Courses table
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Courses')
BEGIN
    CREATE TABLE [Courses] (
        [CourseId] int NOT NULL IDENTITY,
        [CourseCode] nvarchar(20) NOT NULL,
        [CourseName] nvarchar(150) NOT NULL,
        [DepartmentId] int NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Courses] PRIMARY KEY ([CourseId]),
        CONSTRAINT [FK_Courses_Departments_DepartmentId] FOREIGN KEY ([DepartmentId]) REFERENCES [Departments] ([DepartmentId]) ON DELETE NO ACTION
    );
    CREATE INDEX [IX_Courses_DepartmentId] ON [Courses] ([DepartmentId]);
    CREATE UNIQUE INDEX [IX_Courses_CourseCode] ON [Courses] ([CourseCode]);
END
GO

-- ExchangeReviews table
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ExchangeReviews')
BEGIN
    CREATE TABLE [ExchangeReviews] (
        [ReviewId] int NOT NULL IDENTITY,
        [RequestId] int NOT NULL,
        [ReviewerId] int NOT NULL,
        [ReviewedUserId] int NOT NULL,
        [Rating] int NOT NULL,
        [Comment] nvarchar(2000) NOT NULL,
        [IsHidden] bit NOT NULL,
        [ModerationReason] nvarchar(500) NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_ExchangeReviews] PRIMARY KEY ([ReviewId]),
        CONSTRAINT [FK_ExchangeReviews_BookRequests_RequestId] FOREIGN KEY ([RequestId]) REFERENCES [BookRequests] ([RequestId]) ON DELETE CASCADE,
        CONSTRAINT [FK_ExchangeReviews_Users_ReviewedUserId] FOREIGN KEY ([ReviewedUserId]) REFERENCES [Users] ([UserId]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ExchangeReviews_Users_ReviewerId] FOREIGN KEY ([ReviewerId]) REFERENCES [Users] ([UserId]) ON DELETE NO ACTION
    );
    CREATE INDEX [IX_ExchangeReviews_RequestId] ON [ExchangeReviews] ([RequestId]);
    CREATE INDEX [IX_ExchangeReviews_ReviewedUserId] ON [ExchangeReviews] ([ReviewedUserId]);
    CREATE INDEX [IX_ExchangeReviews_ReviewerId] ON [ExchangeReviews] ([ReviewerId]);
END
GO

-- Reports table
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Reports')
BEGIN
    CREATE TABLE [Reports] (
        [ReportId] int NOT NULL IDENTITY,
        [ReporterId] int NOT NULL,
        [TargetType] int NOT NULL,
        [TargetId] int NOT NULL,
        [Reason] nvarchar(100) NOT NULL,
        [Description] nvarchar(2000) NOT NULL,
        [Status] int NOT NULL,
        [AssignedAdminId] int NULL,
        [ResolutionNotes] nvarchar(2000) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [ResolvedAt] datetime2 NULL,
        CONSTRAINT [PK_Reports] PRIMARY KEY ([ReportId]),
        CONSTRAINT [FK_Reports_Users_AssignedAdminId] FOREIGN KEY ([AssignedAdminId]) REFERENCES [Users] ([UserId]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Reports_Users_ReporterId] FOREIGN KEY ([ReporterId]) REFERENCES [Users] ([UserId]) ON DELETE NO ACTION
    );
    CREATE INDEX [IX_Reports_AssignedAdminId] ON [Reports] ([AssignedAdminId]);
    CREATE INDEX [IX_Reports_ReporterId] ON [Reports] ([ReporterId]);
END
GO

-- ============ 3. SEED ADMIN PERMISSIONS (30-43) ============

IF NOT EXISTS (SELECT 1 FROM [Permissions] WHERE [PermissionId] = 30)
BEGIN
    SET IDENTITY_INSERT [Permissions] ON;
    INSERT INTO [Permissions] ([PermissionId], [Name], [Module], [Description], [CreatedAt]) VALUES
        (30, N'Admin.Dashboard.View', N'Administration', N'View real-time database-backed admin metrics and dashboards', '2026-01-01T00:00:00'),
        (31, N'Admin.Students.View', N'Administration', N'Search, filter, and view student accounts and verification status', '2026-01-01T00:00:00'),
        (32, N'Admin.Students.Verify', N'Administration', N'Review university affiliation and verify or reject student ID cards', '2026-01-01T00:00:00'),
        (33, N'Admin.Students.Suspend', N'Administration', N'Suspend or reactivate student accounts with reason recording', '2026-01-01T00:00:00'),
        (34, N'Admin.Students.Manage', N'Administration', N'Comprehensive student account management and profile oversight', '2026-01-01T00:00:00'),
        (35, N'Admin.Books.View', N'Administration', N'Inspect all student book listings across the platform', '2026-01-01T00:00:00'),
        (36, N'Admin.Books.Moderate', N'Administration', N'Approve, reject, or archive inappropriate book listings', '2026-01-01T00:00:00'),
        (37, N'Admin.Categories.Manage', N'Administration', N'Create, edit, and activate/deactivate categories and academic classifications', '2026-01-01T00:00:00'),
        (38, N'Admin.Requests.View', N'Administration', N'Monitor book exchange requests, transactions, and reservation states', '2026-01-01T00:00:00'),
        (39, N'Admin.Exchanges.View', N'Administration', N'Inspect trade offers, completed exchanges, and handover records', '2026-01-01T00:00:00'),
        (40, N'Admin.Reports.Manage', N'Administration', N'Investigate user/listing complaints, resolve or dismiss reports', '2026-01-01T00:00:00'),
        (41, N'Admin.Reviews.Moderate', N'Administration', N'Moderate, hide, or restore student feedback and exchange ratings', '2026-01-01T00:00:00'),
        (42, N'Admin.Analytics.View', N'Administration', N'Access institutional analytics, trend breakdowns, and CSV reports', '2026-01-01T00:00:00'),
        (43, N'Admin.AuditLogs.View', N'Administration', N'Inspect immutable security audit trails and administrative action history', '2026-01-01T00:00:00');
    SET IDENTITY_INSERT [Permissions] OFF;
END
GO

-- Map admin permissions to Admin group (PermissionGroupId = 2)
IF NOT EXISTS (SELECT 1 FROM [PermissionGroupMaps] WHERE [PermissionGroupId] = 2 AND [PermissionId] = 30)
BEGIN
    INSERT INTO [PermissionGroupMaps] ([PermissionGroupId], [PermissionId], [CreatedAt]) VALUES
        (2, 30, '2026-01-01T00:00:00'),
        (2, 31, '2026-01-01T00:00:00'),
        (2, 32, '2026-01-01T00:00:00'),
        (2, 33, '2026-01-01T00:00:00'),
        (2, 34, '2026-01-01T00:00:00'),
        (2, 35, '2026-01-01T00:00:00'),
        (2, 36, '2026-01-01T00:00:00'),
        (2, 37, '2026-01-01T00:00:00'),
        (2, 38, '2026-01-01T00:00:00'),
        (2, 39, '2026-01-01T00:00:00'),
        (2, 40, '2026-01-01T00:00:00'),
        (2, 41, '2026-01-01T00:00:00'),
        (2, 42, '2026-01-01T00:00:00'),
        (2, 43, '2026-01-01T00:00:00');
END
GO

-- Map moderation permissions to Moderator group (PermissionGroupId = 3)
IF NOT EXISTS (SELECT 1 FROM [PermissionGroupMaps] WHERE [PermissionGroupId] = 3 AND [PermissionId] = 35)
BEGIN
    INSERT INTO [PermissionGroupMaps] ([PermissionGroupId], [PermissionId], [CreatedAt]) VALUES
        (3, 35, '2026-01-01T00:00:00'),
        (3, 36, '2026-01-01T00:00:00'),
        (3, 40, '2026-01-01T00:00:00');
END
GO

PRINT 'Admin Module schema migration completed successfully.';
GO
