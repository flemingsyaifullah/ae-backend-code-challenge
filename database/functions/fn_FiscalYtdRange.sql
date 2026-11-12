SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
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
