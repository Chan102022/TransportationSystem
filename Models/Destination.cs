namespace TransportationBookingSystem.Models
{
    public class Destination
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public decimal Fare { get; set; }


        public DateTime DepartureTime { get; set; } // New property
        public DateTime ArrivalTime { get; set; }   // New property
    }
}
