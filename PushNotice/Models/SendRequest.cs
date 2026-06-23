namespace PushNotice.Models;

public class SendRequest
{
    public int id { get; set; }
    public string text { get; set; } = string.Empty;
    public int personalNumber { get; set; }
}
