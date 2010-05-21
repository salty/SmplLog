using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Runtime.CompilerServices;

namespace SmplLog.Core
{  
  /// <summary>
  /// Appender for unit testing
  /// </summary>
  public class TestAppender : AppenderBase
  {
    private List<string> logEventBuffer;
    /// <summary>
    /// Store LogEvents "output" by the WriteLogEvent method
    /// </summary>
    public List<string> LogEventBuffer
    {
      get { return logEventBuffer; }     
    }

    /// <summary>
    /// Retrieves the last LogEvent index  
    /// </summary>
    public int LastEventIndex
    {
      get { return logEventBuffer.Count - 1; }
    }

    public TestAppender(string name) : base(name)
    {
      logEventBuffer = new List<string>();
    }

    public TestAppender(string name, AppenderInitializationData apInitData) : this(name)
    {
    }

    #region IAppender Members

    public override void WriteLogEvent(ILogger logger, LogEventBase logEvent)
    {
      logEventBuffer.Add(logEvent.ToString());
    }

    #endregion    
  }
}
