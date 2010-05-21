using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using SmplLog.Core;

namespace SmplLog.Core
{
  public interface IAppender : IDisposable
  {
    string Name { get; set; }
    bool IsValid { get; set; }
    void WriteLogEvent(ILogger logger, LogEventBase logEvent);
  }
}
