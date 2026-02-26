using ServerCore.Buffers;
using Client;
using System.Net;
using System.Net.Sockets;
using System.Text;

string hostName = Dns.GetHostName();
int port = 8080;
var ip = Dns.GetHostAddresses(hostName).First(addr => addr.AddressFamily == AddressFamily.InterNetwork && addr != IPAddress.Loopback);

IPEndPoint endPoint = new(ip, port);

Socket socket = new(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

socket.Connect(endPoint);

ClientSession session = new();

session.Start(socket);

string? ord;

while (session.Disconnected == 0)
{
    ord = Console.ReadLine();

    if (ord == null || ord == "exit")
    {
        session.Disconnect();
        break;
    }

    if (ord == "")
        continue;

    var segment = SendBufferHandler.Open(Encoding.UTF8.GetMaxByteCount(ord.Length));

    int len = Encoding.UTF8.GetBytes(ord, segment);

    segment = SendBufferHandler.Close(len);

    session.Send(segment);
}