namespace VanriseBackend.Models
{
    public class PhoneNumber
    {
        public int Id { get; set; }

        public string Number { get; set; } = string.Empty;

        public int DeviceId { get; set; }
    }
}
