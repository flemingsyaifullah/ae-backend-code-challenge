SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROCEDURE dbo.usp_FinancialReport_Get
    @ShipCode      VARCHAR(10),
    @AccountPeriod DATE,
    @ReportType    VARCHAR(10)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Message NVARCHAR(2048);
    DECLARE @ShipId INT, @ShipName NVARCHAR(100), @ShipStatus VARCHAR(10), @FiscalYearCode CHAR(4);
    DECLARE @YtdStart DATE, @YtdEnd DATE;

    IF @ReportType NOT IN ('Detail', 'Summary') THROW 50400, N'ReportType must be ''Detail'' or ''Summary''.', 1;
    IF @AccountPeriod IS NULL THROW 50400, N'AccountPeriod is required.', 1;
    IF DAY(@AccountPeriod) <> 1 THROW 50400, N'AccountPeriod must be the first day of a month (e.g. 2025-07-01).', 1;

    SELECT @ShipId = ShipId, @ShipName = ShipName, @ShipStatus = Status, @FiscalYearCode = FiscalYearCode
    FROM dbo.Ship
    WHERE ShipCode = @ShipCode;

    IF @ShipId IS NULL
    BEGIN
        SET @Message = CONCAT(N'Ship ''', @ShipCode, N''' was not found.');
        THROW 50404, @Message, 1;
    END;

    IF @ShipStatus <> 'Active'
    BEGIN
        SET @Message = CONCAT(N'Ship ''', @ShipCode, N''' is inactive. Financial reports are only available for active ships.');
        THROW 50422, @Message, 1;
    END;

    SELECT @YtdStart = YtdStartPeriod, @YtdEnd = YtdEndPeriod
    FROM dbo.fn_FiscalYtdRange(@FiscalYearCode, @AccountPeriod);

    SELECT ShipCode       = @ShipCode,
           ShipName       = @ShipName,
           FiscalYearCode = @FiscalYearCode,
           AccountPeriod  = @AccountPeriod,
           YtdStartPeriod = @YtdStart,
           YtdEndPeriod   = @YtdEnd;

    SELECT AccountNumber, AccountName, AccountType, AccountLevel, ParentAccountNumber,
           MonthActual, MonthBudget, MonthVariance,
           YtdActual, YtdBudget, YtdVariance
    FROM dbo.fn_FinancialReport(@ShipId, @AccountPeriod, @YtdStart)
    WHERE @ReportType = 'Detail' OR AccountType = 'P'
    ORDER BY SortPath;
END;
GO
