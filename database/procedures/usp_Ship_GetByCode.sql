SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
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
