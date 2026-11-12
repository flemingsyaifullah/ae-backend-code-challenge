SET NOCOUNT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER FUNCTION dbo.fn_CrewStatus
(
    @SignOnDate        DATE,
    @SignOffDate       DATE,
    @EndOfContractDate DATE,
    @AsOfDate          DATE
)
RETURNS TABLE
AS
RETURN
    SELECT CrewStatus = CAST(
        CASE
            WHEN @SignOffDate IS NOT NULL                              THEN 'Signed Off'
            WHEN @SignOnDate > @AsOfDate                               THEN 'Planned'
            WHEN @AsOfDate > DATEADD(DAY, 30, @EndOfContractDate)      THEN 'Relief Due'
            ELSE                                                            'Onboard'
        END AS VARCHAR(10));
GO

CREATE OR ALTER FUNCTION dbo.fn_AgeInYears
(
    @BirthDate DATE,
    @AsOfDate  DATE
)
RETURNS TABLE
AS
RETURN
    SELECT Age = DATEDIFF(YEAR, @BirthDate, @AsOfDate)
               - CASE WHEN DATEADD(YEAR, DATEDIFF(YEAR, @BirthDate, @AsOfDate), @BirthDate) > @AsOfDate
                      THEN 1 ELSE 0 END;
GO

CREATE OR ALTER FUNCTION dbo.fn_FormatDisplayDate
(
    @Value DATE
)
RETURNS TABLE
AS
RETURN
    SELECT DisplayDate = CAST(
        RIGHT('0' + CAST(DAY(@Value) AS VARCHAR(2)), 2) + ' '
        + SUBSTRING('JanFebMarAprMayJunJulAugSepOctNovDec', (MONTH(@Value) - 1) * 3 + 1, 3) + ' '
        + CAST(YEAR(@Value) AS CHAR(4)) AS VARCHAR(11));
GO

CREATE OR ALTER FUNCTION dbo.fn_FiscalYtdRange
(
    @FiscalYearCode CHAR(4),
    @AccountPeriod  DATE
)
RETURNS TABLE
AS
RETURN
    SELECT
        YtdStartPeriod = DATEFROMPARTS(
                             YEAR(@AccountPeriod) - CASE WHEN MONTH(@AccountPeriod) < f.StartMonth THEN 1 ELSE 0 END,
                             f.StartMonth,
                             1),
        YtdEndPeriod   = DATEFROMPARTS(YEAR(@AccountPeriod), MONTH(@AccountPeriod), 1)
    FROM dbo.FiscalYearType AS f
    WHERE f.FiscalYearCode = @FiscalYearCode;
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
