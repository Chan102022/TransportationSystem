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
        public async Task<IActionResult> Dashboard(
     string statusFilter,
     string routeFilter,
     DateTime? dateFilter,
     TimeSpan? timeFilter)
        {
            var bookingsQuery = _context.Book.AsQueryable();

            // Filter by status
            if (!string.IsNullOrWhiteSpace(statusFilter))
                bookingsQuery = bookingsQuery.Where(b => b.Status == statusFilter);

            // Filter by destination
            if (!string.IsNullOrWhiteSpace(routeFilter))
                bookingsQuery = bookingsQuery.Where(b => b.Destination == routeFilter);

            // Filter by date
            if (dateFilter.HasValue)
                bookingsQuery = bookingsQuery.Where(b => b.BookingDate.Date == dateFilter.Value.Date);

            // Filter by time (within the hour)
            if (timeFilter.HasValue)
            {
                var start = timeFilter.Value;
                var end = start.Add(TimeSpan.FromMinutes(59));

                bookingsQuery = bookingsQuery.Where(b =>
                    b.BookingDate.TimeOfDay >= start &&
                    b.BookingDate.TimeOfDay <= end
                );
            }

            var bookings = await bookingsQuery
                .Include(b => b.User)
                .ToListAsync();

            // Send back filters to UI
            ViewBag.StatusFilter = statusFilter;
            ViewBag.RouteFilter = routeFilter;
            ViewBag.DateFilter = dateFilter?.ToString("yyyy-MM-dd");
            ViewBag.TimeFilter = timeFilter?.ToString(@"hh\:mm");

            // Load all route names for dropdown
            ViewBag.Routes = await _context.Destinations
                .Select(d => d.Name)
                .Distinct()
                .ToListAsync();

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
        // DESTINATIONS WITH GLOBAL TIME SLOTS
        // ---------------------
        public async Task<IActionResult> Destinations()
        {
            var destinations = await _context.Destinations.ToListAsync();

            // Load global time slot settings
            var settings = await _context.TimeSlots.FirstOrDefaultAsync();

            // If none exist, create default
            if (settings == null)
            {
                settings = new TimeSlotSettings
                {
                    Slot1Start = new TimeSpan(7, 0, 0),
                    Slot1End = new TimeSpan(8, 0, 0),

                    Slot2Start = new TimeSpan(11, 0, 0),
                    Slot2End = new TimeSpan(12, 0, 0),

                    Slot3Start = new TimeSpan(14, 0, 0),
                    Slot3End = new TimeSpan(15, 0, 0),

                    Slot4Start = new TimeSpan(17, 0, 0),
                    Slot4End = new TimeSpan(18, 0, 0)
                };

                _context.TimeSlots.Add(settings);
                await _context.SaveChangesAsync();
            }

            ViewBag.TimeSlots = settings;

            return View("Destinations", destinations);
        }

        [HttpPost]
        public async Task<IActionResult> AddDestination(string name, decimal fare)
        {
            if (!string.IsNullOrWhiteSpace(name))
            {
                var today = DateTime.Today;

                // Load global time slot settings
                var settings = await _context.TimeSlots.FirstAsync();

                var slots = new List<(TimeSpan dep, TimeSpan arr)>
                {
                    (settings.Slot1Start, settings.Slot1End),
                    (settings.Slot2Start, settings.Slot2End),
                    (settings.Slot3Start, settings.Slot3End),
                    (settings.Slot4Start, settings.Slot4End)
                };

                foreach (var slot in slots)
                {
                    _context.Destinations.Add(new Destination
                    {
                        Name = name,
                        Fare = fare,
                        DepartureTime = today.Add(slot.dep),
                        ArrivalTime = today.Add(slot.arr)
                    });
                }

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
        [HttpPost]
        public async Task<IActionResult> DeleteBooking(string BookingId)
        {
            if (string.IsNullOrWhiteSpace(BookingId))
                return BadRequest("Invalid booking ID.");

            var booking = await _context.Book.FirstOrDefaultAsync(b => b.BookingId == BookingId);

            if (booking == null)
                return NotFound("Booking not found.");

            _context.Book.Remove(booking);
            await _context.SaveChangesAsync();

            return RedirectToAction("Dashboard");
        }
        public async Task<IActionResult> Earnings(DateTime? date)
        {
            // If no date provided, use today
            var selectedDate = date?.Date ?? DateTime.Today;

            // Fetch all PAID bookings on that day
            var paidBookings = await _context.Book
                .Where(b => b.PaymentStatus == "Paid"
                            && b.DatePaid.HasValue
                            && b.DatePaid.Value.Date == selectedDate)
                .ToListAsync();

            // Calculate total earned
            decimal total = paidBookings.Sum(p => p.Fare);

            ViewBag.SelectedDate = selectedDate.ToString("yyyy-MM-dd");
            ViewBag.TotalEarnings = total;

            return View(paidBookings);
        }



    }
}
