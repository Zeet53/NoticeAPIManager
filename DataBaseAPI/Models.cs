using LinqToDB.Mapping;

namespace DataBaseAPI.Models
{
    [Table(Name = "MailTasks")]
    public class TaskTableModel
    {
        [Column(IsPrimaryKey = true, IsIdentity = true)]
        public int id { get; set; }

        [Column]
        public int user_id { get; set; }

        [Column]
        public string text { get; set; }

        [Column]
        public string? email_data { get; set; }

        [Column]
        public string? phone_data { get; set; }

        [Column]
        public int? personal_number { get; set; }

        [Column]
        public DateTime created_time { get; set; }

        [Column]
        public DateTime updated_time { get; set; }

        [Column]
        public string status { get; set; }
    }

    [Table(Name = "MailTasksАrchive")]
    public class ArchiveTaskTable
    {
        [Column(IsPrimaryKey = true, IsIdentity = true)]
        public int id { get; set; }
        [Column]
        public int user_id { get; set; }
        [Column]
        public string text { get; set; }

        [Column]
        public string? email_data { get; set; }

        [Column]
        public string? phone_data { get; set; }

        [Column]
        public int? personal_number { get; set; }

        [Column]
        public DateTime archiving_time { get; set; }
    }

    [Table(Name = "Users")]
    public class UserTableModel
    {
        [Column(IsPrimaryKey = true, IsIdentity = true)]
        public int id { get; set; }
        [Column]
        public string name { get; set; }
        [Column]
        public string password { get; set; }
    }
}
