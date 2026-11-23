using Assignment.Models;
using Assignment.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

namespace Assignment.Tests;

/// <summary>An in-memory database with one hotel and two rooms, and a controllable clock.</summary>
public sealed class TestDb : IDisposable
{
    public HotelManagementContext Db { get; }
    public FakeTimeProvider Clock { get; } = new(new DateTimeOffset(2030, 1, 10, 9, 0, 0, TimeSpan.Zero));
    public BillingService Billing { get; } = new(Options.Create(new BillingOptions { TaxRate = 0.10m }));
    public ReservationService Workflow { get; }
    public Room Standard { get; }
    public Room Deluxe { get; }

    public TestDb()
    {
        Clock.SetLocalTimeZone(TimeZoneInfo.Utc);
        var options = new DbContextOptionsBuilder<HotelManagementContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        Db = new HotelManagementContext(options);
        var hotel = new Hotel { HotelId = Guid.NewGuid(), Name = "Test Hotel", Rating = "4", TotalFloors = 2, TotalRooms = 4, IsActive = true };
        Standard = new Room { RoomId = Guid.NewGuid(), Hotel = hotel, Name = "101", Floor = 1, Type = "Standard", Charges = 100, IsActive = true };
        Deluxe = new Room { RoomId = Guid.NewGuid(), Hotel = hotel, Name = "201", Floor = 2, Type = "Deluxe", Charges = 200, IsActive = true };
        Db.AddRange(hotel, Standard, Deluxe);
        Db.SaveChanges();
        Workflow = new ReservationService(Db, Billing, Clock);
    }

    public DateTime Now => Clock.GetLocalNow().DateTime;

    public void Dispose() => Db.Dispose();
}
