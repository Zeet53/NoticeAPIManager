using PushNotice.Models;

namespace PushNotice;

public class PushSender
{
    public void Send(PushMessage msg)
    {
        Console.WriteLine($"[PushNotice] Отправлено push-уведомление: id={msg.id}, текст=\"{msg.Text}\", personal_number={msg.sendData}");
    }
}
