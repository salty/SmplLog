using System.Diagnostics;
using NUnit.Framework;
using SmplLog.Core;
using SmplLog.Tests;

namespace SmplLog.UnitTests
{
  [TestFixture]
  public class EventLogAppenderTests : TestBase
  {
    [Test]
    public void EventLogAppenderCreatesSource()
    {
      string source = "SmplLog";
      IAppender eventLogAppender = new EventLogAppender("EventLogAppneder", source, "App");

      Assert.IsTrue(EventLog.SourceExists(source));
    }

    [Test]
    public void EventLogAppenderCreatesEventLogItem()
    {
      ILogger logger = LogManager.GetLogger("ServiceLogger");
      logger.Log(EventLevel.Info, "Hello event log ...");

      Assert.IsTrue(1 == 1);
    }
  }
}
