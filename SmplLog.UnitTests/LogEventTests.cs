using System;

using SmplLog.Core;
using NUnit.Framework;

namespace SmplLog.Tests
{
  [TestFixture]
  public class LogEventTests : TestBase
  {
    enum LogEventElement
    {
      EventLevel,
      Occurred,      
      Location,
      Message,      
      Data
    }

    private string GetElement(LogEventElement elementToFetch)
    {
      string[] elements = testAppender.LogEventBuffer[testAppender.LastEventIndex].Split(' ');
      return elements[(int)elementToFetch];
    }

    [Test]
    public void TestDefaultLogEventFormattingMessage()
    {
      string expectedFormatString = logEventMessage;

      LogEvent logEvent = GetLogEvent();
      testAppender.WriteLogEvent(null, logEvent);

      Assert.AreEqual(expectedFormatString, GetElement(LogEventElement.Message));
    }
   
    [Test]
    public void TestDefaultLogEventFormattingEventLevel()
    {      
      LogEvent logEvent = GetLogEvent();
      testAppender.WriteLogEvent(null, logEvent);

      Assert.AreEqual(expectedEventLevel, GetElement(LogEventElement.EventLevel));
    }

    [Test]
    public void TestDefaultLogEventFormattingForException()
    {
      LogEvent logEvent = null;

      try
      {
        string x = "x";
        int a = int.Parse(x);
      }
      catch (Exception ex)
      {        
        logEvent = GetLogEvent();
        logEvent.Data = ex;

        testAppender.WriteLogEvent(null, logEvent);
      }
      
      Assert.IsNotNull(logEvent.Data);
    }

    [Test]
    public void TestDefaultLogEventFormattingForExceptionContainsInnerException()
    {
      LogEvent logEvent = null;
      string expectedMessage = "Input string was not in a correct format";
      try
      {
        string x = "x";
        int a = int.Parse(x);
      }
      catch (Exception ex)
      {
        Exception ex2 = new Exception("Outer", ex);
        logEvent = GetLogEvent();
        logEvent.Data = ex2;

        testAppender.WriteLogEvent(null, logEvent);
      }

      Assert.IsTrue(logEvent.ToString().Contains(expectedMessage));
    }
  }
}
