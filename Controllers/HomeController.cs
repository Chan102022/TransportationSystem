using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TransportationBookingSystem.Models;
using TransportationBookingSystem.Services;

namespace TransportationBookingSystem.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly TransportationBookingSystemDbContext _context;
        private readonly PassengersService _passengersService;
        private readonly UserManager<ApplicationUser> _userManager;

        public HomeController(
            ILogger<HomeController> logger,
            TransportationBookingSystemDbContext context,
            PassengersService passengersService,
            UserManager<ApplicationUser> userManager)
        {
            _logger = logger;
            _context = context;
            _passengersService = passengersService;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var destinations = await _context.Destinations.ToListAsync();
            ViewBag.Destinations = destinations;
            return View();
        }

        public async Task<IActionResult> Book()
        {
            var userId = _userManager.GetUserId(User);
            var bookings = await _context.Book
                .Where(b => b.UserId == userId)
                .ToListAsync();

            return View(bookings);
        }

        public async Task<IActionResult> Booking(string? id)
        {
            var userId = _userManager.GetUserId(User);

            // Load destinations for dropdown
            var destinations = await _context.Destinations.ToListAsync();
            ViewBag.Destinations = destinations;

            if (!string.IsNullOrEmpty(id))
            {
                var passengerInDb = await _context.Book
                    .FirstOrDefaultAsync(p => p.BookingId == id && p.UserId == userId);

                if (passengerInDb == null)
                {
                    return NotFound();
                }

                return View(passengerInDb);
            }


            // If no id, create a new Passenger
            var newPassenger = new Passenger
            {
                BookingId = _passengersService.GenerateNextBookingId(),
                BusNo = _passengersService.GenerateNextBusNo(),
                SeatNo = _passengersService.GenerateNextSeatNo(),
                BookingDate = DateTime.Now,
                UserId = userId
            };

            return View(newPassenger);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        
        public async Task<IActionResult> BookingForm(Passenger model)
        {
            // Fill required fields
            model.BookingId ??= _passengersService.GenerateNextBookingId();
            model.BusNo ??= _passengersService.GenerateNextBusNo();
            model.SeatNo ??= _passengersService.GenerateNextSeatNo();
            model.BookingDate = DateTime.Now;
            model.UserId ??= _userManager.GetUserId(User);
            model.Status ??= "Pending";

            // Lookup selected destination to set Fare, DepartureTime, ArrivalTime
            var selectedDestination = await _context.Destinations
                .FirstOrDefaultAsync(d => d.Name == model.Destination);

            if (selectedDestination != null)
            {
                model.Fare = selectedDestination.Fare;
                model.DepartureTime = selectedDestination.DepartureTime;
                model.ArrivalTime = selectedDestination.ArrivalTime;
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Destinations = await _context.Destinations.ToListAsync();
                return View("Booking", model);
            }

            // Save to database
            var existingPassenger = await _context.Book
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.BookingId == model.BookingId && p.UserId == model.UserId);

            if (existingPassenger == null)
            {
                await _context.Book.AddAsync(model);
            }
            else
            {
                _context.Book.Update(model);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Book));
        }


        public async Task<IActionResult> DeleteBook(string id)
        {
            var userId = _userManager.GetUserId(User);

            var passengerInDb = await _context.Book
                .FirstOrDefaultAsync(p => p.BookingId == id && p.UserId == userId);

            if (passengerInDb == null)
            {
                return NotFound();
            }

            _context.Book.Remove(passengerInDb);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Book));
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
            });
        }

        [HttpGet]
        public async Task<IActionResult> Destinations()
        {
            // Fetch all destinations from the database
            var destinations = await _context.Destinations.ToListAsync();

            return View("~/Views/Admin/Destinations.cshtml", destinations);
        }

        public async Task<IActionResult> AdminHome()
        {
            var destinations = await _context.Destinations.ToListAsync();
            var bookings = await _context.Book.ToListAsync();

            var model = new Tuple<List<Destination>, List<Passenger>>(destinations, bookings);

            return View(model);
        }
    }
}
