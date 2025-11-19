
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace TransportationBookingSystem.Models
{
    public class TransportationBookingSystemDbContext : IdentityDbContext<ApplicationUser>
    {
        public DbSet<Passenger> Book { get; set; }
        public DbSet<Destination> Destinations { get; set; }

        public DbSet<TimeSlotSettings> TimeSlots { get; set; }



        public TransportationBookingSystemDbContext(DbContextOptions<TransportationBookingSystemDbContext> options)
            : base(options)
        {
        }
    }
}
