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

        public IActionResult Index()
        {
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
            model.User = null;

            if (!ModelState.IsValid)
            {
                return View("Booking", model);
            }

            var userId = _userManager.GetUserId(User);

            var existingPassenger = await _context.Book
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.BookingId == model.BookingId && p.UserId == userId);

            if (existingPassenger == null)
            {
                model.BookingId = _passengersService.GenerateNextBookingId();
                model.BusNo = _passengersService.GenerateNextBusNo();
                model.SeatNo = _passengersService.GenerateNextSeatNo();
                model.BookingDate = DateTime.Now;
                model.UserId = userId;

                await _context.Book.AddAsync(model);
            }
            else
            {
                model.UserId = userId;
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
    }
}
