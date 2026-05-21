namespace PhoneNotice;

public static class PhoneSender
{
    public static void Send(Message msg)
    {
        Console.WriteLine($"[PhoneNotice] Отправлено SMS: id={msg.id}, текст=\"{msg.Text}\", номер={msg.sendData}");
    }
}

public class Message
{
    public int id { get; set; }
    public string Text { get; set; } = string.Empty;
    public string sendData { get; set; } = string.Empty;
}
