using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SmplLog.Core
{
  /// <summary>
  /// Forwards raw LogEvents to a destination via an event
  /// </summary>
  public class CallbackAppender : AppenderBase
  {
    public delegate void ItemLogged(LogEventBase logEvent);
    public event ItemLogged OnItemLogged;

    public CallbackAppender(string name) : base(name)
    {
    }

    public override void WriteLogEvent(ILogger logger, LogEventBase logEvent)
    {
      if (OnItemLogged != null)
      {
        OnItemLogged(logEvent);
      }
    }

  }
}
