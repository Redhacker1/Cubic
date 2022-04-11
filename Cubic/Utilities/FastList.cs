using System;

namespace Cubic.Utilities;

// Inspired from https://github.com/prime31/Nez/blob/master/Nez.Portable/Utils/Collections/FastList.cs

/// <summary>
/// Represents a List that sacrifices memory and usability for performance.
/// </summary>
/// <typeparam name="T">The type that this <see cref="FastList{T}"/> will contain.</typeparam>
public struct FastList<T>
{
    /// <summary>
    /// The array of items stored in this list. <b>When iterating through the list, use <see cref="_length"/> instead of
    /// Items.Length</b>
    /// </summary>
    public T[] _items;

    private int _maxLength;
    public int MaxLength => _maxLength;

    /// <summary>
    /// The length of the list. <b>Do not adjust this value.</b>
    /// </summary>
    public int _length;

    public int Length => _length;

    public T this[int index] => _items[index];

    public FastList()
    {
        _items = new T[10];
        _maxLength = 10;
        _length = 0;
    }

    public FastList(int size)
    {
        _items = new T[size];
        _maxLength = size;
        _length = 0;
    }

    public void Add(T item)
    {
        _items[_length] = item;
        _length++;
    }

    public void AddWithResize(T item)
    {
        if (_length >= MaxLength)
        {
            _maxLength <<= 1;
            Array.Resize(ref _items, _maxLength);
        }
        
        _items[_length] = item;
        _length++;
    }

    public void Clear()
    {
        _length = 0;
        Array.Clear(_items);
    }

    public void RemoveAt(int index)
    {
        Array.Copy(_items, index + 1, _items, index, _length - index);
        _items[_length] = default;
        _length--;
    }
}