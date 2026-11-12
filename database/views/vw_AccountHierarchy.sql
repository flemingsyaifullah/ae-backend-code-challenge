SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER VIEW dbo.vw_AccountHierarchy
AS
WITH Closure AS
(
    SELECT AncestorAccountNumber   = a.AccountNumber,
           DescendantAccountNumber = a.AccountNumber,
           Depth                   = 0
    FROM dbo.Account AS a

    UNION ALL

    SELECT cl.AncestorAccountNumber,
           c.AccountNumber,
           cl.Depth + 1
    FROM Closure AS cl
    INNER JOIN dbo.Account AS c ON c.ParentAccountNumber = cl.DescendantAccountNumber
)
SELECT AncestorAccountNumber, DescendantAccountNumber, Depth
FROM Closure;
GO
