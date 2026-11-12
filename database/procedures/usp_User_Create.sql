SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
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
