using System;

namespace LoadCompress.Core.Notification
{
    internal interface IProgressNotificationService: IDisposable
    {
        void Start(bool failIfAlreadyStarted);
        void Stop(bool failIfAlreadyStopped);
    }
}