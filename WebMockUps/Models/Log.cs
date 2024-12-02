using System.ComponentModel.DataAnnotations.Schema;

namespace WebMockUps.Models
{
    [Table("Log")]
    public class Log
    {
        public int LogId { get; set; }
        public string Email { get; set; }
        public string Message { get; set; }
        public DateTime LogTime { get; set; }
        public string EventType { get; set; }
    }
}
