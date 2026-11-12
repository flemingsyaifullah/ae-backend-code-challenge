SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
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
