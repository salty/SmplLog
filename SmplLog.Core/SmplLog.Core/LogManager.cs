using System;
using System.Globalization;
using System.Collections.Generic;
using System.Text;
using System.Web;
using System.Configuration;
using System.IO;
using System.Xml;
using System.Reflection;
using System.Diagnostics;

namespace SmplLog.Core
{
  /// <summary>
  /// The LogManager class manages the collection of Loggers and Appenders that combine to
  /// produce log output.
  /// 
  /// The LogManager can construct a Logger hierarchy from a configuration file located in the application or
  /// bin folder, the app.config file if the application has one or the web.config file if the application
  /// is a web application.
  /// 
  /// It also provides the AddLogger method for programmatically creating a Logger hierarchy.
  /// 
  /// A "client" application will retrieve a logger via the GetLogger method.
  /// </summary>
  public sealed class LogManager
  {
    static bool initialised = false;
    static List<IAppender> appenderPool;
    static List<ILogger> loggerPool;
    
    static string newLine = System.Environment.NewLine;
    
    // TODO - move to res    
    static string smplLogDefaultConfigName = "SmplLog.config";

    /// <summary>
    /// Initialize a new instance of the LogManager class.
    /// </summary>
    static LogManager()
    {
      Init();
      LoadConfig(GetCurrentPath());
    }

    static void FromPath(string configPath)
    {
      LoadConfig(configPath);
    }

    static void Init()
    {
      // initialise the appender pool
      appenderPool = new List<IAppender>();

      // initialise the logger pool
      loggerPool = new List<ILogger>();

      // add the root logger
      ILogger rootLogger = new Logger(Common.RootLoggerId, null, EventLevel.Debug);
      loggerPool.Add(rootLogger);

      // create a default apppender      
      rootLogger.AddAppender(new TraceAppender("_Default"));      
    }

    private static void LoadConfig(string configPath)
    {
      initialised = true;

      // search the application path          
      try
      {        
        InitialiseFromPath(Path.Combine(configPath, smplLogDefaultConfigName));
      }
      catch (FileNotFoundException fnfe)
      {
        // Dont allow exception in the logmanger to affect the main application.
        // Expect the user to determine the issue when ... no logging occurs
        LogInternalEvent(EventLevel.Error, "Failed to locate config file", fnfe); 
      }      
    }

    /// <summary>
    /// Get an existing logger from the pool maintained by the LogManager.
    /// </summary>
    /// <param name="loggerId"></param>
    /// <returns>The logger if found, otherwise null.</returns>
    public static ILogger GetLogger(string loggerId)
    {
      Trace.Assert(loggerPool.Count > 0);

      ILogger foundLogger = loggerPool.Find(
        delegate(ILogger logger)
        {
          return string.Compare(logger.LoggerId, loggerId, false, CultureInfo.InvariantCulture) == 0;
        });

      // always return a logger, amongst other things this prevents clients from thowing
      // NullReferenceExceptions.
      if (foundLogger == null)
      {
        LogInternalEvent(EventLevel.Error, 
          string.Format("GetLoggerCalled with id {0} which was not found. Returning root logger.", loggerId), null);
        return GetLogger(Common.RootLoggerId);
      }

      return foundLogger;
    }
   
    public static ILogger AddLogger(ILogger loggerToAdd)
    {
      ILogger existingLogger = loggerPool.Find(
        delegate(ILogger logger) { return logger.LoggerId == loggerToAdd.LoggerId; });

      // if a logger is found return it, 
      if (existingLogger != null) return existingLogger;

      loggerPool.Add(loggerToAdd);
      return loggerToAdd;
    }

