using System.Collections.Concurrent;

namespace ServerCore.Sessions;

public static class SessionPool<S> where S : Session, new()
{
    static ConcurrentStack<S> _sessionPool = new();

    public static S Rent()
    {
        if(_sessionPool.TryPop(out S? session))
        {
            session.Reset();
            return session;
        }

        return new();
    }

    public static void Return(S session)
    {
        _sessionPool.Push(session);
    }
}
