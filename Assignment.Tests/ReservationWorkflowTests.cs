using Assignment.Models;
using Assignment.Services;
using Microsoft.EntityFrameworkCore;

namespace Assignment.Tests;

public class ReservationWorkflowTests
{
    private static BookingRequest Booking(TestDb t, Room room, int startInHours = 24, int nights = 2, string guest = "Ayesha Khan",
        string? email = "ayesha@example.com", Guid? customerId = null) =>
        new(room.RoomId, t.Now.AddHours(startInHours), t.Now.AddHours(startInHours).AddDays(nights), customerId, guest, email, "+92 300 0000000");

    [Fact]
    public async Task Receptionist_bookings_wait_for_approval_and_create_a_guest_profile()
    {
        using var t = new TestDb();
        var result = await t.Workflow.BookAsync(Booking(t, t.Standard), "front@desk", isAdmin: false);

        Assert.True(result.Success, result.Message);
        var r = result.Reservation!;
        Assert.Equal(ReservationStatus.Pending, r.Status);
        Assert.Equal(200, r.Cost);
        Assert.Equal(48, r.DurationHours);
        Assert.Equal("front@desk", r.CreatedBy);
        var guest = await t.Db.Customers.SingleAsync();
        Assert.Equal("Ayesha Khan", guest.FullName);
        Assert.Equal(guest.CustomerId, r.CustomerId);
    }

    [Fact]
    public async Task Admin_bookings_are_auto_approved()
    {
        using var t = new TestDb();
        var result = await t.Workflow.BookAsync(Booking(t, t.Standard), "admin", isAdmin: true);
        Assert.Equal(ReservationStatus.Approved, result.Reservation!.Status);
        Assert.Equal("admin", result.Reservation.DecisionBy);
    }

    [Fact]
    public async Task Returning_guests_are_matched_by_email_or_phone_or_chosen_directly()
    {
        using var t = new TestDb();
        await t.Workflow.BookAsync(Booking(t, t.Standard), "u", true);
        await t.Workflow.BookAsync(Booking(t, t.Deluxe, guest: "Ayesha K.", email: "AYESHA@example.com"), "u", true);
        await t.Workflow.BookAsync(Booking(t, t.Deluxe, startInHours: 200, guest: "A. Khan", email: null), "u", true);
        Assert.Equal(1, await t.Db.Customers.CountAsync());

        var id = (await t.Db.Customers.SingleAsync()).CustomerId;
        var direct = await t.Workflow.BookAsync(Booking(t, t.Standard, startInHours: 300, guest: "", email: null, customerId: id), "u", true);
        Assert.True(direct.Success);
        Assert.Equal(id, direct.Reservation!.CustomerId);
    }

    [Fact]
    public async Task Booking_is_refused_for_bad_dates_missing_rooms_missing_guests_and_overlaps()
    {
        using var t = new TestDb();
        Assert.False((await t.Workflow.BookAsync(Booking(t, t.Standard, startInHours: -2), "u", true)).Success);
        Assert.False((await t.Workflow.BookAsync(Booking(t, t.Standard, nights: 0), "u", true)).Success);
        var ghost = new Room { RoomId = Guid.NewGuid() };
        Assert.Equal("Room not found.", (await t.Workflow.BookAsync(Booking(t, ghost), "u", true)).Message);
        Assert.False((await t.Workflow.BookAsync(Booking(t, t.Standard, guest: " ", email: null), "u", true)).Success);

        Assert.True((await t.Workflow.BookAsync(Booking(t, t.Standard), "u", true)).Success);
        var overlap = await t.Workflow.BookAsync(Booking(t, t.Standard, startInHours: 30), "u", true);
        Assert.False(overlap.Success);
        Assert.Contains("already reserved", overlap.Message);
    }

