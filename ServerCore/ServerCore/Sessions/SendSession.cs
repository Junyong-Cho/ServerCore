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

                bool isContinue = true;

                if (_pendingList.Count > 0)
                    continue;

                lock (_lock)
                {
                    if (_sendingList.Count > 0)
                        (_sendingList, _pendingList) = (_pendingList, _sendingList);
                    else
                    {
                        _isSending = false;
                        isContinue = false;
                    }
                }

                if (isContinue == false)
                {
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

        try
        {
            OnSend(byteTransferred);
        }
        catch(Exception e)
        {
            Console.WriteLine("OnSend Error");
            LogExceptionAndDisconnectAndRelease(e);
            return;
        }

        for (int i = 0; i < _pendingList.Count; i++)
        {
            ArraySegment<byte> seg = _pendingList[i];

            if (byteTransferred < seg.Count)
            {
                _remainList.Add(seg.Slice(byteTransferred));

                while (++i < _pendingList.Count)
                    _remainList.Add(_pendingList[i]);

                break;
            }

            byteTransferred -= seg.Count;
        }

        _pendingList.Clear();

        if (_remainList.Count > 0)
            (_pendingList, _remainList) = (_remainList, _pendingList);
        _sendArgs.BufferList = null;
        
        if (sender == null)
            return;

        bool isContinue = true;

        if(_pendingList.Count==0)
        {
            lock (_lock)
            {
                if (_sendingList.Count > 0)
                    (_sendingList, _pendingList) = (_pendingList, _sendingList);
                else
                {
                    _isSending = false;
                    isContinue = false;
                }
            }
        }

        if (isContinue == false)
        {
            Release();
            return;
        }

        RegisterSend();
        Release();
    }
}