using System.Collections.Generic;
using System.Threading.Tasks;

namespace ClearScriptWorkerSample
{
    internal class AsyncQueue<T>
    {
        private readonly Queue<T> _queue = new Queue<T>();
        private readonly Queue<TaskCompletionSource<T>> _waiters = new Queue<TaskCompletionSource<T>>();

        public void Enqueue(T item)
        {
            lock (_queue)
            {
                if (_waiters.TryDequeue(out var waiter))
                {
                    waiter.SetResult(item);
                }
                else
                {
                    _queue.Enqueue(item);
                }
            }
        }

        public Task<T> DequeueAsync()
        {
            lock (_queue)
            {
                if (_queue.TryDequeue(out var item))
                {
                    return Task.FromResult(item);
                }

                var waiter = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
                _waiters.Enqueue(waiter);
                return waiter.Task;
            }
        }
    }
}
