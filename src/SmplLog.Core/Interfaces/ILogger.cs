using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace SmplLog.Core
{
  public interface ILogger
  {    
    string LoggerId {get;set;}    
    ILogger Parent { get; }
    EventLevel LoggerEventLevel { get; }
    int AppenderCount { get; }
    List<IAppender> Appenders { get; }

    void Log(EventLevel eventLevel, string message);
    void Log(EventLevel eventLevel, string message, object ex);
    void Log(EventLevel eventLevel, string message, string location);
    void Log(EventLevel eventLevel, string message, object ex, string location);

    void LogFormat(EventLevel eventLevel,string message, params object[] paramlist);      
    
    IAppender GetAppender(string appenderId);
    void AddAppender(IAppender appenderToAdd);
  }
}
