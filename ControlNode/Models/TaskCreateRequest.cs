namespace ControlNode.Models;

public class TaskCreateRequest
{
    public int UserId { get; set; }
    public string Text { get; set; } = string.Empty;
    public string? EmailData { get; set; }
    public string? PhoneData { get; set; }
    public int? PersonalNumber { get; set; }
}
