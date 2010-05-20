using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SmplLog.Core
{
  public interface IAssetFactory
  {
    ILogger GetLogger(string loggerId); 
    LogEvent GetLogEvent(string message, DateTime occurred, EventLevel level);    
  }
}
