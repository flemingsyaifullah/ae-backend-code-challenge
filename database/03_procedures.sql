SET NOCOUNT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROCEDURE dbo.usp_UserRole_List
AS
BEGIN
    SET NOCOUNT ON;

    SELECT RoleId, RoleName
    FROM dbo.UserRole
    ORDER BY RoleName;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_User_GetById
    @UserId INT
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM dbo.AppUser WHERE UserId = @UserId)
    BEGIN
        DECLARE @Message NVARCHAR(2048) = CONCAT(N'User ', @UserId, N' was not found.');
        THROW 50404, @Message, 1;
    END;

    SELECT u.UserId, u.FullName, u.Email, r.RoleName, u.CreatedAt
    FROM dbo.AppUser AS u
    INNER JOIN dbo.UserRole AS r ON r.RoleId = u.RoleId
    WHERE u.UserId = @UserId;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_User_Create
    @FullName NVARCHAR(100),
    @Email    VARCHAR(254),
    @RoleName VARCHAR(30)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @Message NVARCHAR(2048);

    SET @FullName = NULLIF(LTRIM(RTRIM(@FullName)), N'');
    SET @Email    = LOWER(NULLIF(LTRIM(RTRIM(@Email)), ''));
    SET @RoleName = NULLIF(LTRIM(RTRIM(@RoleName)), '');

    IF @FullName IS NULL THROW 50400, N'FullName is required.', 1;
    IF @Email IS NULL THROW 50400, N'Email is required.', 1;
    IF @Email NOT LIKE '_%@_%._%' OR @Email LIKE '% %' THROW 50400, N'Email is not a valid e-mail address.', 1;
    IF @RoleName IS NULL THROW 50400, N'RoleName is required.', 1;

    DECLARE @RoleId TINYINT = (SELECT RoleId FROM dbo.UserRole WHERE RoleName = @RoleName);
    IF @RoleId IS NULL
    BEGIN
        SET @Message = CONCAT(N'Role ''', @RoleName, N''' does not exist. Valid roles: ',
                              (SELECT STRING_AGG(RoleName, ', ') WITHIN GROUP (ORDER BY RoleName) FROM dbo.UserRole), N'.');
        THROW 50400, @Message, 1;
    END;

    IF EXISTS (SELECT 1 FROM dbo.AppUser WHERE Email = @Email)
    BEGIN
        SET @Message = CONCAT(N'A user with email ''', @Email, N''' already exists.');
        THROW 50409, @Message, 1;
    END;

    DECLARE @UserId INT;

    BEGIN TRY
        INSERT INTO dbo.AppUser (FullName, Email, RoleId)
        VALUES (@FullName, @Email, @RoleId);

        SET @UserId = CAST(SCOPE_IDENTITY() AS INT);
    END TRY
    BEGIN CATCH
        IF ERROR_NUMBER() IN (2601, 2627)
        BEGIN
            SET @Message = CONCAT(N'A user with email ''', @Email, N''' already exists.');
            THROW 50409, @Message, 1;
        END;
        THROW;
    END CATCH;

    EXEC dbo.usp_User_GetById @UserId = @UserId;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_User_List
    @PageNumber INT = 1,
    @PageSize   INT = 20,
    @TotalCount INT = NULL OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    IF @PageNumber IS NULL OR @PageNumber < 1 THROW 50400, N'PageNumber must be 1 or greater.', 1;
    IF @PageSize IS NULL OR @PageSize NOT BETWEEN 1 AND 100 THROW 50400, N'PageSize must be between 1 and 100.', 1;

    SELECT @TotalCount = COUNT(*) FROM dbo.AppUser;

    SELECT u.UserId, u.FullName, u.Email, r.RoleName, u.CreatedAt
    FROM dbo.AppUser AS u
    INNER JOIN dbo.UserRole AS r ON r.RoleId = u.RoleId
    ORDER BY u.UserId
    OFFSET (@PageNumber - 1) * @PageSize ROWS
    FETCH NEXT @PageSize ROWS ONLY;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_Ship_GetByCode
    @ShipCode VARCHAR(10)
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM dbo.Ship WHERE ShipCode = @ShipCode)
    BEGIN
        DECLARE @Message NVARCHAR(2048) = CONCAT(N'Ship ''', @ShipCode, N''' was not found.');
        THROW 50404, @Message, 1;
    END;

    SELECT s.ShipId, s.ShipCode, s.ShipName, s.FiscalYearCode, f.Description AS FiscalYearDescription,
           s.Status, s.CreatedAt
    FROM dbo.Ship AS s
    INNER JOIN dbo.FiscalYearType AS f ON f.FiscalYearCode = s.FiscalYearCode
    WHERE s.ShipCode = @ShipCode;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_Ship_Create
    @ShipCode       VARCHAR(10),
    @ShipName       NVARCHAR(100),
    @FiscalYearCode CHAR(4),
    @Status         VARCHAR(10) = 'Active'
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @Message NVARCHAR(2048);

    SET @ShipCode       = UPPER(NULLIF(LTRIM(RTRIM(@ShipCode)), ''));
    SET @ShipName       = NULLIF(LTRIM(RTRIM(@ShipName)), N'');
    SET @FiscalYearCode = NULLIF(LTRIM(RTRIM(@FiscalYearCode)), '');
    SET @Status         = ISNULL(NULLIF(LTRIM(RTRIM(@Status)), ''), 'Active');

    IF @ShipCode IS NULL THROW 50400, N'ShipCode is required.', 1;
    IF @ShipCode LIKE '%[^A-Z0-9_-]%' THROW 50400, N'ShipCode may only contain letters, digits, ''-'' and ''_''.', 1;
    IF @ShipName IS NULL THROW 50400, N'ShipName is required.', 1;
    IF @FiscalYearCode IS NULL THROW 50400, N'FiscalYearCode is required.', 1;
    IF @Status NOT IN ('Active', 'Inactive') THROW 50400, N'Status must be ''Active'' or ''Inactive''.', 1;

    IF NOT EXISTS (SELECT 1 FROM dbo.FiscalYearType WHERE FiscalYearCode = @FiscalYearCode)
    BEGIN
        SET @Message = CONCAT(N'Fiscal year code ''', @FiscalYearCode, N''' does not exist. Valid codes: ',
                              (SELECT STRING_AGG(FiscalYearCode, ', ') WITHIN GROUP (ORDER BY FiscalYearCode) FROM dbo.FiscalYearType), N'.');
        THROW 50400, @Message, 1;
    END;

    IF EXISTS (SELECT 1 FROM dbo.Ship WHERE ShipCode = @ShipCode)
    BEGIN
        SET @Message = CONCAT(N'A ship with code ''', @ShipCode, N''' already exists.');
        THROW 50409, @Message, 1;
    END;

    BEGIN TRY
        INSERT INTO dbo.Ship (ShipCode, ShipName, FiscalYearCode, Status)
        VALUES (@ShipCode, @ShipName, @FiscalYearCode, @Status);
    END TRY
    BEGIN CATCH
        IF ERROR_NUMBER() IN (2601, 2627)
        BEGIN
            SET @Message = CONCAT(N'A ship with code ''', @ShipCode, N''' already exists.');
            THROW 50409, @Message, 1;
        END;
        THROW;
    END CATCH;

    EXEC dbo.usp_Ship_GetByCode @ShipCode = @ShipCode;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_Ship_List
    @Status     VARCHAR(10) = NULL,
    @PageNumber INT = 1,
    @PageSize   INT = 20,
    @TotalCount INT = NULL OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    SET @Status = NULLIF(LTRIM(RTRIM(@Status)), '');

    IF @Status IS NOT NULL AND @Status NOT IN ('Active', 'Inactive')
        THROW 50400, N'Status must be ''Active'' or ''Inactive''.', 1;
    IF @PageNumber IS NULL OR @PageNumber < 1 THROW 50400, N'PageNumber must be 1 or greater.', 1;
    IF @PageSize IS NULL OR @PageSize NOT BETWEEN 1 AND 100 THROW 50400, N'PageSize must be between 1 and 100.', 1;

    SELECT @TotalCount = COUNT(*)
    FROM dbo.Ship
    WHERE @Status IS NULL OR Status = @Status;

    SELECT s.ShipId, s.ShipCode, s.ShipName, s.FiscalYearCode, f.Description AS FiscalYearDescription,
           s.Status, s.CreatedAt
    FROM dbo.Ship AS s
    INNER JOIN dbo.FiscalYearType AS f ON f.FiscalYearCode = s.FiscalYearCode
    WHERE @Status IS NULL OR s.Status = @Status
    ORDER BY s.ShipCode
    OFFSET (@PageNumber - 1) * @PageSize ROWS
    FETCH NEXT @PageSize ROWS ONLY;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_UserShip_Assign
    @UserId   INT,
    @ShipCode VARCHAR(10),
    @Created  BIT = NULL OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @Message NVARCHAR(2048);
    DECLARE @ShipId INT = (SELECT ShipId FROM dbo.Ship WHERE ShipCode = @ShipCode);

    IF NOT EXISTS (SELECT 1 FROM dbo.AppUser WHERE UserId = @UserId)
    BEGIN
        SET @Message = CONCAT(N'User ', @UserId, N' was not found.');
        THROW 50404, @Message, 1;
    END;

    IF @ShipId IS NULL
    BEGIN
        SET @Message = CONCAT(N'Ship ''', @ShipCode, N''' was not found.');
        THROW 50404, @Message, 1;
    END;

    SET @Created = 0;

    BEGIN TRY
        INSERT INTO dbo.UserShip (UserId, ShipId)
        SELECT @UserId, @ShipId
        WHERE NOT EXISTS (SELECT 1 FROM dbo.UserShip WHERE UserId = @UserId AND ShipId = @ShipId);

        SET @Created = CASE WHEN @@ROWCOUNT = 1 THEN 1 ELSE 0 END;
    END TRY
    BEGIN CATCH
        IF ERROR_NUMBER() NOT IN (2601, 2627) THROW;
    END CATCH;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_UserShip_Unassign
    @UserId   INT,
    @ShipCode VARCHAR(10)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Message NVARCHAR(2048);
    DECLARE @ShipId INT = (SELECT ShipId FROM dbo.Ship WHERE ShipCode = @ShipCode);

    IF NOT EXISTS (SELECT 1 FROM dbo.AppUser WHERE UserId = @UserId)
    BEGIN
        SET @Message = CONCAT(N'User ', @UserId, N' was not found.');
        THROW 50404, @Message, 1;
    END;

    IF @ShipId IS NULL
    BEGIN
        SET @Message = CONCAT(N'Ship ''', @ShipCode, N''' was not found.');
        THROW 50404, @Message, 1;
    END;

    DELETE FROM dbo.UserShip WHERE UserId = @UserId AND ShipId = @ShipId;

    IF @@ROWCOUNT = 0
    BEGIN
        SET @Message = CONCAT(N'Ship ''', @ShipCode, N''' is not assigned to user ', @UserId, N'.');
        THROW 50404, @Message, 1;
    END;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_UserShip_ListShipsByUser
    @UserId INT
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM dbo.AppUser WHERE UserId = @UserId)
    BEGIN
        DECLARE @Message NVARCHAR(2048) = CONCAT(N'User ', @UserId, N' was not found.');
        THROW 50404, @Message, 1;
    END;

    SELECT s.ShipId, s.ShipCode, s.ShipName, s.FiscalYearCode, f.Description AS FiscalYearDescription,
           s.Status, us.AssignedAt
    FROM dbo.UserShip AS us
    INNER JOIN dbo.Ship AS s ON s.ShipId = us.ShipId
    INNER JOIN dbo.FiscalYearType AS f ON f.FiscalYearCode = s.FiscalYearCode
    WHERE us.UserId = @UserId
    ORDER BY s.ShipCode;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_Crew_ListByShip
    @ShipCode      VARCHAR(10),
    @PageNumber    INT = 1,
    @PageSize      INT = 20,
    @SortBy        VARCHAR(20) = NULL,
    @SortDirection VARCHAR(4) = 'ASC',
    @SearchTerm    NVARCHAR(100) = NULL,
    @AsOfDate      DATE = NULL,
    @TotalCount    INT = NULL OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Message NVARCHAR(2048);
    DECLARE @ShipId INT, @ShipStatus VARCHAR(10);

    SET @AsOfDate      = ISNULL(@AsOfDate, CAST(GETDATE() AS DATE));
    SET @SortDirection = UPPER(ISNULL(NULLIF(LTRIM(RTRIM(@SortDirection)), ''), 'ASC'));
    SET @SortBy        = NULLIF(LTRIM(RTRIM(@SortBy)), '');

    IF @PageNumber IS NULL OR @PageNumber < 1 THROW 50400, N'PageNumber must be 1 or greater.', 1;
    IF @PageSize IS NULL OR @PageSize NOT BETWEEN 1 AND 100 THROW 50400, N'PageSize must be between 1 and 100.', 1;
    IF @SortDirection NOT IN ('ASC', 'DESC') THROW 50400, N'SortDirection must be ''ASC'' or ''DESC''.', 1;

    IF @SortBy IS NOT NULL
    BEGIN
        DECLARE @CanonicalSortBy VARCHAR(20) =
        (
            SELECT c.ColumnName
            FROM (VALUES ('RankName'), ('CrewMemberId'), ('FirstName'), ('LastName'),
                         ('Age'), ('Nationality'), ('SignOnDate'), ('Status')) AS c (ColumnName)
            WHERE c.ColumnName = @SortBy
        );

        IF @CanonicalSortBy IS NULL
            THROW 50400, N'SortBy must be one of: RankName, CrewMemberId, FirstName, LastName, Age, Nationality, SignOnDate, Status.', 1;

        SET @SortBy = @CanonicalSortBy;
    END;

    SELECT @ShipId = ShipId, @ShipStatus = Status FROM dbo.Ship WHERE ShipCode = @ShipCode;

    IF @ShipId IS NULL
    BEGIN
        SET @Message = CONCAT(N'Ship ''', @ShipCode, N''' was not found.');
        THROW 50404, @Message, 1;
    END;

    IF @ShipStatus <> 'Active'
    BEGIN
        SET @Message = CONCAT(N'Ship ''', @ShipCode, N''' is inactive. Crew lists are only available for active ships.');
        THROW 50422, @Message, 1;
    END;

    DECLARE @Pattern NVARCHAR(400) = (SELECT Pattern FROM dbo.fn_ContainsPattern(@SearchTerm));

    CREATE TABLE #Crew
    (
        RankName          NVARCHAR(50) NOT NULL,
        SeniorityOrder    SMALLINT     NOT NULL,
        CrewMemberId      VARCHAR(10)  NOT NULL,
        FirstName         NVARCHAR(50) NOT NULL,
        LastName          NVARCHAR(50) NOT NULL,
        Age               INT          NOT NULL,
        Nationality       NVARCHAR(50) NOT NULL,
        SignOnDate        DATE         NOT NULL,
        SignOnDateDisplay VARCHAR(11)  NOT NULL,
        EndOfContractDate DATE         NOT NULL,
        CrewStatus        VARCHAR(10)  NOT NULL
    );

    INSERT INTO #Crew
    SELECT r.RankName, r.SeniorityOrder, cm.CrewMemberId, cm.FirstName, cm.LastName, age.Age,
           n.NationalityName, csh.SignOnDate, d.DisplayDate, csh.EndOfContractDate, st.CrewStatus
    FROM dbo.CrewServiceHistory AS csh
    INNER JOIN dbo.CrewMember  AS cm ON cm.CrewMemberId = csh.CrewMemberId
    INNER JOIN dbo.[Rank]      AS r  ON r.RankId = csh.RankId
    INNER JOIN dbo.Nationality AS n  ON n.NationalityId = cm.NationalityId
    CROSS APPLY dbo.fn_CrewStatus(csh.SignOnDate, csh.SignOffDate, csh.EndOfContractDate, @AsOfDate) AS st
    CROSS APPLY dbo.fn_AgeInYears(cm.BirthDate, @AsOfDate) AS age
    CROSS APPLY dbo.fn_FormatDisplayDate(csh.SignOnDate) AS d
    WHERE csh.ShipId = @ShipId
      AND csh.SignOffDate IS NULL
      AND st.CrewStatus IN ('Onboard', 'Relief Due')
      AND (
             @Pattern IS NULL
          OR r.RankName                          LIKE @Pattern
          OR cm.CrewMemberId                     LIKE @Pattern
          OR cm.FirstName                        LIKE @Pattern
          OR cm.LastName                         LIKE @Pattern
          OR CAST(age.Age AS VARCHAR(3))         LIKE @Pattern
          OR n.NationalityName                   LIKE @Pattern
          OR d.DisplayDate                       LIKE @Pattern
          OR CONVERT(CHAR(10), csh.SignOnDate, 23) LIKE @Pattern
          );

    SELECT @TotalCount = COUNT(*) FROM #Crew;

    SELECT RankName, CrewMemberId, FirstName, LastName, Age, Nationality,
           SignOnDate, SignOnDateDisplay, EndOfContractDate, CrewStatus AS Status
    FROM #Crew
    ORDER BY
        CASE WHEN @SortDirection = 'ASC' THEN
            CASE @SortBy
                WHEN 'RankName'     THEN RankName
                WHEN 'CrewMemberId' THEN CrewMemberId
                WHEN 'FirstName'    THEN FirstName
                WHEN 'LastName'     THEN LastName
                WHEN 'Nationality'  THEN Nationality
                WHEN 'Status'       THEN CrewStatus
            END
        END ASC,
        CASE WHEN @SortDirection = 'DESC' THEN
            CASE @SortBy
                WHEN 'RankName'     THEN RankName
                WHEN 'CrewMemberId' THEN CrewMemberId
                WHEN 'FirstName'    THEN FirstName
                WHEN 'LastName'     THEN LastName
                WHEN 'Nationality'  THEN Nationality
                WHEN 'Status'       THEN CrewStatus
            END
        END DESC,
        CASE WHEN @SortDirection = 'ASC'  AND @SortBy = 'Age'        THEN Age END ASC,
        CASE WHEN @SortDirection = 'DESC' AND @SortBy = 'Age'        THEN Age END DESC,
        CASE WHEN @SortDirection = 'ASC'  AND @SortBy = 'SignOnDate' THEN SignOnDate END ASC,
        CASE WHEN @SortDirection = 'DESC' AND @SortBy = 'SignOnDate' THEN SignOnDate END DESC,
        CASE WHEN @SortDirection = 'ASC'  AND @SortBy IS NULL        THEN SeniorityOrder END ASC,
        CASE WHEN @SortDirection = 'DESC' AND @SortBy IS NULL        THEN SeniorityOrder END DESC,
        SeniorityOrder ASC,
        CrewMemberId ASC
    OFFSET (@PageNumber - 1) * @PageSize ROWS
    FETCH NEXT @PageSize ROWS ONLY;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_FinancialReport_Get
    @ShipCode      VARCHAR(10),
    @AccountPeriod DATE,
    @ReportType    VARCHAR(10)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Message NVARCHAR(2048);
    DECLARE @ShipId INT, @ShipName NVARCHAR(100), @ShipStatus VARCHAR(10), @FiscalYearCode CHAR(4);
    DECLARE @YtdStart DATE, @YtdEnd DATE;

    IF @ReportType NOT IN ('Detail', 'Summary') THROW 50400, N'ReportType must be ''Detail'' or ''Summary''.', 1;
    IF @AccountPeriod IS NULL THROW 50400, N'AccountPeriod is required.', 1;
    IF DAY(@AccountPeriod) <> 1 THROW 50400, N'AccountPeriod must be the first day of a month (e.g. 2025-07-01).', 1;

    SELECT @ShipId = ShipId, @ShipName = ShipName, @ShipStatus = Status, @FiscalYearCode = FiscalYearCode
    FROM dbo.Ship
    WHERE ShipCode = @ShipCode;

    IF @ShipId IS NULL
    BEGIN
        SET @Message = CONCAT(N'Ship ''', @ShipCode, N''' was not found.');
        THROW 50404, @Message, 1;
    END;

    IF @ShipStatus <> 'Active'
    BEGIN
        SET @Message = CONCAT(N'Ship ''', @ShipCode, N''' is inactive. Financial reports are only available for active ships.');
        THROW 50422, @Message, 1;
    END;

    SELECT @YtdStart = YtdStartPeriod, @YtdEnd = YtdEndPeriod
    FROM dbo.fn_FiscalYtdRange(@FiscalYearCode, @AccountPeriod);

    SELECT ShipCode       = @ShipCode,
           ShipName       = @ShipName,
           FiscalYearCode = @FiscalYearCode,
           AccountPeriod  = @AccountPeriod,
           YtdStartPeriod = @YtdStart,
           YtdEndPeriod   = @YtdEnd;

    SELECT AccountNumber, AccountName, AccountType, AccountLevel, ParentAccountNumber,
           MonthActual, MonthBudget, MonthVariance,
           YtdActual, YtdBudget, YtdVariance
    FROM dbo.fn_FinancialReport(@ShipId, @AccountPeriod, @YtdStart)
    WHERE @ReportType = 'Detail' OR AccountType = 'P'
    ORDER BY SortPath;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_FinancialReport_Detail
    @ShipCode      VARCHAR(10),
    @AccountPeriod DATE
AS
BEGIN
    SET NOCOUNT ON;
    EXEC dbo.usp_FinancialReport_Get @ShipCode = @ShipCode, @AccountPeriod = @AccountPeriod, @ReportType = 'Detail';
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_FinancialReport_Summary
    @ShipCode      VARCHAR(10),
    @AccountPeriod DATE
AS
BEGIN
    SET NOCOUNT ON;
    EXEC dbo.usp_FinancialReport_Get @ShipCode = @ShipCode, @AccountPeriod = @AccountPeriod, @ReportType = 'Summary';
END;
GO
