using System;
using System.Collections.Generic;
using System.Threading;
using JetBrains.Annotations;

namespace UniSaga
{
    public delegate IEnumerator<IEffect> Saga();

    public sealed class SagaTask
    {
        [CanBeNull] private CancellationTokenSource _cancellationTokenSource;
        [CanBeNull] private SagaTask _parentTask;
        private readonly List<SagaTask> _childTasks = new List<SagaTask>();
        private readonly object _lock = new object();

        internal SagaTask(CancellationTokenSource cancellationTokenSource, SagaTask parentTask = null)
        {
            _cancellationTokenSource = cancellationTokenSource;
            _parentTask = parentTask;
            _parentTask?.SetChildTask(this);
        }

        public bool IsCompleted { get; private set; }
        internal CancellationToken Token => _cancellationTokenSource?.Token ?? CancellationToken.None;

        internal bool TryComplete()
        {
            if (IsCompleted) return true;
            SagaTask parentTask;
            lock (_lock)
            {
                if (_childTasks.Count != 0) return IsCompleted;
                parentTask = _parentTask;
                IsCompleted = true;
                _parentTask = null;
                _cancellationTokenSource = null;
            }

            parentTask?.RemoveChildTask(this);
            return IsCompleted;
        }

        private void SetChildTask(SagaTask childTask)
        {
            if (IsCompleted) return;
            lock (_lock)
            {
                if (_childTasks.Contains(childTask)) throw new InvalidOperationException();
                _childTasks.Add(childTask);
            }
        }

        private void RemoveChildTask(SagaTask childTask)
        {
            if (IsCompleted) return;
            lock (_lock)
            {
                _childTasks.Remove(childTask);
            }
        }
    }
}