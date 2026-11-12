SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
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
