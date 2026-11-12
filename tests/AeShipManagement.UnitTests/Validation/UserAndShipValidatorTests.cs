using AeShipManagement.Application.Ships;
using AeShipManagement.Application.Users;

namespace AeShipManagement.UnitTests.Validation;

public sealed class UserAndShipValidatorTests
{
    private readonly CreateUserRequestValidator _userValidator = new();
    private readonly CreateShipRequestValidator _shipValidator = new();

    [Fact]
    public void Valid_user_request_passes()
    {
        var result = _userValidator.Validate(new CreateUserRequest("Jane Doe", "jane.doe@example.com", "Viewer"));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void User_request_reports_every_missing_field()
    {
        var result = _userValidator.Validate(new CreateUserRequest(null, " ", ""));

        Assert.Equal(["FullName", "Email", "RoleName"], result.Errors.Select(e => e.PropertyName).Distinct());
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("missing-at.example.com")]
    public void User_request_rejects_invalid_email(string email)
    {
        var result = _userValidator.Validate(new CreateUserRequest("Jane", email, "Viewer"));

        Assert.Contains(result.Errors, e => e.PropertyName == "Email");
    }

    [Fact]
    public void User_request_rejects_name_longer_than_column()
    {
        var result = _userValidator.Validate(new CreateUserRequest(new string('a', 101), "a@example.com", "Viewer"));

        Assert.Contains(result.Errors, e => e.PropertyName == "FullName");
    }

    [Theory]
    [InlineData("SHIP01", "0112", null)]
    [InlineData("ship-02_a", "0403", "inactive")]
    [InlineData("S1", "1009", "ACTIVE")]
    public void Valid_ship_requests_pass(string code, string fiscalYear, string? status)
    {
        var result = _shipValidator.Validate(new CreateShipRequest(code, "Ocean Pioneer", fiscalYear, status));

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("SHIP0123456", "ShipCode")]
    [InlineData("SHIP 01", "ShipCode")]
    [InlineData("SHIP'01", "ShipCode")]
    [InlineData("", "ShipCode")]
    public void Ship_code_rules_are_enforced(string code, string property)
    {
        var result = _shipValidator.Validate(new CreateShipRequest(code, "Name", "0112", null));

        Assert.Contains(result.Errors, e => e.PropertyName == property);
    }

    [Theory]
    [InlineData("112")]
    [InlineData("1301")]
    [InlineData("0013")]
    [InlineData("ABCD")]
    public void Fiscal_year_code_must_be_two_valid_months(string fiscalYear)
    {
        var result = _shipValidator.Validate(new CreateShipRequest("SHIP01", "Name", fiscalYear, null));

        Assert.Contains(result.Errors, e => e.PropertyName == "FiscalYearCode");
    }

    [Fact]
    public void Ship_status_must_be_active_or_inactive()
    {
        var result = _shipValidator.Validate(new CreateShipRequest("SHIP01", "Name", "0112", "Docked"));

        Assert.Contains(result.Errors, e => e.PropertyName == "Status");
    }
}
