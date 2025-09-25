using System;
using System.Collections.Generic;

namespace Assignment.Models;

public partial class Room
{
    public Guid RoomId { get; set; }

    public string Name { get; set; } = null!;

    public int Floor { get; set; }

    public Guid HotelId { get; set; }

    public string Type { get; set; } = null!;

    public int Charges { get; set; }

    public bool IsActive { get; set; } = true;

    public virtual Hotel Hotel { get; set; } = null!;

    public virtual ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
}
