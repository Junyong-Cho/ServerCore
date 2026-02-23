using System.Net;
using System.Net.Sockets;

internal class ClientListener
{
    Socket? _listenSocket;
    Func<Session>? _sessionFactory;

    public void Init(EndPoint localEndPoint, Func<Session> sessionFactory, int argsCount = 10, int listenCount = 1000)
    {
        _listenSocket = new(localEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

        _listenSocket.Bind(localEndPoint);
        _listenSocket.Listen(listenCount);

        _sessionFactory = sessionFactory;

        Console.WriteLine($"server Opened at {localEndPoint}");

        for (int i = 0; i < argsCount; i++)
        {
            SocketAsyncEventArgs args = new();

            args.Completed += OnAcceptComplete;

            RegisterAccept(args);
        }
    }

    void RegisterAccept(SocketAsyncEventArgs accpArgs)
    {
        accpArgs.AcceptSocket = null;

        try
        {
            bool pending = _listenSocket!.AcceptAsync(accpArgs);

            if (pending == false)
                OnAcceptComplete(null, accpArgs);
        }
        catch(Exception e)
        {
            Console.WriteLine("RegisterAccept Error");
            Console.WriteLine(e);
        }
    }

    void OnAcceptComplete(object? obj, SocketAsyncEventArgs accpArgs)
    {
        if (accpArgs.SocketError == SocketError.Success)
        {
            _sessionFactory?.Invoke().Start(accpArgs.AcceptSocket!);
        }

        RegisterAccept(accpArgs);
    }
}