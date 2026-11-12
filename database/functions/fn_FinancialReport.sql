SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER FUNCTION dbo.fn_FinancialReport
(
    @ShipId          INT,
    @AccountPeriod   DATE,
    @YtdStartPeriod  DATE
)
RETURNS TABLE
AS
RETURN
WITH Actuals AS
(
    SELECT t.AccountNumber,
           MonthActual = SUM(CASE WHEN t.AccountPeriod = @AccountPeriod THEN t.ActualValue END),
           YtdActual   = SUM(t.ActualValue)
    FROM dbo.AccountTransaction AS t
    WHERE t.ShipId = @ShipId
      AND t.AccountPeriod >= @YtdStartPeriod
      AND t.AccountPeriod <= @AccountPeriod
    GROUP BY t.AccountNumber
),
Budgets AS
(
    SELECT b.AccountNumber,
           MonthBudget = SUM(CASE WHEN b.AccountPeriod = @AccountPeriod THEN b.BudgetValue END),
           YtdBudget   = SUM(b.BudgetValue)
    FROM dbo.BudgetEntry AS b
    WHERE b.ShipId = @ShipId
      AND b.AccountPeriod >= @YtdStartPeriod
      AND b.AccountPeriod <= @AccountPeriod
    GROUP BY b.AccountNumber
),
ChildValues AS
(
    SELECT AccountNumber = COALESCE(a.AccountNumber, b.AccountNumber),
           a.MonthActual, b.MonthBudget, a.YtdActual, b.YtdBudget
    FROM Actuals AS a
    FULL OUTER JOIN Budgets AS b ON b.AccountNumber = a.AccountNumber
),
RolledUp AS
(
    SELECT h.AncestorAccountNumber AS AccountNumber,
           MonthActual = SUM(cv.MonthActual),
           MonthBudget = SUM(cv.MonthBudget),
           YtdActual   = SUM(cv.YtdActual),
           YtdBudget   = SUM(cv.YtdBudget)
    FROM ChildValues AS cv
    INNER JOIN dbo.vw_AccountHierarchy AS h ON h.DescendantAccountNumber = cv.AccountNumber
    GROUP BY h.AncestorAccountNumber
)
SELECT
    acc.AccountNumber,
    acc.AccountName,
    acc.AccountType,
    tr.AccountLevel,
    acc.ParentAccountNumber,
    tr.SortPath,
    r.MonthActual,
    r.MonthBudget,
    MonthVariance = CASE WHEN r.MonthActual IS NULL AND r.MonthBudget IS NULL THEN NULL
                         ELSE ISNULL(r.MonthActual, 0) - ISNULL(r.MonthBudget, 0) END,
    r.YtdActual,
    r.YtdBudget,
    YtdVariance   = CASE WHEN r.YtdActual IS NULL AND r.YtdBudget IS NULL THEN NULL
                         ELSE ISNULL(r.YtdActual, 0) - ISNULL(r.YtdBudget, 0) END
FROM RolledUp AS r
INNER JOIN dbo.Account AS acc ON acc.AccountNumber = r.AccountNumber
INNER JOIN dbo.vw_AccountTree AS tr ON tr.AccountNumber = r.AccountNumber
WHERE ISNULL(r.MonthActual, 0) <> 0
   OR ISNULL(r.MonthBudget, 0) <> 0
   OR ISNULL(r.YtdActual, 0)   <> 0
   OR ISNULL(r.YtdBudget, 0)   <> 0;
GO
