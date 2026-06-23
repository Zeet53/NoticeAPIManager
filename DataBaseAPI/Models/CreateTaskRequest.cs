namespace DataBaseAPI.Models;

public class CreateTaskRequest
{
    public int UserId { get; set; }
    public string Text { get; set; }
    public string? EmailData { get; set; }
    public string? PhoneData { get; set; }
    public int? PersonalNumber { get; set; }
}
