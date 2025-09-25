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
            ReservationViewModel model = new ReservationViewModel();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SearchRooms(ReservationViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("Index", model);
            }

            DateTime now = DateTime.Now;
            if (model.FromDate < now)
            {
                ModelState.AddModelError(nameof(model.FromDate), "From date cannot be in the past");
                return View("Index", model);
            }

            if (model.ToDate < now)
            {
                ModelState.AddModelError(nameof(model.ToDate), "To date cannot be in the past");
                return View("Index", model);
            }

            if (model.FromDate >= model.ToDate)
            {
                ModelState.AddModelError(nameof(model.ToDate), "To date must be after From date");
                return View("Index", model);
            }

            try
            {
                List<Guid> overlappingReservations = await _context.Reservations
                    .Where(r => !(r.To <= model.FromDate || r.From >= model.ToDate))
                    .Select(r => r.RoomId)
                    .ToListAsync();

                List<AvailableRoomViewModel> availableRooms = await _context.Rooms
                    .Where(r => !overlappingReservations.Contains(r.RoomId) && r.IsActive)
                    .Select(r => new AvailableRoomViewModel
                    {
                        RoomId = r.RoomId,
                        RoomName = r.Name,
                        HotelName = r.Hotel.Name,
                        Floor = r.Floor,
                        Type = r.Type,
                        Charges = r.Charges
                    })
                    .ToListAsync();


                model.AvailableRooms = availableRooms;

                if (!availableRooms.Any())
                {
                    TempData["InfoMessage"] = "No rooms available for the selected dates.";
                }
                else
                {
                    TempData["SuccessMessage"] = $"{availableRooms.Count} room(s) found for your selected dates.";
                }
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "An error occurred while searching for rooms. Please try again.";
                return View("Index", model);
            }

            return View("Index", model);
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
                decimal dailyRate = room.Charges;
                int totalCharges = (int)Math.Round(totalDays * (double)dailyRate, MidpointRounding.AwayFromZero);
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