using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace SmplLog.Core
{
  internal sealed class LoggingEventQueue : IDisposable
  {
    Queue<LogEventBase> logEvents { get; set; }
    static Thread[] workerThreads;
    object locker = new object();

    public void EnqueueLogEvent(LogEventBase logEvent)
    {      
      lock (locker)
      {
        logEvents.Enqueue(logEvent);               
        Monitor.PulseAll(locker);
      }
    }

    private void DoLog(object context)
    {      
      while (true)
      {
        LogEventBase logEvent;

        lock (locker)
        {
          // Spin here until there are LogEvents to log
          while (logEvents.Count == 0) Monitor.Wait(locker);

          // We have a logEvent
          logEvent = logEvents.Dequeue();
          ILogger logger = logEvent.logger;

          // DoAppend
          foreach (IAppender appender in logger.Appenders)
          {
            // TODO - better for perf, is the appender is invalid, remove from the appenders for the logger
            if (appender.IsValid)
              appender.WriteLogEvent(logger, logEvent);
          }
        }
      }
    }

    public void Dispose()
    {
      LogManager.LogInternalEvent(EventLevel.Info, "Disposing logger", null);

      // Let worker thread exit
      EnqueueLogEvent(null);

      foreach (Thread t in workerThreads)
        t.Join();
    }

    public LoggingEventQueue(int workerCount)
    {
      logEvents = new Queue<LogEventBase>();
      workerThreads = new Thread[workerCount];

      // Create and start a separate thread for each worker
      for (int i = 0; i < workerCount; i++)
        (workerThreads[i] = new Thread(DoLog)).Start();
    }
  }
}
