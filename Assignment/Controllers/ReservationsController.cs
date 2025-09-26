using Assignment.Models;
using Assignment.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Assignment.Controllers
{
    public class ReservationsController : Controller
    {
        private readonly HotelManagementContext _context;

        public ReservationsController(HotelManagementContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return RedirectToAction("ViewReservations");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SearchRooms(DateTime FromDate, DateTime ToDate)
        {
            try
            {
                DateTime now = DateTime.Now;

                if (FromDate < now)
                {
                    return Json(new { success = false, message = "From date cannot be in the past" });
                }

                if (ToDate < now)
                {
                    return Json(new { success = false, message = "To date cannot be in the past" });
                }

                if (FromDate >= ToDate)
                {
                    return Json(new { success = false, message = "To date must be after From date" });
                }

                List<Guid> overlappingReservations = await _context.Reservations
                    .Where(r => !(r.To <= FromDate || r.From >= ToDate))
                    .Select(r => r.RoomId)
                    .ToListAsync();

                var availableRooms = await _context.Rooms
                    .Where(r => !overlappingReservations.Contains(r.RoomId) && r.IsActive)
                    .Include(r => r.Hotel)
                    .Select(r => new {
                        roomId = r.RoomId,
                        roomName = r.Name,
                        hotelName = r.Hotel.Name,
                        floor = r.Floor,
                        type = r.Type,
                        charges = r.Charges
                    })
                    .OrderBy(r => r.type == "Standard" ? 1 : r.type == "Deluxe" ? 2 : 3)
                    .ThenBy(r => r.hotelName)
                    .ToListAsync();

                return Json(new
                {
                    success = true,
                    rooms = availableRooms
                });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "An error occurred while searching for rooms. Please try again." });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReserveRoom(CreateReservationViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                    return Json(new { success = false, message = "Invalid input data. Please check all fields." });

                if (model.From >= model.To)
                    return Json(new { success = false, message = "Invalid date range. 'To' date must be after 'From' date." });

                List<Reservation> overlappingReservations = await _context.Reservations
                    .Where(r => r.RoomId == model.RoomId &&
                        !(r.To <= model.From || r.From >= model.To))
                    .ToListAsync();

                if (overlappingReservations.Any())
                {
                    return Json(new { success = false, message = "This room is already reserved for the selected time period." });
                }

                Room? room = await _context.Rooms
                    .Where(r => r.IsActive)
                    .Include(r => r.Hotel)
                    .FirstOrDefaultAsync(r => r.RoomId == model.RoomId);

                if (room == null)
                {
                    return Json(new { success = false, message = "Room not found." });
                }

                double totalDays = (model.To - model.From).TotalDays;
                int durationHours = (int)Math.Ceiling((model.To - model.From).TotalHours);
                double dailyRate = room.Charges;
                double totalCharges = Math.Round(totalDays * (double)dailyRate, MidpointRounding.AwayFromZero);

                Reservation reservation = new Reservation
                {
                    ReservationId = Guid.NewGuid(),
                    Name = model.Name.Trim(),
                    RoomId = model.RoomId,
                    From = model.From,
                    To = model.To,
                    DurationHours = durationHours,
                    Cost = totalCharges
                };

                _context.Reservations.Add(reservation);
                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = $"Room {room.Name} at {room.Hotel.Name} reserved successfully for {model.Name}!",
                    reservationId = reservation.ReservationId,
                    cost = totalCharges,
                    durationHours = durationHours,
                    totalDays = Math.Round(totalDays, 2)
                });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "An error occurred while processing your reservation. Please try again." });
            }
        }

        public async Task<IActionResult> ViewReservations()
        {
            try
            {
                List<Reservation> reservations = await _context.Reservations
                    .Include(r => r.Room)
                    .ThenInclude(room => room.Hotel)
                    .AsNoTracking()
                    .OrderByDescending(r => r.From)
                    .ThenByDescending(r => r.ReservationId)
                    .ToListAsync();

                return View(reservations);
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "An error occurred while loading reservations.";
                return View(new List<Reservation>());
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelReservation(Guid reservationId)
        {
            try
            {
                if (reservationId == Guid.Empty)
                {
                    return Json(new { success = false, message = "Invalid reservation ID" });
                }

                Reservation? reservation = await _context.Reservations
                    .Include(r => r.Room)
                    .ThenInclude(room => room.Hotel)
                    .FirstOrDefaultAsync(r => r.ReservationId == reservationId);

                if (reservation == null)
                {
                    return Json(new { success = false, message = "Reservation not found" });
                }

                DateTime now = DateTime.Now;
                if (reservation.To <= now)
                {
                    return Json(new { success = false, message = "Cannot cancel completed reservations" });
                }

                string customerName = reservation.Name;
                string roomName = reservation.Room.Name;
                string hotelName = reservation.Room.Hotel.Name;

                _context.Reservations.Remove(reservation);
                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = $"Reservation for {customerName} at {hotelName} - {roomName} has been cancelled successfully. The room is now available for booking."
                });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "An error occurred while cancelling the reservation. Please try again." });
            }
        }
    }
}