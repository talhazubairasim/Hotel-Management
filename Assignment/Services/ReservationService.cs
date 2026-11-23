using Assignment.Models;
using Microsoft.EntityFrameworkCore;

namespace Assignment.Services;

/// <summary>Result of a workflow action: success, or a message explaining why it was refused.</summary>
public record WorkflowResult(bool Success, string Message, Reservation? Reservation = null)
{
    public static WorkflowResult Fail(string message) => new(false, message);
}

public record BookingRequest(Guid RoomId, DateTime From, DateTime To, Guid? CustomerId, string? GuestName, string? GuestEmail, string? GuestPhone);

/// <summary>
/// The reservation workflow. Bookings made by an Admin are approved immediately;
/// bookings made by a Receptionist wait for an Admin's approval. Checking out
/// issues the invoice. <see cref="RunAutomationAsync"/> expires stale requests and
/// marks no-shows.
/// </summary>
public class ReservationService
{
    private readonly HotelManagementContext _db;
    private readonly BillingService _billing;
    private readonly TimeProvider _clock;

    public ReservationService(HotelManagementContext db, BillingService billing, TimeProvider clock)
    {
        _db = db;
        _billing = billing;
        _clock = clock;
    }

    private DateTime Now => _clock.GetLocalNow().DateTime;

    /// <summary>Active rooms with no blocking reservation overlapping [from, to).</summary>
    public async Task<List<Room>> AvailableRoomsAsync(DateTime from, DateTime to)
    {
        var busy = await _db.Reservations
            .Where(r => ReservationStatusExtensions.Blocking.Contains(r.Status) && r.From < to && r.To > from)
            .Select(r => r.RoomId)
            .ToListAsync();
        return await _db.Rooms
            .Include(r => r.Hotel)
            .Where(r => r.IsActive && r.Hotel.IsActive && !busy.Contains(r.RoomId))
            .OrderBy(r => r.Type == "Standard" ? 1 : r.Type == "Deluxe" ? 2 : 3)
            .ThenBy(r => r.Hotel.Name).ThenBy(r => r.Name)
            .ToListAsync();
    }

    public async Task<WorkflowResult> BookAsync(BookingRequest req, string user, bool isAdmin)
    {
        if (req.From < Now.AddMinutes(-5)) return WorkflowResult.Fail("The start of the stay cannot be in the past.");
        if (req.To <= req.From) return WorkflowResult.Fail("Check-out must be after check-in.");

        Room? room = await _db.Rooms.Include(r => r.Hotel).FirstOrDefaultAsync(r => r.RoomId == req.RoomId && r.IsActive);
        if (room == null) return WorkflowResult.Fail("Room not found.");

        bool overlaps = await _db.Reservations.AnyAsync(r => r.RoomId == req.RoomId
            && ReservationStatusExtensions.Blocking.Contains(r.Status) && r.From < req.To && r.To > req.From);
        if (overlaps) return WorkflowResult.Fail("This room is already reserved for the selected time period.");

        Customer? customer = await ResolveCustomerAsync(req);
        if (customer == null) return WorkflowResult.Fail("Choose an existing guest or enter the new guest's name.");

        var reservation = new Reservation
        {
            ReservationId = Guid.NewGuid(),
            RoomId = room.RoomId,
            Customer = customer,
            Name = customer.FullName,
            From = req.From,
            To = req.To,
            Cost = BillingService.CalculateCost(room.Charges, req.From, req.To),
            DurationHours = BillingService.DurationHours(req.From, req.To),
            CreatedAt = Now,
            CreatedBy = user,
            Status = isAdmin ? ReservationStatus.Approved : ReservationStatus.Pending,
        };
        if (isAdmin)
        {
            reservation.DecisionBy = user;
            reservation.DecisionNote = "Auto-approved: booked by an administrator";
        }
        _db.Reservations.Add(reservation);
        await _db.SaveChangesAsync();

        string state = isAdmin ? "confirmed" : "sent for approval";
        return new WorkflowResult(true, $"Room {room.Name} at {room.Hotel.Name} {state} for {customer.FullName}.", reservation);
    }

    /// <summary>Uses the chosen profile, an existing profile with the same email or phone, or creates a new one.</summary>
    private async Task<Customer?> ResolveCustomerAsync(BookingRequest req)
    {
        if (req.CustomerId is Guid id && id != Guid.Empty)
            return await _db.Customers.FindAsync(id);

        string? name = req.GuestName?.Trim();
        if (string.IsNullOrEmpty(name)) return null;
        string? email = string.IsNullOrWhiteSpace(req.GuestEmail) ? null : req.GuestEmail.Trim().ToLowerInvariant();
        string? phone = string.IsNullOrWhiteSpace(req.GuestPhone) ? null : req.GuestPhone.Trim();

        Customer? existing = null;
        if (email != null) existing = await _db.Customers.FirstOrDefaultAsync(c => c.Email == email);
        if (existing == null && phone != null) existing = await _db.Customers.FirstOrDefaultAsync(c => c.Phone == phone);
        if (existing != null) return existing;

        var customer = new Customer { CustomerId = Guid.NewGuid(), FullName = name, Email = email, Phone = phone, CreatedAt = Now };
        _db.Customers.Add(customer);
        return customer;
    }

