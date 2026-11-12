SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
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
