using System.ComponentModel.DataAnnotations;
using Assignment.Models;
using Assignment.Validation;

namespace Assignment.ViewModels
{
    /// <summary>The booking form. Validated on the server and, through unobtrusive validation, in the browser.</summary>
    public class CreateReservationViewModel : IValidatableObject
    {
        [Required(ErrorMessage = "Check-in is required")]
        [Display(Name = "Check-in")]
        [NotInPast]
        public DateTime From { get; set; } = DateTime.Today.AddDays(1).AddHours(14);

        [Required(ErrorMessage = "Check-out is required")]
        [Display(Name = "Check-out")]
        [DateAfter(nameof(From), ErrorMessage = "Check-out must be after check-in.")]
        public DateTime To { get; set; } = DateTime.Today.AddDays(2).AddHours(12);

        [Required(ErrorMessage = "Choose a room")]
        [Display(Name = "Room")]
        public Guid? RoomId { get; set; }

        [Display(Name = "Existing guest")]
        public Guid? CustomerId { get; set; }

        [StringLength(50)]
        [Display(Name = "Guest name")]
        public string? GuestName { get; set; }

        [EmailAddress, StringLength(100)]
        [Display(Name = "Guest email")]
        public string? GuestEmail { get; set; }

        [Phone, StringLength(20)]
        [Display(Name = "Guest phone")]
        public string? GuestPhone { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if ((CustomerId == null || CustomerId == Guid.Empty) && string.IsNullOrWhiteSpace(GuestName))
                yield return new ValidationResult("Choose an existing guest or enter the new guest's name.", new[] { nameof(GuestName) });
        }
    }

    public class ReservationListViewModel
    {
        public List<Reservation> Reservations { get; set; } = new();
        public ReservationStatus? Status { get; set; }
        public string? Search { get; set; }
    }
}
