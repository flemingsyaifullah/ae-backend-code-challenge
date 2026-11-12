SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROCEDURE dbo.usp_Ship_Create
    @ShipCode       VARCHAR(10),
    @ShipName       NVARCHAR(100),
    @FiscalYearCode CHAR(4),
    @Status         VARCHAR(10) = 'Active'
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @Message NVARCHAR(2048);

    SET @ShipCode       = UPPER(NULLIF(LTRIM(RTRIM(@ShipCode)), ''));
    SET @ShipName       = NULLIF(LTRIM(RTRIM(@ShipName)), N'');
    SET @FiscalYearCode = NULLIF(LTRIM(RTRIM(@FiscalYearCode)), '');
    SET @Status         = ISNULL(NULLIF(LTRIM(RTRIM(@Status)), ''), 'Active');

    IF @ShipCode IS NULL THROW 50400, N'ShipCode is required.', 1;
    IF @ShipCode LIKE '%[^A-Z0-9_-]%' THROW 50400, N'ShipCode may only contain letters, digits, ''-'' and ''_''.', 1;
    IF @ShipName IS NULL THROW 50400, N'ShipName is required.', 1;
    IF @FiscalYearCode IS NULL THROW 50400, N'FiscalYearCode is required.', 1;
    IF @Status NOT IN ('Active', 'Inactive') THROW 50400, N'Status must be ''Active'' or ''Inactive''.', 1;

    IF NOT EXISTS (SELECT 1 FROM dbo.FiscalYearType WHERE FiscalYearCode = @FiscalYearCode)
    BEGIN
        SET @Message = CONCAT(N'Fiscal year code ''', @FiscalYearCode, N''' does not exist. Valid codes: ',
                              (SELECT STRING_AGG(FiscalYearCode, ', ') WITHIN GROUP (ORDER BY FiscalYearCode) FROM dbo.FiscalYearType), N'.');
        THROW 50400, @Message, 1;
    END;

    IF EXISTS (SELECT 1 FROM dbo.Ship WHERE ShipCode = @ShipCode)
    BEGIN
        SET @Message = CONCAT(N'A ship with code ''', @ShipCode, N''' already exists.');
        THROW 50409, @Message, 1;
    END;

    BEGIN TRY
        INSERT INTO dbo.Ship (ShipCode, ShipName, FiscalYearCode, Status)
        VALUES (@ShipCode, @ShipName, @FiscalYearCode, @Status);
    END TRY
    BEGIN CATCH
        IF ERROR_NUMBER() IN (2601, 2627)
        BEGIN
            SET @Message = CONCAT(N'A ship with code ''', @ShipCode, N''' already exists.');
            THROW 50409, @Message, 1;
        END;
        THROW;
    END CATCH;

    EXEC dbo.usp_Ship_GetByCode @ShipCode = @ShipCode;
END;
GO
