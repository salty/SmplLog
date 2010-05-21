using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using SmplLog.Core;
using System.Data;
using System.Data.SqlClient;

namespace SmplLog.UnitTests
{
  [TestFixture]
  public class SqlDbAppenderTests
  {
    string connString = @"Data Source=SFTWR-EVAL-KC\sqlexpress;Initial Catalog=MyQ;Integrated Security=True;Pooling=False";

    [Test]
    public void Can_Create_Valid_Sql_Db_Appender_Via_InitialisationParams()
    {
    }

    [Test]
    public void Can_Write_Log_Event_To_Db()
    {
      // Setup
      ILogger logger = new Logger("testLogger", null, EventLevel.Debug);
      logger.AddAppender(new SqlDbAppender("SqlDbAppender") { ConnectionString = connString, CacheSize=0 });
      
      // Act
      logger.Log(EventLevel.Debug, "Logged to the Db?");

      // Assert
      DataTable dt = GetLoggedRows();      
      Assert.Greater(0, dt.Rows.Count);
    }

    private DataTable GetLoggedRows()
    {
      using (SqlConnection conn = new SqlConnection(connString))
      {
        conn.Open();

        SqlCommand cmd = new SqlCommand("select * from LogEvent", conn);
        SqlDataAdapter adaptor = new SqlDataAdapter();
        adaptor.SelectCommand = cmd;

        DataTable resultsTable = new DataTable();
        adaptor.Fill(resultsTable);

        return resultsTable;
      }
    }
  }
}
