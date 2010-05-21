using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using SmplLog.Core;
using System.IO;
using System.Xml;
using System.Reflection;
using System.Diagnostics;
using System.Configuration;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace SmplLog.UnitTests
{
  [TestFixture]
  public class FileAppenderTests
  {
    [Test]
    public void FileAppenderCreatesFileIfNotExists()
    {            
      string filePath = ConfigurationManager.AppSettings["FileAppenderTestTempPath"] + "log.txt";
      
      FileAppender fileAppender = new FileAppender(filePath, "FileAppender1");

      LogEvent logEvent = new LogEvent();
      logEvent.Message = "Hello!";
      fileAppender.WriteLogEvent(null, logEvent);

      Assert.IsTrue(File.Exists(filePath), "Path was " + filePath);
    }

    [Test]
    public void MinimalLockPreventFileIOExceptionsWithMultipleAppenders()
    {
      string filePath = ConfigurationManager.AppSettings["FileAppenderTestTempPath"];
            
      string logFilePath = filePath + "\\log-mnultiple.txt";

      Trace.WriteLine("Logfilepath is " + logFilePath);

      if (File.Exists(logFilePath))
        File.Delete(logFilePath);

      FileAppender fileAppender = new FileAppender(logFilePath, "FileAppender1");
      FileAppender fileAppender2 = new FileAppender(logFilePath, "FileAppender2");

      LogEvent logEvent = new LogEvent();
      logEvent.Message = "Hello!";

      //fileAppender.AppenderLockMode = FileAppender.LockMode.MinimalLock;
      //fileAppender2.AppenderLockMode = FileAppender.LockMode.MinimalLock;

      for (int i = 0; i < 1000; i++)
      {
        fileAppender.WriteLogEvent(null, logEvent);
        fileAppender2.WriteLogEvent(null, logEvent);       
      }

      Assert.IsTrue(File.Exists(logFilePath));      
    }

    [Test]
    public void CanInitiliseAppenderParamsFromConfigFile()
    {
      //LogManager.AutoLoadConfig = false;
      //LogManager.InitialiseFromAppConfig();

      ILogger logger = LogManager.GetLogger("MyTestLogger");
      logger.Log(EventLevel.Info, "Logging!");
    }

    [Test]
    public void OverwriteFileSetToFalseGeneratesNewUniqueFileName()
    {      
      string path = ConfigurationManager.AppSettings["FileAppenderTestTempPath"];
      
      string loggerId = "Logger1";
            
      string expectedPath = path + "log-a1.txt";
      string logFilePath = path + "log-a.txt";
      
      Trace.WriteLine("Log file path is " + logFilePath);
      Trace.WriteLine("Expected path is " + expectedPath);

      try
      {
        if (!File.Exists(logFilePath))
        {
          FileStream fs = File.Create(logFilePath);
          fs.Close();
          fs.Dispose();
        }

        string xml = string.Format(@"<SmplLog>
          <Appenders>
            <Appender Name='FileAppender' Type='SmplLog.Core.FileAppender'>
              <Params>
              <FilePath>{1}</FilePath>
              <OverwriteExistingFile>False</OverwriteExistingFile>
              </Params>
            </Appender>
          </Appenders>
          <Loggers>
          <Logger Id='{0}' EventLevel='Debug'><AppenderRef Name='FileAppender' /></Logger>
          </Loggers>
        </SmplLog>", loggerId, logFilePath);

        XmlDocument doc = new XmlDocument();
        doc.LoadXml(xml);
        ConfigurationDeserializer.InitialiseFromConfigurationData(doc);
        ILogger logger = LogManager.GetLogger(loggerId);
        logger.Log(EventLevel.Debug, "Test");

        Assert.IsTrue(File.Exists(expectedPath), "Path was " + expectedPath);        
      }
      finally
      {        
        //if (File.Exists(expectedPath))
        //  File.Delete(expectedPath);

        if (File.Exists(logFilePath))
          File.Delete(logFilePath);
      }
    }

    [Test]
    public void AppenderCreatesNewFileWhenMaxFileSizeExceededIfOverwriteIsFalse()
    {
      string loggerId = "Logger1";
      string logFilePath = "log.txt";

      if (File.Exists(logFilePath))
      {
        FileStream fs = File.Create(logFilePath);
        fs.Close();
        fs.Dispose();
      }

      string xml = string.Format(@"<SmplLog>
          <Appenders>
            <Appender Name='FileAppender' Type='SmplLog.Core.FileAppender'>
              <Params>
              <FilePath>{1}</FilePath>
              <OverwriteExistingFile>False</OverwriteExistingFile>
              <MaxFileSizeBytes>10</MaxFileSizeBytes>
              </Params>
            </Appender>
          </Appenders>
          <Loggers>
          <Logger Id='{0}' EventLevel='Debug'><AppenderRef Name='FileAppender' /></Logger>
          </Loggers>
        </SmplLog>", loggerId, logFilePath);

      XmlDocument doc = new XmlDocument();
      doc.LoadXml(xml);
      ConfigurationDeserializer.InitialiseFromConfigurationData(doc);
      
      ILogger logger = LogManager.GetLogger(loggerId);
      logger.Log(EventLevel.Debug, "No Push");
      
      // two tests in one
      string originalPath = Path.GetFileName(((FileAppender)logger.GetAppender("FileAppender")).FilePath);
      
      logger.Log(EventLevel.Info, "This will push it over...");
      logger.Log(EventLevel.Info, "This in a new file");

      string currentPath = Path.GetFileName(((FileAppender)logger.GetAppender("FileAppender")).FilePath);
      Assert.IsTrue(currentPath != originalPath);
    }

    [Test]
    public void AppenderOverwritesExistingFileWhenMaxFileSizeExceededIfOverwriteIsTrue()
    {
      string loggerId = "Logger1";
      string logFilePath = "log.txt";

      if (File.Exists(logFilePath))
      {
        FileStream fs = File.Create(logFilePath);
        fs.Close();
        fs.Dispose();
      }

      string xml = string.Format(@"<SmplLog>
          <Appenders>
            <Appender Name='FileAppender' Type='SmplLog.Core.FileAppender'>
              <Params>
              <FilePath>{1}</FilePath>
              <OverwriteExistingFile>True</OverwriteExistingFile>
              <MaxFileSizeBytes>10</MaxFileSizeBytes>
              </Params>
            </Appender>
          </Appenders>
          <Loggers>
          <Logger Id='{0}' EventLevel='Debug'><AppenderRef Name='FileAppender' /></Logger>
          </Loggers>
        </SmplLog>", loggerId, logFilePath);

      XmlDocument doc = new XmlDocument();
      doc.LoadXml(xml);
      ConfigurationDeserializer.InitialiseFromConfigurationData(doc);

      ILogger logger = LogManager.GetLogger(loggerId);
      string orginalPath = ((FileAppender)logger.GetAppender("FileAppender")).FilePath;      

      logger.Log(EventLevel.Debug, "No Push");      
      logger.Log(EventLevel.Info, "In a new file...");
      logger.Log(EventLevel.Info, "This also in a new file");

      string currentPath = ((FileAppender)logger.GetAppender("FileAppender")).FilePath;
      Assert.IsTrue(currentPath == orginalPath);
    }

    [Test]
    public void MaxFileSizeIsOSMaxWhenNotSpecified()
    {
      string loggerId = "Logger1";
      string logFilePath = "log.txt";

      // get the O/S max ...
      long expectedSize = (long)Math.Pow(2, 32) - 1; 
      uint serialNum, serialNumLength, flags;
      StringBuilder volumename = new StringBuilder(256);
      StringBuilder fstype = new StringBuilder(256); 
      StringBuilder volumeName = new StringBuilder();
      bool ok = GetVolumeInformation("c:\\", volumeName, (uint)volumename.Capacity - 1, out serialNum, out serialNumLength, out flags, fstype, (uint)fstype.Capacity - 1);

      if (ok)
      {
        switch (fstype.ToString().ToLower())
        {
          case "ntfs":
            expectedSize = (long)Math.Pow(2, 64) - 1;
            break;
        }
        
        // read config
        string xml = string.Format(@"<SmplLog>
          <Appenders>
            <Appender Name='FileAppender' Type='SmplLog.Core.FileAppender'>
              <Params>
              <FilePath>{1}</FilePath>
              <OverwriteExistingFile>True</OverwriteExistingFile>              
              </Params>
            </Appender>
          </Appenders>
          <Loggers>
          <Logger Id='{0}' EventLevel='Debug'><AppenderRef Name='FileAppender' /></Logger>
          </Loggers>
        </SmplLog>", loggerId, logFilePath);

        XmlDocument doc = new XmlDocument();
        doc.LoadXml(xml);
        ConfigurationDeserializer.InitialiseFromConfigurationData(doc);
      }

      // assert
      ILogger logger = LogManager.GetLogger(loggerId);
      Assert.AreEqual(((FileAppender)logger.GetAppender("FileAppender")).MaxFileSizeBytes, expectedSize);  
    }

    [Test]
    public void TimestampIsReplacedWithDateTime()
    {
      string xml = @"<SmplLog>
          <Appenders>
            <Appender Name='FileAppender' Type='SmplLog.Core.FileAppender'>
              <Params>
              <FilePath>Temp\{Timestamp}.txt</FilePath>
              <OverwriteExistingFile>True</OverwriteExistingFile>              
              </Params>
            </Appender>
          </Appenders>
          <Loggers>
          <Logger Id='Logger' EventLevel='Debug'><AppenderRef Name='FileAppender' /></Logger>
          </Loggers>
        </SmplLog>";

      XmlDocument doc = new XmlDocument();
      doc.LoadXml(xml);
      ConfigurationDeserializer.InitialiseFromConfigurationData(doc);

      ILogger logger = LogManager.GetLogger("Logger");

      Assert.True(Regex.Match((logger.GetAppender("FileAppender") as FileAppender).FilePath, 
        @"\d{8}_\d{2}_\d{2}_\d{2}").Length > 0);
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
    static extern bool GetVolumeInformation(string Volume, StringBuilder VolumeName, uint VolumeNameSize, out uint SerialNumber, out uint SerialNumberLength, out uint flags, StringBuilder fs, uint fs_size);
  }
}
