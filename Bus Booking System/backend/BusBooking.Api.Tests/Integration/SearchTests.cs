using System.Net;
using System.Net.Http.Json;
using BusBooking.Api.Dtos;
using BusBooking.Api.Infrastructure;
using BusBooking.Api.Models;
using BusBooking.Api.Tests.Support;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace BusBooking.Api.Tests.Integration;

[Collection("Integration")]
public class SearchTests : IAsyncLifetime
{
    private readonly IntegrationFixture _fx;
    public SearchTests(IntegrationFixture fx) => _fx = fx;
    public async Task InitializeAsync() => await _fx.ResetAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    // Seeds a full chain: operator + approved bus + offices + route + active schedule for every day
    private async Task<(City src, City dst, BusSchedule schedule)> SeedSearchFixture()
    {
        var (op, _) = await OperatorTokenFactory.CreateAsync(_fx, [Roles.Operator], email: "search-op@t.local");
        using var scope = _fx.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var src = new City { Id = Guid.NewGuid(), Name = "Hyderabad", State = "TS", IsActive = true };
        var dst = new City { Id = Guid.NewGuid(), Name = "Pune",      State = "MH", IsActive = true };
        db.Cities.AddRange(src, dst);

        var route = new Models.Route { Id = Guid.NewGuid(), SourceCityId = src.Id, DestinationCityId = dst.Id, IsActive = true };
        db.Routes.Add(route);

        db.OperatorOffices.AddRange(
            new OperatorOffice { Id = Guid.NewGuid(), OperatorUserId = op.Id, CityId = src.Id, AddressLine = "Addr1", Phone = "111", IsActive = true },
            new OperatorOffice { Id = Guid.NewGuid(), OperatorUserId = op.Id, CityId = dst.Id, AddressLine = "Addr2", Phone = "222", IsActive = true }
        );

        var bus = new Bus
        {
            Id = Guid.NewGuid(), OperatorUserId = op.Id,
            RegistrationNumber = "TS-SEARCH-01", BusName = "Search Express",
            BusType = BusType.Seater, Capacity = 12,
            ApprovalStatus = BusApprovalStatus.Approved,
            OperationalStatus = BusOperationalStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
        db.Buses.Add(bus);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var schedule = new BusSchedule
        {
            Id = Guid.NewGuid(), BusId = bus.Id, RouteId = route.Id,
            DepartureTime = new TimeOnly(9, 0), ArrivalTime = new TimeOnly(18, 0),
            FarePerSeat = 400m,
            ValidFrom = today, ValidTo = today.AddDays(30),
            DaysOfWeek = 127, // every day
            IsActive = true
        };
        db.BusSchedules.Add(schedule);

        // Generate seat definitions (3 rows × 4 cols)
        for (var r = 0; r < 3; r++)
            for (var c = 0; c < 4; c++)
                db.SeatDefinitions.Add(new SeatDefinition
                {
                    Id = Guid.NewGuid(), BusId = bus.Id,
                    SeatNumber = $"{(char)('A' + r)}{c + 1}",
                    RowIndex = r, ColumnIndex = c, SeatCategory = SeatCategory.Regular
                });

        await db.SaveChangesAsync();
        return (src, dst, schedule);
    }

    [Fact]
    public async Task Search_returns_trip_on_valid_route_and_date()
    {
        var (src, dst, _) = await SeedSearchFixture();
        var client = _fx.CreateClient();
        var date = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd");

        var results = await (await client.GetAsync(
            $"/api/v1/search?src={src.Id}&dst={dst.Id}&date={date}"))
            .Content.ReadFromJsonAsync<List<SearchResultDto>>();

        results!.Should().HaveCount(1);
        results![0].BusName.Should().Be("Search Express");
        results![0].SeatsLeft.Should().Be(12);
    }

    [Fact]
    public async Task Search_materializes_trip_idempotently()
    {
        var (src, dst, _) = await SeedSearchFixture();
        var client = _fx.CreateClient();
        var date = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd");
        var url = $"/api/v1/search?src={src.Id}&dst={dst.Id}&date={date}";

        var r1 = await (await client.GetAsync(url)).Content.ReadFromJsonAsync<List<SearchResultDto>>();
        var r2 = await (await client.GetAsync(url)).Content.ReadFromJsonAsync<List<SearchResultDto>>();
        r1![0].TripId.Should().Be(r2![0].TripId);
    }

    [Fact]
    public async Task Search_unknown_route_returns_empty_array()
    {
        var client = _fx.CreateClient();
        var date = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd");
        var results = await (await client.GetAsync(
            $"/api/v1/search?src={Guid.NewGuid()}&dst={Guid.NewGuid()}&date={date}"))
            .Content.ReadFromJsonAsync<List<SearchResultDto>>();
        results!.Should().BeEmpty();
    }

    [Fact]
    public async Task GetTripDetail_returns_seat_layout()
    {
        var (src, dst, _) = await SeedSearchFixture();
        var client = _fx.CreateClient();
        var date = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd");
        var res = await (await client.GetAsync(
            $"/api/v1/search?src={src.Id}&dst={dst.Id}&date={date}"))
            .Content.ReadFromJsonAsync<List<SearchResultDto>>();
        res.Should().NotBeNull();
        var tripId = res![0].TripId;

        var detail = await (await client.GetAsync($"/api/v1/trips/{tripId}"))
            .Content.ReadFromJsonAsync<TripDetailDto>();

        detail!.BusName.Should().Be("Search Express");
        detail.SeatLayout.Rows.Should().Be(3);
        detail.SeatLayout.Columns.Should().Be(4);
        detail.SeatLayout.Seats.Should().HaveCount(12);
        detail.SeatLayout.Seats.Should().OnlyContain(s => s.Status == "available");
    }

    [Fact]
    public async Task GetSeatLayout_returns_all_available_seats()
    {
        var (src, dst, _) = await SeedSearchFixture();
        var client = _fx.CreateClient();
        var date = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd");
        var res = await (await client.GetAsync(
            $"/api/v1/search?src={src.Id}&dst={dst.Id}&date={date}"))
            .Content.ReadFromJsonAsync<List<SearchResultDto>>();
        res.Should().NotBeNull();
        var tripId = res![0].TripId;

        var layout = await (await client.GetAsync($"/api/v1/trips/{tripId}/seats"))
            .Content.ReadFromJsonAsync<SeatLayoutDto>();

        layout!.Seats.Should().HaveCount(12)
            .And.OnlyContain(s => s.Status == "available");
    }
}
