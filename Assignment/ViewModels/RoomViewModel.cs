using System.ComponentModel.DataAnnotations;

namespace Assignment.ViewModels
{
    public class RoomViewModel
    {
        public Guid? RoomId { get; set; }

        [Required(ErrorMessage = "Name is required")]
        [StringLength(20, ErrorMessage = "Name cannot be more than 20 characters")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Floor is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Floor must be a positive integer")]
        public int Floor { get; set; }

        [Required(ErrorMessage = "Hotel is required")]
        public Guid HotelId { get; set; }

        [Required(ErrorMessage = "Type is required")]
        public string Type { get; set; } = string.Empty;

        [Required(ErrorMessage = "Charges are required")]
        [Range(1, int.MaxValue, ErrorMessage = "Charges must be a positive integer")]
        public int Charges { get; set; }
    }
}
