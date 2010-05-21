using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using SmplLog.Core;
using System.Xml;
using System.Threading;

namespace SmplLog.UnitTests
{
  //[TestFixture]
  public class SmtpEmailAppenderTests
  {
    //[Test]
    public void CanSend()
    {
      string toAddress = "kelly@montage.co.nz";
      string fromAddress = "kelly@montage.co.nz";
      string server = "192.168.1.12";

      string xml = string.Format(@"<SmplLog>
          <Appenders>
<Appender Name='SmtpAppender' Type='SmplLog.Core.SmtpAppender'>
  <Params>
    <ToAddress>{0}</ToAddress>
    <FromAddress>{1}</FromAddress>
    <Subject>Logging</Subject>
    <SMTPServer>{2}</SMTPServer>
    <Timeout>10000</Timeout>
  </Params>
</Appender>
</Appenders>
<Loggers>
<Logger Id='Smtp' EventLevel='Info'><AppenderRef Name='SmtpAppender' /></Logger>
</Loggers>
        </SmplLog>",
      toAddress,
      fromAddress,
      server);

      XmlDocument doc = new XmlDocument();
      doc.LoadXml(xml);

      ConfigurationDeserializer.InitialiseFromConfigurationData(doc);

      ILogger logger = LogManager.GetLogger("Smtp");
      logger.Log(EventLevel.Info, "Testing :-)");

      Thread.Sleep(50000);

      Assert.AreEqual(1, 1);
    }
  }
}
