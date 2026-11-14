namespace ServerCore.Sessions.Managements
{
    internal static class SessionIdGenerator
    {
        static long _lastSessionId;

        internal static long Next()
        {
            long sessionId = Interlocked.Increment(ref _lastSessionId);
            return sessionId;
        }
    }
}
