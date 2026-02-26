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
        if (_disconnected == 1)
            return;

        _sendArgs.BufferList = _pendingList;

        try
        {
            bool pending = _socket!.SendAsync(_sendArgs);

            if (pending == false)
                OnSendComplete(null, _sendArgs);
        }
        catch(Exception e)
        {
            Console.WriteLine($"UnExpected RegisterSend Error : {_socket!.RemoteEndPoint}");
            Console.WriteLine(e);
            Disconnect();
        }
    }

    protected virtual void OnSendComplete(object? sender, SocketAsyncEventArgs sendArgs)
    {
        if (sendArgs.SocketError != SocketError.Success)
        {
            Console.WriteLine($"{sendArgs.SocketError} : {_socket!.RemoteEndPoint}");
            Disconnect();
            return;
        }

        int byteTransferred = sendArgs.BytesTransferred;

        if (byteTransferred <= 0)
        {
            Console.WriteLine($"Zero Byte Sended");
            Disconnect();
            return;
        }

        OnSend(byteTransferred);

        _pendingList.Clear();
        _sendArgs.BufferList = null;

        lock (_lock)
        {
            if (_sendingList.Count == 0)
            {
                _isSending = false;
                return;
            }

            (_sendingList, _pendingList) = (_pendingList, _sendingList);
        }

        RegisterSend();
    }
}
