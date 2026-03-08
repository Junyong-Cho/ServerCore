using System.Net.Sockets;

namespace ServerCore.Sessions;

partial class Session
{
    public virtual void Send(ArraySegment<byte> buffer)
    {
        lock (_lock)
        {
            _sendingList.Add(buffer);

            if (_isSending == true)
                return;

            _isSending = true;

            (_sendingList, _pendingList) = (_pendingList, _sendingList);
        }

        RegisterSend();
    }

    public virtual void Send(IList<ArraySegment<byte>> buffers)
    {
        lock (_lock)
        {
            _sendingList.AddRange(buffers);

            if (_isSending == true)
                return;

            _isSending = true;

            (_sendingList, _pendingList) = (_pendingList, _sendingList);
        }

        RegisterSend();
    }

    protected virtual void RegisterSend()
    {
        Interlocked.Increment(ref _refCount);

        while (true)
        {
            if (_disconnected == 1)
            {
                Release();
                return;
            }

            _sendArgs.BufferList = _pendingList;

            try
            {
                bool pending = _socket!.SendAsync(_sendArgs);

                if (pending == true)
                    return;

                OnSendComplete(null, _sendArgs);

                if (_pendingList.Count == 0)
                {
                    lock (_lock)
                    {
                        _isSending = false;
                    }
                    Release();
                    return;
                }
            }
            catch(Exception e)
            {
                LogExceptionAndDisconnectAndRelease(e);
                return;
            }
        }
    }

    protected virtual void OnSendComplete(object? sender, SocketAsyncEventArgs sendArgs)
    {
        if (sendArgs.SocketError != SocketError.Success)
        {
            LogExceptionAndDisconnectAndRelease(sendArgs.SocketError);
            return;
        }

        int byteTransferred = sendArgs.BytesTransferred;

        if (byteTransferred <= 0)
        {
            LogExceptionAndDisconnectAndRelease(null);
            return;
        }

        OnSend(byteTransferred);

        _pendingList.Clear();
        _sendArgs.BufferList = null;

        lock (_lock)
        {
            if (_sendingList.Count > 0)
                (_sendingList, _pendingList) = (_pendingList, _sendingList);
        }

        if (sender == null)
            return;

        if (_pendingList.Count > 0)
            RegisterSend();
        else
        {
            lock (_lock)
            {
                _isSending = false;
            }
        }

        Release();
    }
}
