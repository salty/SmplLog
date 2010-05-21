
using NUnit.Framework;
using SmplLog.Core;
using System.Xml;
using System.Diagnostics;


namespace SmplLog.Tests
{
  [TestFixture]
  public sealed class LoggerTests : TestBase
  {    
    [Test]
    public void CanRetrieveExistingLogger()
    {
      string testLoggerId = "TestLogger";

      ILogger testLogger = LogManager.AddLogger(testLoggerId, null, EventLevel.Debug);
      ILogger testLoggerRetrieved = LogManager.GetLogger(testLoggerId);

      Assert.IsNotNull(testLoggerRetrieved);
    }

    [Test]
    public void LoggerCreatesDefaultLogger()
    {
      // after the log call, we'll expect to have 1 logEvent in the TestAppender LogEvent buffer 
      int expectedAppendersAttachedToLogger = 1;
      ILogger logger = GetRootLogger();

      Assert.AreEqual(expectedAppendersAttachedToLogger, logger.AppenderCount);
    }
    
    [Test]
    public void LoggerInheritsParentLoggerByDefault()
    {
      ILogger testLogger = LogManager.AddLogger("TestLogger", null, EventLevel.Debug);
      
      Assert.IsNotNull(testLogger.Parent);
    }

    [Test]
    public void AddDuplicateAppenderByNameDoesNotAddDuplicateAppender()
    {
      ILogger testLogger = LogManager.AddLogger("TestLogger", null, EventLevel.Debug);
      TestAppender appender2 = new TestAppender("TestApppender");
      
      Assert.IsNotNull(testLogger.AppenderCount);
    }

    [Test]
    public void ChildLoggerAttachesToValidParent()
    {
      ILogger testLogger = LogManager.AddLogger("TestLogger", null, EventLevel.Debug);
      ILogger childLogger = LogManager.AddLogger("ChildLogger", "TestLogger", EventLevel.Debug);
      ILogger parentLogger = childLogger.Parent;    

      Assert.IsTrue(testLogger == parentLogger);
    }

    [Test]
    public void DebugLevelCallToLogWorksInDebugModeOnly()
    {
      int expectedLogEventCount = 0;

      TestAppender testAppender = new TestAppender("TestAppender");
      ILogger rootLogger = LogManager.AddLogger("InfoLevelLogger", null, EventLevel.Info);
      rootLogger.AddAppender(testAppender);
      rootLogger.Log(EventLevel.Debug, "Debug");

      int eventsLogged = testAppender.LogEventBuffer.Count;
      Assert.AreEqual(expectedLogEventCount, eventsLogged);
    }

    [Test]
    public void InfoLevelCallToLogWorksInInfoModeOrLower()
    {
      int expectedLogEventCount = 0;

      TestAppender testAppender = new TestAppender("TestAppender");
      ILogger rootLogger = LogManager.AddLogger("WarnLevelLogger", null, EventLevel.Warn);
      rootLogger.AddAppender(testAppender);
      rootLogger.Log(EventLevel.Info, "Warn");

      int eventsLogged = testAppender.LogEventBuffer.Count;
      Assert.AreEqual(expectedLogEventCount, eventsLogged);
    }

    [Test]
    public void WarnLevelCallToLogWorksInWarnModeOrLower()
    {
      int expectedLogEventCount = 0;

      TestAppender testAppender = new TestAppender("TestAppender");
      ILogger rootLogger = LogManager.AddLogger("ErrorLevelLogger", null, EventLevel.Error);
      rootLogger.AddAppender(testAppender);
      rootLogger.Log(EventLevel.Warn, "Error");

      int eventsLogged = testAppender.LogEventBuffer.Count;
      Assert.AreEqual(expectedLogEventCount, eventsLogged);
    }

    [Test]
    public void ErrorLevelCallToLogWorksInErrorModeOrLower()
    {
      int expectedLogEventCount = 0;

      TestAppender testAppender = new TestAppender("TestAppender");
      ILogger rootLogger = LogManager.AddLogger("FatalLevelLogger", null, EventLevel.Fatal);
      rootLogger.AddAppender(testAppender);
      rootLogger.Log(EventLevel.Error, "Fatal");

      int eventsLogged = testAppender.LogEventBuffer.Count;
      Assert.AreEqual(expectedLogEventCount, eventsLogged);
    }

    [Test]
    public void InvalidAppenderDoNotWriteLogEvents()
    {
      ConfigurationDeserializer.ClearConfig();

      string loggerId = "Test1";
      string loggerId2 = "Test2";

      // invalid config
      string xml = string.Format(@"<SmplLog>
          <Appenders>
<Appender Name='TestAppender8' Type='SmplLog.Core.TestAppender' />
<Appender Name='InvalidAppender' Type='SmplLog.Core.InvalidAppender' />
</Appenders>
<Loggers>
<Logger Id='{0}' EventLevel='Info'><AppenderRef Name='TestAppender8' /></Logger>
<Logger Id='{1}' EventLevel='Info'><AppenderRef Name='InvalidAppender' /></Logger>
</Loggers>
        </SmplLog>", loggerId, loggerId2);

      XmlDocument doc = new XmlDocument();
      doc.LoadXml(xml);
      ConfigurationDeserializer.InitialiseFromConfigurationData(doc);

      // invalid appender
      ILogger logger = LogManager.GetLogger(loggerId2);
      logger.Log(EventLevel.Info, "Test!");


      Trace.Write("We got this far!");
      InvalidAppender app = (InvalidAppender)logger.GetAppender("InvalidAppender");

      Assert.AreEqual(app.LogEventBuffer.Count, 0);
    }
  }
}
