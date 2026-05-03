using ServerCore.Buffers;
using System.Net.Sockets;

namespace ServerCore.Sessions;

partial class Session
{
    public virtual void Send(SendBufferWrapper wrapper)
    {
        if (wrapper.Segment.Count == 0)
            return;

        wrapper.Buffer.IncreaseReference();

        lock (_lock)
        {
            _sendingList.Add(wrapper.Segment);
            _refBufferQueue.Enqueue(wrapper.Buffer);

            if (_isSending == true)
                return;

            _isSending = true;
            (_sendingList, _pendingList) = (_pendingList, _sendingList);
        }

        RegisterSend();
    }

    //public virtual void Send(ArraySegment<byte> buffer)
    //{
    //    if (buffer.Count == 0)
    //        return;
        
    //    lock (_lock)
    //    {
    //        _sendingList.Add(buffer);

    //        if (_isSending == true)
    //            return;

    //        _isSending = true;

    //        (_sendingList, _pendingList) = (_pendingList, _sendingList);
    //    }

    //    RegisterSend();
    //}

    //public virtual void Send(IList<ArraySegment<byte>> buffers)
    //{
    //    lock (_lock)
    //    {
    //        _sendingList.AddRange(buffers);

    //        if (_isSending == true)
    //            return;

    //        _isSending = true;

    //        (_sendingList, _pendingList) = (_pendingList, _sendingList);
    //    }

    //    RegisterSend();
    //}

    protected virtual void RegisterSend()
    {   
        Interlocked.Increment(ref _refCount);

        while (true)
        {
            if (_isDisconnected == 1)
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

                if (_remainList.Count > 0)
                {
                    (_pendingList, _remainList) = (_remainList, _pendingList);
                }
                else
                {
                    pending = true;
                    lock (_lock)
                    {
                        if (_sendingList.Count > 0)
                            (_pendingList, _sendingList) = (_sendingList, _pendingList);
                        else
                        {
                            _isSending = false;
                            pending = false;
                            break;
                        }
                    }

                    if (pending == false)
                    {
                        Release();
                        return;
                    }
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
            LogExceptionAndDisconnectAndRelease($"OnSendComplete : {sendArgs.SocketError}");
            return;
        }

        int bytesTransferred = sendArgs.BytesTransferred;

        if (bytesTransferred <= 0)
        {
            LogExceptionAndDisconnectAndRelease($"OnSendComplete BytesTransferred {bytesTransferred}\n pendingList Count : {_pendingList.Count}");
            return;
        }

        for (int i = 0; i < _pendingList.Count; i++)
        {
            ArraySegment<byte> seg = _pendingList[i];

            if (bytesTransferred < seg.Count)
            {
                _remainList.Add(seg.Slice(bytesTransferred));

                while (++i < _pendingList.Count)
                    _remainList.Add(_pendingList[i]);

                break;
            }

            _refBufferQueue.Dequeue().DecreaseReference();
            bytesTransferred -= seg.Count;
        }

        _pendingList.Clear();

        if (sender == null)
            return;

        bool pending = true;

        if (_remainList.Count > 0)
        {
            (_pendingList, _remainList) = (_remainList, _pendingList);
        }
        else
        {
            lock (_lock)
            {
                if (_sendingList.Count > 0)
                    (_pendingList, _sendingList) = (_sendingList, _pendingList);
                else
                {
                    _isSending = false;
                    pending = false;
                }
            }
        }

        if (pending == true)
            RegisterSend();
        Release();
    }
}