using System.ComponentModel.DataAnnotations;

namespace Assignment.Models;

/// <summary>A guest profile. Reservations and invoices are linked to it.</summary>
public class Customer
{
    public Guid CustomerId { get; set; }

    [Required, StringLength(50)]
    public string FullName { get; set; } = null!;

    [EmailAddress, StringLength(100)]
    public string? Email { get; set; }

    [Phone, StringLength(20)]
    public string? Phone { get; set; }

    [StringLength(30)]
    public string? IdentityNumber { get; set; }

    [StringLength(200)]
    public string? Address { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public virtual ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
}
