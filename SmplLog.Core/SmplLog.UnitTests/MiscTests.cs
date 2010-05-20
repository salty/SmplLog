using System;
using System.Collections.Generic;

using SmplLog.Core;
using NUnit.Framework;

namespace SmplLog.Tests
{
  [TestFixture]
  public class MiscTests : TestBase
  {
    [Test]
    public void AppenderInitializationDataReturnsValidConfigNode()
    {
      string expectedNodeName = "Fred";
      string configData = @"<Params><Name>Fred</Name><Number>1</Number></Params>";
      AppenderInitializationData apData = new AppenderInitializationData(configData);

      Assert.AreEqual(expectedNodeName, apData.GetInitialisationElementAsNode("Name").InnerText);
    }

    [Test]
    public void AppenderInitializationDataReturnsValidTypedValue()
    {
      int expectedNumber = 1;
      string configData = @"<Params><Name>Fred</Name><Number>1</Number></Params>";
      AppenderInitializationData apData = new AppenderInitializationData(configData);

      Assert.AreEqual(expectedNumber, apData.GetInitialisationElementValue<Int32>("Number"));
    }

    [Test]
    public void AppenderInitializationDataReturnsDefaultValueForInvalidCast()
    {
      int expectedNumber = default(int);
      string configData = @"<Params><Name>Fred</Name><Number>1</Number></Params>";
      AppenderInitializationData apData = new AppenderInitializationData(configData);

      Assert.AreEqual(expectedNumber, apData.GetInitialisationElementValue<Int32>("Name"));      
    }
  }
}