    private async Task<Reservation?> LoadAsync(Guid id) =>
        await _db.Reservations.Include(r => r.Room).ThenInclude(r => r.Hotel).Include(r => r.Invoice)
            .FirstOrDefaultAsync(r => r.ReservationId == id);

    private async Task<WorkflowResult> TransitionAsync(Guid id, ReservationStatus[] allowedFrom, Func<Reservation, string?> apply, string success)
    {
        Reservation? r = await LoadAsync(id);
        if (r == null) return WorkflowResult.Fail("Reservation not found.");
        if (!allowedFrom.Contains(r.Status))
            return WorkflowResult.Fail($"A reservation that is {r.Status.Label().ToLowerInvariant()} cannot be changed this way.");
        string? error = apply(r);
        if (error != null) return WorkflowResult.Fail(error);
        await _db.SaveChangesAsync();
        return new WorkflowResult(true, string.Format(success, r.Name), r);
    }

    public Task<WorkflowResult> ApproveAsync(Guid id, string admin) =>
        TransitionAsync(id, new[] { ReservationStatus.Pending }, r =>
        {
            r.Status = ReservationStatus.Approved;
            r.DecisionBy = admin;
            r.DecisionNote = null;
            return null;
        }, "Reservation for {0} approved.");

    public Task<WorkflowResult> RejectAsync(Guid id, string admin, string? reason) =>
        TransitionAsync(id, new[] { ReservationStatus.Pending }, r =>
        {
            r.Status = ReservationStatus.Rejected;
            r.DecisionBy = admin;
            r.DecisionNote = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
            return null;
        }, "Reservation for {0} rejected.");

    public Task<WorkflowResult> CancelAsync(Guid id, string user) =>
        TransitionAsync(id, new[] { ReservationStatus.Pending, ReservationStatus.Approved }, r =>
        {
            r.Status = ReservationStatus.Cancelled;
            r.DecisionBy = user;
            return null;
        }, "Reservation for {0} cancelled. The room is available again.");

    public Task<WorkflowResult> CheckInAsync(Guid id) =>
        TransitionAsync(id, new[] { ReservationStatus.Approved }, r =>
        {
            if (Now < r.From.Date) return "Guests can check in from the day their stay starts.";
            r.Status = ReservationStatus.CheckedIn;
            r.CheckedInAt = Now;
            return null;
        }, "{0} checked in.");

    /// <summary>Checks the guest out and issues the invoice.</summary>
    public async Task<WorkflowResult> CheckOutAsync(Guid id)
    {
        Reservation? r = await LoadAsync(id);
        if (r == null) return WorkflowResult.Fail("Reservation not found.");
        if (r.Status != ReservationStatus.CheckedIn) return WorkflowResult.Fail("Only checked-in guests can check out.");

        DateTime now = Now;
        r.Status = ReservationStatus.CheckedOut;
        r.CheckedOutAt = now;
        if (r.Invoice == null)
        {
            int sequence = await _db.Invoices.CountAsync(i => i.IssuedAt.Date == now.Date) + 1;
            r.Invoice = _billing.CreateInvoice(r, r.Room.Charges, now, sequence);
        }
        await _db.SaveChangesAsync();
        return new WorkflowResult(true, $"{r.Name} checked out. Invoice {r.Invoice.InvoiceNumber} issued.", r);
    }

    /// <summary>
    /// Workflow automation, run periodically: pending requests whose stay has started
    /// expire, and approved bookings whose stay has ended without a check-in become no-shows.
    /// Returns the number of reservations changed.
    /// </summary>
    public async Task<int> RunAutomationAsync()
    {
        DateTime now = Now;
        var stalePending = await _db.Reservations.Where(r => r.Status == ReservationStatus.Pending && r.From < now).ToListAsync();
        foreach (var r in stalePending)
        {
            r.Status = ReservationStatus.Expired;
            r.DecisionNote = "Expired: not approved before the stay started";
        }
        var noShows = await _db.Reservations.Where(r => r.Status == ReservationStatus.Approved && r.To < now).ToListAsync();
        foreach (var r in noShows)
        {
            r.Status = ReservationStatus.NoShow;
        }
        await _db.SaveChangesAsync();
        return stalePending.Count + noShows.Count;
    }
}
