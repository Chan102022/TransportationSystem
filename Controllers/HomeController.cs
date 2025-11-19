using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TransportationBookingSystem.Models;
using TransportationBookingSystem.Services;
using QRCoder;
using System.Drawing;
using System.Drawing.Imaging;

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

        // HOME PAGE
        public async Task<IActionResult> Index()
        {
            var destinations = await _context.Destinations.ToListAsync();
            ViewBag.Destinations = destinations;
            return View();
        }

        // USER BOOKINGS
        public async Task<IActionResult> Book()
        {
            var userId = _userManager.GetUserId(User);
            var bookings = await _context.Book
                .Where(b => b.UserId == userId)
                .ToListAsync();

            return View(bookings);
        }

        // BOOKING PAGE (CREATES or EDITS BOOKING)
        public async Task<IActionResult> Booking(string? id)
        {
            var userId = _userManager.GetUserId(User);

            var destinations = await _context.Destinations.ToListAsync();
            ViewBag.Destinations = destinations;

            // ============================
            // 1️⃣ USER CLICKED A DESTINATION CARD
            // ============================
            if (!string.IsNullOrEmpty(id))
            {
                var selectedDestination = await _context.Destinations
                    .FirstOrDefaultAsync(d => d.Name == id);

                if (selectedDestination != null)
                {
                    var newPassenger = new Passenger
                    {
                        BookingId = _passengersService.GenerateNextBookingId(),
                        BusNo = _passengersService.GenerateNextBusNo(),
                        SeatNo = _passengersService.GenerateNextSeatNo(),
                        BookingDate = DateTime.Now,
                        UserId = userId,

                        // Auto-fill details from clicked card
                        Destination = selectedDestination.Name,
                        Fare = selectedDestination.Fare,
                        DepartureTime = selectedDestination.DepartureTime,
                        ArrivalTime = selectedDestination.ArrivalTime
                    };

                    return View(newPassenger);
                }

                // ============================
                // 2️⃣ USER IS EDITING AN EXISTING BOOKING
                // ============================
                var passengerInDb = await _context.Book
                    .FirstOrDefaultAsync(p => p.BookingId == id && p.UserId == userId);

                if (passengerInDb != null)
                    return View(passengerInDb);

                return NotFound();
            }

            // Default new booking
            var blankPassenger = new Passenger
            {
                BookingId = _passengersService.GenerateNextBookingId(),
                BusNo = _passengersService.GenerateNextBusNo(),
                SeatNo = _passengersService.GenerateNextSeatNo(),
                BookingDate = DateTime.Now,
                UserId = userId
            };

            return View(blankPassenger);
        }

        // SAVE BOOKING
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BookingForm(Passenger model)
        {// 🚫 Prevent duplicate booking for same user / same destination / same name
            var duplicate = await _context.Book
                .AnyAsync(b => b.UserId == model.UserId
                            && b.Destination == model.Destination
                            && b.Name == model.Name
                            && b.Status != "Canceled"); // You can edit this rule

            if (duplicate)
            {
                ModelState.AddModelError("", "You already have a booking for this destination.");
                ViewBag.Destinations = await _context.Destinations.ToListAsync();
                return View("Booking", model);
            }

            model.BookingId ??= _passengersService.GenerateNextBookingId();
            model.BusNo ??= _passengersService.GenerateNextBusNo();
            model.SeatNo ??= _passengersService.GenerateNextSeatNo();
            model.BookingDate = DateTime.Now;
            model.UserId ??= _userManager.GetUserId(User);
            model.Status ??= "Pending";

            // Fill destination data
            var dest = await _context.Destinations.FirstOrDefaultAsync(d => d.Name == model.Destination);
            if (dest != null)
            {
                model.Fare = dest.Fare;
                model.DepartureTime = dest.DepartureTime;
                model.ArrivalTime = dest.ArrivalTime;
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Destinations = await _context.Destinations.ToListAsync();
                return View("Booking", model);
            }

            var exists = await _context.Book
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.BookingId == model.BookingId);

            // NEW BOOKING
            if (exists == null)
            {
                string baseUrl = $"{Request.Scheme}://{Request.Host}";
                string verifyUrl = $"{baseUrl}/Conductor/Verify?id={model.BookingId}";

                model.QRCodeImage = GenerateQrCodeBase64(verifyUrl);
                model.PaymentStatus = "Unpaid";
                model.DatePaid = null;

                await _context.Book.AddAsync(model);
            }
            else
            {
                _context.Book.Update(model);
            }

            await _context.SaveChangesAsync();

            return RedirectToAction("Receipt", new { id = model.BookingId });
        }

        // DELETE BOOKING
        public async Task<IActionResult> DeleteBook(string id)
        {
            var userId = _userManager.GetUserId(User);

            var passengerInDb = await _context.Book
                .FirstOrDefaultAsync(p => p.BookingId == id && p.UserId == userId);

            if (passengerInDb == null)
                return NotFound();

            _context.Book.Remove(passengerInDb);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Book));
        }

        // QR GENERATION
        private string GenerateQrCodeBase64(string text)
        {
            using (var qrGenerator = new QRCodeGenerator())
            using (var qrData = qrGenerator.CreateQrCode(text, QRCodeGenerator.ECCLevel.Q))
            using (var qrCode = new QRCode(qrData))
            using (var bitmap = qrCode.GetGraphic(20))
            using (var ms = new MemoryStream())
            {
                bitmap.Save(ms, ImageFormat.Png);
                return "data:image/png;base64," + Convert.ToBase64String(ms.ToArray());
            }
        }

        // RECEIPT PAGE
        public async Task<IActionResult> Receipt(string id)
        {
            var userId = _userManager.GetUserId(User);

            var booking = await _context.Book
                .FirstOrDefaultAsync(p => p.BookingId == id && p.UserId == userId);

            if (booking == null)
                return NotFound();

            return View(booking);
        }
    }
}
