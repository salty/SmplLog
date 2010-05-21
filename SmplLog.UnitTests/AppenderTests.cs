using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;

using SmplLog.Core;
using NUnit.Framework;

namespace SmplLog.Tests
{
  /// <summary>
  /// Summary description for UnitTest1
  /// </summary>
  [TestFixture]
  public class AppenderTests : TestBase
  {
    public AppenderTests()
    {      
    }  

    #region Additional test attributes
    //
    // You can use the following additional attributes as you write your tests:
    //
    // Use ClassInitialize to run code before running the first test in the class
    // [ClassInitialize()]
    // public static void MyClassInitialize(TestContext testContext) { }
    //
    // Use ClassCleanup to run code after all tests in a class have run
    // [ClassCleanup()]
    // public static void MyClassCleanup() { }
    //
    // Use TestInitialize to run code before running each test 
    // [TestInitialize()]
    // public void MyTestInitialize() { }
    //
    // Use TestCleanup to run code after each test has run
    // [TestCleanup()]
    // public void MyTestCleanup() { }
    //
    #endregion

    [Test]
    public void TestAppendToLogBasic()
    {
      LogEvent logEvent = GetLogEvent();
      testAppender.WriteLogEvent(null, logEvent);

      Assert.AreEqual(testAppender.LogEventBuffer.Count, 1);
    }    
  }
}
