using ServerCore.Sessions;
using ServerCore.Sessions.Managements;
using System.Net;
using System.Net.Sockets;

namespace ServerCore.Listeners;

public abstract class SocketListener<S> where S : Session, new()
{
    protected Socket _listener;
    protected int _argsCount;
    protected bool _shutdown = false;
    protected Action<S>? _sessionInitializer;

    public SocketListener(EndPoint endPoint, Action<S>? sessioninitializer = null)
    {
        if (endPoint == null)
            throw new ArgumentNullException(nameof(endPoint));

        _listener = new(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

        _listener.Bind(endPoint);

        _sessionInitializer = sessioninitializer;
    }

    public abstract void Quit(object log);
    protected abstract void OnStart();
    protected abstract void OnRegister();
    protected abstract void SessionManagerInitializer(SessionManager<S> manager);

    public virtual void Start(int argsCount = 10, int listenCount = 1000)
    {
        if (argsCount <= 0)
            throw new ArgumentOutOfRangeException(nameof(argsCount), "Size must be greater than zero");
        if (listenCount <= 0)
            throw new ArgumentOutOfRangeException(nameof(listenCount), "Size must be greater than zero");

        OnStart();

        _argsCount = argsCount;

        _listener.Listen(listenCount);

        while (argsCount-- > 0)
        {
            SocketAsyncEventArgs args = new();

            args.Completed += OnAccpComplete;

            RegisterAccp(args);
        }

        OnRegister();
    }

    protected virtual void RegisterAccp(SocketAsyncEventArgs args)
    {
        while (_shutdown == false)
        {
            args.AcceptSocket = null;

            try
            {
                bool pending = _listener.AcceptAsync(args);

                if (pending == true)
                    return;

                OnAccpComplete(null, args);
            }
            catch (Exception e)
            {
                Quit(e);
                return;
            }
        }
    }

    protected virtual void OnAccpComplete(object? sender, SocketAsyncEventArgs args)
    {
        try
        {
            if (args.SocketError == SocketError.Success)
            {
                SessionManager<S> manager = new(args.AcceptSocket!, SessionManagerInitializer, _sessionInitializer);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            args.AcceptSocket?.Dispose();
        }
        finally
        {
            if (sender != null)
                RegisterAccp(args);
        }
    }
}