    /// <summary>
    /// Add a new logger to the logger pool.
    /// </summary>
    /// <param name="loggerId"></param>
    /// <param name="parentLoggerId"></param>
    /// <returns>The new logger if an existing logger is not found, otherwise the existing logger</returns>
    public static ILogger AddLogger(string loggerId,
      string parentLoggerId,
      EventLevel loggerEventLevel)
    {
      // attempt to find the logger by Id
      ILogger existingLogger = loggerPool.Find(
        delegate(ILogger logger) { return logger.LoggerId == loggerId; });

      // if a logger is found return it, 
      if (existingLogger != null) return existingLogger;

      // otherwise add the logger to the logger pool and return it
      ILogger newLogger = new Logger(loggerId, parentLoggerId, loggerEventLevel);
      loggerPool.Add(newLogger);
      return newLogger;
    }

    internal static IAppender AddAppender(IAppender appenderToAdd)
    {
      IAppender existingAppender = appenderPool.Find(
       delegate(IAppender appender) { return appender.Name == appenderToAdd.Name; });

      // if a logger is found return it, 
      if (existingAppender != null) return existingAppender;

      appenderPool.Add(appenderToAdd);
      return appenderToAdd;
    }

    /// <summary>
    /// Output internal messages to System.Diagnostics targets to allow users to determine issues with
    /// the Loggers or Appenders using DbgView
    /// </summary>
    /// <param name="eventLevel"></param>
    /// <param name="message"></param>
    /// <param name="ex"></param>
    public static void LogInternalEvent(EventLevel eventLevel,
      string message,
      Exception ex)
    {
      StringBuilder sb = new StringBuilder();

#if(DEBUG)
      Debug.WriteLine(
        string.Format("[0] {1} {2}\r\n{3}", eventLevel,
          DateTime.Now,
          message,
          ex == null ? string.Empty : ExpandException(ex, sb))
       );
#else
      Trace.WriteLine(
        string.Format("[0] {1} {2}\r\n{3}", eventLevel,
          DateTime.Now,
          message,
          ex == null ? string.Empty : ExpandException(ex, sb)));
#endif
    }

    /// <summary>
    /// For class libraries that are hosted inside components like the WCF or Workflow runtimes,
    /// a path specifed by the path parameter can be used to locate the configuration file
    /// </summary>
    /// <param name="path"></param>
    public static bool InitialiseFromPath(string path)
    {
      XmlDocument configSection = GetConfigurationFromPath(path);

      if (configSection != null)
      {
        ConfigurationDeserializer.InitialiseFromConfigurationData(configSection);
        return true;
      }

      return false;
    }

    /// <summary>
    /// Construct the path to the bin folder containing the assembly
    /// </summary>
    /// <returns></returns>
    public static string GetCurrentPath()
    {
      try
      {
        if (HttpRuntime.UsingIntegratedPipeline)
        {
          LogInternalEvent(EventLevel.Info, "Integrated pipline mode detected, using current path " + HttpRuntime.AppDomainAppPath, null);
          return HttpRuntime.AppDomainAppPath;
        }
      }
      catch (Exception)
      {
      }

      if (HttpContext.Current != null)
      {
        return HttpContext.Current.Request.PhysicalApplicationPath;
      }
      else
      {
        // If the exe is in bin, assume config file is in root
        string binPath = "\\bin\\";
        string currentAssembly = Assembly.GetCallingAssembly().Location;

        int binIndex = currentAssembly.IndexOf(binPath);
        if (binIndex > 0)
          return currentAssembly.Substring(0, binIndex);

        return currentAssembly.Substring(0, currentAssembly.LastIndexOf('\\'));
      }
    }
    
    /// <summary>
    /// Attempt to load the config data from a file
    /// </summary>
    /// <param name="pathToSearch">The path of the file containing the config data</param>
    /// <returns>An XmlDocument containing configuration data or null if no configsection is found.</returns>
    private static XmlDocument GetConfigurationFromPath(string pathToSearch)
    {      
      XmlDocument configDoc = new XmlDocument();
      configDoc.Load(pathToSearch);

      return configDoc;
    }
   
    private static string ExpandException(Exception exception, StringBuilder sb)
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
