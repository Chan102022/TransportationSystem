using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TransportationBookingSystem.Models;

namespace TransportationBookingSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class TimeSettingsController : Controller
    {
        private readonly TransportationBookingSystemDbContext _context;

        public TimeSettingsController(TransportationBookingSystemDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var settings = await _context.TimeSlots.FirstOrDefaultAsync();

            // If no settings exist, create default
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
                    Slot4End = new TimeSpan(18, 0, 0),
                };

                _context.TimeSlots.Add(settings);
                await _context.SaveChangesAsync();
            }

            return View(settings);
        }

        [HttpPost]
        public async Task<IActionResult> Save(TimeSlotSettings model)
        {
            var settings = await _context.TimeSlots.FirstAsync();

            settings.Slot1Start = model.Slot1Start;
            settings.Slot1End = model.Slot1End;

            settings.Slot2Start = model.Slot2Start;
            settings.Slot2End = model.Slot2End;

            settings.Slot3Start = model.Slot3Start;
            settings.Slot3End = model.Slot3End;

            settings.Slot4Start = model.Slot4Start;
            settings.Slot4End = model.Slot4End;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Time settings updated successfully!";
            return RedirectToAction("Index");
        }
    }
}
