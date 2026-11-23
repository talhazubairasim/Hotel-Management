using Assignment.Models;
using Assignment.Services;
using Assignment.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Assignment.Controllers
{
    /// <summary>
    /// Booking requests and the front-desk workflow. Receptionists create requests,
    /// admins approve or reject them, and either role checks guests in and out.
    /// </summary>
    public class ReservationsController : Controller
    {
        private readonly HotelManagementContext _context;
        private readonly ReservationService _workflow;

        public ReservationsController(HotelManagementContext context, ReservationService workflow)
        {
            _context = context;
            _workflow = workflow;
        }

        private string CurrentUser => User.Identity?.Name ?? "unknown";

        public IActionResult Index() => RedirectToAction(nameof(ViewReservations));

        public async Task<IActionResult> ViewReservations(ReservationStatus? status, string? search)
        {
            IQueryable<Reservation> q = _context.Reservations
                .Include(r => r.Room).ThenInclude(room => room.Hotel)
                .Include(r => r.Customer)
                .Include(r => r.Invoice)
                .AsNoTracking();
            if (status != null) q = q.Where(r => r.Status == status);
            if (!string.IsNullOrWhiteSpace(search))
            {
                string s = search.Trim();
                q = q.Where(r => r.Name.Contains(s) || r.Room.Name.Contains(s) || r.Room.Hotel.Name.Contains(s));
            }
            return View(new ReservationListViewModel
            {
                Reservations = await q.OrderByDescending(r => r.From).ToListAsync(),
                Status = status,
                Search = search,
            });
        }

        [HttpGet]
        public async Task<IActionResult> Create(Guid? customerId)
        {
            var model = new CreateReservationViewModel { CustomerId = customerId };
            await LoadFormDataAsync(model);
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateReservationViewModel model)
        {
            if (ModelState.IsValid)
            {
                var result = await _workflow.BookAsync(
                    new BookingRequest(model.RoomId!.Value, model.From, model.To, model.CustomerId, model.GuestName, model.GuestEmail, model.GuestPhone),
                    CurrentUser, User.IsInRole(Roles.Admin));
                if (result.Success)
                {
                    TempData["SuccessMessage"] = result.Message;
                    return RedirectToAction(nameof(ViewReservations));
                }
                ModelState.AddModelError(string.Empty, result.Message);
            }
            await LoadFormDataAsync(model);
            return View(model);
        }

        private async Task LoadFormDataAsync(CreateReservationViewModel model)
        {
            if (model.CustomerId is Guid id)
                ViewBag.SelectedCustomer = await _context.Customers.FindAsync(id);
            ViewBag.Rooms = model.To > model.From ? await _workflow.AvailableRoomsAsync(model.From, model.To) : new List<Room>();
        }

        /// <summary>Available rooms for the dates, used by the booking form as the dates change.</summary>
        [HttpGet]
        public async Task<IActionResult> SearchRooms(DateTime from, DateTime to)
        {
            if (to <= from) return Json(new { success = false, message = "Check-out must be after check-in." });
            var rooms = await _workflow.AvailableRoomsAsync(from, to);
            return Json(new
            {
                success = true,
                rooms = rooms.Select(r => new
                {
                    roomId = r.RoomId,
                    roomName = r.Name,
                    hotelName = r.Hotel.Name,
                    floor = r.Floor,
                    type = r.Type,
                    charges = r.Charges,
                    estimate = BillingService.CalculateCost(r.Charges, from, to),
                }),
            });
        }

        private IActionResult Done(WorkflowResult result)
        {
            TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
            return RedirectToAction(nameof(ViewReservations));
        }

        [HttpPost]
        [Authorize(Roles = Roles.Admin)]
        public async Task<IActionResult> Approve(Guid id) => Done(await _workflow.ApproveAsync(id, CurrentUser));

        [HttpPost]
        [Authorize(Roles = Roles.Admin)]
        public async Task<IActionResult> Reject(Guid id, string? reason) => Done(await _workflow.RejectAsync(id, CurrentUser, reason));

        [HttpPost]
        public async Task<IActionResult> Cancel(Guid id) => Done(await _workflow.CancelAsync(id, CurrentUser));

        [HttpPost]
        public async Task<IActionResult> CheckIn(Guid id) => Done(await _workflow.CheckInAsync(id));

        [HttpPost]
        public async Task<IActionResult> CheckOut(Guid id)
        {
            var result = await _workflow.CheckOutAsync(id);
            if (result.Success && result.Reservation?.Invoice != null)
            {
                TempData["SuccessMessage"] = result.Message;
                return RedirectToAction("Details", "Invoices", new { id = result.Reservation.Invoice.InvoiceId });
            }
            return Done(result);
        }
    }
}
