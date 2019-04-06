using System;
using System.Diagnostics;
using System.Threading;
using FluentAssertions;
using NUnit.Framework;

namespace LoadCompress.Core.Tests.Threading
{
    [TestFixture]
    [Parallelizable]
    public class WorkersPoolTests
    {
        [SetUp]
        public void SetUp()
        {
            _pool = new WorkersPool();
        }


        [TestCase(1)]
        [TestCase(20)]
        [TestCase(1000)]
        public void Should_complete_all_scheduled_work(int totalCount)
        {
            var counter = 0;

            for (var i = 0; i < totalCount; i++)
            {
                _pool.TryScheduleWork(CreateWork());
            }

            _pool.Wait();
            counter.Should().Be(totalCount);

            Action CreateWork() => () => Interlocked.Increment(ref counter);
        }

        [Test]
        public void TryScheduleWork_should_return_false_and_ignore_work_if_pool_signaled_cancellation()
        {
            var counter = 0;
            _pool.RequestCancellation();
            _pool.TryScheduleWork(() => Interlocked.Increment(ref counter)).Should().BeFalse();
            _pool.Wait();
            counter.Should().Be(0);
        }

        [Test]
        public void TryScheduleWork_should_complete_work_that_started_before_cancellation()
        {
            var counter = 0;
            _pool.TryScheduleWork(() =>
            {
                Thread.Sleep(200);
                Interlocked.Increment(ref counter);
            }).Should().BeTrue();

            Thread.Sleep(100);
            _pool.RequestCancellation();
            _pool.Wait();
            counter.Should().Be(1);
        }

        [Test]
        public void TryScheduleWork_should_wait_when_queue_is_full()
        {
            var queueSize = 100;
            var totalCount = 101;

            _pool = new WorkersPool(4, queueSize);
            var counter = 0;

            for (var i = 0; i < queueSize; i++)
            {
                var sw1 = Stopwatch.StartNew();
                _pool.TryScheduleWork(() =>
                {
                    Thread.Sleep(1000);
                    Interlocked.Increment(ref counter);
                });
                sw1.Elapsed.Should().BeLessThan(TimeSpan.FromMilliseconds(10));
            }

            var sw2 = Stopwatch.StartNew();
            _pool.TryScheduleWork(() =>
            {
                Thread.Sleep(1000);
                Interlocked.Increment(ref counter);
            });

            sw2.Elapsed.Should().BeGreaterThan(TimeSpan.FromMilliseconds(500));
            _pool.Wait();
            counter.Should().Be(totalCount);
        }

        [Test]
        public void Wait_should_leave_pool_reusable()
        {
            var queueSize = 100;
            var totalCount = 101;

            _pool = new WorkersPool(4, queueSize);
            var counter = 0;

            DoWork();

            _pool.Wait();

            DoWork();

            _pool.Wait();

            counter.Should().Be(totalCount * 2);

            void DoWork()
            {
                for (var i = 0; i < queueSize; i++)
                {
                    _pool.TryScheduleWork(() =>
                    {
                        Thread.Sleep(200);
                        Interlocked.Increment(ref counter);
                    });
                }
                _pool.TryScheduleWork(() =>
                {
                    Thread.Sleep(200);
                    Interlocked.Increment(ref counter);
                });
            }
        }

        [Test]
        public void Dispose_should_complete_when_all_threads_completed()
        {
            const int totalCount = 100;
            var counter = 0;

            for (var i = 0; i < totalCount; i++)
            {
                _pool.TryScheduleWork(CreateWork());
            }

            _pool.Dispose();

            Action CreateWork() => () =>
            {
                Thread.Sleep(1000 - Interlocked.Increment(ref counter));
            };
        }

        private WorkersPool _pool;
    }
}
