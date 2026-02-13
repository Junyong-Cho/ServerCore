using System.Net;
using System.Net.Sockets;

string hostName = Dns.GetHostName();
IPAddress address = Dns.GetHostAddresses(hostName)
    .FirstOrDefault(addr => addr.AddressFamily == AddressFamily.InterNetwork &&
    addr != IPAddress.Loopback)!;
int port = 8080;

ClientListener listener = new();

listener.Init(new IPEndPoint(address, port), () => new TestSession());

while (true)
{
    Console.Write(">> ");
    string ord = Console.ReadLine()!;

    if (ord == "exit")
    {
        Console.WriteLine("서버를 종료합니다.");
        return;
    }
}