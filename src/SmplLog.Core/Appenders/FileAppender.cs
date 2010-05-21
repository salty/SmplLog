#region Using

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Web;
using System.Diagnostics;
using System.Threading;
using System.Text.RegularExpressions;

#endregion

namespace SmplLog.Core
{
  /// <summary>
  /// Thread safe appender that logs to a file specified by the filePath param
  /// </summary>
  /// <remarks>
  /// Valid Params:
  /// filePath: Absolute path of the text file. Defaults to the location / filename of the current assembly
  ///  with _log.txt appended
  /// </remarks>
  public class FileAppender : AppenderBase
  {
    //public enum LockMode
    //{
    //  // most performant, hold the streamwriter open between calls to LogEvent method but will introduce locking issues if mulitple
    //  // threads writing to the file (asp.net...)
    //  SingleAppenderLock,
    //  // least performant, the stream is only open for the duration of the LogEvent method but allows multiple processes to write to 
    //  // the file while reducing locking issues 
    //  MinimalLock
    //}

    #region Fields

    private const long NO_MAX_FILE_SIZE = -1;
    private const string TIME_STAMP_PLACEHOLDER = "{Timestamp}";
    private const string DATETIME_FORMAT = "yyyyMMdd_hh_mm_ss";

    /// <summary>
    /// Stores the file name from the config data (may be different to the eventual file path)
    /// </summary>
    private string originalFilePath;

    /// <summary>
    /// We attempt to use absolute paths. This stores the directory portion of the path
    /// </summary>
    private string directoryPath = string.Empty;

    /// <summary>
    /// The path of the file specified by the FilePath configuration parameter 
    /// </summary>
    /// 
    private string filePath = string.Empty;
    public string FilePath { get { return filePath; } }

    private TextWriter streamWriter;
    /// <summary>
    /// The textwriter used to append log events to the output file 
    /// </summary>
    private TextWriter StreamWriter
    {
      get
      {
        if (streamWriter == null)
        {
          try
          {
            StreamWriter writer;

            if (!File.Exists(filePath))
              writer = File.CreateText(filePath);
            else
              writer = File.AppendText(filePath);

            streamWriter = TextWriter.Synchronized(writer);
            failCount = 0;
          }
          catch (Exception ex)
          {
            LogManager.LogInternalEvent(EventLevel.Fatal, string.Format("Could not create the file logger for the path ", filePath), ex);
            IncrementFailCount();
          }
        }

        return streamWriter;
      }
    }

    private bool overwriteExistingFile = false;
    /// <summary>
    /// Determines if existing log files should be overwritten when MaxFileSize is reached.
    /// </summary>
    public bool OverwriteExistingFile
    {
      get { return overwriteExistingFile; }
      set { overwriteExistingFile = value; }
    }

    private long maxFileSize = NO_MAX_FILE_SIZE;
    public long MaxFileSizeBytes
    {
      get
      {
        return maxFileSize;
      }
      set
      {
        maxFileSize = value;
      }
    }

    #endregion

