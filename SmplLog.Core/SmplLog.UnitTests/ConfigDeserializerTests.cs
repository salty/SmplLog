using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SmplLog.Tests;
using System.Xml;
using SmplLog.Core;
using NUnit.Framework;

namespace SmplLog.UnitTests
{
  [TestFixture]
  public class ConfigDeserializerTests : TestBase
  {
    [Test]
    public void ConfigDeserializerCreatesValidLoggersFromConfigFile()
    {
      ConfigurationDeserializer.ClearConfig();

      string loggerId = "TestLogger";
      string xml = string.Format(@"<SmplLog>
          <Appenders><Appender Name='TestAppender2' Type='SmplLog.Core.TestAppender' /></Appenders>
          <Loggers><Logger Id='{0}' Type='SmplLog.Core.Logger'><AppenderRef Name='TestAppender2' /></Logger></Loggers>
        </SmplLog>", loggerId);

      XmlDocument doc = new XmlDocument();
      doc.LoadXml(xml);
      ConfigurationDeserializer.InitialiseFromConfigurationData(doc);

      ILogger logger = LogManager.GetLogger(loggerId);

      Assert.IsNotNull(logger);
    }

    [Test]
    public void ConfigDeserializerCreatesValidAppendersFromConfigFile()
    {
      ConfigurationDeserializer.ClearConfig();

      string loggerId = "TestLogger4";
      string xml = string.Format(@"<SmplLog>
          <Appenders><Appender Name='TestAppender6' Type='SmplLog.Core.TestAppender' /></Appenders>
          <Loggers><Logger Id='{0}' Type='SmplLog.Core.Logger'><AppenderRef Name='TestAppender6' /></Logger></Loggers>
        </SmplLog>", loggerId);
      
      XmlDocument doc = new XmlDocument();
      doc.LoadXml(xml);
      ConfigurationDeserializer.InitialiseFromConfigurationData(doc);
      
      // Root logger will be returned - because the config fragment above has no valid appenders. So no logger created.
      ILogger logger = LogManager.GetLogger(loggerId);
      // Can still add the test appender though    
      logger.Log(EventLevel.Info, "Yup?");

      Assert.Greater((logger.GetAppender("TestAppender6") as TestAppender).LogEventBuffer.Count(), 0);
    }

    [Test]
    [ExpectedException("SmplLog.Core.ConfigurationException")]
    public void ConfigDeserializerThrowsConfigurationExceptionForInvalidAppenderRef()
    {
      ConfigurationDeserializer.ClearConfig();

      string loggerId = "TestLogger";
      string xml = string.Format(@"<SmplLog>
          <Appenders><Appender Name='TestAppender' Type='SmplLog.Core.TestAppender' /></Appenders>
          <Loggers><Logger Id='{0}' Type='SmplLog.Core.Logger'><AppenderRef Name='InvalidRef' /></Logger></Loggers>
        </SmplLog>", loggerId);

      XmlDocument doc = new XmlDocument();
      doc.LoadXml(xml);
      ConfigurationDeserializer.InitialiseFromConfigurationData(doc);
    }

    [Test]
    [ExpectedException("SmplLog.Core.ConfigurationException")]
    public void ConfigDeserializerThrowsForAppendersMissingRequiredAttributes()
    {
      ConfigurationDeserializer.ClearConfig();

      string loggerId = "TestLogger";
      string xml = string.Format(@"<SmplLog>
          <Appenders><Appender /></Appenders>
          <Loggers><Logger Id='{0}' Type='SmplLog.Core.Logger'><AppenderRef Name='InvalidRef' /></Logger></Loggers>
        </SmplLog>", loggerId);

      XmlDocument doc = new XmlDocument();
      doc.LoadXml(xml);
      ConfigurationDeserializer.InitialiseFromConfigurationData(doc);
    }

    [Test]
    [ExpectedException("SmplLog.Core.ConfigurationException")]
    public void ConfigDeserializerThrowsForLoggersMissingRequiredAttributes()
    {
      ConfigurationDeserializer.ClearConfig();

      string loggerId = "TestLogger";
      string xml = string.Format(@"<SmplLog>
          <Appenders><Appender Name='TestAppender' Type='SmplLog.Core.TestAppender' /></Appenders>
          <Loggers><Logger><AppenderRef Name='TestAppender' /></Logger></Loggers>
        </SmplLog>", loggerId);

      XmlDocument doc = new XmlDocument();
      doc.LoadXml(xml);
      ConfigurationDeserializer.InitialiseFromConfigurationData(doc);
    }

    [Test]
    public void ConfigDeserializerCreateLoggersWithCorrectEventLevel()
    {
      ConfigurationDeserializer.ClearConfig();

      string loggerId = "TestEventLevelLogger";
      string loggerId2 = "TestEventLevelLogger2";
      EventLevel expectedlevel1 = EventLevel.Fatal;
      EventLevel expectedlevel2 = EventLevel.Info;

      string xml = string.Format(@"<SmplLog>
          <Appenders>
<Appender Name='TestAppender' Type='SmplLog.Core.TestAppender' />
<Appender Name='TestAppender2' Type='SmplLog.Core.TestAppender' />
</Appenders>
<Loggers>
<Logger Id='{0}' EventLevel='Fatal'><AppenderRef Name='TestAppender' /></Logger>
<Logger Id='{1}' EventLevel='Info'><AppenderRef Name='TestAppender' /></Logger>
</Loggers>
        </SmplLog>", loggerId, loggerId2);

      XmlDocument doc = new XmlDocument();
      doc.LoadXml(xml);

      ConfigurationDeserializer.InitialiseFromConfigurationData(doc);

      ILogger logger = LogManager.GetLogger(loggerId);
      Assert.AreEqual(expectedlevel1, logger.LoggerEventLevel);

      ILogger logger2 = LogManager.GetLogger(loggerId2);
      Assert.AreEqual(expectedlevel2, logger2.LoggerEventLevel);
    }

    [Test]
    public void ConfigDeserializerCreateLoggersWithCorrectParent()
    {
      ConfigurationDeserializer.ClearConfig();

      string loggerId = "TestAppender1";
      string loggerId2 = "TestAppender2";

      string xml = string.Format(@"<SmplLog>
          <Appenders>
<Appender Name='TestAppender6' Type='SmplLog.Core.TestAppender' />
<Appender Name='TestAppender2' Type='SmplLog.Core.TestAppender' />
</Appenders>
<Loggers>
<Logger Id='{0}' EventLevel='Fatal'><AppenderRef Name='TestAppender6' /></Logger>
<Logger Id='{1}' Parent='{0}' EventLevel='Info'><AppenderRef Name='TestAppender2' /></Logger>
</Loggers>
        </SmplLog>", loggerId, loggerId2);

      XmlDocument doc = new XmlDocument();
      doc.LoadXml(xml);

      ConfigurationDeserializer.InitialiseFromConfigurationData(doc);

      ILogger logger = LogManager.GetLogger(loggerId);
      ILogger logger2 = LogManager.GetLogger(loggerId2);

      Assert.IsNotNull(logger2.Parent);
      Assert.AreEqual(logger2.Parent.LoggerId, logger.LoggerId);
    }
  }
}
