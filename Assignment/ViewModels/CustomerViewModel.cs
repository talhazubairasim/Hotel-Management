using System.ComponentModel.DataAnnotations;

namespace Assignment.ViewModels;

public class CustomerViewModel
{
    public Guid? CustomerId { get; set; }

    [Required(ErrorMessage = "Full name is required")]
    [StringLength(50)]
    [Display(Name = "Full name")]
    public string FullName { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "Enter a valid email address")]
    [StringLength(100)]
    public string? Email { get; set; }

    [Phone(ErrorMessage = "Enter a valid phone number")]
    [StringLength(20)]
    public string? Phone { get; set; }

    [StringLength(30)]
    [Display(Name = "ID / passport number")]
    public string? IdentityNumber { get; set; }

    [StringLength(200)]
    public string? Address { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }
}
