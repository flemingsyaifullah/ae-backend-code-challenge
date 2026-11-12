SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER FUNCTION dbo.fn_ContainsPattern
(
    @SearchTerm NVARCHAR(100)
)
RETURNS TABLE
AS
RETURN
    SELECT Pattern = CASE
                         WHEN NULLIF(LTRIM(RTRIM(@SearchTerm)), N'') IS NULL THEN NULL
                         ELSE N'%' + REPLACE(REPLACE(REPLACE(LTRIM(RTRIM(@SearchTerm)),
                                  N'[', N'[[]'), N'%', N'[%]'), N'_', N'[_]') + N'%'
                     END;
GO
