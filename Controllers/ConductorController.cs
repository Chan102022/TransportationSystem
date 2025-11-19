using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TransportationBookingSystem.Models;

[Authorize(Roles = "Conductor")]
public class ConductorController : Controller
{
    private readonly TransportationBookingSystemDbContext _context;

    public ConductorController(TransportationBookingSystemDbContext context)
    {
        _context = context;
    }

    // ✔ matches QR code
    [HttpGet]
    public async Task<IActionResult> Verify(string id)
    {
        if (string.IsNullOrEmpty(id))
            return BadRequest("Invalid QR code.");

        var booking = await _context.Book
            .FirstOrDefaultAsync(b => b.BookingId == id);

        if (booking == null)
            return NotFound("Booking not found.");

        return View(booking);
    }

    [HttpPost]
    public async Task<IActionResult> MarkAsPaid(string bookingId)
    {
        var booking = await _context.Book
            .FirstOrDefaultAsync(b => b.BookingId == bookingId);

        if (booking == null)
            return NotFound();

        booking.PaymentStatus = "Paid";
        booking.DatePaid = DateTime.Now;

        await _context.SaveChangesAsync();

        TempData["Success"] = "Payment marked as PAID.";

        return RedirectToAction("Verify", new { id = bookingId });
    }

    public async Task<IActionResult> Dashboard()
    {
        var today = DateTime.Today;

        var bookings = await _context.Book
            .Where(b => b.BookingDate.Date == today)
            .ToListAsync();

        ViewBag.TotalPassengers = bookings.Count;
        ViewBag.PaidPassengers = bookings.Count(b => b.PaymentStatus == "Paid");
        ViewBag.UnpaidPassengers = bookings.Count(b => b.PaymentStatus != "Paid");
        ViewBag.TotalEarnings = bookings
            .Where(b => b.PaymentStatus == "Paid")
            .Sum(b => b.Fare);

        return View(bookings);
    }

    // ✔ The scanner page (camera)
    public IActionResult Scanner()
    {
        return View();
    }
}
