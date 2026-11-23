namespace Assignment.Models;

public partial class Reservation
{
    public Guid ReservationId { get; set; }

    /// <summary>Guest name at the time of booking (kept even if the profile changes).</summary>
    public string Name { get; set; } = null!;

    public Guid RoomId { get; set; }

    public Guid? CustomerId { get; set; }

    public DateTime From { get; set; }

    public DateTime To { get; set; }

    public double Cost { get; set; }

    public int DurationHours { get; set; }

    public ReservationStatus Status { get; set; } = ReservationStatus.Pending;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public string? CreatedBy { get; set; }

    public string? DecisionBy { get; set; }

    public string? DecisionNote { get; set; }

    public DateTime? CheckedInAt { get; set; }

    public DateTime? CheckedOutAt { get; set; }

    public virtual Room Room { get; set; } = null!;

    public virtual Customer? Customer { get; set; }

    public virtual Invoice? Invoice { get; set; }
}
