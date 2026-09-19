using System.Collections.Concurrent;

namespace ServerCore.Sessions;

public static class SessionPool<S> where S : Session, new()
{
    static ConcurrentStack<S> _sessionPool = new();

    public static S Rent(Action<S>? sessionInitializer)
    {
        if (_sessionPool.TryPop(out S? session) == false)
        {
            session = new();
        }

        sessionInitializer?.Invoke(session);

        return session;
    }

    public static void Return(S session)
    {
        _sessionPool.Push(session);
    }
}
