using Assignment.Models;
using Assignment.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Assignment.Controllers;

/// <summary>Guest profiles: contact details, notes and booking history.</summary>
public class CustomersController : Controller
{
    private readonly HotelManagementContext _context;

    public CustomersController(HotelManagementContext context) => _context = context;

    public async Task<IActionResult> Index(string? search)
    {
        IQueryable<Customer> q = _context.Customers.AsNoTracking().Include(c => c.Reservations);
        if (!string.IsNullOrWhiteSpace(search))
        {
            string s = search.Trim();
            q = q.Where(c => c.FullName.Contains(s) || (c.Email != null && c.Email.Contains(s)) || (c.Phone != null && c.Phone.Contains(s)));
        }
        ViewBag.Search = search;
        return View(await q.OrderBy(c => c.FullName).ToListAsync());
    }

    public async Task<IActionResult> Details(Guid id)
    {
        Customer? customer = await _context.Customers.AsNoTracking()
            .Include(c => c.Reservations).ThenInclude(r => r.Room).ThenInclude(r => r.Hotel)
            .Include(c => c.Reservations).ThenInclude(r => r.Invoice)
            .FirstOrDefaultAsync(c => c.CustomerId == id);
        return customer == null ? NotFound() : View(customer);
    }

    /// <summary>JSON search used by the booking form to pick an existing guest.</summary>
    public async Task<IActionResult> Lookup(string term)
    {
        string t = (term ?? string.Empty).Trim();
        var matches = await _context.Customers.AsNoTracking()
            .Where(c => t.Length > 0 && (c.FullName.Contains(t) || (c.Email != null && c.Email.Contains(t)) || (c.Phone != null && c.Phone.Contains(t))))
            .OrderBy(c => c.FullName).Take(10)
            .Select(c => new { id = c.CustomerId, name = c.FullName, email = c.Email, phone = c.Phone })
            .ToListAsync();
        return Json(matches);
    }

    public IActionResult Create() => View("Edit", new CustomerViewModel());

    public async Task<IActionResult> Edit(Guid id)
    {
        Customer? c = await _context.Customers.FindAsync(id);
        if (c == null) return NotFound();
        return View(new CustomerViewModel
        {
            CustomerId = c.CustomerId, FullName = c.FullName, Email = c.Email, Phone = c.Phone,
            IdentityNumber = c.IdentityNumber, Address = c.Address, Notes = c.Notes,
        });
    }

    [HttpPost]
    public async Task<IActionResult> Save(CustomerViewModel model)
    {
        string? email = string.IsNullOrWhiteSpace(model.Email) ? null : model.Email.Trim().ToLowerInvariant();
        if (email != null && await _context.Customers.AnyAsync(c => c.Email == email && c.CustomerId != model.CustomerId))
            ModelState.AddModelError(nameof(model.Email), "Another guest already uses this email.");
        if (!ModelState.IsValid) return View("Edit", model);

        Customer? c;
        if (model.CustomerId is Guid id)
        {
            c = await _context.Customers.FindAsync(id);
            if (c == null) return NotFound();
        }
        else
        {
            c = new Customer { CustomerId = Guid.NewGuid() };
            _context.Customers.Add(c);
        }
        c.FullName = model.FullName.Trim();
        c.Email = email;
        c.Phone = model.Phone?.Trim();
        c.IdentityNumber = model.IdentityNumber?.Trim();
        c.Address = model.Address?.Trim();
        c.Notes = model.Notes?.Trim();
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = $"Guest profile for {c.FullName} saved.";
        return RedirectToAction(nameof(Details), new { id = c.CustomerId });
    }

    [HttpPost]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Delete(Guid id)
    {
        Customer? c = await _context.Customers.Include(x => x.Reservations).FirstOrDefaultAsync(x => x.CustomerId == id);
        if (c == null) return NotFound();
        if (c.Reservations.Any())
        {
            TempData["ErrorMessage"] = "Guests with reservations cannot be deleted.";
            return RedirectToAction(nameof(Details), new { id });
        }
        _context.Customers.Remove(c);
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = $"Guest {c.FullName} deleted.";
        return RedirectToAction(nameof(Index));
    }
}
