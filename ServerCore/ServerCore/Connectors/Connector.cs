using System.Net;
using System.Net.Sockets;
using ServerCore.Sessions;

namespace ServerCore.Connectors;

public abstract class Connector<S> where S : Session, new()
{
    EndPoint _endPoint;

    public Connector(EndPoint endPoint)
    {
        _endPoint = endPoint;
    }

    protected abstract void OnConnect(S session);

    public virtual void Connect()
    {
        Socket socket = new(_endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        SocketAsyncEventArgs args = new();
        args.Completed += OnConnectComplete;
        args.RemoteEndPoint = _endPoint;

        try
        {
            bool pending = socket.ConnectAsync(args);

            if (pending == false)
                OnConnectComplete(null, args);
        }
        catch(Exception e)
        {
            Console.WriteLine("Connect Failed");
            Console.WriteLine(e);
        }
    }

    protected virtual void OnConnectComplete(object? sender, SocketAsyncEventArgs args)
    {
        if (args.SocketError == SocketError.Success)
        {
            S session = new();
            session.Start(args.ConnectSocket!);

            OnConnect(session);
        }
        else
        {
            Console.WriteLine("Connect Failed");
            Console.WriteLine(args.SocketError);
            Connect();
        }
    }
}
