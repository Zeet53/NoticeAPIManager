using PushNotice.Models;

namespace PushNotice;

public interface IPushSender
{
    void Send(PushMessage msg);
}
