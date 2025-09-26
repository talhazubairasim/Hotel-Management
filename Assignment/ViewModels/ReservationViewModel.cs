using System.ComponentModel.DataAnnotations;

namespace Assignment.ViewModels
{
    public class ReservationViewModel
    {
        [Required(ErrorMessage = "From date is required")]
        [DataType(DataType.Date)]
        [Display(Name = "From")]
        public DateTime FromDate { get; set; } = DateTime.Now;

        [Required(ErrorMessage = "To date is required")]
        [DataType(DataType.Date)]
        [Display(Name = "To")]
        public DateTime ToDate { get; set; } = DateTime.Now.AddHours(1);

        public List<AvailableRoomViewModel> AvailableRooms { get; set; } = new List<AvailableRoomViewModel>();
    }

    public class AvailableRoomViewModel
    {
        public Guid RoomId { get; set; }
        public string RoomName { get; set; } = string.Empty;
        public string HotelName { get; set; } = string.Empty;
        public int Floor { get; set; }
        public string Type { get; set; } = string.Empty;
        public int Charges { get; set; }
    }

    public class CreateReservationViewModel
    {
        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        public Guid RoomId { get; set; }

        [Required]
        public DateTime From { get; set; }

        [Required]
        public DateTime To { get; set; }

        public double Cost { get; set; }

        public int DurationHours { get; set; }

        public string RoomName { get; set; } = string.Empty;
        public string HotelName { get; set; } = string.Empty;
    }
}