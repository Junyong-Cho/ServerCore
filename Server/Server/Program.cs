using Server;
using System.Net;
using System.Net.Sockets;

string hostName = Dns.GetHostName();
var ip = Dns.GetHostAddresses(hostName).First(addr => addr.AddressFamily == AddressFamily.InterNetwork && addr != IPAddress.Loopback);
int port = 8080;

IPEndPoint endPoint = new(ip, port);

ClientListener listener = new(endPoint);

listener.Start();

string? ord;

Console.WriteLine($"Server Open {endPoint}");

while (true)
{
    ord = Console.ReadLine();

    if (ord == "exit")
    {
        return;
    }
}