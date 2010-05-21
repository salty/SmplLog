using System;
using System.Collections.Generic;
using System.Xml;

namespace SmplLog.Core
{
  /// <summary>
  /// Create concrete instances of Loggers and Appenders from a declarative Xml file  
  /// </summary>
  /// <remarks>
  /// Existing serializers like the XmlSerilizer cannot perform the task because they will
  /// not Deserialize inherited Appender classes which would mean no new Appenders would be able to 
  /// be deserialized.
  /// </remarks>
  public class ConfigurationDeserializer
  {
    static bool configInitialised = false;

    /// <summary>
    /// A tempororary list constructed while deserializing Appenders
    /// </summary>
    static List<IAppender> deserializedAppenders;

    /// <summary>
    /// Create a new instance of the ConfigurationDeserializer class
    /// </summary>
    public ConfigurationDeserializer()
    {
      deserializedAppenders = new List<IAppender>();
    }

    /// <summary>
    /// Only here because this is a static class which prevents initialising the config more > once
    /// Which means when unit tests are run in the same session which try to inject their own config 
    /// by calling InitialiseFromConfigurationData, they fail!
    /// </summary>
    public static void ClearConfig()
    {
      configInitialised = false;
    }

    /// <summary>
    /// Create loggers and appenders from the XlmDocument passed via the configSection parameter
    /// </summary>
    /// <param name="configSection"></param>
    /// <remarks>This is public only for unit test</remarks>
    public static void InitialiseFromConfigurationData(XmlDocument configSection)
    {
      // Can't see a reason why we'd want to init the logger hierarchy > once
      if (configInitialised)
        return;

      configInitialised = true;

      if (configSection == null)
        throw new ArgumentNullException("config");

      deserializedAppenders = new List<IAppender>();

      // check there are appenders to process
      XmlNodeList appenders = configSection.SelectNodes("//Appender");
      if (appenders == null) return;

      // check there are loggers to process
      XmlNodeList loggers = configSection.SelectNodes("//Logger");
      if (loggers == null) return;

      try
      {
        DeserializeAppenders(appenders);
        DeserializeLoggers(loggers);
      }
      catch (ConfigurationException cfgEx)
      {
        LogManager.LogInternalEvent(EventLevel.Error, "SmplLog Configuration Error.", cfgEx);
        
        throw;
      }
      catch (Exception ex)
      {        
        LogManager.LogInternalEvent(EventLevel.Error, "SmplLog Configuration Error.", ex);

        throw;
      }
    }

    /// <summary>
    /// Create appenders from the XlmDocument passed via the configSection parameter
    /// </summary>
    /// <param name="appenders"></param>
    /// <remarks>This is public only for unit test</remarks>
    public static void DeserializeAppenders(XmlNodeList appenders)
    {
      LogManager.LogInternalEvent(EventLevel.Info, "Deserializing appenders ...", null);

      // define attributes that must be specified for an appender
      string[] requiredAttrbiutes = new string[] { "Name", "Type" };

      foreach (XmlNode appenderNode in appenders)
      {
        // ensure required attributes are present
        ValidateRequiredAttributes(appenderNode, requiredAttrbiutes);

        // appender type must be specified
        string type = appenderNode.Attributes["Type"].Value;
        // as must be the name
        string name = appenderNode.Attributes["Name"].Value;
        // if appender has params, pass them to the AppenderInitializationData class for parsing
        AppenderInitializationData apInitData = GetAppenderParams(appenderNode);

        // create an instance of the appender
        Type appenderType = Type.GetType(type);
        if (appenderType == null)
        {
          throw new ConfigurationException(
              string.Format("Cannot create appender {0}, the type is invalid",
              name));
        }

        LogManager.LogInternalEvent(EventLevel.Info,
          string.Format("Creating appender {0} of type {1}", name, appenderType), null);

        // create an instance of the appender type        
        IAppender appender = CreateType<IAppender>(appenderType, new object[] { name, apInitData }, "Appender");

        if (appender != null)
        {
          // add the appender to the appender pool          
          deserializedAppenders.Add(appender);
        }
      }
    }
   
