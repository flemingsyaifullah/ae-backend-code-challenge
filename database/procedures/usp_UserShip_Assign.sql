SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
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
