namespace PushNotice;

public static class PushSender
{
    public static void Send(PushMessage msg)
    {
        Console.WriteLine($"[PushNotice] Отправлено push-уведомление: id={msg.id}, текст=\"{msg.Text}\", personal_number={msg.sendData}");
    }
}

public class PushMessage
{
    public int id { get; set; }
    public string Text { get; set; } = string.Empty;
    public string sendData { get; set; } = string.Empty;
}
