using System;
using System.Threading;
using FluentAssertions;
using LoadCompress.Core.GZipFast.Data;
using NUnit.Framework;

namespace LoadCompress.Core.Tests.Threading
{
    [TestFixture]
    public class ProgressNotifierTests
    {

        [Test]
        public void Start_should_start_notification_process()
        {
            var notifiedEvent = new AutoResetEvent(false);
            var notifier = new ProgressNotifier(e => notifiedEvent.Set(), block => block.OriginalSize);

            notifier.Start(10);
            notifier.TryNotify(new GZipBlock(0, 10, 20)).Should().BeTrue();

            var wasFired = notifiedEvent.WaitOne(TimeSpan.FromMilliseconds(200));
            wasFired.Should().BeTrue("Notifications should work");

            notifier.Stop();
        }

        [Test]
        public void Start_should_fail_if_already_started()
        {
            var notifiedEvent = new AutoResetEvent(false);
            var notifier = new ProgressNotifier(e => notifiedEvent.Set(), block => block.OriginalSize);

            notifier.Start(10);
            notifier.Invoking(n => n.Start(5)).Should()
                .Throw<InvalidOperationException>();

            notifier.Stop();
        }

        [Test]
        public void Stop_should_stop_notification_process()
        {
            var firstNotificationFiredEvent = new ManualResetEvent(false);
            var notifiedEvent = new CountdownEvent(2);
            var notifier = new ProgressNotifier(e =>
            {
                notifiedEvent.Signal();
                firstNotificationFiredEvent.Set();
            }, block => block.OriginalSize);

            notifier.Start(10);
            notifier.TryNotify(new GZipBlock(0, 10, 20)).Should().BeTrue();
            firstNotificationFiredEvent.WaitOne(TimeSpan.FromMilliseconds(100)).Should()
                .BeTrue("first notification should be handled");

            notifier.Stop();

            notifier.TryNotify(new GZipBlock(0, 10, 20)).Should().BeFalse("notification was disabled");
            notifiedEvent.CurrentCount.Should().Be(1, "second notification was not fired");
        }

        [Test]
        public void Stop_should_fail_if_not_started()
        {
            var notifiedEvent = new AutoResetEvent(false);
            var notifier = new ProgressNotifier(e => notifiedEvent.Set(), block => block.OriginalSize);
            notifier.Invoking(n => n.Stop()).Should().Throw<InvalidOperationException>();
        }

        [Test]
        public void Stop_should_leave_reusable_notifier()
        {
            var allNotificationsFiredEvent = new CountdownEvent(2);
            var notificationFiredEvent = new AutoResetEvent(false);
            var notifier = new ProgressNotifier(e =>
            {
                notificationFiredEvent.Set();
                allNotificationsFiredEvent.Signal();
            }, block => block.OriginalSize);

            notifier.Start(10);
            notifier.TryNotify(new GZipBlock(0, 10, 20)).Should().BeTrue();
            notificationFiredEvent.WaitOne(TimeSpan.FromMilliseconds(100)).Should().BeTrue();
            notifier.Stop();

            notifier.Start(20);
            notifier.TryNotify(new GZipBlock(0, 10, 20)).Should().BeTrue();
            allNotificationsFiredEvent.Wait(TimeSpan.FromMilliseconds(100)).Should().BeTrue();

            notifier.Stop();
        }

        [Test]
        public void TryNotify_should_pass_correct_event()
        {
            CompressionStatus passedStatus = null;
            var notificationFiredEvent = new CountdownEvent(3);
            var notifier = new ProgressNotifier(e =>
            {
                notificationFiredEvent.Signal();
                passedStatus = e;
            }, block => block.OriginalSize);
            notifier.Start(10);

            notifier.TryNotify(new GZipBlock(0, 15, 20)).Should().BeTrue();
            notifier.TryNotify(new GZipBlock(0, 15, 20)).Should().BeTrue();
            notifier.TryNotify(new GZipBlock(0, 15, 20)).Should().BeTrue();

            notificationFiredEvent.Wait(TimeSpan.FromMilliseconds(100)).Should().BeTrue();
            passedStatus.Should().NotBeNull();
            passedStatus.BlocksInTotal.Should().Be(10);
            passedStatus.ProceededBlocks.Should().Be(3);
            passedStatus.TotalBytesProceeded.Should().Be(20 * 3);

            notifier.Stop();
        }
    }
}