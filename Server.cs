using System.Net;
using System.Net.Sockets;
using System.Text;

namespace lesson5;

public class Server
{
    private readonly Dictionary<string, IPEndPoint> clients = new Dictionary<string, IPEndPoint>();
    private UdpClient udpClient;

    public void Register(MessageUDP message, IPEndPoint fromep)
    {
        System.Console.WriteLine($"Message Register, name = {message.FromName}");
        clients.Add(message.FromName, fromep);
        
        using(var ctx = new Context())
        {
            if (ctx.Users.FirstOrDefault(x => x.Name == message.FromName) != null) return;

            ctx.Add(new User { Name = message.FromName });

            ctx.SaveChanges();
        }
    }

    public void ConfimMessageReceived(int? id)
    {
        System.Console.WriteLine($"Message confirmation id = {id}");

        using (var ctx = new Context())
        {
            var msg = ctx.Messages.FirstOrDefault(x => x.Id == id);

            if (msg != null)
            {
                msg.Received = true;
                ctx.SaveChanges();
            }
        }
    }

    public void RelayMessage(MessageUDP message)
    {
        int? id = null;

        if (clients.TryGetValue(message.ToName, out IPEndPoint ep))
        {
            using (var ctx = new Context())
            {
                var fromUser = ctx.Users.First(x => x.Name == message.FromName);
                var toUser = ctx.Users.First(x => x.Name == message.ToName);
                var msg = new Message { FromUser = fromUser, ToUser = toUser, Received = false, Text = message.Text };
                ctx.Messages.Add(msg);

                ctx.SaveChanges();

                id = msg.Id;
            }

            var forwardMessagwJson = new MessageUDP { Id = id, Command = Command.Message, ToName = message.ToName, 
            FromName = message.FromName, Text = message.Text }.ToJson();

            byte[] forwardBytes = Encoding.ASCII.GetBytes(forwardMessagwJson);

            udpClient.Send(forwardBytes, forwardBytes.Length, ep);

            System.Console.WriteLine($"Message Relayed, from = {message.FromName} to = {message.ToName}");
        }
        else
        {
            System.Console.WriteLine("User not found");
        }
    }

    public void ProcessMessage(MessageUDP message, IPEndPoint fromep)
    {
        System.Console.WriteLine($"Received message from {message.FromName} for {message.ToName} with command {message.Command}");
        System.Console.WriteLine(message.Text);

        if (message.Command == Command.Register)
        {
            Register(message, fromep);
        }
        else if (message.Command == Command.Confimation)
        {
            System.Console.WriteLine("Confirmation receiver");
            ConfimMessageReceived(message.Id);
        }
        else if (message.Command == Command.Message)
        {
            RelayMessage(message);
        }
    }

    void GetUnreadMessages(string userName)
    {
        if (clients.TryGetValue(userName, out IPEndPoint ep))
        {
            using (var ctx = new Context())
            {
                var user = ctx.Users.FirstOrDefault(x => x.Name == userName);
                if (user != null)
                {
                    var unreadMessages = user.ToMessages.Where(msg => msg.Received).Select(msg => msg.Text).ToList();

                    var unreadMessagesJson = new MessageUDP
                    {
                        Command = Command.GetUnreadMessages,
                        FromName = "Server",
                        UnreadMessages = unreadMessages
                    };

                    byte[] unreadBytes = Encoding.ASCII.GetBytes(unreadMessagesJson.ToJson());
                    udpClient.Send(unreadBytes, unreadBytes.Length, ep);
                    System.Console.WriteLine($"Unread messages sent to {userName}");
                }
            }
        }
        else
        {
            System.Console.WriteLine($"User {userName} not found");
        }
    }

    public void Work()
    {
        IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
        udpClient = new UdpClient(5438);

        System.Console.WriteLine("UDP Client wait messages... ");

        while (true)
        {
            System.Console.WriteLine("Waiting message... ");
            byte[] receiveBytes = udpClient.Receive(ref remoteEndPoint);
            string receiveData = Encoding.ASCII.GetString(receiveBytes);

            System.Console.WriteLine(receiveData);

            try
            {
                var message = MessageUDP.FromJson(receiveData);

                ProcessMessage(message, remoteEndPoint);
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Error processing message {ex.Message}");
            }
        }
    }

}