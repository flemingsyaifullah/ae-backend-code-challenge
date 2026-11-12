SET NOCOUNT ON;
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

CREATE TABLE dbo.FiscalYearType
(
    FiscalYearCode  CHAR(4)       NOT NULL,
    StartMonth      AS CONVERT(TINYINT, LEFT(FiscalYearCode, 2)) PERSISTED,
    EndMonth        AS CONVERT(TINYINT, RIGHT(FiscalYearCode, 2)) PERSISTED,
    Description     NVARCHAR(50)  NOT NULL,
    CONSTRAINT PK_FiscalYearType PRIMARY KEY (FiscalYearCode),
    CONSTRAINT CK_FiscalYearType_Code CHECK
    (
        FiscalYearCode LIKE '[0-1][0-9][0-1][0-9]'
        AND TRY_CONVERT(INT, LEFT(FiscalYearCode, 2)) BETWEEN 1 AND 12
        AND TRY_CONVERT(INT, RIGHT(FiscalYearCode, 2)) = (TRY_CONVERT(INT, LEFT(FiscalYearCode, 2)) + 10) % 12 + 1
    )
);
GO

CREATE TABLE dbo.Nationality
(
    NationalityId   SMALLINT      IDENTITY(1, 1) NOT NULL,
    NationalityName NVARCHAR(50)  NOT NULL,
    CONSTRAINT PK_Nationality PRIMARY KEY (NationalityId),
    CONSTRAINT UQ_Nationality_Name UNIQUE (NationalityName)
);
GO

CREATE TABLE dbo.[Rank]
(
    RankId          SMALLINT      IDENTITY(1, 1) NOT NULL,
    RankCode        VARCHAR(10)   NOT NULL,
    RankName        NVARCHAR(50)  NOT NULL,
    Department      VARCHAR(10)   NOT NULL,
    SeniorityOrder  SMALLINT      NOT NULL,
    CONSTRAINT PK_Rank PRIMARY KEY (RankId),
    CONSTRAINT UQ_Rank_Code UNIQUE (RankCode),
    CONSTRAINT UQ_Rank_Name UNIQUE (RankName),
    CONSTRAINT UQ_Rank_SeniorityOrder UNIQUE (SeniorityOrder),
    CONSTRAINT CK_Rank_Department CHECK (Department IN ('Deck', 'Engine', 'Catering')),
    CONSTRAINT CK_Rank_SeniorityOrder CHECK (SeniorityOrder > 0)
);
GO

CREATE TABLE dbo.UserRole
(
    RoleId          TINYINT       IDENTITY(1, 1) NOT NULL,
    RoleName        VARCHAR(30)   NOT NULL,
    CONSTRAINT PK_UserRole PRIMARY KEY (RoleId),
    CONSTRAINT UQ_UserRole_Name UNIQUE (RoleName)
);
GO

CREATE TABLE dbo.Ship
(
    ShipId          INT           IDENTITY(1, 1) NOT NULL,
    ShipCode        VARCHAR(10)   NOT NULL,
    ShipName        NVARCHAR(100) NOT NULL,
    FiscalYearCode  CHAR(4)       NOT NULL,
    Status          VARCHAR(10)   NOT NULL CONSTRAINT DF_Ship_Status DEFAULT ('Active'),
    CreatedAt       DATETIME2(0)  NOT NULL CONSTRAINT DF_Ship_CreatedAt DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT PK_Ship PRIMARY KEY (ShipId),
    CONSTRAINT UQ_Ship_Code UNIQUE (ShipCode),
    CONSTRAINT FK_Ship_FiscalYearType FOREIGN KEY (FiscalYearCode) REFERENCES dbo.FiscalYearType (FiscalYearCode),
    CONSTRAINT CK_Ship_Status CHECK (Status IN ('Active', 'Inactive')),
    CONSTRAINT CK_Ship_Code CHECK (LEN(ShipCode) > 0 AND ShipCode NOT LIKE '%[^A-Za-z0-9_-]%'),
    CONSTRAINT CK_Ship_Name CHECK (LEN(LTRIM(RTRIM(ShipName))) > 0)
);
GO

CREATE INDEX IX_Ship_Status ON dbo.Ship (Status) INCLUDE (ShipCode, ShipName, FiscalYearCode);
GO

