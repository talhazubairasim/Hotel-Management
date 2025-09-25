using System;
using System.ComponentModel.DataAnnotations;

namespace Assignment.ViewModels
{
    public class HotelViewModel
    {
        public Guid? HotelId { get; set; }

        [Required(ErrorMessage = "Name is required")]
        [StringLength(20, ErrorMessage = "Name cannot be more than 20 characters")]
        public string Name { get; set; } = string.Empty;

        [StringLength(100)]
        public string? Address { get; set; }

        [Required(ErrorMessage = "Rating is required")]
        public string Rating { get; set; } = string.Empty;

        [Required(ErrorMessage = "Total Floors is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Total Floors must be a positive integer")]
        public int TotalFloors { get; set; }

        [Required(ErrorMessage = "Total Rooms is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Total Rooms must be a positive integer")]
        public int TotalRooms { get; set; }

        public int? RoomsPerFloor { get; set; }
    }
}
