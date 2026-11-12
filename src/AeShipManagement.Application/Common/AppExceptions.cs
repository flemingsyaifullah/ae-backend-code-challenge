namespace AeShipManagement.Application.Common;

public abstract class AppException(string message) : Exception(message);

public sealed class InvalidRequestException(string message) : AppException(message);

public sealed class ResourceNotFoundException(string message) : AppException(message);

public sealed class ConflictException(string message) : AppException(message);

public sealed class BusinessRuleException(string message) : AppException(message);
