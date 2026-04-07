using System.Collections;

namespace ServerCore.Sessions;

public class SessionList<T> : IList<T> where T : struct
{
    T[] _list;

    int count;
    public SessionList(int bufferSize)
    {
        _list = new T[bufferSize];
        count = 0;
    }

    public T this[int index] { get => _list[index]; set => _list[index] = value; }

    public int Count => count;

    [Obsolete("사용 금지", true)]
    public bool IsReadOnly => throw new NotImplementedException();

    public void Add(T item)
    {
        if (count == _list.Length)
        {
            T[] tList = new T[_list.Length << 1];
            _list.AsSpan().CopyTo(tList.AsSpan());

            _list = tList;
        }

        _list[count++] = item;
    }

    public void Clear()
    {
        count = 0;
    }

    [Obsolete("사용 금지", true)]
    public bool Contains(T item)
    {
        throw new NotImplementedException();
    }
    [Obsolete("사용 금지", true)]
    public void CopyTo(T[] array, int arrayIndex)
    {
        throw new NotImplementedException();
    }
    [Obsolete("사용 금지", true)]
    public IEnumerator<T> GetEnumerator()
    {
        throw new NotImplementedException();
    }
    [Obsolete("사용 금지", true)]
    public int IndexOf(T item)
    {
        throw new NotImplementedException();
    }
    [Obsolete("사용 금지", true)]
    public void Insert(int index, T item)
    {
        throw new NotImplementedException();
    }
    [Obsolete("사용 금지", true)]
    public bool Remove(T item)
    {
        throw new NotImplementedException();
    }
    [Obsolete("사용 금지", true)]
    public void RemoveAt(int index)
    {
        throw new NotImplementedException();
    }
    [Obsolete("사용 금지", true)]
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
