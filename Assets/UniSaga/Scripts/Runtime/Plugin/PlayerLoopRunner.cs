using System;
using UnityEngine;

namespace UniSaga.Plugin
{
    internal sealed class PlayerLoopRunner
    {
        private const int InitialSize = 16;
        private int _tail;
        private bool _running;
        private readonly object _runningAndQueueLock = new object();
        private readonly object _arrayLock = new object();
        private IPlayerLoopItem[] _loopItems = new IPlayerLoopItem[InitialSize];
        private readonly OptimizedQueue<IPlayerLoopItem> _waitQueue = new OptimizedQueue<IPlayerLoopItem>(InitialSize);

        public void AddAction(IPlayerLoopItem item)
        {
            lock (_runningAndQueueLock)
            {
                if (_running)
                {
                    _waitQueue.Enqueue(item);
                    return;
                }
            }

            lock (_arrayLock)
            {
                // Ensure Capacity
                if (_loopItems.Length == _tail)
                {
                    Array.Resize(ref _loopItems, checked(_tail * 2));
                }

                _loopItems[_tail++] = item;
            }
        }

        public void Run()
        {
            RunCore();

            void RunCore()
            {
                lock (_runningAndQueueLock)
                {
                    _running = true;
                }

                lock (_arrayLock)
                {
                    var j = _tail - 1;

                    for (var i = 0; i < _loopItems.Length; i++)
                    {
                        var action = _loopItems[i];
                        if (action != null)
                        {
                            try
                            {
                                if (!action.MoveNext())
                                {
                                    _loopItems[i] = null;
                                }
                                else
                                {
                                    continue; // next i 
                                }
                            }
                            catch (Exception ex)
                            {
                                _loopItems[i] = null;
                                try
                                {
                                    Debug.LogException(ex);
                                }
                                catch
                                {
                                    // ignored
                                }
                            }
                        }

                        if (UpdateLoopItems(i, ref j)) continue; // next i 
                        _tail = i;
                        break; // loop END
                    }

                    lock (_runningAndQueueLock)
                    {
                        _running = false;
                        while (_waitQueue.Count != 0)
                        {
                            if (_loopItems.Length == _tail)
                            {
                                Array.Resize(ref _loopItems, checked(_tail * 2));
                            }

                            _loopItems[_tail++] = _waitQueue.Dequeue();
                        }
                    }
                }
            }

            bool UpdateLoopItems(int i, ref int j)
            {
                // find null, loop from tail
                while (i < j)
                {
                    var fromTail = _loopItems[j];
                    if (fromTail != null)
                    {
                        try
                        {
                            if (!fromTail.MoveNext())
                            {
                                _loopItems[j] = null;
                                j--;
                            }
                            else
                            {
                                // swap
                                _loopItems[i] = fromTail;
                                _loopItems[j] = null;
                                j--;
                                return true;
                            }
                        }
                        catch (Exception ex)
                        {
                            _loopItems[j] = null;
                            j--;
                            try
                            {
                                Debug.LogException(ex);
                            }
                            catch
                            {
                                // ignored
                            }
                        }
                    }
                    else
                    {
                        j--;
                    }
                }

                return false;
            }
        }

        public int Clear()
        {
            lock (_arrayLock)
            {
                var rest = 0;

                for (var index = 0; index < _loopItems.Length; index++)
                {
                    if (_loopItems[index] != null)
                    {
                        rest++;
                    }

                    _loopItems[index] = null;
                }

                _tail = 0;
                return rest;
            }
        }
    }
}