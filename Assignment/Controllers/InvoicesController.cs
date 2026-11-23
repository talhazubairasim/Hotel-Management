using Assignment.Models;
using Assignment.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Assignment.Controllers;

/// <summary>Invoices are generated automatically at check-out (see ReservationService.CheckOutAsync).</summary>
public class InvoicesController : Controller
{
    private readonly HotelManagementContext _context;
    private readonly BillingService _billing;
    private readonly TimeProvider _clock;

    public InvoicesController(HotelManagementContext context, BillingService billing, TimeProvider clock)
    {
        _context = context;
        _billing = billing;
        _clock = clock;
    }

    private IQueryable<Invoice> Query() => _context.Invoices.AsNoTracking()
        .Include(i => i.Reservation).ThenInclude(r => r.Room).ThenInclude(r => r.Hotel)
        .Include(i => i.Reservation).ThenInclude(r => r.Customer);

    public async Task<IActionResult> Index(bool? unpaid)
    {
        IQueryable<Invoice> q = Query();
        if (unpaid == true) q = q.Where(i => !i.IsPaid);
        ViewBag.Unpaid = unpaid == true;
        ViewBag.Currency = _billing.Options.Currency;
        return View(await q.OrderByDescending(i => i.IssuedAt).ToListAsync());
    }

    public async Task<IActionResult> Details(Guid id)
    {
        Invoice? invoice = await Query().FirstOrDefaultAsync(i => i.InvoiceId == id);
        if (invoice == null) return NotFound();
        ViewBag.Currency = _billing.Options.Currency;
        return View(invoice);
    }

    [HttpPost]
    public async Task<IActionResult> MarkPaid(Guid id)
    {
        Invoice? invoice = await _context.Invoices.FindAsync(id);
        if (invoice == null) return NotFound();
        if (!invoice.IsPaid)
        {
            invoice.IsPaid = true;
            invoice.PaidAt = _clock.GetLocalNow().DateTime;
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Invoice {invoice.InvoiceNumber} marked as paid.";
        }
        return RedirectToAction(nameof(Details), new { id });
    }
}
