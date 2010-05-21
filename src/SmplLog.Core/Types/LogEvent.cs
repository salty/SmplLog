using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SmplLog.Core
{  
  /// <summary>
  /// Encapsulates an event to be logged via an Appender
  /// </summary>
  public class LogEvent : LogEventBase
  {       
    private DateTime occurred;
    /// <summary>
    /// When the event occurred   
    /// </summary>
    public DateTime Occurred
    {
      get { return occurred; }
      set { occurred = value; }
    }

    private EventLevel logEventLevel;
    /// <summary>
    /// The level assigned to the LogEvent
    /// </summary>
    public EventLevel LogEventLevel
    {
      get { return logEventLevel; }
      set { logEventLevel = value; }
    }

    private string location;
    /// <summary>
    /// The assembly / class / method where the Log call was made
    /// </summary>
    public string Location
    {
      get { return location; }
      set { location = value; }
    }

    private object data;
    /// <summary>
    /// Extra data - generally for exceptions 
    /// </summary>
    public object Data
    {
      get { return data; }
      set { data = value; }
    }

    public string ExceptionFormatted
    {
      get 
      {
        if (data == null)
          return string.Empty;

        if (data is Exception)
          return ExpandException(data as Exception, new StringBuilder());

        return data.ToString();
      }      
    }

    public override string ToString()
    {
      string temp = string.Format("[{0}] {1} {2} {3}",
        logEventLevel,        
        occurred.ToString("d-MMM-yyyy,h:m:s:fftt"),
        location,
        message
        );

      bool dataIsException = data is Exception;
      bool dataIsString = data is String;
      if (dataIsException || dataIsString)
      {
        string exceptionData = 
          dataIsException ? ExpandException((Exception)data, new StringBuilder()) : data.ToString();
        
        temp += exceptionData;
      }

      return temp;
    }
  }
}
