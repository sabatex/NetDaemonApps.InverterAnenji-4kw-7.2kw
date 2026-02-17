// Copyright (c) 2024 Serhiy Lakas
// https://sabatex.github.io

using System;
using System.Collections.Generic;
using System.Linq;

namespace InverterAnenji;

/// <summary>
/// Медіанний фільтр для згладжування аномальних значень
/// </summary>
public class MedianFilter<T> where T : struct, IComparable<T>
{
    private readonly Queue<T> _values = new Queue<T>();
    private readonly int _windowSize;

    public MedianFilter(int windowSize = 3)
    {
        if (windowSize < 1)
            throw new ArgumentException("Window size must be at least 1", nameof(windowSize));
        
        _windowSize = windowSize;
    }

    /// <summary>
    /// Додає нове значення та повертає медіану
    /// </summary>
    public T Filter(T newValue)
    {
        _values.Enqueue(newValue);
        
        if (_values.Count > _windowSize)
            _values.Dequeue();
        
        var sorted = _values.OrderBy(x => x).ToList();
        return sorted[sorted.Count / 2];
    }

    /// <summary>
    /// Скидає фільтр до початкового стану
    /// </summary>
    public void Reset()
    {
        _values.Clear();
    }

    /// <summary>
    /// Повертає кількість збережених значень
    /// </summary>
    public int Count => _values.Count;
}

/// <summary>
/// Медіанний фільтр для float значень з додатковими функціями
/// </summary>
public class FloatMedianFilter
{
    private readonly Queue<float> _values = new Queue<float>();
    private readonly int _windowSize;

    public FloatMedianFilter(int windowSize = 3)
    {
        if (windowSize < 1)
            throw new ArgumentException("Window size must be at least 1", nameof(windowSize));
        
        _windowSize = windowSize;
    }

    public float Filter(float newValue)
    {
        _values.Enqueue(newValue);
        
        if (_values.Count > _windowSize)
            _values.Dequeue();
        
        var sorted = _values.OrderBy(x => x).ToList();
        return sorted[sorted.Count / 2];
    }

    public void Reset()
    {
        _values.Clear();
    }

    public int Count => _values.Count;
}

/// <summary>
/// Медіанний фільтр для short значень
/// </summary>
public class ShortMedianFilter
{
    private readonly Queue<short> _values = new Queue<short>();
    private readonly int _windowSize;

    public ShortMedianFilter(int windowSize = 3)
    {
        if (windowSize < 1)
            throw new ArgumentException("Window size must be at least 1", nameof(windowSize));
        
        _windowSize = windowSize;
    }

    public short Filter(short newValue)
    {
        _values.Enqueue(newValue);
        
        if (_values.Count > _windowSize)
            _values.Dequeue();
        
        var sorted = _values.OrderBy(x => x).ToList();
        return sorted[sorted.Count / 2];
    }

    public void Reset()
    {
        _values.Clear();
    }

    public int Count => _values.Count;
}