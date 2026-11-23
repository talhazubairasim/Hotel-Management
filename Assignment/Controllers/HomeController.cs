using System.Diagnostics;
using Assignment.Models;
using Assignment.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Assignment.Controllers
{
    public class HomeController : Controller
    {
        private readonly HotelManagementContext _context;
        private readonly TimeProvider _clock;

        public HomeController(HotelManagementContext context, TimeProvider clock)
        {
            _context = context;
            _clock = clock;
        }

        /// <summary>Admins and receptionists each get their own dashboard.</summary>
        public async Task<IActionResult> Index()
        {
            DateTime now = _clock.GetLocalNow().DateTime;
            DateTime today = now.Date;
            var vm = new DashboardViewModel
            {
                HotelsCount = await _context.Hotels.CountAsync(h => h.IsActive),
                RoomsCount = await _context.Rooms.CountAsync(r => r.IsActive),
                ActiveReservationsCount = await _context.Reservations.CountAsync(r => r.Status == ReservationStatus.CheckedIn),
                TotalReservationsCount = await _context.Reservations.CountAsync(),
            };

            IQueryable<Reservation> withRoom = _context.Reservations
                .Include(r => r.Room).ThenInclude(r => r.Hotel).AsNoTracking();

            if (User.IsInRole(Roles.Admin))
            {
                var monthStart = new DateTime(today.Year, today.Month, 1);
                vm.PendingApprovals = await withRoom.Where(r => r.Status == ReservationStatus.Pending).OrderBy(r => r.From).ToListAsync();
                vm.PendingApprovalsCount = vm.PendingApprovals.Count;
                var invoices = await _context.Invoices.AsNoTracking().ToListAsync();
                vm.RevenueThisMonth = invoices.Where(i => i.IssuedAt >= monthStart).Sum(i => i.Total);
                vm.UnpaidInvoicesTotal = invoices.Where(i => !i.IsPaid).Sum(i => i.Total);
                vm.OccupancyRate = vm.RoomsCount == 0 ? 0 : (double)vm.ActiveReservationsCount / vm.RoomsCount;
                return View("AdminDashboard", vm);
            }

            vm.ArrivalsToday = await withRoom
                .Where(r => r.Status == ReservationStatus.Approved && r.From.Date <= today && r.To > now)
                .OrderBy(r => r.From).ToListAsync();
            vm.DeparturesToday = await withRoom
                .Where(r => r.Status == ReservationStatus.CheckedIn && r.To.Date <= today)
                .OrderBy(r => r.To).ToListAsync();
            vm.InHouse = await withRoom.Where(r => r.Status == ReservationStatus.CheckedIn).OrderBy(r => r.To).ToListAsync();
            vm.PendingApprovalsCount = await _context.Reservations.CountAsync(r => r.Status == ReservationStatus.Pending);
            vm.CustomersCount = await _context.Customers.CountAsync();
            return View("ReceptionistDashboard", vm);
        }

        [AllowAnonymous]
        public IActionResult Privacy() => View();

        [AllowAnonymous]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error() =>
            View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
