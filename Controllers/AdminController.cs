using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TransportationBookingSystem.Models;

namespace TransportationBookingSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly TransportationBookingSystemDbContext _context;

        public AdminController(TransportationBookingSystemDbContext context)
        {
            _context = context;
        }

        // ---------------------
        // BOOKINGS DASHBOARD
        // ---------------------
        public async Task<IActionResult> Dashboard(string statusFilter, string routeFilter)
        {
            // Start with all bookings
            var bookingsQuery = _context.Book.AsQueryable();

            // Filter by status
            if (!string.IsNullOrWhiteSpace(statusFilter))
                bookingsQuery = bookingsQuery.Where(b => b.Status == statusFilter);

            // Filter by destination/route
            if (!string.IsNullOrWhiteSpace(routeFilter))
                bookingsQuery = bookingsQuery.Where(b => b.Destination == routeFilter);

            var bookings = await bookingsQuery
                .Include(b => b.User)
                .ToListAsync();

            // Prepare list of distinct routes for dropdown
            ViewBag.Routes = await _context.Destinations
                .Select(d => d.Name)
                .ToListAsync();

            ViewBag.StatusFilter = statusFilter;
            ViewBag.RouteFilter = routeFilter;

            return View(bookings);
        }

        public async Task<IActionResult> ViewAllBookings()
        {
            var allBookings = await _context.Book
                .Include(b => b.User)
                .ToListAsync();

            return View("ViewAllBookings", allBookings);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(string BookingId, string Status)
        {
            if (string.IsNullOrWhiteSpace(BookingId) || string.IsNullOrWhiteSpace(Status))
                return BadRequest("Invalid booking ID or status.");

            var booking = await _context.Book.FirstOrDefaultAsync(b => b.BookingId == BookingId);
            if (booking == null) return NotFound("Booking not found.");

            booking.Status = Status;
            await _context.SaveChangesAsync();

            return RedirectToAction("Dashboard");
        }

        // ---------------------
        // DESTINATIONS WITH FARE AND TIMES
        // ---------------------
        public async Task<IActionResult> Destinations()
        {
            var destinations = await _context.Destinations.ToListAsync();
            return View("Destinations", destinations);
        }

        [HttpPost]
        public async Task<IActionResult> AddDestination(string name, decimal fare, DateTime departureTime, DateTime arrivalTime)
        {
            if (!string.IsNullOrWhiteSpace(name))
            {
                _context.Destinations.Add(new Destination
                {
                    Name = name,
                    Fare = fare,
                    DepartureTime = departureTime,
                    ArrivalTime = arrivalTime
                });
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Destinations");
        }

        [HttpPost]
        public async Task<IActionResult> EditDestination(int id, string name, decimal fare, DateTime departureTime, DateTime arrivalTime)
        {
            var destination = await _context.Destinations.FindAsync(id);
            if (destination != null)
            {
                destination.Name = name;
                destination.Fare = fare;
                destination.DepartureTime = departureTime;
                destination.ArrivalTime = arrivalTime;
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Destinations");
        }

        public async Task<IActionResult> DeleteDestination(int id)
        {
            var destination = await _context.Destinations.FindAsync(id);
            if (destination != null)
            {
                _context.Destinations.Remove(destination);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Destinations");
        }

        // ---------------------
        // ADMIN HOME SUMMARY
        // ---------------------
        public async Task<IActionResult> AdminHome()
        {
            var destinations = await _context.Destinations.ToListAsync();
            var bookings = await _context.Book.ToListAsync();

            var model = new Tuple<List<Destination>, List<Passenger>>(destinations, bookings);

            return View(model);
        }
    }
}