    /// <summary>
    /// Create a new instance of a FileLogger component.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="setLogEventFormatter"></param>
    /// <param name="configParams"></param>    
    public FileAppender(string name,
        AppenderInitializationData configInitialisationData)
      :
        base(name)
    {
      if (configInitialisationData == null)
      {
        LogManager.LogInternalEvent(EventLevel.Error, "The appender requires configuration data that was not provided. It will be disabled", null);
        this.IsValid = false;
        return;
      }

      // Get the file path
      string tmpFilePath = configInitialisationData.GetInitialisationElementValue<string>("FilePath");
      if (string.IsNullOrEmpty(tmpFilePath))
      {
        throw new ConfigurationException("The file appender path was not provided.");
      }

      // If there is a {Timestamp} in the path, replace with formatted datetime
      if (tmpFilePath.IndexOf(TIME_STAMP_PLACEHOLDER) >= 0)
      {
        tmpFilePath = tmpFilePath.Replace(TIME_STAMP_PLACEHOLDER, DateTime.Now.ToString(DATETIME_FORMAT));
      }

      // Store the original file path
      originalFilePath = Path.GetFileName(tmpFilePath);

      // If relative, attempt to convert to absolute path
      tmpFilePath = GetAbsoluteFilePath(tmpFilePath);

      // And store the directory portion
      directoryPath = Path.GetDirectoryName(tmpFilePath);

      // Are we going to overwrite any existing files?
      overwriteExistingFile = configInitialisationData.GetInitialisationElementValue<bool>("OverwriteExistingFile");

      // If overwriteExistingFile is disabled, create a new unique file name
      if (!overwriteExistingFile)
        GetUniqueFilePath(ref tmpFilePath);

      // Create the directory if it doesn't exist
      if (!Directory.Exists(Path.GetDirectoryName(tmpFilePath)))
        Directory.CreateDirectory(Path.GetDirectoryName(tmpFilePath));

      filePath = tmpFilePath;

      // Get the max file size. if not supplied, default is 0L.
      MaxFileSizeBytes = configInitialisationData.GetInitialisationElementValue<long>("MaxFileSizeBytes");
      LogManager.LogInternalEvent(EventLevel.Info, string.Format("Config specifies {0} as max file size", MaxFileSizeBytes), null);
      if (MaxFileSizeBytes == 0) MaxFileSizeBytes = GetMaxFileSize(filePath);

      if (this.IsValid)
      {
        LogManager.LogInternalEvent(EventLevel.Info,
          string.Format("FileAppender {0} successfully created at {1}. Overwrite is {2}, MaxFileSize={3}",
            this.Name,
            this.filePath,
            this.overwriteExistingFile,
          // this.lockMode,
            this.maxFileSize == NO_MAX_FILE_SIZE ? "None" : this.maxFileSize.ToString()), null);
      }
    }

