using System.Collections.Concurrent;

namespace ServerCore.Sessions.Managements;

internal static class SessionPool<S> where S : Session, new()
{
    static ConcurrentStack<S> _sessionPool = new();

    internal static S Rent(Action<S>? sessionInitializer)
    {
        if (_sessionPool.TryPop(out S? session) == false)
        {
            session = new();
            session.ReturnPool = () => Return(session);
        }

        try
        {
            sessionInitializer?.Invoke(session);
        }
        catch
        {
            Return(session);
            throw;
        }

        return session;
    }

    public static void Return(S session)
    {
        _sessionPool.Push(session);
    }
}
