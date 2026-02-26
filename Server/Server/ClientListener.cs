using ServerCore.Sessions;
using System.Net;
using System.Net.Sockets;

namespace Server;

internal class ClientListener
{
    Socket _listener;

    int _argsCount;

    public ClientListener(EndPoint endPoint)
    {
        _listener = new(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

        _listener.Bind(endPoint);
        _argsCount = 0;
    }

    public void Start(int argsCount = 10, int listenCount = 100)
    {
        _listener.Listen(listenCount);

        while (argsCount-- > 0)
        {
            SocketAsyncEventArgs accpArgs = new();

            accpArgs.Completed += OnAccpComplete;

            RegisterAccp(accpArgs);
        }

        Console.WriteLine($"Complete Reigst {_argsCount}");
    }

    void RegisterAccp(SocketAsyncEventArgs accpArgs)
    {
        accpArgs.AcceptSocket = null;

        try
        {
            bool pending = _listener.AcceptAsync(accpArgs);

            if (pending == false)
                OnAccpComplete(null, accpArgs);
        }
        catch(Exception e)
        {
            Console.WriteLine("RegisterAccp Failed");
            Console.WriteLine(e);
            return;
        }

        _argsCount++;
    }

    void OnAccpComplete(object? sender, SocketAsyncEventArgs accpArgs)
    {
        _argsCount--;
        if(accpArgs.SocketError != SocketError.Success)
        {
            RegisterAccp(accpArgs);
            return;
        }

        SessionPool<ServerSession>.Rent()!.Start(accpArgs.AcceptSocket!);

        RegisterAccp(accpArgs);
    }
}
