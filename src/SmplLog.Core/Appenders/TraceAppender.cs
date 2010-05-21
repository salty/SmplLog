#region Using

using System;
using System.Collections.Generic;
using System.Diagnostics;

using SmplLog;
using SmplLog.Core;
using System.Collections.Specialized;

#endregion

namespace SmplLog.Core
{
  /// <summary>
  /// Thread-safe logger that outputs to valid trace targets.
  /// </summary>
  public sealed class TraceAppender : AppenderBase, IAppender
  {
    /// <summary>
    /// Constructs a new instance of the TraceAppender
    /// </summary>
    /// <remarks>Allows simpler programmatic construction</remarks>
    public TraceAppender(string name)
      : base(name)
    {
    }

    /// <summary>
    /// Create a new instance of a TraceAppender
    /// </summary>
    /// <param name="name"></param>
    /// <param name="setLogEventFormatter"></param>
    /// <param name="appenderParams"></param>
    //public TraceAppender(string name,      
    //  Dictionary<string, string> appenderParams)
    //  : base(name)
    //{      
    //}
    
    #region ILogger Members

    /// <summary>
    /// Output a log event to trace targets
    /// </summary>
    /// <param name="logEvent"></param>
    /// <returns></returns>
    public override void WriteLogEvent(ILogger logger, LogEventBase logEvent)
    {  
#if(DEBUG)          
      Debug.WriteLine(logEvent.ToString());
#else
      Trace.WriteLine(logEvent.ToString());      
#endif
    }
    
    #endregion
  }
}
