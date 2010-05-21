using System;
using System.Text;

namespace SmplLog.Core
{
  /// <summary>
  /// The level of the event
  /// </summary>
  public enum EventLevel
  {
    Debug,
    Info,
    Warn,
    Error,
    Fatal
  }

  /// <summary>
  /// The base class for log events
  /// </summary>
  public class LogEventBase
  {
    static string newLine = Environment.NewLine;

    // Not so sure about this. But for now we require it since the new LoggingEventQueue uses a queue.
    // And that queue can only store 1 object. And we currently pass LogEventBase. So...
    // Then again. By doing this we are only saving the speed of a foreach loop. 
    // Then again, passing the logger might be more future proof...
    public ILogger logger { get; set; }

    protected string message;
    /// <summary>
    /// A Short description of the event
    /// </summary>
    public string Message
    {
      get { return message; }
      set { message = value; }
    }

    /// <summary>
    /// Build a string containing exception information for the passed exception and it's inner 
    /// exceptions
    /// </summary>
    /// <param name="exception"></param>
    /// <param name="sb"></param>
    /// <returns></returns>
    protected virtual string ExpandException(Exception exception, StringBuilder sb)
    {
      sb.Append(exception.Message);
      sb.Append(newLine);
      sb.Append(exception.StackTrace);
      sb.Append(newLine);

      if (exception.InnerException != null)
        ExpandException(exception.InnerException, sb);

      return sb.ToString();
    }
  }
}
