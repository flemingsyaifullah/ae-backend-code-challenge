SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
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
