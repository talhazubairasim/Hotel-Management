using System;
using System.Collections.Generic;

namespace Assignment.Models;

public partial class Hotel
{
    public Guid HotelId { get; set; }

    public string Name { get; set; } = null!;

    public string? Address { get; set; }

    public string Rating { get; set; } = null!;

    public int TotalRooms { get; set; }

    public int TotalFloors { get; set; }

    public int? RoomsPerFloor { get; set; }

    public bool IsActive { get; set; } = true;

    public virtual ICollection<Room> Rooms { get; set; } = new List<Room>();
}
