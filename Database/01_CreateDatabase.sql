CREATE TABLE [AuditLogs] (
    [AuditLogId] int NOT NULL IDENTITY,
    [CreatedAt] datetime2 NOT NULL,
    [UserId] int NULL,
    [Username] nvarchar(50) NULL,
    [Role] nvarchar(20) NULL,
    [Action] nvarchar(100) NOT NULL,
    [Detail] nvarchar(500) NULL,
    [Success] bit NOT NULL,
    [IpAddress] nvarchar(45) NULL,
    CONSTRAINT [PK_AuditLogs] PRIMARY KEY ([AuditLogId])
);
GO


CREATE TABLE [Categories] (
    [CategoryId] int NOT NULL IDENTITY,
    [Name] nvarchar(100) NOT NULL,
    [Description] nvarchar(255) NULL,
    CONSTRAINT [PK_Categories] PRIMARY KEY ([CategoryId])
);
GO


CREATE TABLE [Settings] (
    [SettingKey] nvarchar(50) NOT NULL,
    [SettingValue] nvarchar(200) NOT NULL,
    CONSTRAINT [PK_Settings] PRIMARY KEY ([SettingKey])
);
GO


CREATE TABLE [Users] (
    [UserId] int NOT NULL IDENTITY,
    [Username] nvarchar(50) NOT NULL,
    [PasswordHash] nvarchar(256) NOT NULL,
    [FullName] nvarchar(100) NOT NULL,
    [Email] nvarchar(100) NULL,
    [Role] nvarchar(20) NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Users] PRIMARY KEY ([UserId])
);
GO


CREATE TABLE [Books] (
    [BookId] int NOT NULL IDENTITY,
    [BookCode] nvarchar(20) NOT NULL,
    [Title] nvarchar(200) NOT NULL,
    [ISBN] nvarchar(20) NULL,
    [CategoryId] int NOT NULL,
    [Author] nvarchar(150) NOT NULL,
    [Publisher] nvarchar(150) NULL,
    [PublishYear] int NULL,
    [TotalCopies] int NOT NULL,
    [AvailableCopies] int NOT NULL,
    [ShelfLocation] nvarchar(50) NULL,
    [CoverImageUrl] nvarchar(500) NULL,
    [Description] nvarchar(2000) NULL,
    [IsActive] bit NOT NULL,
    [RowVersion] rowversion NULL,
    CONSTRAINT [PK_Books] PRIMARY KEY ([BookId]),
    CONSTRAINT [CK_Books_Copies] CHECK ([AvailableCopies] >= 0 AND [AvailableCopies] <= [TotalCopies]),
    CONSTRAINT [FK_Books_Categories_CategoryId] FOREIGN KEY ([CategoryId]) REFERENCES [Categories] ([CategoryId]) ON DELETE NO ACTION
);
GO


