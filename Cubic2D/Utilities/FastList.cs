using System;
using System.Collections;

namespace Cubic2D.Utilities;

// Inspired from https://github.com/prime31/Nez/blob/master/Nez.Portable/Utils/Collections/FastList.cs

/// <summary>
/// Represents a List that sacrifices memory and usability for performance.
/// </summary>
/// <typeparam name="T"></typeparam>
public struct FastList<T>
{
    /// <summary>
    /// The array of items stored in this list. <b>When iterating through the list, use <see cref="Length"/> instead of
    /// Items.Length</b>
    /// </summary>
    public T[] Items;

    private int _maxLength;

    /// <summary>
    /// The length of the list. <b>Do not adjust this value.</b>
    /// </summary>
    public int Length;

    public FastList(int initialSize)
    {
        Items = new T[initialSize];
        _maxLength = initialSize;
        Length = 0;
    }

    public FastList() : this(10) { }

    public void Add(T item)
    {
        if (Length >= _maxLength)
        {
            _maxLength = Length << 1;
            Array.Resize(ref Items, _maxLength);
        }

        Items[Length] = item;
        Length++;
    }
}