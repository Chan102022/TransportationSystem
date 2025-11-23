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

            // AUTO-SET TODAY'S DATE FOR DISPLAY ONLY
            foreach (var d in destinations)
            {
                d.DepartureTime = DateTime.Today.AddHours(d.DepartureTime.Hour)
                                                .AddMinutes(d.DepartureTime.Minute);

                d.ArrivalTime = DateTime.Today.AddHours(d.ArrivalTime.Hour)
                                              .AddMinutes(d.ArrivalTime.Minute);
            }

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

        // BOOKING PAGE (CREATE OR EDIT)
        public async Task<IActionResult> Booking(string? id)
        {
            var userId = _userManager.GetUserId(User);

            // load raw destinations
            var destinations = await _context.Destinations.ToListAsync();

            // AUTO-SET TODAY'S DATE FOR DROPDOWN DISPLAY
            foreach (var d in destinations)
            {
                d.DepartureTime = DateTime.Today.AddHours(d.DepartureTime.Hour)
                                                .AddMinutes(d.DepartureTime.Minute);

                d.ArrivalTime = DateTime.Today.AddHours(d.ArrivalTime.Hour)
                                              .AddMinutes(d.ArrivalTime.Minute);
            }

            ViewBag.Destinations = destinations;

            // USER CLICKED DESTINATION CARD (id is the destination name or id depending on your link)
            if (!string.IsNullOrEmpty(id))
            {
                // try to match by name first, if id is numeric use Id match (safe)
                Destination selectedDestination = null;
                if (int.TryParse(id, out int parsedId))
                {
                    selectedDestination = destinations.FirstOrDefault(d => d.Id == parsedId);
                }
                if (selectedDestination == null)
                {
                    selectedDestination = destinations.FirstOrDefault(d => d.Name == id);
                }

                if (selectedDestination != null)
                {
                    var newPassenger = new Passenger
                    {
                        BookingId = _passengersService.GenerateNextBookingId(),
                        BusNo = _passengersService.GenerateNextBusNo(),
                        SeatNo = _passengersService.GenerateNextSeatNo(),
                        BookingDate = DateTime.Now,
                        UserId = userId,
                        Status = "Pending",

                        DestinationId = selectedDestination.Id,   // NEW: bind Id
                        Destination = selectedDestination.Name,
                        Fare = selectedDestination.Fare,
                        DepartureTime = selectedDestination.DepartureTime,
                        ArrivalTime = selectedDestination.ArrivalTime
                    };

                    return View(newPassenger);
                }

                // EDITING EXISTING BOOKING
                var passengerInDb = await _context.Book
                    .FirstOrDefaultAsync(p => p.BookingId == id && p.UserId == userId);

                if (passengerInDb != null)
                {
                    // ensure dropdown shows the selected schedule correctly (DestinationId is present on model)
                    return View(passengerInDb);
                }

                return NotFound();
            }

            // BLANK BOOKING
            var blankPassenger = new Passenger
            {
                BookingId = _passengersService.GenerateNextBookingId(),
                BusNo = _passengersService.GenerateNextBusNo(),
                SeatNo = _passengersService.GenerateNextSeatNo(),
                BookingDate = DateTime.Now,
                UserId = userId,
                Status = "Pending"
            };

            return View(blankPassenger);
        }

        // SAVE BOOKING
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BookingForm(Passenger model)
        {
            // Prevent duplicate booking
            var duplicate = await _context.Book
                .AnyAsync(b => b.UserId == model.UserId
                            && b.DestinationId == model.DestinationId
                            && b.Name == model.Name
                            && b.Status != "Canceled");

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

            // LOOKUP DESTINATION BY ID (if present). Do NOT overwrite the DepartureTime/ArrivalTime that came from the form.
            Destination dest = null;
            if (model.DestinationId.HasValue)
            {
                dest = await _context.Destinations.FirstOrDefaultAsync(d => d.Id == model.DestinationId.Value);
            }
            else if (!string.IsNullOrEmpty(model.Destination))
            {
                // fallback to name if legacy posts exist
                dest = await _context.Destinations.FirstOrDefaultAsync(d => d.Name == model.Destination);
            }

            if (dest != null)
            {
                model.Fare = dest.Fare;
                if (string.IsNullOrEmpty(model.Destination))
                    model.Destination = dest.Name;
                // IMPORTANT: DO NOT set model.DepartureTime = dest.DepartureTime
                // the form already provided the user-visible departure/arrival values.
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

        // RECEIPT
        public async Task<IActionResult> Receipt(string id)
        {
            var userId = _userManager.GetUserId(User);

            var booking = await _context.Book
                .FirstOrDefaultAsync(p => p.BookingId == id && p.UserId == userId);

            if (booking == null)
                return NotFound();

            return View(booking);
        }

        [HttpGet]
        public IActionResult GetPassengersByRoute(int id)
        {
            var destination = _context.Destinations.FirstOrDefault(d => d.Id == id);

            if (destination == null)
                return Json(new { error = "Destination not found" });

            string routeName = destination.Name;

            var passengers = _context.Book
                .Where(b => b.DestinationId == id &&
                            b.DepartureTime.Date == DateTime.Today)
                .Select(b => new
                {
                    name = b.Name,
                    seatNo = b.SeatNo,
                    status = b.Status
                })
                .ToList();

            return Json(passengers);
        }
    }
}
