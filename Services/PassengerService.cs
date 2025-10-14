using System;
using System.Linq;
using TransportationBookingSystem.Models;

namespace TransportationBookingSystem.Services
{
    public class PassengersService
    {
        private readonly TransportationBookingSystemDbContext _context;

        public PassengersService(TransportationBookingSystemDbContext context)
        {
            _context = context;
        }

        public string GenerateNextBusNo()
        {
            string prefix = "ABC";

            var lastBusNo = _context.Book
                .OrderByDescending(p => p.BookingId)
                .Select(p => p.BusNo)
                .FirstOrDefault();

            int numericPart = 0;

            if (!string.IsNullOrEmpty(lastBusNo) && lastBusNo.StartsWith(prefix))
            {
                string numberPart = lastBusNo.Substring(prefix.Length);
                int.TryParse(numberPart, out numericPart);
            }

            numericPart++;
            return prefix + numericPart.ToString("D2");
        }

        public string GenerateNextBookingId()
        {
            string prefix = $"B{DateTime.Now.Year}-0";

            var lastBookingId = _context.Book
                .OrderByDescending(p => p.BookingId)
                .Select(p => p.BookingId)
                .FirstOrDefault();

            int numericPart = 0;

            if (!string.IsNullOrEmpty(lastBookingId) && lastBookingId.StartsWith(prefix))
            {
                string numberPart = lastBookingId.Substring(prefix.Length);
                int.TryParse(numberPart, out numericPart);
            }

            numericPart++;
            return prefix + numericPart.ToString("D2");
        }

        public string GenerateNextSeatNo()
        {
            string prefix = "S";

            var lastSeatNo = _context.Book
                .OrderByDescending(p => p.BookingId)
                .Select(p => p.SeatNo)
                .FirstOrDefault();

            int numericPart = 0;

            if (!string.IsNullOrEmpty(lastSeatNo) && lastSeatNo.StartsWith(prefix))
            {
                string numberPart = lastSeatNo.Substring(prefix.Length);
                int.TryParse(numberPart, out numericPart);
            }

            numericPart++;
            return prefix + numericPart.ToString("D2");
        }
    }
}
