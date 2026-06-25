using PhoneNotice.Models;

namespace PhoneNotice;

public class PhoneSender : IPhoneSender
{
    public void Send(PhoneMessage msg)
    {
        Console.WriteLine($"[PhoneNotice] Отправлено SMS: id={msg.id}, текст=\"{msg.Text}\", номер={msg.sendData}");
    }
}
