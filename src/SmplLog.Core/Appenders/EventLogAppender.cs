
using System.Diagnostics;
using System;
namespace SmplLog.Core
{
  public sealed class EventLogAppender : AppenderBase
  {
    private string source;
    public string Source
    {
      get { return source; }
      set { source = value; }
    }

    private string log;
    public string Log
    {
      get { return log; }
      set { log = value; }
    }

    public EventLogAppender(string name,
        AppenderInitializationData configInitialisationData)
      : base(name)
    {
      if (configInitialisationData == null)
      {
        LogManager.LogInternalEvent(EventLevel.Error, "The appender requires .... It will be disabled", null);
        this.IsValid = false;
        return;
      }

      source = configInitialisationData.GetInitialisationElementValue<string>("Source");
      if (source == null)
      {
        LogManager.LogInternalEvent(EventLevel.Error, "The appender requires a Source parameter which is not present. It will be disabled", null);
        this.IsValid = false;
        return;
      }

      log = configInitialisationData.GetInitialisationElementValue<string>("Log");
      if (log == null)
      {
        LogManager.LogInternalEvent(EventLevel.Error, "The appender requires a Log parameter which is not present. It will be disabled", null);
        this.IsValid = false;
        return;
      }

      if (!EventLog.SourceExists(source))
      {
        EventLog.CreateEventSource(source, log);

        LogManager.LogInternalEvent(EventLevel.Info,
          string.Format("Event source {0} / log {1} does not exist - creating now", source, log), null);
      }
    }

    public EventLogAppender(string name, string source, string log)
      : base(name)
    {
      this.source = source;
      this.log = log;

      // TODO - This will fail in Vista if the user is not an Admin...
      // The event source would have to be created by an installer for the target application
      if (!EventLog.SourceExists(source))
      {
        try
        {
          EventLog.CreateEventSource(source, log);
        }
        catch(Exception ex)
        {
          LogManager.LogInternalEvent(EventLevel.Error,
            string.Format("The EventLogAppender cannot create the source {0} in the log {1}. This might be due to the fact that the platform is Vista / 2008 / Windows 7 and the current user is not an administrator. The appender will we marked invalid."), ex);
          
          this.IsValid = false;
        }

        LogManager.LogInternalEvent(EventLevel.Info,
          string.Format("Event source {0} / log {1} does not exist - creating now", source, log), null);
      }
    }

    public override void WriteLogEvent(ILogger logger, LogEventBase logEvent)
    {
      EventLogEntryType eventLogEntryType;

      switch(logger.LoggerEventLevel)
      {
        case EventLevel.Debug:
        case EventLevel.Info:
          eventLogEntryType = EventLogEntryType.Information;
          break;

        case EventLevel.Error:        
        case EventLevel.Warn:
        case EventLevel.Fatal:
          eventLogEntryType = EventLogEntryType.Error;
          break;

        default:
          eventLogEntryType = EventLogEntryType.Information;
          break;
      }

      EventLog.WriteEntry(source, logEvent.ToString(), eventLogEntryType);      
    }
  }
}
