using System.Collections;
using System.Runtime.CompilerServices;

namespace KIlian.Shared.Collections;

public class MaxCapacityList<T>(int maxCapacity, IEnumerable<T> items) : ICollection<T>
{
    public MaxCapacityList(int maxCapacity) : this(maxCapacity, [])
    {
    }
    
    private readonly IList _list = ArrayList.Synchronized(items.TakeLast(maxCapacity).ToList());

    public IEnumerator<T> GetEnumerator() => _list.Cast<T>().GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public void Add(T item)
    {
        _list.Add(item);
        for (var i = 0; i < _list.Count - maxCapacity; i++)
        {
            _list.RemoveAt(i);
        }
    }

    public void Clear() => _list.Clear();

    public bool Contains(T item) => _list.Contains(item);

    public void CopyTo(T[] array, int arrayIndex) => _list.CopyTo(array, arrayIndex);

    public bool Remove(T item)
    {
        var success = _list.Contains(item);
        _list.Remove(item);
        return success;
    }

    public int Count => _list.Count;

    public bool IsReadOnly => _list.IsReadOnly;
}