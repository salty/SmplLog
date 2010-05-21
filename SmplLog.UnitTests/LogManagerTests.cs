using System.Linq;
using System.Xml;

using NUnit.Framework;

using SmplLog.Core;
using System.IO;
using System.Reflection;
using System.Configuration;
using System;

namespace SmplLog.Tests
{
  [TestFixture]
  public class LogManagerTests : TestBase
  {
    [Test]
    public void AddDuplicateLoggerbyIdReturnsExisingLogger()
    {
      ILogger testLogger = LogManager.AddLogger("TestLogger", null, EventLevel.Debug);
      ILogger duplicateLogger = LogManager.AddLogger("TestLogger", null, EventLevel.Debug);

      Assert.AreEqual(testLogger, duplicateLogger);
    }

    [Test]
    public void GetLoggerReturnsRootForInvalidLogger()
    {
      ILogger logger = LogManager.GetLogger("BogusLoggerId");

      Assert.IsTrue(logger.LoggerId == "RootLogger");
    }

    [Test]
    public void GetLoggerReturnsCorrectLoggerById()
    {
      string newLoggerId = "newLoggerId";
      ILogger testLogger = LogManager.AddLogger(newLoggerId, null, EventLevel.Debug);

      Assert.AreEqual(newLoggerId, testLogger.LoggerId);
    }

    [Test]
    public void LogManagerLocatesExternalConfigFileInApplicationFolder()
    {
      string currentAssemblyPath = Assembly.GetExecutingAssembly().Location;
      string logConfigPath = currentAssemblyPath.Substring(0, currentAssemblyPath.LastIndexOf('\\')) +
        "\\SmplLog.Config";

      if (File.Exists(logConfigPath))
        File.Delete(logConfigPath);

      using (StreamWriter sw = new StreamWriter(logConfigPath, false))
      {
        string xml = string.Format(@"<SmplLog>
                <Appenders><Appender Name='TestAppender1' Type='SmplLog.Core.TestAppender' /></Appenders>
                <Loggers><Logger Id='TestLogger' Type='SmplLog.Core.Logger'><AppenderRef Name='TestAppender1' /></Logger></Loggers>
              </SmplLog>");

        sw.Write(xml);
        sw.Flush();
        sw.Close();
      }
      
      ILogger testLogger = LogManager.GetLogger("TestLogger");
      Assert.IsNotNull(testLogger);

      File.Delete(logConfigPath);
    } 

    [Test]
    [ExpectedException(typeof(FileNotFoundException))]
    public void LogManagerThrowsFileNotFoundIfConfigNotFoundAtUserSpecifiedLocation()
    {
      string s = Assembly.GetExecutingAssembly().Location.Substring(0, Assembly.GetExecutingAssembly().Location.LastIndexOf("\\"));

      string invalidLogConfigPath = s + "\\NoLogFileHere.config";
      LogManager.InitialiseFromPath(invalidLogConfigPath);
    }

    [Test]
    public void LogManagerLocatesConfigFileAtSuppliedLocation()
    {
      string logConfigPath = 
        Assembly.GetExecutingAssembly().Location.Substring(0, Assembly.GetExecutingAssembly().Location.LastIndexOf("\\")) + 
          "smpllog.config";

      if (File.Exists(logConfigPath))
        File.Delete(logConfigPath);

      using (StreamWriter sw = new StreamWriter(logConfigPath, false))
      {
        string xml = string.Format(@"<SmplLog>
                <Appenders><Appender Name='TestAppender1' Type='SmplLog.Core.TestAppender' /></Appenders>
                <Loggers><Logger Id='TestLogger' Type='SmplLog.Core.Logger'><AppenderRef Name='TestAppender1' /></Logger></Loggers>
              </SmplLog>");

        sw.Write(xml);
        sw.Flush();
        sw.Close();
      }

      LogManager.InitialiseFromPath(logConfigPath);
      ILogger testLogger = LogManager.GetLogger("TestLogger");

      File.Delete(logConfigPath);
      Assert.IsNotNull(testLogger);      
    }   
  }
}
