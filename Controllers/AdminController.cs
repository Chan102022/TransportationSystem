using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TransportationBookingSystem.Models;

namespace TransportationBookingSystem.Controllers
{
    // ✅ Only allow access to Admins
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly TransportationBookingSystemDbContext _context;

        public AdminController(TransportationBookingSystemDbContext context)
        {
            _context = context;
        }

        // ✅ Admin Dashboard
        public async Task<IActionResult> Dashboard()
        {
            var bookings = await _context.Book.ToListAsync();
            return View("Dashboard", bookings); // View: /Views/Admin/Dashboard.cshtml
        }

        // ✅ View all bookings (same as Dashboard, but separate if needed)
        public async Task<IActionResult> ViewAllBookings()
        {
            var allBookings = await _context.Book
                .Include(b => b.User) // Optional: if you want to show user info
                .ToListAsync();

            return View("ViewAllBookings", allBookings); // View: /Views/Admin/ViewAllBookings.cshtml
        }

        // ✅ Update booking status
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(string BookingId, string Status)
        {
            if (string.IsNullOrWhiteSpace(BookingId) || string.IsNullOrWhiteSpace(Status))
                return BadRequest("Invalid booking ID or status.");

            var booking = await _context.Book.FirstOrDefaultAsync(b => b.BookingId == BookingId);

            if (booking == null)
                return NotFound("Booking not found.");

            booking.Status = Status;

            await _context.SaveChangesAsync();

            return RedirectToAction("Dashboard");
        }
    }
}
