// Copyright @2021 COMCREATE. All rights reserved.

using System;
using System.Runtime.CompilerServices;

namespace UniSaga.Plugin
{
    internal class OptimizedQueue<T>
    {
        private const int MinimumGrow = 4;
        private const int GrowFactor = 200;

        private T[] _array;
        private int _head;
        private int _tail;
        private int _size;

        public OptimizedQueue(int capacity)
        {
            if (capacity < 0) throw new ArgumentOutOfRangeException(nameof(capacity));
            _array = new T[capacity];
            _head = _tail = _size = 0;
        }

        public int Count
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _size;
        }

        public T Peek()
        {
            if (_size == 0) ThrowForEmptyQueue();
            return _array[_head];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Enqueue(T item)
        {
            if (_size == _array.Length)
            {
                Grow();
            }

            _array[_tail] = item;
            MoveNext(ref _tail);
            _size++;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Dequeue()
        {
            if (_size == 0) ThrowForEmptyQueue();

            var head = _head;
            var array = _array;
            var removed = array[head];
            array[head] = default;
            MoveNext(ref _head);
            _size--;
            return removed;
        }

        private void Grow()
        {
            var newCapacity = (int)((long)_array.Length * GrowFactor / 100);
            if (newCapacity < _array.Length + MinimumGrow)
            {
                newCapacity = _array.Length + MinimumGrow;
            }

            SetCapacity(newCapacity);
        }

        private void SetCapacity(int capacity)
        {
            var newArray = new T[capacity];
            if (_size > 0)
            {
                if (_head < _tail)
                {
                    Array.Copy(_array, _head, newArray, 0, _size);
                }
                else
                {
                    Array.Copy(_array, _head, newArray, 0, _array.Length - _head);
                    Array.Copy(_array, 0, newArray, _array.Length - _head, _tail);
                }
            }

            _array = newArray;
            _head = 0;
            _tail = (_size == capacity) ? 0 : _size;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void MoveNext(ref int index)
        {
            var tmp = index + 1;
            if (tmp == _array.Length)
            {
                tmp = 0;
            }

            index = tmp;
        }

        private static void ThrowForEmptyQueue()
        {
            throw new InvalidOperationException("EmptyQueue");
        }
    }
}