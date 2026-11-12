SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER VIEW dbo.vw_AccountTree
AS
WITH Tree AS
(
    SELECT a.AccountNumber,
           a.ParentAccountNumber,
           AccountLevel = 1,
           SortPath     = CAST(a.AccountNumber AS VARCHAR(400))
    FROM dbo.Account AS a
    WHERE a.ParentAccountNumber IS NULL

    UNION ALL

    SELECT c.AccountNumber,
           c.ParentAccountNumber,
           t.AccountLevel + 1,
           CAST(t.SortPath + '/' + c.AccountNumber AS VARCHAR(400))
    FROM dbo.Account AS c
    INNER JOIN Tree AS t ON t.AccountNumber = c.ParentAccountNumber
)
SELECT AccountNumber, ParentAccountNumber, AccountLevel, SortPath
FROM Tree;
GO
