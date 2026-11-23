namespace Assignment.Models;

/// <summary>Issued automatically when a guest checks out.</summary>
public class Invoice
{
    public Guid InvoiceId { get; set; }

    public string InvoiceNumber { get; set; } = null!;

    public Guid ReservationId { get; set; }

    public DateTime IssuedAt { get; set; }

    public decimal Nights { get; set; }

    public decimal RoomRate { get; set; }

    public decimal Subtotal { get; set; }

    public decimal TaxRate { get; set; }

    public decimal Tax { get; set; }

    public decimal Total { get; set; }

    public bool IsPaid { get; set; }

    public DateTime? PaidAt { get; set; }

    public virtual Reservation Reservation { get; set; } = null!;
}
