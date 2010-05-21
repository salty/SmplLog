using System;
using System.Collections.Generic;
using System.Xml;

namespace SmplLog.Core
{
  public sealed class AppenderInitializationData
  {
    private readonly XmlDocument initializationData;

    /// <summary>
    /// Create a new instance of the AppenderInitializationData class
    /// </summary>
    /// <param name="data"></param>
    public AppenderInitializationData(string data)
    {
      if (!string.IsNullOrEmpty(data))
      {
        this.initializationData = new XmlDocument();
        this.initializationData.LoadXml(data);
      }
    }

    /// <summary>
    /// Return the node with the name specified by rootNodeName
    /// </summary>
    /// <param name="rootNodeName"></param>
    /// <returns></returns>
    public XmlNode GetInitialisationElementAsNode(string rootNodeName)
    {
      return initializationData.SelectSingleNode("//" + rootNodeName);
    }

    /// <summary>
    /// Return the innertext of a node as a typed value 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="nodeName"></param>
    /// <returns></returns>    
    public T GetInitialisationElementValue<T>(string nodeName) where T : IConvertible
    {
      XmlNode initializationDataNode = GetInitialisationElementAsNode(nodeName); 
      
      if (initializationDataNode != null)
      {
        // apparently a little slow since it just checks the type and then performs the cast
        // but we're not concerned overly about perf during init
        try
        {
          return (T)Convert.ChangeType(initializationDataNode.InnerText, typeof(T));
        }
        catch
        {
          // naughty. yah so what? we're just easing the burden on callers having to 
          // trap each call to the method, of course callers will have to check for the value
          // of the default below which might in rare cases be valid but it beats a method for
          // every value type :-)
        }
      }

      return default(T);
    }    
  }
}
