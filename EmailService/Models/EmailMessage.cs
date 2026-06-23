namespace EmailService.Models;

public class EmailMessage
{
    public int id { get; set; }
    public string Text { get; set; } = string.Empty;
    public string sendData { get; set; } = string.Empty;
}
