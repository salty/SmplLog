using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using System.Xml;
using SmplLog.Core;

namespace SmplLog.UnitTests
{
  [TestFixture]
  public class ConsoleAppenderTests
  {
    [Test]
    public void Can_Log_Msg_Only()
    {
      string xml = @"<?xml version='1.0' encoding='utf-8' ?>
<configuration>
  <SmplLogConfigurationSection>
    <Appenders>
      <Appender Name='ConsoleAppender' Type='SmplLog.Core.ConsoleAppender'>
        <Params>
          <MsgOnlyFormat>True</MsgOnlyFormat>
        </Params>
      </Appender>      
    </Appenders>
    <Loggers>
      <Logger Id='Logger' Type='SmplLog.Core.Logger' EventLevel='Info'>
        <AppenderRef Name='ConsoleAppender' />      
      </Logger>
    </Loggers>
  </SmplLogConfigurationSection>
</configuration>";
      
      XmlDocument doc = new XmlDocument();
      doc.LoadXml(xml);
      ConfigurationDeserializer.InitialiseFromConfigurationData(doc);

      ILogger logger = LogManager.GetLogger("Logger");
      logger.Log(EventLevel.Debug, "Message Only");
    }
  }
}
