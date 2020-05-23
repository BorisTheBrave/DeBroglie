using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace DeBroglie.Wfc
{
    internal class Deque<T> : IEnumerable<T>
    {
        private T[] _data;
        private int _dataLength;

        // Data is in range lo to hi, exclusive of hi
        // hi == lo if the Deque is empty
        // You may have hi < lo if we've wrapped the end of data
        private int _lo;
        private int _hi;

        public Deque(int capacity = 4)
        {
            _data = new T[capacity];
            _dataLength = capacity;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Push(T t)
        {
            var hi = _hi;
            var lo = _lo;
            _data[hi] = t;
            hi++;
            if (hi == _dataLength) hi = 0;
            _hi = hi;
            if (hi == lo)
            {
                ResizeFromFull();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Pop()
        {
            var lo = _lo;
            var hi = _hi;
            if (lo == hi)
                throw new Exception("Deque is empty");
            if (hi == 0) hi = _dataLength;
            hi--;
            _hi = hi;
            return _data[hi];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Shift(T t)
        {
            var lo = _lo;
            var hi = _hi;
            if (lo == 0) lo = _dataLength;
            lo--;
            _data[lo] = t;
            _lo = lo;
            if (hi == lo)
            {
                ResizeFromFull();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Unshift()
        {
            var lo = _lo;
            var hi = _hi;
            if (lo == hi)
                throw new Exception("Deque is empty");
            var oldLo = lo;
            lo++;
            if (lo == _dataLength) lo = 0;
            _lo = lo;
            return _data[oldLo];
        }

        public void DropFirst(int n)
        {
            var hi = _hi;
            var lo = _lo;
            if (lo <= hi)
            {
                lo += n;
                if(lo >= hi)
                {
                    // Empty
                    _lo = _hi = 0;
                }
                else
                {
                    _lo = lo;
                }
            }
            else
            {
                lo += n;
                if(lo >= _dataLength)
                {
                    lo -= _dataLength;
                    if(lo >= hi)
                    {
                        // Empty
                        _lo = _hi = 0;
                    }
                    else
                    {
                        _lo = lo;
                    }
                }
            }
        }

        public void DropLast(int n)
        {
            var hi = _hi;
            var lo = _lo;
            if (lo <= hi)
            {
                hi -= n;
                if(lo >= hi)
                {
                    // Empty
                    _lo = _hi = 0;
                }
                else
                {
                    _hi = hi;
                }
            }
            else
            {
                hi -= n;
                if(hi < 0)
                {
                    hi += _dataLength;
                    if(lo >= hi)
                    {
                        // Empty
                        _lo = _hi = 0;
                    }
                    else
                    {
                        _hi = hi;
                    }
                }
            }
        }

        public int Count
        {
            get
            {
                var c = _hi - _lo;
                if (c < 0)
                    c += _dataLength;
                return c;
            }
        }

        private void ResizeFromFull()
        {
            var dataLength = _dataLength;
            var newLength = dataLength * 2;
            var newData = new T[newLength];

            int i = _lo;
            int j = 0;
            var hi = _hi;

            do
            {
                newData[j] = _data[i];

                j++;
                i++;
                if (i == dataLength) i = 0;
            } while (i != hi);
            _data = newData;
            _dataLength = newLength;
            _lo = 0;
            _hi = j;
        }

        public IEnumerable<T> Slice(int start, int end)
        {
            var lo = _lo;
            var hi = _hi;
            var data = _data;
            var dataLength = _dataLength;
            var i = lo + start;
            var e = lo + end;

            if (start < 0)
                throw new Exception();

            if (hi >= lo)
            {
                if (e > hi)
                    throw new Exception();
            }
            else
            {
                if (e > hi + dataLength)
                    throw new Exception();
            }

            if (start >= end)
                yield break;

            if (i >= dataLength) i -= dataLength;
            if (e >= dataLength) e -= dataLength;

            do
            {
                yield return data[i];
                i++;
                if (i == dataLength) i = 0;
            } while (i != e);
        }

        public IEnumerable<T> ReverseSlice(int start, int end)
        {
            var lo = _lo;
            var hi = _hi;
            var data = _data;
            var dataLength = _dataLength;
            var i = lo + start;
            var e = lo + end;

            if (start < 0)
                throw new Exception();

            if (hi >= lo)
            {
                if (e > hi)
                    throw new Exception();
            }
            else
            {
                if (e > hi + dataLength)
                    throw new Exception();
            }

            if (start >= end)
                yield break;

            if (i >= dataLength) i -= dataLength;
            if (e >= dataLength) e -= dataLength;

            do
            {
                e--;
                yield return data[e];
                if (e == 0) e = dataLength - 1;
            } while (i != e);
        }

        public IEnumerator<T> GetEnumerator()
        {

            var lo = _lo;
            var hi = _hi;
            var data = _data;
            var dataLength = _dataLength;
            var i = lo;
            var e = hi;

            if (hi >= lo)
            {
                if (e > hi)
                    throw new Exception();
            }
            else
            {
                if (e > hi + dataLength)
                    throw new Exception();
            }

            if (lo == hi)
                yield break;

            if (i >= dataLength) i -= dataLength;
            if (e >= dataLength) e -= dataLength;

            do
            {
                yield return data[i];
                i++;
                if (i == dataLength) i = 0;
            } while (i != e);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
