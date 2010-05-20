using System;
using System.Collections.Generic;
using System.Text;

using SmplLog.Core;
using NUnit.Framework;

namespace SmplLog.Tests
{
  public class TestBase
  {
    protected TestAppender testAppender;
    protected string logEventMessage = "Log_Event";    
    protected DateTime logEventOccurred = DateTime.Now;
    protected EventLevel logEventLevel = EventLevel.Debug;

    protected string expectedOccurred;
    protected string expectedEventLevel = "[Debug]";

    //ILogger rootLogger = null;
    
    [SetUp]
    public void Setup()
    {
      testAppender = new TestAppender("TestAppender");
      expectedOccurred = logEventOccurred.ToString();
    }

    public LogEvent GetLogEvent()
    {
      LogEvent logEvent = new LogEvent();
      logEvent.Message = logEventMessage;
      logEvent.Occurred = logEventOccurred;
      logEvent.LogEventLevel = logEventLevel;
      logEvent.Data = string.Empty;

      return logEvent;
    }
    
    public ILogger GetRootLogger()
    {
      return LogManager.GetLogger("Root");
      //if (rootLogger == null)        
      //  rootLogger = new Logger("Root", string.Empty, EventLevel.Debug);

      //return rootLogger;
    }
  }
}
