using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using SmplLog.Core;
using System.Xml;

namespace SmplLog.UnitTests
{
  [TestFixture]
  public class CallbackAppenderTests
  {
    bool called;

    [SetUp]
    public void Setup()
    {
      called = false;
    }

    [Test]
    public void Appender_Raises_LogEvent()
    {
      CallbackAppender callbackAppender = new CallbackAppender("Fred");
      callbackAppender.OnItemLogged += new CallbackAppender.ItemLogged(CallbackAppenderTests_OnItemLogged);
      ILogger logger = LogManager.GetLogger("Logger");
      logger.Appenders.Add(callbackAppender);     

      logger.Log(EventLevel.Debug, "Calling...");

      Assert.True(called);
    }

    void CallbackAppenderTests_OnItemLogged(LogEventBase logEvent)
    {
      called = true;
    }
  }
}
