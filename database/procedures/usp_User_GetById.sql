SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
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
