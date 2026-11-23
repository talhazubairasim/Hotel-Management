using Assignment.Models;
using Assignment.Services;
using Microsoft.Extensions.Options;

namespace Assignment.Tests;

public class BillingServiceTests
{
    [Fact]
    public void Cost_is_pro_rated_per_day()
    {
        var from = new DateTime(2030, 1, 1, 14, 0, 0);
        Assert.Equal(200, BillingService.CalculateCost(100, from, from.AddDays(2)));
        Assert.Equal(150, BillingService.CalculateCost(100, from, from.AddHours(36)));
        Assert.Equal(36, BillingService.DurationHours(from, from.AddHours(35.5)));
    }

    [Fact]
    public void Cost_rejects_an_empty_stay()
    {
        var at = new DateTime(2030, 1, 1);
        Assert.Throws<ArgumentException>(() => BillingService.CalculateCost(100, at, at));
    }

    [Fact]
    public void Invoice_adds_tax_and_numbers_itself()
    {
        var billing = new BillingService(Options.Create(new BillingOptions { TaxRate = 0.16m }));
        var from = new DateTime(2030, 3, 1, 14, 0, 0);
        var reservation = new Reservation { ReservationId = Guid.NewGuid(), From = from, To = from.AddDays(3), Cost = 300 };
        var invoice = billing.CreateInvoice(reservation, 100, new DateTime(2030, 3, 4, 11, 0, 0), 7);

        Assert.Equal("INV-20300304-0007", invoice.InvoiceNumber);
        Assert.Equal(3m, invoice.Nights);
        Assert.Equal(100m, invoice.RoomRate);
        Assert.Equal(300m, invoice.Subtotal);
        Assert.Equal(48m, invoice.Tax);
        Assert.Equal(348m, invoice.Total);
        Assert.False(invoice.IsPaid);
        Assert.Equal(reservation.ReservationId, invoice.ReservationId);
    }
}
