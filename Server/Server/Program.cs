using Server;
using System.Net;
using System.Net.Sockets;

string hostName = Dns.GetHostName();
var ip = Dns.GetHostAddresses(hostName).First(addr => addr.AddressFamily == AddressFamily.InterNetwork && addr != IPAddress.Loopback);
int port = 8080;

IPEndPoint endPoint = new(ip, port);

ClientListener listener = new(endPoint);

listener.Start(listenCount: 2001);

string? ord;

Console.WriteLine($"Server Open {endPoint}");

//var ConsoleOut = Console.Out;

//Console.SetOut(TextWriter.Null); // 스트레스 테스트시 출력 방지

while (true)
{
    ord = Console.ReadLine();

    if (ord == "exit")
        return;
}