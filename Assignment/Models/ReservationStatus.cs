namespace Assignment.Models;

/// <summary>
/// The booking workflow:
/// Pending → Approved → CheckedIn → CheckedOut (invoice issued).
/// Pending can be Rejected or Expire; Pending/Approved can be Cancelled; Approved can become a NoShow.
/// </summary>
public enum ReservationStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2,
    CheckedIn = 3,
    CheckedOut = 4,
    Cancelled = 5,
    Expired = 6,
    NoShow = 7,
}

public static class ReservationStatusExtensions
{
    /// <summary>Statuses that keep the room occupied for the booked period.</summary>
    public static readonly ReservationStatus[] Blocking =
        { ReservationStatus.Pending, ReservationStatus.Approved, ReservationStatus.CheckedIn };

    public static string BadgeClass(this ReservationStatus s) => s switch
    {
        ReservationStatus.Pending => "bg-warning text-dark",
        ReservationStatus.Approved => "bg-primary",
        ReservationStatus.CheckedIn => "bg-success",
        ReservationStatus.CheckedOut => "bg-secondary",
        ReservationStatus.Rejected or ReservationStatus.Cancelled => "bg-danger",
        _ => "bg-dark",
    };

    public static string Label(this ReservationStatus s) => s switch
    {
        ReservationStatus.CheckedIn => "Checked in",
        ReservationStatus.CheckedOut => "Checked out",
        ReservationStatus.NoShow => "No-show",
        _ => s.ToString(),
    };
}
