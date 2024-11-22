using System.Net;

namespace lesson5;

public class Client
{
    private readonly IMassageSource _massageSource;
    private readonly IPEndPoint _iPEndPoint;
    private readonly string _name;

    public Client(IMassageSource massageSource, IPEndPoint iPEndPoint, string name)
    {
        _massageSource = massageSource;
        _iPEndPoint = iPEndPoint;
        _name = name;
    }

    public void ClientSendler ()
    {
        Registred();
        while(true)
        {
            System.Console.WriteLine("Enter message...");
            string message = Console.ReadLine();
            System.Console.WriteLine("Enter name...");
            string userName = Console.ReadLine();
            if (string.IsNullOrEmpty(userName))
             continue;
            var messageJson = new MessageUDP()
            {
                Text = message,
                FromName = _name,
                ToName = userName
            };
            _massageSource.SendMessage(messageJson, _iPEndPoint);
        }
    }

    private void Registred()
    {
        
        var messageJson = new MessageUDP()
        {
            Command = Command.Register,
            FromName = _name
        };
        _massageSource.SendMessage(messageJson, _iPEndPoint);
    }

    public void ClientListener ()
    {
        Registred();
        IPEndPoint ep = new IPEndPoint(_iPEndPoint.Address, _iPEndPoint.Port);
        while(true)
        {
            MessageUDP message = _massageSource.ReceiveMessage(ref ep);
            System.Console.WriteLine(message.ToString());
        }
    }

}