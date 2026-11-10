using ServerCore.Buffers;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace ServerCore.Sessions;

partial class Session
{
    internal virtual void Send(SendBufferWrapper wrapper, int version)
    {
        if (TryHold() == false)
            return;
        try
        {
            lock (_lock)
            {
                if (version != _version)
                    return;
            }
            Send(wrapper);
        }
        finally
        {
            Release();
        }
    }

    protected void Send(ReadOnlySpan<byte> data)
    {
        ArraySegment<byte> segment = SendBufferHandler.Open(data.Length);

        data.CopyTo(segment);

        SendBufferWrapper wrapper = SendBufferHandler.Close(data.Length);

        Send(wrapper);
    }

    protected void Send(string data)
    {
        ArraySegment<byte> segment = SendBufferHandler.Open(Encoding.UTF8.GetMaxByteCount(data.Length));

        int len = Encoding.UTF8.GetBytes(data, segment);

        SendBufferWrapper wrapper = SendBufferHandler.Close(len);

        Send(wrapper);
    }

    protected void Send<S>(S data) where S : struct
    {
        int size = Unsafe.SizeOf<S>();

        ArraySegment<byte> segment = SendBufferHandler.Open(size);
#if NET10_0
        MemoryMarshal.Write(segment, in data);
#elif NETSTANDARD2_1
        MemoryMarshal.Write(segment, ref data);
#endif
        
        SendBufferWrapper wrapper = SendBufferHandler.Close(size);

        Send(wrapper);
    }

    protected virtual void Send(SendBufferWrapper wrapper)
    {
        if (TryHold() == false)
            return;

        try
        {
            if (wrapper.Segment.Count == 0 || _isDisconnected == 1)
                return;

            wrapper.Buffer.Hold();

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
        finally
        {
            Release();
        }
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
        if (TryHold() == false)
            return;

        while (true)
        {
            if (_isDisconnected == 1)
            {
                Release();
                return;
            }


            try
            {
                _sendArgs.BufferList = _pendingList;

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
                        }
                    }

                    if (pending == false)
                    {
                        Release();
                        return;
                    }
                }
            }
            catch (Exception e)
            {
                LogExceptionAndDisconnect(e);
                Release();
                return;
            }
        }
    }

    protected virtual void OnSendComplete(object? sender, SocketAsyncEventArgs sendArgs)
    {
        try
        {
            if (sendArgs.SocketError != SocketError.Success)
            {
                LogExceptionAndDisconnect($"OnSendComplete : {sendArgs.SocketError}");
                return;
            }

            int bytesTransferred = sendArgs.BytesTransferred;

            if (bytesTransferred <= 0)
            {
                LogExceptionAndDisconnect($"OnSendComplete BytesTransferred {bytesTransferred}\n pendingList Count : {_pendingList.Count}");
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

                if (_refBufferQueue.TryDequeue(out SendBuffer? buffer) == true)
                    buffer.Release();
                else
                {
                    LogExceptionAndDisconnect($"RefBufferQueue Error");
                    return;
                }
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
        }
        finally
        {
            if (sender != null)
                Release();
        }
    }
}