CREATE TABLE dbo.CrewMember
(
    CrewMemberId    VARCHAR(10)   NOT NULL,
    FirstName       NVARCHAR(50)  NOT NULL,
    LastName        NVARCHAR(50)  NOT NULL,
    BirthDate       DATE          NOT NULL,
    NationalityId   SMALLINT      NOT NULL,
    CreatedAt       DATETIME2(0)  NOT NULL CONSTRAINT DF_CrewMember_CreatedAt DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT PK_CrewMember PRIMARY KEY (CrewMemberId),
    CONSTRAINT FK_CrewMember_Nationality FOREIGN KEY (NationalityId) REFERENCES dbo.Nationality (NationalityId),
    CONSTRAINT CK_CrewMember_Id CHECK (LEN(CrewMemberId) > 0 AND CrewMemberId NOT LIKE '%[^A-Za-z0-9_-]%'),
    CONSTRAINT CK_CrewMember_BirthDate CHECK (BirthDate >= '1900-01-01')
);
GO

CREATE INDEX IX_CrewMember_Nationality ON dbo.CrewMember (NationalityId);
GO

CREATE TABLE dbo.CrewServiceHistory
(
    CrewServiceId     INT           IDENTITY(1, 1) NOT NULL,
    CrewMemberId      VARCHAR(10)   NOT NULL,
    ShipId            INT           NOT NULL,
    RankId            SMALLINT      NOT NULL,
    SignOnDate        DATE          NOT NULL,
    SignOffDate       DATE          NULL,
    EndOfContractDate DATE          NOT NULL,
    CreatedAt         DATETIME2(0)  NOT NULL CONSTRAINT DF_CrewServiceHistory_CreatedAt DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT PK_CrewServiceHistory PRIMARY KEY (CrewServiceId),
    CONSTRAINT FK_CrewServiceHistory_CrewMember FOREIGN KEY (CrewMemberId) REFERENCES dbo.CrewMember (CrewMemberId),
    CONSTRAINT FK_CrewServiceHistory_Ship FOREIGN KEY (ShipId) REFERENCES dbo.Ship (ShipId),
    CONSTRAINT FK_CrewServiceHistory_Rank FOREIGN KEY (RankId) REFERENCES dbo.[Rank] (RankId),
    CONSTRAINT CK_CrewServiceHistory_EndOfContract CHECK (EndOfContractDate >= SignOnDate),
    CONSTRAINT CK_CrewServiceHistory_SignOff CHECK (SignOffDate IS NULL OR SignOffDate >= SignOnDate)
);
GO

CREATE INDEX IX_CrewServiceHistory_Ship_Open
    ON dbo.CrewServiceHistory (ShipId)
    INCLUDE (CrewMemberId, RankId, SignOnDate, EndOfContractDate)
    WHERE SignOffDate IS NULL;
GO

CREATE INDEX IX_CrewServiceHistory_CrewMember ON dbo.CrewServiceHistory (CrewMemberId, SignOnDate);
GO

CREATE TABLE dbo.AppUser
(
    UserId          INT           IDENTITY(1, 1) NOT NULL,
    FullName        NVARCHAR(100) NOT NULL,
    Email           VARCHAR(254)  NOT NULL,
    RoleId          TINYINT       NOT NULL,
    CreatedAt       DATETIME2(0)  NOT NULL CONSTRAINT DF_AppUser_CreatedAt DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT PK_AppUser PRIMARY KEY (UserId),
    CONSTRAINT UQ_AppUser_Email UNIQUE (Email),
    CONSTRAINT FK_AppUser_UserRole FOREIGN KEY (RoleId) REFERENCES dbo.UserRole (RoleId),
    CONSTRAINT CK_AppUser_FullName CHECK (LEN(LTRIM(RTRIM(FullName))) > 0),
    CONSTRAINT CK_AppUser_Email CHECK (Email LIKE '_%@_%._%' AND Email NOT LIKE '% %')
);
GO

CREATE TABLE dbo.UserShip
(
    UserId          INT           NOT NULL,
    ShipId          INT           NOT NULL,
    AssignedAt      DATETIME2(0)  NOT NULL CONSTRAINT DF_UserShip_AssignedAt DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT PK_UserShip PRIMARY KEY (UserId, ShipId),
    CONSTRAINT FK_UserShip_AppUser FOREIGN KEY (UserId) REFERENCES dbo.AppUser (UserId) ON DELETE CASCADE,
    CONSTRAINT FK_UserShip_Ship FOREIGN KEY (ShipId) REFERENCES dbo.Ship (ShipId) ON DELETE CASCADE
);
GO

