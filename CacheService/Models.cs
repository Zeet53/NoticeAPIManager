namespace CacheService.Models
{
    public class TaskModel
    {
        public int id { get; set; }
        public string text { get; set; }
        public string? email_data { get; set; }
        public string? phone_data { get; set; }
        public int? personal_number { get; set; }
        public DateTime created_time { get; set; }
        public DateTime updated_time { get; set; }
        public string status { get; set; }
    }

    public class ArchiveModel
    {
        public int id { get; set; }
        public string text { get; set; }
        public string? email_data { get; set; }
        public string? phone_data { get; set; }
        public int? personal_number { get; set; }
    }

    public class UserModel
    {
        public int id { get; set; }
        public string name { get; set; }
        public string password { get; set; }
    }
}
