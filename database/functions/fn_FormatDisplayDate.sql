SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
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
