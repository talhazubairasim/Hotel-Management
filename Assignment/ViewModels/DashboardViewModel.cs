using Assignment.Models;

namespace Assignment.ViewModels
{
    public class DashboardViewModel
    {
        public int HotelsCount { get; set; }
        public int RoomsCount { get; set; }
        public int ActiveReservationsCount { get; set; }
        public int TotalReservationsCount { get; set; }

        // Admin dashboard
        public int PendingApprovalsCount { get; set; }
        public decimal RevenueThisMonth { get; set; }
        public decimal UnpaidInvoicesTotal { get; set; }
        public double OccupancyRate { get; set; }
        public List<Reservation> PendingApprovals { get; set; } = new();

        // Receptionist dashboard
        public List<Reservation> ArrivalsToday { get; set; } = new();
        public List<Reservation> DeparturesToday { get; set; } = new();
        public List<Reservation> InHouse { get; set; } = new();
        public int CustomersCount { get; set; }
    }
}
