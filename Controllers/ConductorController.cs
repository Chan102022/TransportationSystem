using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TransportationBookingSystem.Models;

public class ConductorController : Controller
{
    private readonly TransportationBookingSystemDbContext _context;

    public ConductorController(TransportationBookingSystemDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> VerifyBooking(string bookingId)
    {
        if (bookingId == null) return BadRequest();

        var booking = await _context.Book
            .FirstOrDefaultAsync(b => b.BookingId == bookingId);

        if (booking == null) return NotFound();

        return View(booking);
    }

    [HttpPost]
    public async Task<IActionResult> MarkPaid(string bookingId)
    {
        var booking = await _context.Book
            .FirstOrDefaultAsync(b => b.BookingId == bookingId);

        if (booking == null) return NotFound();

        booking.PaymentStatus = "Paid";
        booking.DatePaid = DateTime.Now;

        await _context.SaveChangesAsync();

        TempData["Success"] = "Payment marked as PAID.";
        return RedirectToAction("VerifyBooking", new { bookingId });
    }
}
