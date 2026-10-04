namespace VanriseBackend.Models
{
    public class PhoneNumberReservation
    {
        public int Id { get; set; }

        public int ClientId { get; set; }

        public int PhoneNumberId { get; set; }

        public DateTime BED { get; set; }

        public DateTime? EED { get; set; }
    }
}
