using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TransportationBookingSystem.Models
{
    public class Passenger
    {
        [Key]
        [Required]
        public string? BookingId { get; set; }

        [Required]
        public string? Name { get; set; }

        [Required]
        public string? Departure { get; set; }

        [Required]
        public string? Arrival { get; set; }

        [Required]
        public string? BusNo { get; set; }

        [Required]
        public string? SeatNo { get; set; }

        [Required]
        public string? Status { get; set; }

        [Required]
        public DateTime BookingDate { get; set; } = DateTime.Now;

        [Required]
        public string? UserId { get; set; }

        [ForeignKey("UserId")]
        public ApplicationUser? User { get; set; }

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal Fare { get; set; }
    }
}
