namespace PhoneNotice.Models;

public class SendRequest
{
    public int id { get; set; }
    public string text { get; set; } = string.Empty;
    public string phoneNumber { get; set; } = string.Empty;
}
