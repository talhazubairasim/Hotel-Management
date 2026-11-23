using Assignment.Models;
using Microsoft.Extensions.Options;

namespace Assignment.Services;

public class BillingOptions
{
    /// <summary>Sales tax applied to invoices, e.g. 0.16 for 16%.</summary>
    public decimal TaxRate { get; set; } = 0.16m;

    public string Currency { get; set; } = "$";
}

/// <summary>Calculates booking costs and builds invoices.</summary>
public class BillingService
{
    private readonly BillingOptions _options;

    public BillingService(IOptions<BillingOptions> options) => _options = options.Value;

    public BillingOptions Options => _options;

    /// <summary>Room charges are per day, pro-rated by the hour, rounded to whole currency units.</summary>
    public static double CalculateCost(double dailyRate, DateTime from, DateTime to)
    {
        if (to <= from) throw new ArgumentException("'To' must be after 'From'.");
        double days = (to - from).TotalDays;
        return Math.Round(days * dailyRate, MidpointRounding.AwayFromZero);
    }

    public static int DurationHours(DateTime from, DateTime to) => (int)Math.Ceiling((to - from).TotalHours);

    /// <summary>Builds the invoice for a checked-out reservation.</summary>
    public Invoice CreateInvoice(Reservation reservation, double dailyRate, DateTime issuedAt, int sequence)
    {
        decimal subtotal = Math.Round((decimal)reservation.Cost, 2);
        decimal tax = Math.Round(subtotal * _options.TaxRate, 2, MidpointRounding.AwayFromZero);
        return new Invoice
        {
            InvoiceId = Guid.NewGuid(),
            InvoiceNumber = $"INV-{issuedAt:yyyyMMdd}-{sequence:D4}",
            ReservationId = reservation.ReservationId,
            IssuedAt = issuedAt,
            Nights = Math.Round((decimal)(reservation.To - reservation.From).TotalDays, 2),
            RoomRate = (decimal)dailyRate,
            Subtotal = subtotal,
            TaxRate = _options.TaxRate,
            Tax = tax,
            Total = subtotal + tax,
        };
    }
}
