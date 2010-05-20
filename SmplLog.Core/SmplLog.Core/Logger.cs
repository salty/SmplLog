using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;

namespace SmplLog.Core
{
  public class Logger : ILogger
  {
    #region Properties

    private const int ExternalStackframeIndex = 1;
    private const int MaxThreadCount = 10;
    private LoggingEventQueue loggingEventQueue;

    List<IAppender> appenders;
    /// <summary>
    /// The appenders attached to this logger
    /// </summary>
    public List<IAppender> Appenders
    {
      get { return appenders; }
    }

    private ILogger parent;
    /// <summary>
    /// The parent of this logger
    /// </summary>
    public ILogger Parent
    {
      get { return parent; }
      set { parent = value; }
    }

    private string loggerId;
    /// <summary>
    /// A Unique id for a logger
    /// </summary>
    public string LoggerId
    {
      get { return loggerId; }
      set { loggerId = value; }
    }

    /// <summary>
    /// The logger will log for LogEvent of the same level or lower
    /// </summary>
    private readonly EventLevel loggerEventLevel = EventLevel.Info;
    public EventLevel LoggerEventLevel
    {
      get { return loggerEventLevel; }
    }

    public int AppenderCount
    {
      get { return appenders.Count; }
    }

    //private bool isDisposed = false;

    #endregion

    /// <summary>
    /// Construct a new instance of a logger
    /// </summary>
    public Logger(string loggerId,
      string parentLoggerId,
      EventLevel loggerEventLevel)
    {
      // assign the id that makes this logger unique
      this.loggerId = loggerId;

      // assign the logger event level
      this.loggerEventLevel = loggerEventLevel;

      // initialise the appender collection
      appenders = new List<IAppender>();

      // Initialise the new LoggingEventQueue - a new multithreaded workhorse which distributes logEvents
      // to appenders in a thread safe manner
      loggingEventQueue = new LoggingEventQueue(MaxThreadCount);

      // if root logger , don't attempt to parent
      if (string.Compare(loggerId, Common.RootLoggerId, false, CultureInfo.InvariantCulture) == 0)
        return;

      // parent wasn't supplied, so we'll parent to the root      
      if (!String.IsNullOrEmpty(parentLoggerId))
      {
        ILogger parentLogger = LogManager.GetLogger(parentLoggerId);
        if (parentLogger != null)
          this.parent = parentLogger;

        return;
      }

      // parent to the root
      LogManager.LogInternalEvent(EventLevel.Warn,
             string.Format("Parent logger not found or not supplied for logger {0} - using root", this.LoggerId), null);
      this.parent = LogManager.GetLogger(Common.RootLoggerId);
    }

    #region ILogger Members
    
    /// <summary>
    /// Create a LogEvent and forward it to the loggers attached appenders
    /// </summary>
    /// <param name="message"></param>
    public virtual void LogFormat(EventLevel eventLevel,
      string message,
      params object[] paramlist
      )
    {
      if (appenders.Count == 0) return;
      if (eventLevel < loggerEventLevel) return;

      StackFrame sf = new StackFrame(ExternalStackframeIndex);
      string location = GetCallerLocation(sf);

      LogEvent logEvent = GetLogEvent(string.Format(message, paramlist), System.DateTime.Now, eventLevel, location);
      DoAppend(logEvent);

      if (this.parent != null)
        ((Logger)this.parent).Log(eventLevel, message, location);
    }

    /// <summary>
    /// Create a LogEvent and forward it to the loggers attached appenders
    /// </summary>
    /// <param name="message"></param>
    public virtual void Log(EventLevel eventLevel,
      string message)
    {
      if (appenders.Count == 0) return;
      if (eventLevel < loggerEventLevel) return;

      StackFrame sf = new StackFrame(ExternalStackframeIndex);
      string location = GetCallerLocation(sf);

      LogEvent logEvent = GetLogEvent(message, System.DateTime.Now, eventLevel, location);
      DoAppend(logEvent);

      if (this.parent != null)
        ((Logger)this.parent).Log(eventLevel, message, location);
    }

