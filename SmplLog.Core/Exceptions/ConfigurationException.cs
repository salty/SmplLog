using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace SmplLog.Core
{
  public class ConfigurationException : Exception
  {
    public ConfigurationException()
    {
    }

    public ConfigurationException(string message)
      : base(message)
    {
    }

    public ConfigurationException(string message, Exception ex)
      : base(message, ex)
    {
    }

    public ConfigurationException(SerializationInfo info, StreamingContext context) : base (info, context)
    {
    }
  }
}
