using VanriseBackend.Enums;

namespace VanriseBackend.Models
{
    public class Client
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public ClientType Type { get; set; }

        public DateTime? BirthDate { get; set; }
    }
}