CREATE TABLE [Readers] (
    [ReaderId] int NOT NULL IDENTITY,
    [ReaderCode] nvarchar(20) NOT NULL,
    [FullName] nvarchar(100) NOT NULL,
    [Phone] nvarchar(20) NULL,
    [Email] nvarchar(100) NULL,
    [Address] nvarchar(255) NULL,
    [CardIssuedDate] datetime2 NOT NULL,
    [CardExpiryDate] datetime2 NOT NULL,
    [IsActive] bit NOT NULL,
    [UserId] int NULL,
    CONSTRAINT [PK_Readers] PRIMARY KEY ([ReaderId]),
    CONSTRAINT [FK_Readers_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([UserId]) ON DELETE NO ACTION
);
GO


CREATE TABLE [BookReservations] (
    [BookReservationId] int NOT NULL IDENTITY,
    [BookId] int NOT NULL,
    [ReaderId] int NOT NULL,
    [RequestedAt] datetime2 NOT NULL,
    [Status] nvarchar(20) NOT NULL,
    [ReadyAt] datetime2 NULL,
    [ExpiresAt] datetime2 NULL,
    [FulfilledAt] datetime2 NULL,
    [CancelledAt] datetime2 NULL,
    [FulfilledLoanId] int NULL,
    CONSTRAINT [PK_BookReservations] PRIMARY KEY ([BookReservationId]),
    CONSTRAINT [FK_BookReservations_Books_BookId] FOREIGN KEY ([BookId]) REFERENCES [Books] ([BookId]) ON DELETE NO ACTION,
    CONSTRAINT [FK_BookReservations_Readers_ReaderId] FOREIGN KEY ([ReaderId]) REFERENCES [Readers] ([ReaderId]) ON DELETE NO ACTION
);
GO


CREATE TABLE [Loans] (
    [LoanId] int NOT NULL IDENTITY,
    [ReaderId] int NOT NULL,
    [CreatedByUserId] int NOT NULL,
    [LoanDate] datetime2 NOT NULL,
    [DueDate] datetime2 NOT NULL,
    [Note] nvarchar(255) NULL,
    [RenewCount] int NOT NULL,
    CONSTRAINT [PK_Loans] PRIMARY KEY ([LoanId]),
    CONSTRAINT [FK_Loans_Readers_ReaderId] FOREIGN KEY ([ReaderId]) REFERENCES [Readers] ([ReaderId]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Loans_Users_CreatedByUserId] FOREIGN KEY ([CreatedByUserId]) REFERENCES [Users] ([UserId]) ON DELETE NO ACTION
);
GO


CREATE TABLE [LoanDetails] (
    [LoanDetailId] int NOT NULL IDENTITY,
    [LoanId] int NOT NULL,
    [BookId] int NOT NULL,
    [ReturnDate] datetime2 NULL,
    [FineAmount] decimal(18,2) NOT NULL,
    [ReceivedByUserId] int NULL,
    CONSTRAINT [PK_LoanDetails] PRIMARY KEY ([LoanDetailId]),
    CONSTRAINT [FK_LoanDetails_Books_BookId] FOREIGN KEY ([BookId]) REFERENCES [Books] ([BookId]) ON DELETE NO ACTION,
    CONSTRAINT [FK_LoanDetails_Loans_LoanId] FOREIGN KEY ([LoanId]) REFERENCES [Loans] ([LoanId]) ON DELETE CASCADE,
    CONSTRAINT [FK_LoanDetails_Users_ReceivedByUserId] FOREIGN KEY ([ReceivedByUserId]) REFERENCES [Users] ([UserId]) ON DELETE NO ACTION
);
GO


CREATE TABLE [LoanRenewals] (
    [LoanRenewalId] int NOT NULL IDENTITY,
    [LoanId] int NOT NULL,
    [RenewedAt] datetime2 NOT NULL,
    [RenewDays] int NOT NULL,
    [RenewCount] int NOT NULL,
    [OutstandingCopies] int NOT NULL,
    [FeeAmount] decimal(18,2) NOT NULL,
    [RenewedByUserId] int NOT NULL,
    CONSTRAINT [PK_LoanRenewals] PRIMARY KEY ([LoanRenewalId]),
    CONSTRAINT [FK_LoanRenewals_Loans_LoanId] FOREIGN KEY ([LoanId]) REFERENCES [Loans] ([LoanId]) ON DELETE NO ACTION,
    CONSTRAINT [FK_LoanRenewals_Users_RenewedByUserId] FOREIGN KEY ([RenewedByUserId]) REFERENCES [Users] ([UserId]) ON DELETE NO ACTION
);
GO


CREATE INDEX [IX_BookReservations_BookId_Status_RequestedAt] ON [BookReservations] ([BookId], [Status], [RequestedAt]);
GO


CREATE INDEX [IX_BookReservations_ReaderId_Status_RequestedAt] ON [BookReservations] ([ReaderId], [Status], [RequestedAt]);
GO


CREATE UNIQUE INDEX [IX_Books_BookCode] ON [Books] ([BookCode]);
GO


CREATE INDEX [IX_Books_CategoryId] ON [Books] ([CategoryId]);
GO


CREATE INDEX [IX_Books_Title] ON [Books] ([Title]);
GO


CREATE UNIQUE INDEX [IX_Categories_Name] ON [Categories] ([Name]);
GO


CREATE INDEX [IX_LoanDetails_BookId] ON [LoanDetails] ([BookId]);
GO


CREATE UNIQUE INDEX [IX_LoanDetails_LoanId_BookId] ON [LoanDetails] ([LoanId], [BookId]);
GO


CREATE INDEX [IX_LoanDetails_ReceivedByUserId] ON [LoanDetails] ([ReceivedByUserId]);
GO


CREATE UNIQUE INDEX [IX_LoanRenewals_LoanId_RenewCount] ON [LoanRenewals] ([LoanId], [RenewCount]);
GO


CREATE INDEX [IX_LoanRenewals_RenewedAt] ON [LoanRenewals] ([RenewedAt]);
GO


CREATE INDEX [IX_LoanRenewals_RenewedByUserId] ON [LoanRenewals] ([RenewedByUserId]);
GO


CREATE INDEX [IX_Loans_CreatedByUserId] ON [Loans] ([CreatedByUserId]);
GO


CREATE INDEX [IX_Loans_ReaderId_LoanDate] ON [Loans] ([ReaderId], [LoanDate]);
GO


CREATE UNIQUE INDEX [IX_Readers_ReaderCode] ON [Readers] ([ReaderCode]);
GO


CREATE UNIQUE INDEX [IX_Readers_UserId] ON [Readers] ([UserId]) WHERE [UserId] IS NOT NULL;
GO


CREATE UNIQUE INDEX [IX_Users_Username] ON [Users] ([Username]);
GO


