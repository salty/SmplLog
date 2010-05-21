using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace SmplLog.Core
{
  public class AssetFactory : IAssetFactory
  {
    List<ILogger> loggers;
    List<IAppender> appenders;

    #region IAssetFactory Members

    //public ILogger GetLogger(string loggerId)
    //{
    //  return loggers.Find(
    //    delegate (ILogger logger) { return string.Compare(logger.LoggerId, loggerId, }
    //}
    
    #endregion
  }
}