    [Fact]
    public async Task Cancelled_and_rejected_bookings_free_the_room()
    {
        using var t = new TestDb();
        var first = (await t.Workflow.BookAsync(Booking(t, t.Standard), "u", false)).Reservation!;
        Assert.DoesNotContain(await t.Workflow.AvailableRoomsAsync(first.From, first.To), r => r.RoomId == t.Standard.RoomId);

        Assert.True((await t.Workflow.RejectAsync(first.ReservationId, "admin", " full ")).Success);
        Assert.Equal("full", (await t.Db.Reservations.FindAsync(first.ReservationId))!.DecisionNote);
        Assert.Contains(await t.Workflow.AvailableRoomsAsync(first.From, first.To), r => r.RoomId == t.Standard.RoomId);

        var second = (await t.Workflow.BookAsync(Booking(t, t.Standard), "u", true)).Reservation!;
        Assert.True((await t.Workflow.CancelAsync(second.ReservationId, "u")).Success);
        Assert.True((await t.Workflow.BookAsync(Booking(t, t.Standard), "u", true)).Success);
    }

    [Fact]
    public async Task Full_stay_issues_an_invoice_on_check_out()
    {
        using var t = new TestDb();
        var r = (await t.Workflow.BookAsync(Booking(t, t.Deluxe, startInHours: 1), "desk", false)).Reservation!;

        Assert.False((await t.Workflow.CheckInAsync(r.ReservationId)).Success); // not approved yet
        Assert.True((await t.Workflow.ApproveAsync(r.ReservationId, "admin")).Success);
        Assert.False((await t.Workflow.ApproveAsync(r.ReservationId, "admin")).Success); // already approved

        Assert.False((await t.Workflow.CheckOutAsync(r.ReservationId)).Success); // not checked in
        t.Clock.Advance(TimeSpan.FromHours(2));
        Assert.True((await t.Workflow.CheckInAsync(r.ReservationId)).Success);
        Assert.False((await t.Workflow.CancelAsync(r.ReservationId, "u")).Success); // in house

        t.Clock.Advance(TimeSpan.FromDays(2));
        var done = await t.Workflow.CheckOutAsync(r.ReservationId);
        Assert.True(done.Success, done.Message);
        var invoice = await t.Db.Invoices.SingleAsync();
        Assert.Equal(400m, invoice.Subtotal);
        Assert.Equal(40m, invoice.Tax);
        Assert.Equal(440m, invoice.Total);
        Assert.StartsWith("INV-20300112-0001", invoice.InvoiceNumber);
        Assert.Equal(ReservationStatus.CheckedOut, (await t.Db.Reservations.FindAsync(r.ReservationId))!.Status);
    }

    [Fact]
    public async Task Check_in_is_not_allowed_before_the_stay_day()
    {
        using var t = new TestDb();
        var r = (await t.Workflow.BookAsync(Booking(t, t.Standard, startInHours: 72), "a", true)).Reservation!;
        var result = await t.Workflow.CheckInAsync(r.ReservationId);
        Assert.False(result.Success);
        Assert.Contains("day their stay starts", result.Message);
    }

    [Fact]
    public async Task Unknown_reservations_are_reported()
    {
        using var t = new TestDb();
        Assert.Equal("Reservation not found.", (await t.Workflow.ApproveAsync(Guid.NewGuid(), "a")).Message);
        Assert.Equal("Reservation not found.", (await t.Workflow.CheckOutAsync(Guid.NewGuid())).Message);
    }

    [Fact]
    public async Task Automation_expires_stale_requests_and_marks_no_shows()
    {
        using var t = new TestDb();
        var pending = (await t.Workflow.BookAsync(Booking(t, t.Standard, startInHours: 1), "desk", false)).Reservation!;
        var approved = (await t.Workflow.BookAsync(Booking(t, t.Deluxe, startInHours: 1, nights: 1), "admin", true)).Reservation!;

        Assert.Equal(0, await t.Workflow.RunAutomationAsync());
        t.Clock.Advance(TimeSpan.FromDays(3));
        Assert.Equal(2, await t.Workflow.RunAutomationAsync());

        Assert.Equal(ReservationStatus.Expired, (await t.Db.Reservations.FindAsync(pending.ReservationId))!.Status);
        Assert.Equal(ReservationStatus.NoShow, (await t.Db.Reservations.FindAsync(approved.ReservationId))!.Status);
    }
}