CREATE INDEX IX_UserShip_Ship ON dbo.UserShip (ShipId);
GO

CREATE TABLE dbo.Account
(
    AccountNumber       VARCHAR(10)   NOT NULL,
    AccountName         NVARCHAR(150) NOT NULL,
    AccountType         CHAR(1)       NOT NULL,
    ParentAccountNumber VARCHAR(10)   NULL,
    ParentAccountType   AS CONVERT(CHAR(1), 'P') PERSISTED,
    CONSTRAINT PK_Account PRIMARY KEY (AccountNumber),
    CONSTRAINT UQ_Account_Number_Type UNIQUE (AccountNumber, AccountType),
    CONSTRAINT FK_Account_Parent FOREIGN KEY (ParentAccountNumber, ParentAccountType)
        REFERENCES dbo.Account (AccountNumber, AccountType),
    CONSTRAINT CK_Account_Type CHECK (AccountType IN ('P', 'C')),
    CONSTRAINT CK_Account_Number CHECK (LEN(AccountNumber) > 0 AND AccountNumber NOT LIKE '%[^0-9]%'),
    CONSTRAINT CK_Account_NotOwnParent CHECK (ParentAccountNumber IS NULL OR ParentAccountNumber <> AccountNumber),
    CONSTRAINT CK_Account_Name CHECK (LEN(LTRIM(RTRIM(AccountName))) > 0)
);
GO

CREATE INDEX IX_Account_Parent ON dbo.Account (ParentAccountNumber);
GO

CREATE TABLE dbo.BudgetEntry
(
    BudgetEntryId   BIGINT        IDENTITY(1, 1) NOT NULL,
    ShipId          INT           NOT NULL,
    AccountNumber   VARCHAR(10)   NOT NULL,
    AccountType     AS CONVERT(CHAR(1), 'C') PERSISTED,
    AccountPeriod   DATE          NOT NULL,
    BudgetValue     DECIMAL(18, 2) NOT NULL,
    CreatedAt       DATETIME2(0)  NOT NULL CONSTRAINT DF_BudgetEntry_CreatedAt DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT PK_BudgetEntry PRIMARY KEY (BudgetEntryId),
    CONSTRAINT FK_BudgetEntry_Ship FOREIGN KEY (ShipId) REFERENCES dbo.Ship (ShipId),
    CONSTRAINT FK_BudgetEntry_ChildAccount FOREIGN KEY (AccountNumber, AccountType)
        REFERENCES dbo.Account (AccountNumber, AccountType),
    CONSTRAINT CK_BudgetEntry_Period CHECK (DAY(AccountPeriod) = 1),
    CONSTRAINT CK_BudgetEntry_Value CHECK (BudgetValue >= 0)
);
GO

CREATE INDEX IX_BudgetEntry_Ship_Period
    ON dbo.BudgetEntry (ShipId, AccountPeriod)
    INCLUDE (AccountNumber, BudgetValue);
GO

CREATE TABLE dbo.AccountTransaction
(
    TransactionId   BIGINT        IDENTITY(1, 1) NOT NULL,
    ShipId          INT           NOT NULL,
    AccountNumber   VARCHAR(10)   NOT NULL,
    AccountType     AS CONVERT(CHAR(1), 'C') PERSISTED,
    AccountPeriod   DATE          NOT NULL,
    ActualValue     DECIMAL(18, 2) NOT NULL,
    Description     NVARCHAR(200) NULL,
    CreatedAt       DATETIME2(0)  NOT NULL CONSTRAINT DF_AccountTransaction_CreatedAt DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT PK_AccountTransaction PRIMARY KEY (TransactionId),
    CONSTRAINT FK_AccountTransaction_Ship FOREIGN KEY (ShipId) REFERENCES dbo.Ship (ShipId),
    CONSTRAINT FK_AccountTransaction_ChildAccount FOREIGN KEY (AccountNumber, AccountType)
        REFERENCES dbo.Account (AccountNumber, AccountType),
    CONSTRAINT CK_AccountTransaction_Period CHECK (DAY(AccountPeriod) = 1),
    CONSTRAINT CK_AccountTransaction_Value CHECK (ActualValue >= 0)
);
GO

CREATE INDEX IX_AccountTransaction_Ship_Period
    ON dbo.AccountTransaction (ShipId, AccountPeriod)
    INCLUDE (AccountNumber, ActualValue);
GO
