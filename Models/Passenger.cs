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

        public int? DestinationId { get; set; }     // NEW FIELD

        [Required]
        public string Destination { get; set; }

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

        public DateTime DepartureTime { get; set; }
        public DateTime ArrivalTime { get; set; }

        public string? QRCodeImage { get; set; }
        public string PaymentStatus { get; set; } = "Unpaid";
        public DateTime? DatePaid { get; set; }
    }

}
