using System;

namespace SmplLog.Core
{
  /// <summary>
  /// A base class for SmplLog appenders
  /// </summary>
  public class AppenderBase : IAppender
  {
    #region Properties
    
    /// <summary>
    /// Records the number of consecutive times the LogAppender has failed to log. If this value reached
    /// the failCountLimit, the LogAppender is marked as Invalid and will not participate in logging.
    /// </summary>
    protected byte failCount = 0;

    /// <summary>
    /// If a LogAppender fails consecutively this many times, it is marked as Invalid and its 
    /// LogEvent method will no longer be called
    /// </summary>
    protected byte failCountLimit = 5;

    /// <summary>
    /// Flags an instance has been disposed
    /// </summary>
    protected bool isDisposed = false;

    private bool isValid = true;
    /// <summary>
    /// Indicates that the appender is valid and the call to LogEvent can be made
    /// </summary>    
    public bool IsValid
    {
      get { return isValid; }
      set { isValid = value; }
    }

    private string name;
    /// <summary>
    /// The name of the appender
    /// </summary>
    public string Name
    {
      get { return name; }
      set { name = value; }
    }

    /// <summary>
    /// Used for thread safe code regions
    /// </summary>
    protected object appenderSynchLock = new object();

    #endregion

    public AppenderBase(string name)
    {
      this.name = name;
    }
    
    public virtual void WriteLogEvent(ILogger logger, LogEventBase logEvent)
    {
      // Commented exception as the AppenderBase will be called when parent logger is not null. 
      throw new NotImplementedException();
    }

    /// <summary>
    /// Record the failure of a LogAppender to log. If the LogAppender has exceeded the failCount,
    /// mark it is marked as Inactive
    /// </summary>
    /// <remarks>Public because appenders that use async methods require public methods 
    /// </remarks>
    public void IncrementFailCount()
    {
      if (failCount++ >= failCountLimit)
      {
        isValid = false;
      }
    }

    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(Boolean isDisposing)
    {
    }

    ~AppenderBase()
    {
      Dispose(false);
    }
  }
}
