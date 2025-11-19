using System;

namespace TransportationBookingSystem.Models
{
    public class TimeSlotSettings
    {
        public int Id { get; set; }

        public TimeSpan Slot1Start { get; set; }
        public TimeSpan Slot1End { get; set; }

        public TimeSpan Slot2Start { get; set; }
        public TimeSpan Slot2End { get; set; }

        public TimeSpan Slot3Start { get; set; }
        public TimeSpan Slot3End { get; set; }

        public TimeSpan Slot4Start { get; set; }
        public TimeSpan Slot4End { get; set; }
    }
}
