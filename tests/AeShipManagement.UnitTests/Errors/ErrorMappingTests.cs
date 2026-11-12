using AeShipManagement.Api.Errors;
using AeShipManagement.Application.Common;
using AeShipManagement.Infrastructure.Data;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;

namespace AeShipManagement.UnitTests.Errors;

public sealed class ErrorMappingTests
{
    [Theory]
    [InlineData(50400, typeof(InvalidRequestException))]
    [InlineData(50404, typeof(ResourceNotFoundException))]
    [InlineData(50409, typeof(ConflictException))]
    [InlineData(50422, typeof(BusinessRuleException))]
    [InlineData(2601, typeof(ConflictException))]
    [InlineData(2627, typeof(ConflictException))]
    public void Stored_procedure_error_numbers_map_to_application_exceptions(int number, Type expected)
    {
        var translated = SqlErrorTranslator.Translate(number, "message from procedure");

        Assert.IsType(expected, translated);
    }

    [Fact]
    public void Procedure_message_is_kept_for_business_errors()
    {
        var translated = SqlErrorTranslator.Translate(50404, "Ship 'X' was not found.");

        Assert.Equal("Ship 'X' was not found.", translated!.Message);
    }

    [Theory]
    [InlineData(547)]
    [InlineData(1205)]
    [InlineData(-2)]
    public void Unknown_sql_errors_are_not_translated(int number)
    {
        Assert.Null(SqlErrorTranslator.Translate(number, "internal detail"));
    }

    [Theory]
    [InlineData(typeof(InvalidRequestException), 400)]
    [InlineData(typeof(ResourceNotFoundException), 404)]
    [InlineData(typeof(ConflictException), 409)]
    [InlineData(typeof(BusinessRuleException), 422)]
    public void Application_exceptions_map_to_http_status(Type exceptionType, int status)
    {
        var exception = (Exception)Activator.CreateInstance(exceptionType, "detail")!;

        var problem = ApiExceptionHandler.ToProblemDetails(exception);

        Assert.Equal(status, problem.Status);
        Assert.Equal("detail", problem.Detail);
    }

    [Fact]
    public void Validation_exception_maps_to_400_with_camel_case_field_errors()
    {
        var exception = new ValidationException([new ValidationFailure("PageSize", "Page size must be between 1 and 100.")]);

        var problem = Assert.IsType<ValidationProblemDetails>(ApiExceptionHandler.ToProblemDetails(exception));

        Assert.Equal(400, problem.Status);
        Assert.Equal(["Page size must be between 1 and 100."], problem.Errors["pageSize"]);
    }

    [Fact]
    public void Unexpected_exception_hides_internal_details()
    {
        var problem = ApiExceptionHandler.ToProblemDetails(new InvalidOperationException("connection string secret"));

        Assert.Equal(500, problem.Status);
        Assert.DoesNotContain("secret", problem.Detail);
    }
}
