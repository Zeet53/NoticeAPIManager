using PhoneNotice.Models;

namespace PhoneNotice;

public interface IPhoneSender
{
    void Send(PhoneMessage msg);
}
