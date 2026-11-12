using AeShipManagement.Application.Common;
using AeShipManagement.Application.Crew;
using AeShipManagement.Application.FinancialReports;
using AeShipManagement.Application.Ships;
using AeShipManagement.Application.Users;
using FluentValidation;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace AeShipManagement.UnitTests.Services;

public sealed class ServiceTests
{
    private static readonly CancellationToken Token = CancellationToken.None;

    [Fact]
    public async Task Crew_service_normalizes_sort_and_search_before_calling_repository()
    {
        var repository = Substitute.For<ICrewRepository>();
        repository.ListByShipAsync(Arg.Any<CrewListQuery>(), Token)
            .Returns(new PagedResult<CrewMemberResponse>([], 1, 20, 0));
        var service = new CrewService(repository, new CrewListQueryValidator());

        await service.ListByShipAsync(new CrewListQuery("SHIP01", SortBy: "signondate", SortDirection: "desc", Search: "  05 Apr "), Token);

        await repository.Received(1).ListByShipAsync(
            Arg.Is<CrewListQuery>(q => q.SortBy == "SignOnDate" && q.SortDirection == "DESC" && q.Search == "05 Apr"),
            Token);
    }

    [Fact]
    public async Task Crew_service_defaults_to_ascending_and_no_search()
    {
        var repository = Substitute.For<ICrewRepository>();
        repository.ListByShipAsync(Arg.Any<CrewListQuery>(), Token)
            .Returns(new PagedResult<CrewMemberResponse>([], 1, 20, 0));
        var service = new CrewService(repository, new CrewListQueryValidator());

        await service.ListByShipAsync(new CrewListQuery("SHIP01", Search: "   "), Token);

        await repository.Received(1).ListByShipAsync(
            Arg.Is<CrewListQuery>(q => q.SortBy == null && q.SortDirection == "ASC" && q.Search == null), Token);
    }

    [Fact]
    public async Task Invalid_crew_query_never_reaches_the_database()
    {
        var repository = Substitute.For<ICrewRepository>();
        var service = new CrewService(repository, new CrewListQueryValidator());

        await Assert.ThrowsAsync<ValidationException>(() =>
            service.ListByShipAsync(new CrewListQuery("SHIP01", SortBy: "Salary"), Token));

        await repository.DidNotReceiveWithAnyArgs().ListByShipAsync(default!, default);
    }

    [Fact]
    public async Task Ship_service_defaults_status_to_active_and_normalizes_case()
    {
        var repository = Substitute.For<IShipRepository>();
        repository.CreateAsync(Arg.Any<CreateShipRequest>(), Token)
            .Returns(new ShipResponse("SHIP09", "Name", "0112", "January - December", "Active", DateTime.UtcNow));
        var service = CreateShipService(repository);

        await service.CreateAsync(new CreateShipRequest("SHIP09", "Name", "0112", null), Token);
        await service.CreateAsync(new CreateShipRequest("SHIP10", "Name", "0112", "inactive"), Token);

        await repository.Received(1).CreateAsync(Arg.Is<CreateShipRequest>(r => r.ShipCode == "SHIP09" && r.Status == "Active"), Token);
        await repository.Received(1).CreateAsync(Arg.Is<CreateShipRequest>(r => r.ShipCode == "SHIP10" && r.Status == "Inactive"), Token);
    }

    [Fact]
    public async Task Ship_list_normalizes_status_filter()
    {
        var repository = Substitute.For<IShipRepository>();
        repository.ListAsync(Arg.Any<ListShipsQuery>(), Token).Returns(new PagedResult<ShipResponse>([], 1, 20, 0));
        var service = CreateShipService(repository);

        await service.ListAsync(new ListShipsQuery("active"), Token);

        await repository.Received(1).ListAsync(Arg.Is<ListShipsQuery>(q => q.Status == "Active"), Token);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public async Task Assignment_rejects_non_positive_user_id(int userId)
    {
        var repository = Substitute.For<IShipAssignmentRepository>();
        var service = new ShipAssignmentService(repository, new ShipCodeValidator(), NullLogger<ShipAssignmentService>.Instance);

        await Assert.ThrowsAsync<InvalidRequestException>(() => service.AssignAsync(userId, "SHIP01", Token));
        await repository.DidNotReceiveWithAnyArgs().AssignAsync(default, default!, default);
    }

    [Fact]
    public async Task Assignment_rejects_invalid_ship_code()
    {
        var repository = Substitute.For<IShipAssignmentRepository>();
        var service = new ShipAssignmentService(repository, new ShipCodeValidator(), NullLogger<ShipAssignmentService>.Instance);

        var error = await Assert.ThrowsAsync<ValidationException>(() => service.UnassignAsync(1, "TOO-LONG-CODE", Token));

        Assert.Contains(error.Errors, e => e.PropertyName == "shipCode");
    }

    [Fact]
    public async Task User_service_returns_created_user()
    {
        var repository = Substitute.For<IUserRepository>();
        var expected = new UserResponse(10, "Jane Doe", "jane@example.com", "Viewer", DateTime.UtcNow);
        repository.CreateAsync(Arg.Any<CreateUserRequest>(), Token).Returns(expected);
        var service = new UserService(repository, new CreateUserRequestValidator(), new ListUsersQueryValidator(),
            NullLogger<UserService>.Instance);

        var created = await service.CreateAsync(new CreateUserRequest("Jane Doe", "jane@example.com", "Viewer"), Token);

        Assert.Same(expected, created);
    }

    [Fact]
    public async Task Report_service_passes_first_day_of_month_and_report_type()
    {
        var repository = Substitute.For<IFinancialReportRepository>();
        var service = new FinancialReportService(repository, new FinancialReportQueryValidator());

        await service.GetAsync(new FinancialReportQuery("SHIP02", "2025-02", FinancialReportType.Summary), Token);

        await repository.Received(1).GetAsync("SHIP02", new DateOnly(2025, 2, 1), FinancialReportType.Summary, Token);
    }

    private static ShipService CreateShipService(IShipRepository repository) =>
        new(repository, new CreateShipRequestValidator(), new ListShipsQueryValidator(), new ShipCodeValidator(),
            NullLogger<ShipService>.Instance);
}
