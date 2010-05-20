using System;

namespace SmplLog.Core
{
  public class ConsoleAppender : AppenderBase
  {
    public ConsoleAppender(string name,
        AppenderInitializationData configInitialisationData)
      : base(name)
    {
      MsgOnly = configInitialisationData.GetInitialisationElementValue<bool>("MsgOnlyFormat");
    }
            
    public override void WriteLogEvent(ILogger logger, LogEventBase logEvent)
    {
      if (MsgOnly)
      {
        Console.WriteLine(logEvent.Message);
        return;
      }
      
      Console.WriteLine(logEvent.ToString());
    }

    /// <summary>
    /// No log event formatting - just msg
    /// </summary>
    bool MsgOnly { get; set; }
  }
}
