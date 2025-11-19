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

            // ===============================
            // DEFAULT: show only TODAY’S bookings
            // ===============================
            if (!dateFilter.HasValue)
            {
                var today = DateTime.Now.Date;
                bookingsQuery = bookingsQuery.Where(b => b.BookingDate.Date == today);

                // Pass default date (today) to UI
                ViewBag.DateFilter = today.ToString("yyyy-MM-dd");
            }
            else
            {
                // If user selects a date, filter normally
                bookingsQuery = bookingsQuery.Where(b => b.BookingDate.Date == dateFilter.Value.Date);
                ViewBag.DateFilter = dateFilter.Value.ToString("yyyy-MM-dd");
            }

            // Filter by status
            if (!string.IsNullOrWhiteSpace(statusFilter))
                bookingsQuery = bookingsQuery.Where(b => b.Status == statusFilter);

            // Filter by destination
            if (!string.IsNullOrWhiteSpace(routeFilter))
                bookingsQuery = bookingsQuery.Where(b => b.Destination == routeFilter);

            // Filter by time (same hour range)
            if (timeFilter.HasValue)
            {
                var start = timeFilter.Value;
                var end = start.Add(TimeSpan.FromMinutes(59));

                bookingsQuery = bookingsQuery.Where(b =>
                    b.BookingDate.TimeOfDay >= start &&
                    b.BookingDate.TimeOfDay <= end
                );
            }

            // Final list with User relation included
            var bookings = await bookingsQuery
                .Include(b => b.User)
                .ToListAsync();

            // Send filters to view
            ViewBag.StatusFilter = statusFilter;
            ViewBag.RouteFilter = routeFilter;
            ViewBag.TimeFilter = timeFilter?.ToString(@"hh\:mm");

            // Load route dropdown
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
        public async Task<IActionResult> AdminRevenue(DateTime? date, bool monthly = false)
        {
            var selectedDate = date?.Date ?? DateTime.Today;

            IQueryable<Passenger> query = _context.Book
                .Where(b => b.PaymentStatus == "Paid" && b.DatePaid.HasValue);

            // Monthly Mode
            if (monthly)
            {
                query = query.Where(b =>
                    b.DatePaid.Value.Month == selectedDate.Month &&
                    b.DatePaid.Value.Year == selectedDate.Year);

                ViewBag.EarningsTitle = $"{selectedDate:MMMM yyyy} - Monthly Earnings";
            }
            else
            {
                // Daily Mode
                query = query.Where(b =>
                    b.DatePaid.Value.Date == selectedDate.Date);

                ViewBag.EarningsTitle = $"{selectedDate:MMMM dd, yyyy} - Daily Earnings";
            }

            var bookings = await query.ToListAsync();

            ViewBag.SelectedDate = selectedDate.ToString("yyyy-MM-dd");
            ViewBag.TotalEarnings = bookings.Sum(b => b.Fare);

            return View("AdminRevenue", bookings);
        }


        [HttpPost]
        public IActionResult BulkUpdateStatus([FromBody] BulkUpdateModel data)
        {
            var bookings = _context.Book.Where(b => data.Ids.Contains(b.BookingId)).ToList();

            foreach (var b in bookings)
                b.Status = data.Status;

            _context.SaveChanges();

            return Ok();
        }

        public class BulkUpdateModel
        {
            public List<string> Ids { get; set; }
            public string Status { get; set; }
        }




    }
} 