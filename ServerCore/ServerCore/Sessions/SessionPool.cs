using System.Collections.Concurrent;

namespace ServerCore.Sessions;

public static class SessionPool<S> where S : Session, new()
{
    static ConcurrentBag<S> _sessionPool = new();

    public static S? Rent()
    {
        if(_sessionPool.TryTake(out S? session))
        {
            session.Reset();
            return session;
        }

        return new();
    }

    public static void Return(S session)
    {
        _sessionPool.Add(session);
    }
}