    /// <summary>
    /// Create loggers from the XlmDocument passed via the configSection parameter
    /// </summary>
    internal static void DeserializeLoggers(XmlNodeList loggers)
    {
      LogManager.LogInternalEvent(EventLevel.Info, "Deserializing loggers ...", null);

      string[] requiredAttrbiutes = new string[] { "Id" };

      // read loggers, create a logger only if valid appenders specified
      foreach (XmlNode loggerNode in loggers)
      {
        ValidateRequiredAttributes(loggerNode, requiredAttrbiutes);

        // get required attributes        
        string id = loggerNode.Attributes["Id"].Value;
        
        // get the appenders attached to the logger
        XmlNodeList loggerAppenders = loggerNode.SelectNodes("AppenderRef");
        bool loggerHasAppenders = loggerAppenders != null && loggerAppenders.Count > 0;
        
        // stop processing the logger if there are no appenders referenced (no default used)
        if (!loggerHasAppenders) continue;

        // ensure the appender-refs all point to a valid appender
        ValidateAppenderReferencesForLogger(id, loggerAppenders);

        // get the type and ensure it's valid
        XmlAttribute type = loggerNode.Attributes["Type"];
        Type loggerType = null;
        if (type != null) loggerType = Type.GetType(type.Value);

        // if the logger type is not specified, create a Logger 
        if (loggerType == null) loggerType = typeof(Logger);

        // get the eventLevel - if not provided a default of Info is used
        EventLevel loggerEventLevel = GetLoggerEventLevelFromConfigNode(loggerNode, id);

        // set parent        
        string parentId = Common.RootLoggerId;
        XmlAttribute parent = loggerNode.Attributes["Parent"];
        if (parent != null)
          parentId = parent.Value;
        
        LogManager.LogInternalEvent(EventLevel.Info,
          string.Format("Creating logger {0} of type {1} with EventLevel {2}, parent {3}", id, loggerType, loggerEventLevel, parentId), null);

        // create an instance of the logger
        ILogger logger = CreateType<ILogger>(loggerType, new object[] { id, parentId, loggerEventLevel }, "Logger");
        
        // add valid appenders
        foreach (XmlNode loggerAppender in loggerAppenders)
        {          
          // todo readonly prop!
          logger.AddAppender(deserializedAppenders.Find(
            delegate(IAppender appender) { return appender.Name == loggerAppender.Attributes["Name"].Value; })
          );

          // TODO LOG

          LogManager.AddLogger(logger);
        }
      }
    }

    /// <summary>
    /// Get the event level for the logger
    /// </summary>
    /// <param name="loggerNode"></param>
    /// <param name="loggerId"></param>
    /// <returns></returns>
    public static EventLevel GetLoggerEventLevelFromConfigNode(XmlNode loggerNode,
      string loggerId)
    {
      string eventLevelAttributeName = "EventLevel";

      // get the eventLevel and ensure it's valid
      if (loggerNode.Attributes[eventLevelAttributeName] == null) return EventLevel.Info;

      EventLevel eventLevel;
      string configEventLevel = loggerNode.Attributes[eventLevelAttributeName].Value;      
      try
      {
        eventLevel = (EventLevel)Enum.Parse(typeof(EventLevel), configEventLevel);
      }
      catch
      {
        throw new ConfigurationException(
          string.Format("The event level is invalid for logger {0]. Specify Debug, Info, Warn, Error, or Fatal.", loggerId));
      }

      return eventLevel;
    }

    /// <summary>
    /// Extract all appenders listed against the logger.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="loggerAppenders"></param>
    /// <returns></returns>
    private static void ValidateAppenderReferencesForLogger(string id, XmlNodeList loggerAppenders)
    {   
      // iterate the <appender> nodes in the config, creating an appender for each
      foreach (XmlNode loggerAppender in loggerAppenders)
      {
        // get the appender name
        string appenderRefName = loggerAppender.Attributes["Name"].Value;
        
        // find the appender in the deserializedAppenders collection
        if (deserializedAppenders.Find(
          delegate(IAppender appender) { return appender.Name == appenderRefName; }) == null)
        {
          // an appender referenced by a logger was not defined
          throw new ConfigurationException(
            string.Format("Logger {0} has a reference to an invalid appender {1}",
            id, appenderRefName));
        }
       
        // ensure the appender-ref required attributes are present
        string[] requiredAttrbiutes = new string[] { "Name" };
        ValidateRequiredAttributes(loggerAppender, requiredAttrbiutes);
      }      
    }

    /// <summary>
    /// Validate that the required nodes of an appender are present
    /// </summary>
    /// <param name="appenderNode"></param>
    /// <returns></returns>
    private static void ValidateRequiredAttributes(XmlNode appenderNode, string[] requiredAttributes)
    {
      foreach (string requiredAttributeName in requiredAttributes)
      {
        if (appenderNode.Attributes[requiredAttributeName] == null)
          throw new ConfigurationException(string.Format("Logger or Appender or AppenderRef {1} missing required attribute {0}",
            appenderNode.InnerText, requiredAttributeName));
      }
    }

    /// <summary>
    /// Create an instance of a runtime type
    /// </summary>
    /// <param name="typeToCreate"></param>
    /// <param name="ctorArgs"></param>
    /// <param name="exceptionObjectType"></param>
    /// <returns></returns>
    private static T CreateType<T>(Type typeToCreate,
      object[] ctorArgs,
      string exceptionObjectType) where T : class
    {      
      return Activator.CreateInstance(typeToCreate, ctorArgs) as T;         
    }

    /// <summary>
    /// Extract the <params> tag from an appender node and create a new AppenderInitializationData class
    /// </summary>
    /// <param name="appenderNode"></param>
    /// <returns></returns>
    private static AppenderInitializationData GetAppenderParams(XmlNode appenderNode)
    {
      if (appenderNode.ChildNodes.Count > 0)
      {
        XmlNode appenderParams = appenderNode.SelectSingleNode("Params");
        return new AppenderInitializationData(appenderParams.OuterXml);
      }

      return null;
    }
  }
}