    /// <summary>
    /// Constructs a new version of the FileAppender
    /// </summary>
    /// <param name="setFilePath"></param>
    /// <remarks>Allows simpler programmatic construction</remarks>
    public FileAppender(string setFilePath,
      string name)
      : base(name)
    {
      this.filePath = setFilePath;

      maxFileSize = GetMaxFileSize(GetAbsoluteFilePath(filePath));
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
    static extern bool GetVolumeInformation(string Volume, StringBuilder VolumeName, uint VolumeNameSize, out uint SerialNumber, out uint SerialNumberLength, out uint flags, StringBuilder fs, uint fs_size);

    private long GetMaxFileSize(string volumePath)
    {
      // default is ok for Fat16 / Fat32
      long maxFileSize = (long)Math.Pow(2, 32) - 1;
      int pathVolumeSize = 3;
      string volume = volumePath.Substring(0, pathVolumeSize);

      // determine volume type
      uint serialNum, serialNumLength, flags;
      StringBuilder volumename = new StringBuilder(256);
      StringBuilder fstype = new StringBuilder(256);

      bool ok = GetVolumeInformation(volume, volumename, (uint)volumename.Capacity - 1, out serialNum, out serialNumLength, out flags, fstype, (uint)fstype.Capacity - 1);

      if (ok)
      {
        switch (fstype.ToString().ToLower())
        {
          case "ntfs":
            LogManager.LogInternalEvent(EventLevel.Info, string.Format("File Appender {0} NTFS volume detected.", this.Name), null);
            maxFileSize = (long)Math.Pow(2, 64) - 1;
            break;
        }
      }

      LogManager.LogInternalEvent(EventLevel.Info, string.Format("Max file size set to {0} bytes", maxFileSize), null);

      return maxFileSize;
    }

    private string GetAbsoluteFilePath(string filePath)
    {
      if (!Path.IsPathRooted(filePath))
      {
        if (HttpContext.Current == null)
        {
          // path is relative, no http
          string path = Path.GetFullPath(filePath);

          LogManager.LogInternalEvent(EventLevel.Info, "Path is " + path, null);
          return path;
        }
        else
        {
          try
          {
            string path = HttpContext.Current.Server.MapPath(filePath);

            LogManager.LogInternalEvent(EventLevel.Info, "(Http-Mapped) path is " + path, null);

            return path;
          }
          catch (Exception ex)
          {
            LogManager.LogInternalEvent(EventLevel.Error,
              string.Format("{0} path {1} is invalid.", this.Name, filePath), ex);

            throw new ConfigurationException(string.Format("The filePath {0} was invalid for appender {1}", filePath, this.Name), ex);
          }
        }
      }
      else
      {
        LogManager.LogInternalEvent(EventLevel.Error,
              string.Format("path {0} is rooted.", filePath), null);
      }

      return filePath;
    }

    /// <summary>
    /// Generate a unique file name by adding incrementing numbers to the 
    /// supplied filePath
    /// </summary>
    /// <param name="tmpFilePath"></param>
    private void GetUniqueFilePath(ref string tmpFilePath)
    {
      if (!Path.IsPathRooted(tmpFilePath))
        tmpFilePath = directoryPath + Path.DirectorySeparatorChar + tmpFilePath;

      bool uniqueFilePathFound = !File.Exists(tmpFilePath);
      int filenamePostfix = 0;

      while (!uniqueFilePathFound)
      {
        string tmpFileName = Path.GetFileNameWithoutExtension(originalFilePath);
        tmpFileName += (++filenamePostfix).ToString();

        // reassemble the file path
        tmpFilePath =
          string.Format("{0}\\{1}{2}",
            directoryPath,
            tmpFileName,
            Path.GetExtension(originalFilePath));

        uniqueFilePathFound = !File.Exists(tmpFilePath);
      }
    }

    /// <summary>
    /// Log the event to the output file specified by the filePath parameter
    /// </summary>
    /// <param name="logEvent"></param>
    /// <returns></returns>
    /// <remarks>Exceptions propogate up the the Logger instance</remarks>
    public override void WriteLogEvent(ILogger logger, LogEventBase logEvent)
    {
      try
      {
#if (DEBUG)
          LogManager.LogInternalEvent(EventLevel.Debug,
            string.Format("Thread {2}: Appender {0} attempting to write to file {1}", this.Name, this.filePath, Thread.CurrentThread.GetHashCode()), null);
#endif

        // If max file size is set, check the current file size and rollover to a new file if necessary
        if (maxFileSize != NO_MAX_FILE_SIZE)
        {
          RolloverIfFileSizeExceeded();
        }

        StreamWriter.WriteLine(logEvent.ToString());
        StreamWriter.Flush();

      }
      catch (Exception ex)
      {
        LogManager.LogInternalEvent(EventLevel.Fatal, string.Format("Could not create the file logger for the path ", filePath), ex);
        IncrementFailCount();
      }
    }

    private void RolloverIfFileSizeExceeded()
    {
      // Get the filesize
      // Slower this way but foolproof! (Alternative would be to do this at construction time and track it)
      FileInfo fileInfo = new FileInfo(this.filePath);

      if (!fileInfo.Exists)
      {
        return;
      }

      // TODO? Length in current encoding? UTF-8?
      long fileSize = fileInfo.Length;

      if (fileSize > this.maxFileSize)
      {
        LogManager.LogInternalEvent(EventLevel.Debug,
              string.Format("Appender {0} rolling over for max file size {1}", this.Name, maxFileSize), null);

        if (!this.overwriteExistingFile)
        {
          string newFilePath = this.originalFilePath;
          GetUniqueFilePath(ref newFilePath);

          // Path.Combine?
          this.filePath = newFilePath;
          streamWriter = null;
        }
        else
        {
          // KC -- Commented, we already have the lock
          //lock (appenderSynchLock)
          //{    rolling over      
          File.Delete(filePath);
          File.CreateText(filePath);
          //}
        }
      }
    }

    #region IDisposable members

    /// <summary>
    /// Disposes any unmanaged resources
    /// </summary>
    /// <param name="isDisposing"></param>
    protected override void Dispose(bool isDisposing)
    {
      if (!isDisposed)
      {
        if (isDisposing)
        {
        }

        //Trace.WriteLine("FileAppender " + filePath + " disposing.");

        if (streamWriter != null)
        {
          try
          {
            streamWriter.Flush();
            streamWriter.Close();
            //streamWriter.Dispose();
          }
          catch { }
        }

        isDisposed = true;
      }
    }

    #endregion
  }
}