    /// <summary>
    /// Create a LogEvent and forward it to the loggers attached appenders
    /// </summary>
    /// <param name="message"></param>
    public virtual void Log(EventLevel eventLevel, string message, Object data)
    {
      if (appenders.Count == 0) return;
      if (eventLevel < loggerEventLevel) return;

      StackFrame sf = new StackFrame(ExternalStackframeIndex);
      string location = GetCallerLocation(sf);

      LogEvent logEvent = GetLogEvent(message, System.DateTime.Now, eventLevel, location);
      logEvent.Data = data;
      DoAppend(logEvent);

      if (this.parent != null)        
        // bummer - we need to call an internal method
        // loggers specified in the config which are not of type logger will break this
        // the simplest fix is to make the location call public
        // which might be ok
        ((Logger)this.parent).Log(eventLevel, message, data, location);
    }
    
    /// <summary>
    /// Create a LogEvent and forward it to the loggers attached appenders
    /// </summary>
    /// <param name="message"></param>
    public void Log(EventLevel eventLevel,
      string message,
      string callerLocation)
    {
      if (appenders.Count == 0) return;
      if (eventLevel < loggerEventLevel) return;
      
      LogEvent logEvent = GetLogEvent(message, System.DateTime.Now, eventLevel, callerLocation);
      DoAppend(logEvent);

      if (this.parent != null)
        this.parent.Log(eventLevel, message);
    }

    /// <summary>
    /// Create a LogEvent and forward it to the loggers attached appenders
    /// </summary>
    /// <param name="message"></param>
    public void Log(EventLevel eventLevel, 
      string message, 
      Object data, 
      String callerLocation)
    {
      if (appenders.Count == 0) return;
      if (eventLevel < loggerEventLevel) return;
      
      LogEvent logEvent = GetLogEvent(message, System.DateTime.Now, eventLevel, callerLocation);
      logEvent.Data = data;
      DoAppend(logEvent);

      if (this.parent != null)
        this.parent.Log(eventLevel, message, data);
    }

    /// <summary>
    /// Return a named appender
    /// </summary>
    /// <param name="appenderName"></param>
    /// <returns></returns>
    public virtual IAppender GetAppender(string appenderName)
    {
      return this.appenders.Find(delegate(IAppender app)
      {
        return string.Compare(app.Name, appenderName, true, CultureInfo.InvariantCulture) == 0;
      });
    }

    /// <summary>
    /// Create and return a LogEvent
    /// </summary>
    /// <param name="message">The message to be output</param>
    /// <param name="occurred">The DateTime when the event occurred</param>
    /// <param name="level">The level specified for the event</param>
    /// <returns></returns>
    public virtual LogEvent GetLogEvent(
      string message,
      DateTime occurred,
      EventLevel level,
      string location)
    {
      LogEvent logEvent = new LogEvent();
      logEvent.Message = message;
      logEvent.Occurred = occurred;
      logEvent.LogEventLevel = level;
      logEvent.Location = location;

      return logEvent;
    }

    public void AddAppender(IAppender appenderToAdd)
    {
      IAppender appender = this.appenders.Find(
        delegate(IAppender ap) { return ap.Name == appenderToAdd.Name; });

      if (appender == null)
        this.appenders.Add(appenderToAdd);

      LogManager.AddAppender(appenderToAdd);
    }

    #endregion

    /// <summary>
    /// Pass the logevent to all attached appenders
    /// </summary>
    /// <param name="logEvent"></param>
    private void DoAppend(LogEventBase logEvent)
    {
      //foreach (IAppender appender in appenders)
      //{
      //  // TODO - better for perf, is the appender is invalid, remove from the appenders for the logger
      //  if (appender.IsValid)
      //    appender.WriteLogEvent(this, logEvent);        
      //}

      logEvent.logger = this;
      loggingEventQueue.EnqueueLogEvent(logEvent);
    }

    /// <summary>
    /// Format the method caller location
    /// </summary>
    /// <param name="sf">The stackframe containing caller info</param>
    /// <returns></returns>
    private static string GetCallerLocation(StackFrame sf)
    {
      MethodBase mb = sf.GetMethod();
      string assemblyName = mb.DeclaringType.Assembly.GetName().Name;
      string classFunctionNames = mb.DeclaringType.Name + "." + mb.Name + "()";

      return string.Format(CultureInfo.InvariantCulture, "{0},{1}", assemblyName, classFunctionNames);
    }
  }
}
