using System;
using System.Collections.Generic;
using System.Threading;

namespace LoadCompress.Core
{
    /// <summary>
    /// Generic fixed-size pool of workers.
    /// </summary>
    internal class WorkersPool: IDisposable
    {
        private readonly int _workersCount;
        private readonly CountdownEvent _allThreadsCompletedEvent;
        private readonly ManualResetEvent _allTasksCompletedEvent;
        private readonly Semaphore _addTasksSemaphore;

        private readonly Queue<Thread> _workers;
        private readonly Queue<Action> _tasks;

        private volatile bool _cancellationRequested;
        private volatile bool _disableAdd;

        private volatile int _tasksCounter;

        public WorkersPool() : this(Environment.ProcessorCount, Environment.ProcessorCount * 16)
        {
        }

        public WorkersPool(int workersCount, int taskQueueMaxLength)
        {
            _workersCount = workersCount;
            _cancellationRequested = false;
            _disableAdd = false;

            _tasksCounter = 0;

            _tasks = new Queue<Action>(taskQueueMaxLength);
            _workers = new Queue<Thread>(_workersCount);

            _allThreadsCompletedEvent = new CountdownEvent(_workersCount);
            _allTasksCompletedEvent = new ManualResetEvent(true);
            _addTasksSemaphore = new Semaphore(taskQueueMaxLength, taskQueueMaxLength);

            Swarm();
        }

        public void RequestCancellation()
        {
            // Activate idle threads
            lock (_tasks)
            {
                _disableAdd = true;
                _cancellationRequested = true;
                Monitor.PulseAll(_tasks);
            }
        }

        public void Wait()
        {
            _disableAdd = true;            
            _allTasksCompletedEvent.WaitOne();
            _disableAdd = false;
        }

        public bool TryScheduleWork(Action task)
        {
            if (_disableAdd)
                return false;

            _addTasksSemaphore.WaitOne();
            lock (_tasks)
            {
                if (_disableAdd)
                {
                    _addTasksSemaphore.Release();
                    return false;
                }

                Interlocked.Increment(ref _tasksCounter);
                _allTasksCompletedEvent.Reset();
                _tasks.Enqueue(task);
                Monitor.PulseAll(_tasks);
                return true;
            }
        }

        private void Swarm()
        {
            for (var i = 0; i < _workersCount; i++)
            {
                var worker = new Thread(EventLoop)
                {
                    Name = $"Worker {i}"
                };
                _workers.Enqueue(worker);
                worker.Start();
            }
        }

        private void EventLoop()
        {
            while (true)
            {
                Action pendingTask;
                lock (_tasks)
                {
                    while (true)
                    {
                        if(_cancellationRequested)
                        {
                            SignalWorkerStopped();
                            while (_tasks.TryDequeue(out pendingTask))
                            {
                                CheckAndSignalWorkFinished();
                            }
                            return;
                        }

                        if (_workers.TryPeek(out var availableWorker) && Thread.CurrentThread == availableWorker && _tasks.TryDequeue(out pendingTask))
                        {
                            _workers.Dequeue();
                            Monitor.PulseAll(_tasks);
                            break;
                        }

                        Monitor.Wait(_tasks);
                    }
                }

                pendingTask();

                lock (_tasks)
                {
                    _workers.Enqueue(Thread.CurrentThread);
                }

                CheckAndSignalWorkFinished();
            }

            void SignalWorkerStopped()
            {
                _allThreadsCompletedEvent.Signal();
            }

            void CheckAndSignalWorkFinished()
            {
                if (Interlocked.Decrement(ref _tasksCounter) == 0)
                    _allTasksCompletedEvent.Set();

                _addTasksSemaphore.Release();
            }
        }

        public void Dispose()
        {
            RequestCancellation();
            _allThreadsCompletedEvent.Wait();

            for (var i = 0; i < _workers.Count; i++)
            {
                var worker = _workers.Dequeue();
                worker.Join();
            }

            _allThreadsCompletedEvent?.Dispose();
            _allTasksCompletedEvent?.Dispose();
            _addTasksSemaphore?.Dispose();
        }
    }
}