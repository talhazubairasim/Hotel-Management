using System;
using System.Collections.Generic;

namespace Assignment.Models;

public partial class Reservation
{
    public Guid ReservationId { get; set; }

    public string Name { get; set; } = null!;

    public Guid RoomId { get; set; }

    public DateTime From { get; set; }

    public DateTime To { get; set; }

    public int Cost { get; set; }

    public int DurationHours { get; set; }

    public virtual Room Room { get; set; } = null!;
}
