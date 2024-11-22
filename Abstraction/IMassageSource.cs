using System.Net;

namespace lesson5;

public interface IMassageSource
{
    void SendMessage(MessageUDP message, IPEndPoint endPoint);
    MessageUDP ReceiveMessage(ref IPEndPoint endPoint);

}