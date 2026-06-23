namespace EmailService.Models;

public class SendRequest
{
    public int id { get; set; }
    public string text { get; set; } = string.Empty;
    public string receiver { get; set; } = string.Empty;
}
