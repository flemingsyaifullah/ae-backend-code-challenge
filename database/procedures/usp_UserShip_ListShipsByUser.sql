SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
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
