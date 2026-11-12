SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROCEDURE dbo.usp_FinancialReport_Summary
    @ShipCode      VARCHAR(10),
    @AccountPeriod DATE
AS
BEGIN
    SET NOCOUNT ON;
    EXEC dbo.usp_FinancialReport_Get @ShipCode = @ShipCode, @AccountPeriod = @AccountPeriod, @ReportType = 'Summary';
END;
GO
