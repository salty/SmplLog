using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;

namespace SmplLog.Core
{
  public class SqlDbAppender : AppenderBase
  {
    string connectionString;
    public string ConnectionString
    {
      get { return connectionString; }
      set { connectionString = value; }
    }

    string insertStatement;
    public string InsertStatement
    {
      get { return insertStatement; }
      set { insertStatement = value; }
    }

    /// <summary>
    /// Determines how many logEvents we'll collect before flushing to the DB. 0 = no cache and immediate flush
    /// </summary>
    byte cacheSize = 10;
    public byte CacheSize
    {
      get { return cacheSize; }
      set { cacheSize = value; }
    }

    public SqlDbAppender(string name) : base(name)
    {
      insertStatement = "insert LogEvent (DateLogged, Severity, Location, Message, Data) values (@dateLogged, @severity, @location, @message, @data)";
    }

    public SqlDbAppender(string name, AppenderInitializationData configInitialisationData)
      : base(name)
    {
      if (configInitialisationData == null)
      {
        LogManager.LogInternalEvent(EventLevel.Error, "The appender requires initialization data which was not supplied (invalid config?). It will be disabled", null);
        this.IsValid = false;
        return;
      }

      connectionString = configInitialisationData.GetInitialisationElementValue<string>("dbConnectionString");
      if (string.IsNullOrEmpty(connectionString))
      {
        LogManager.LogInternalEvent(EventLevel.Error, "No connection string was found in the initialisation data. Appender will be disabled.", null);
        this.IsValid = false;
        return;
      }

      // Check Db

      // Set default insert statement
      insertStatement = "insert LogEvent (DateLogged, Severity, Location, Message, Data) values (@dateLogged, @severity, @location, @message, @data)";
    }
    
    public virtual void PrepareInsertCommand(LogEventBase logEvent, ref SqlCommand cmd)
    {
      // TODO. Should not be required
      LogEvent evt = logEvent as LogEvent;

      cmd.Parameters.Add(new SqlParameter("@dateLogged", evt.Occurred));
      cmd.Parameters.Add(new SqlParameter("@severity", evt.LogEventLevel));
      cmd.Parameters.Add(new SqlParameter("@location", evt.Location));
      cmd.Parameters.Add(new SqlParameter("@message", evt.Message));
      cmd.Parameters.Add(new SqlParameter("@data", evt.ExceptionFormatted));     
    }

    public override void WriteLogEvent(ILogger logger, LogEventBase logEvent)
    {
      using (SqlConnection conn = new SqlConnection(connectionString))
      {
        conn.Open();

        SqlCommand cmd = new SqlCommand(insertStatement, conn);
        PrepareInsertCommand(logEvent, ref cmd);

        cmd.ExecuteNonQuery();

        conn.Close();
      }
    }
  }
}
