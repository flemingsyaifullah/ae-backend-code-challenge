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
