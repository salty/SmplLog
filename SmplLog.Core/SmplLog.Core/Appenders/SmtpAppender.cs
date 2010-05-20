
using System;
using System.Net.Mail;
using System.ComponentModel;

namespace SmplLog.Core
{
  public sealed class SmtpAppender : AppenderBase
  {
    static bool mailSent = true;
    SmtpClient client = null;

    private string toAddress;
    public string ToAddress
    {
      get { return toAddress; }
    }

    private string fromAddress;
    public string FromAddress
    {
      get { return fromAddress; }
    }

    private string smtpServer;
    public string SMTPServer 
    {
      get { return smtpServer; } 
    }

    private string subject;
    public string Subject
    {
      get { return subject; } 
    }

    private bool isHtml = true;
    public bool IsHtml
    {
      get { return isHtml; }
    }

    private int timeout = 100000;
    public int Timeout
    {
      get { return timeout; }      
    }

    public SmtpAppender(string name,
        AppenderInitializationData configInitialisationData) : base(name)
    {
      if (configInitialisationData == null)
      {
        LogManager.LogInternalEvent(EventLevel.Error, "The appender requires .... It will be disabled", null);
        this.IsValid = false;
        return;
      }

      toAddress = configInitialisationData.GetInitialisationElementValue<string>("ToAddress");
      if (toAddress == null)
      {
        LogManager.LogInternalEvent(EventLevel.Error, "The appender requires a ToAddress parameter which is not present. It will be disabled", null);
        this.IsValid = false;
        return;
      }

      fromAddress = configInitialisationData.GetInitialisationElementValue<string>("FromAddress");
      if (fromAddress == null)
      {
        LogManager.LogInternalEvent(EventLevel.Error, "The appender requires a FromAddress parameter which is not present. It will be disabled", null);
        this.IsValid = false;
        return;
      }

      subject = configInitialisationData.GetInitialisationElementValue<string>("Subject");
      if (subject == null)
      {
        LogManager.LogInternalEvent(EventLevel.Error, "The appender requires a Subject parameter which is not present. It will be disabled", null);
        this.IsValid = false;
        return;
      }

      smtpServer = configInitialisationData.GetInitialisationElementValue<string>("SMTPServer");
      if (smtpServer == null)
      {
        LogManager.LogInternalEvent(EventLevel.Error, "The appender requires a SMTPServer parameter which is not present. It will be disabled", null);
        this.IsValid = false;
        return;
      }

      bool? isHtmlFromConfig = configInitialisationData.GetInitialisationElementValue<bool>("IsHtml");
      if (isHtmlFromConfig == null)
      {
        isHtml = isHtmlFromConfig.Value;
      }

      string timeoutFromConfig = configInitialisationData.GetInitialisationElementValue<string>("Timeout");
      if (!string.IsNullOrEmpty(timeoutFromConfig))
      {                
        bool ok = int.TryParse(timeoutFromConfig, out timeout);
        if (!ok)
        {
          LogManager.LogInternalEvent(EventLevel.Error, 
            string.Format("Timeout was supplied but invalid, using default ({0})", timeout), null);
        }
      }

      client = new SmtpClient(smtpServer);
      client.Timeout = timeout;
      client.SendCompleted += new SendCompletedEventHandler(SendCompletedCallback);

      //client.DeliveryMethod = SmtpDeliveryMethod.
      //client.EnableSsl = 
      //client.Port = 
      //client.Timeout
      //client.UseDefaultCredentials = 
    }

    public SmtpAppender(string name, 
      string to, 
      string from, 
      string subject, 
      string smtpServer,
      bool isHtml) : base(name)
    {
      this.toAddress = to;
      this.fromAddress = from;
      this.subject = subject;
      this.smtpServer = smtpServer;
      this.isHtml = isHtml;
           
      client = new SmtpClient(smtpServer);     
    }

    public override void WriteLogEvent(ILogger logger, LogEventBase logEvent)
    {
      try
      {
        MailAddressCollection mac = new MailAddressCollection();
        mac.Add(new MailAddress(toAddress));

        MailMessage msg = new MailMessage(fromAddress, toAddress, subject, logEvent.ToString());                
        mailSent = false;
        client.SendAsync(msg, failCount);
        
        //msg.BodyEncoding = 
        //msg.BodyFormat = isHtml ? MailFormat.Html : MailFormat.Text;
        //msg.Priority = 
        //msg.Bcc
        //msg.Cc =
        //msg.Attachments = 
        //msg.DeliveryNotificationOptions = DeliveryNotificationOptions.        
      }
      catch (Exception ex)
      {
        LogManager.LogInternalEvent(EventLevel.Error,
          string.Format("SMTP appender {0} failed to send", this.Name), ex);

        base.IncrementFailCount();
      }
    }
    
    private void SendCompletedCallback(object sender, AsyncCompletedEventArgs e)
    {            
      if (e.Cancelled)
      {
        LogManager.LogInternalEvent(EventLevel.Warn, "Send canceled.", null);
      }
      if (e.Error != null)
      {                
        LogManager.LogInternalEvent(EventLevel.Error,
          String.Format("{0}", e.Error.ToString()), e.Error.InnerException);

        ((AppenderBase)sender).IncrementFailCount();
      }
            
      mailSent = true;
    }

    protected override void Dispose(bool isDisposing)
    {
      if (!isDisposed)
      {
        if (mailSent == false)
        {
          client.SendAsyncCancel();          
        }

        if (isDisposing)
        {                    
        }

        isDisposed = true;
      }

      base.Dispose(isDisposing);
    }
  }
}
