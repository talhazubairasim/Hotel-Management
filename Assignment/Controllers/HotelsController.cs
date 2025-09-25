using Assignment.Models;
using Assignment.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Assignment.Controllers
{
    public class HotelsController : Controller
    {
        private readonly HotelManagementContext _context;

        public HotelsController(HotelManagementContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                List<Hotel> hotels = await _context.Hotels
                    .Where(h => h.IsActive)
                    .AsNoTracking()
                    .ToListAsync();
                return View(hotels);
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "An error occurred while loading hotels.";
                return View(new List<Hotel>());
            }
        }

        public IActionResult Create()
        {
            return View(new HotelViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(HotelViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                    return View(model);

                bool exists = await _context.Hotels
                    .Where(h => h.IsActive)
                    .AnyAsync(h => h.Name == model.Name);

                if (exists)
                {
                    ModelState.AddModelError(nameof(model.Name), "Hotel with this name already exists.");
                    return View(model);
                }

                Hotel hotel = new Hotel
                {
                    HotelId = Guid.NewGuid(),
                    Name = model.Name,
                    Address = model.Address,
                    Rating = model.Rating,
                    TotalFloors = model.TotalFloors,
                    TotalRooms = model.TotalRooms,
                    RoomsPerFloor = model.RoomsPerFloor ?? 0,
                    IsActive = true
                };

                _context.Hotels.Add(hotel);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "A new hotel is added.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "An error occurred while creating the hotel. Please try again.";
                return View(model);
            }
        }

        public async Task<IActionResult> Edit(Guid id)
        {
            try
            {
                Hotel? hotel = await _context.Hotels.FindAsync(id);
                if (hotel == null) return NotFound();

                HotelViewModel vm = new HotelViewModel
                {
                    HotelId = hotel.HotelId,
                    Name = hotel.Name,
                    Address = hotel.Address,
                    Rating = hotel.Rating,
                    TotalFloors = hotel.TotalFloors,
                    TotalRooms = hotel.TotalRooms,
                    RoomsPerFloor = hotel.RoomsPerFloor
                };
                return View(vm);
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "An error occurred while loading the hotel for editing.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, HotelViewModel vm)
        {
            try
            {
                if (id != vm.HotelId) return BadRequest();

                if (ModelState.IsValid)
                {
                    Hotel? hotel = await _context.Hotels.FindAsync(id);
                    if (hotel == null) return NotFound();

                    hotel.Name = vm.Name;
                    hotel.Address = vm.Address;
                    hotel.Rating = vm.Rating;
                    hotel.TotalFloors = vm.TotalFloors;
                    hotel.TotalRooms = vm.TotalRooms;
                    hotel.RoomsPerFloor = vm.RoomsPerFloor;

                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Hotel updated successfully!";
                    return RedirectToAction(nameof(Index));
                }
                return View(vm);
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "An error occurred while updating the hotel. Please try again.";
                return View(vm);
            }
        }

        [HttpGet]
        public async Task<JsonResult> IsNameAvailable(string name, Guid? id)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name)) return Json(true);

                bool exists = await _context.Hotels
                    .AnyAsync(h => h.IsActive && h.Name == name && (!id.HasValue || h.HotelId != id.Value));

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
                Hotel? hotel = await _context.Hotels.FindAsync(id);
                if (hotel == null)
                {
                    return Json(new { success = false, message = "Hotel not found." });
                }

                bool hasRooms = await _context.Rooms.AnyAsync(r => r.HotelId == id && r.IsActive);
                if (hasRooms)
                {
                    return Json(new { success = false, message = "Cannot delete hotel with assigned active rooms." });
                }

                hotel.IsActive = false;
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Hotel deactivated successfully." });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "Error occurred while deactivating hotel." });
            }
        }
    }
}