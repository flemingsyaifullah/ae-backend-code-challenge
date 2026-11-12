SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
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
