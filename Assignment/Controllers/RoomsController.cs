using Assignment.Models;
using Assignment.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Assignment.Controllers
{
    public class RoomsController : Controller
    {
        private readonly HotelManagementContext _context;

        public RoomsController(HotelManagementContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                List<Room> rooms = await _context.Rooms
                    .Where(r => r.IsActive)
                    .Include(r => r.Hotel)
                    .AsNoTracking()
                    .ToListAsync();
                return View(rooms);
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "An error occurred while loading rooms.";
                return View(new List<Room>());
            }
        }

        public async Task<IActionResult> Create()
        {
            try
            {
                ViewBag.Hotels = await _context.Hotels.Where(h => h.IsActive).OrderBy(x => x.Name).ToListAsync();
                return View(new RoomViewModel());
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "An error occurred while loading hotel data.";
                return View(new RoomViewModel());
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(RoomViewModel model)
        {
            try
            {
                ViewBag.Hotels = await _context.Hotels.Where(h => h.IsActive).OrderBy(x => x.Name).ToListAsync();

                if (ModelState.IsValid)
                {
                    Hotel? hotel = await _context.Hotels.FirstOrDefaultAsync(h => h.HotelId == model.HotelId && h.IsActive);
                    if (hotel == null)
                    {
                        TempData["ErrorMessage"] = "Selected hotel not found.";
                        return View(model);
                    }

                    bool exists = await _context.Rooms
                        .Where(r => r.IsActive)
                        .AnyAsync(r =>
                            r.Name == model.Name &&
                            r.Floor == model.Floor &&
                            r.HotelId == model.HotelId
                        );

                    if (exists)
                    {
                        TempData["ErrorMessage"] = "Room with this Name and Floor already exists for this hotel.";
                        return View(model);
                    }

                    if (model.Floor > hotel.TotalFloors)
                    {
                        TempData["ErrorMessage"] = $"Hotel only has {hotel.TotalFloors} floors. Please choose a valid floor.";
                        return View(model);
                    }

                    int roomCount = await _context.Rooms.Where(r => r.IsActive).CountAsync(r => r.HotelId == model.HotelId);
                    if (roomCount >= hotel.TotalRooms)
                    {
                        TempData["ErrorMessage"] = $"Hotel already has the maximum number of rooms ({hotel.TotalRooms}).";
                        return View(model);
                    }

                    Room room = new Room
                    {
                        RoomId = Guid.NewGuid(),
                        Name = model.Name,
                        Floor = model.Floor,
                        HotelId = model.HotelId,
                        Type = model.Type,
                        Charges = model.Charges,
                        IsActive = true
                    };

                    _context.Rooms.Add(room);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "A new Room is added successfully!";
                    return RedirectToAction("Index");
                }

                return Json(new { success = false, message = "Validation failed." });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "An error occurred while creating the room. Please try again." });
            }
        }

        public async Task<IActionResult> Edit(Guid id)
        {
            try
            {
                Room? room = await _context.Rooms.FindAsync(id);
                if (room == null) return NotFound();

                RoomViewModel model = new RoomViewModel
                {
                    RoomId = room.RoomId,
                    Name = room.Name,
                    Floor = room.Floor,
                    HotelId = room.HotelId,
                    Type = room.Type,
                    Charges = room.Charges
                };

                ViewBag.Hotels = await _context.Hotels.Where(h => h.IsActive).OrderBy(x => x.Name).ToListAsync();
                return View(model);
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "An error occurred while loading the room for editing.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, RoomViewModel model)
        {
            try
            {
                if (id != model.RoomId)
                {
                    return Json(new { success = false, message = "Invalid room ID." });
                }

                if (!ModelState.IsValid)
                {
                    return Json(new { success = false, message = "Please fill in all required fields correctly." });
                }

                Room? currentRoom = await _context.Rooms.FindAsync(id);
                if (currentRoom == null)
                {
                    return Json(new { success = false, message = "Room not found." });
                }

                Hotel? hotel = await _context.Hotels.FirstOrDefaultAsync(h => h.HotelId == model.HotelId && h.IsActive);
                if (hotel == null)
                {
                    return Json(new { success = false, message = "Selected hotel not found." });
                }

                if (currentRoom.HotelId != model.HotelId)
                {
                    int targetHotelRoomCount = await _context.Rooms.Where(r => r.IsActive).CountAsync(r => r.HotelId == model.HotelId);
                    if (targetHotelRoomCount >= hotel.TotalRooms)
                    {
                        return Json(new { success = false, message = $"Target hotel already has the maximum number of rooms ({hotel.TotalRooms}). Cannot move room to this hotel." });
                    }
                }

                if (model.Floor > hotel.TotalFloors)
                {
                    return Json(new { success = false, message = $"Hotel only has {hotel.TotalFloors} floors. Please choose a valid floor." });
                }

                bool exists = await _context.Rooms
                    .Where(r => r.IsActive)
                    .AnyAsync(r =>
                        r.Name == model.Name &&
                        r.Floor == model.Floor &&
                        r.HotelId == model.HotelId &&
                        r.RoomId != model.RoomId
                    );

                if (exists)
                {
                    return Json(new { success = false, message = "A room with the same name and floor already exists in this hotel." });
                }

                currentRoom.Name = model.Name;
                currentRoom.Floor = model.Floor;
                currentRoom.HotelId = model.HotelId;
                currentRoom.Type = model.Type;
                currentRoom.Charges = model.Charges;

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Room updated successfully!" });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "An error occurred while updating the room. Please try again." });
            }
        }

        [HttpGet]
        public async Task<JsonResult> IsRoomAvailable(string name, int floor, Guid hotelId, Guid? id)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name) || floor <= 0 || hotelId == Guid.Empty)
                    return Json(true);

                bool exists = await _context.Rooms.Where(r => r.IsActive).AnyAsync(r =>
                    r.Name == name &&
                    r.Floor == floor &&
                    r.HotelId == hotelId &&
                    (!id.HasValue || r.RoomId != id.Value));

                return Json(!exists);
            }
            catch (Exception)
            {
                return Json(true);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                Room? room = await _context.Rooms.FindAsync(id);
                if (room == null)
                {
                    return Json(new { success = false, message = "Room not found." });
                }

                room.IsActive = false;
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Room deactivated successfully!" });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "An error occurred while marking the room inactive." });
            }
        }
    }
}