// Copyright @2021 COMCREATE. All rights reserved.

using System;
using System.Collections;
using System.Collections.Generic;

namespace UniSaga.Plugin
{
    internal class CapacityArray<T> : IEnumerable<T>
    {
        private const int MaxArrayLength = 0x7FEFFFFF;
        private const int InitialSize = 16;
        private int _listCount;
        private T[] _list;

        public CapacityArray()
        {
            _list = new T[InitialSize];
        }

        public void Add(T item)
        {
            if (_list.Length == _listCount)
            {
                var newLength = _listCount * 2;
                if ((uint)newLength > MaxArrayLength) newLength = MaxArrayLength;
                var newArray = new T[newLength];
                Array.Copy(_list, newArray, _listCount);
                _list = newArray;
            }

            _list[_listCount] = item;
            _listCount++;
        }

        public void Clear()
        {
            _listCount = 0;
        }

        public void Swap(CapacityArray<T> capacityArray)
        {
            var listCount = _listCount;
            var list = _list;
            _listCount = capacityArray._listCount;
            _list = capacityArray._list;
            capacityArray._listCount = listCount;
            capacityArray._list = list;
        }

        public IEnumerator<T> GetEnumerator()
        {
            for (var i = 0; i < _listCount; i++)
            {
                yield return _list[i];
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}