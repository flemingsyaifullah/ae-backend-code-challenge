using AeShipManagement.Application.Common;

namespace AeShipManagement.Infrastructure.Data;

public static class SqlErrorNumbers
{
    public const int InvalidInput = 50400;
    public const int NotFound = 50404;
    public const int Conflict = 50409;
    public const int BusinessRule = 50422;
    public const int UniqueIndexViolation = 2601;
    public const int UniqueConstraintViolation = 2627;
}

public static class SqlErrorTranslator
{
    public static AppException? Translate(int errorNumber, string message) => errorNumber switch
    {
        SqlErrorNumbers.InvalidInput => new InvalidRequestException(message),
        SqlErrorNumbers.NotFound => new ResourceNotFoundException(message),
        SqlErrorNumbers.Conflict => new ConflictException(message),
        SqlErrorNumbers.BusinessRule => new BusinessRuleException(message),
        SqlErrorNumbers.UniqueIndexViolation or SqlErrorNumbers.UniqueConstraintViolation =>
            new ConflictException("A record with the same unique key already exists."),
        _ => null
    };
}
