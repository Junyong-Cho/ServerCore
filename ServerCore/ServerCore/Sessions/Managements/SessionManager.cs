using ServerCore.Buffers;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace ServerCore.Sessions.Managements
{
    public class SessionManager<S> where S : Session, new()
    {
        readonly S _session;
        readonly int _version;
        readonly Action<SessionManager<S>>? _managerInitializer;

        public SessionManager(Socket socket, Action<SessionManager<S>>? managerInitializer = null, Action<S>? sessionInitializer = null)
        {
            _managerInitializer = managerInitializer;
            _session = SessionPool<S>.Rent(sessionInitializer);
            _session.Start(socket, out _version, Init);
        }

        void Init()
        {
            _managerInitializer?.Invoke(this);
        }
        public void RegisterOnConnect(Action? callback)
        {
            _session.RegisterOnConnect(callback, _version);
        }

        public void RegisterOnDisconnect(Action? callback)
        {
            _session.RegisterOnDisconnect(callback, _version);
        }

        public bool TryConfigureSession<TState>(TState state, Func<S, TState, bool> configure)
        {
            if (configure == null)
                throw new ArgumentNullException(nameof(configure));

            return _session.TryConfigureSession(_version, state, configure);
        }

        public void Send(ReadOnlySpan<byte> data)
        {
            ArraySegment<byte> segment = SendBufferHandler.Open(data.Length);

            data.CopyTo(segment);

            SendBufferWrapper wrapper = SendBufferHandler.Close(data.Length);

            Send(wrapper);
        }

        public void Send(string data)
        {
            ArraySegment<byte> segment = SendBufferHandler.Open(Encoding.UTF8.GetMaxByteCount(data.Length));

            int len = Encoding.UTF8.GetBytes(data, segment);

            SendBufferWrapper wrapper = SendBufferHandler.Close(len);

            Send(wrapper);
        }

        public void Send<ST>(ST data) where ST : struct
        {
            int size = Unsafe.SizeOf<ST>();

            ArraySegment<byte> segment = SendBufferHandler.Open(size);

#if NET10_0
            MemoryMarshal.Write(segment, in data);
#elif NETSTANDARD2_1
            MemoryMarshal.Write(segment, ref data);
#endif

            SendBufferWrapper wrapper = SendBufferHandler.Close(size);

            Send(wrapper);
        }

        public void Send(SendBufferWrapper wrapper)
        {
            _session.Send(wrapper, _version);
        }

        public virtual void Disconnect()
        {
            _session.Disconnect(_version);
        }
    }
}
