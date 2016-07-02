#Region "Copyright (C) 2005-2011 Team MediaPortal"

' Copyright (C) 2005-2011 Team MediaPortal
' http://www.team-mediaportal.com
' 
' MediaPortal is free software: you can redistribute it and/or modify
' it under the terms of the GNU General Public License as published by
' the Free Software Foundation, either version 2 of the License, or
' (at your option) any later version.
' 
' MediaPortal is distributed in the hope that it will be useful,
' but WITHOUT ANY WARRANTY; without even the implied warranty of
' MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
' GNU General Public License for more details.
' 
' You should have received a copy of the GNU General Public License
' along with MediaPortal. If not, see <http://www.gnu.org/licenses/>.

#End Region

Imports System
Imports System.Collections.Generic
Imports System.IO
Imports System.Text
Imports System.Text.RegularExpressions

''' <summary>
''' An implementation of a log mechanism for the GUI library.
''' </summary>
Public Class MyLog

    Private Enum LogType
        ''' <summary>
        ''' Debug logging
        ''' </summary>
        Debug
        ''' <summary>
        ''' normal logging
        ''' </summary>
        Info
        ''' <summary>
        ''' normal logging
        ''' </summary>
        Warn
        ''' <summary>
        ''' error logging
        ''' </summary>
        [Error]

    End Enum

    ''' <summary>
    ''' Configure after how many days the log file shall be rotated when a new line is added
    ''' </summary>
    Private Shared ReadOnly _logDaysToKeep As New TimeSpan(14, 0, 0, 0)

    ''' <summary>
    ''' The maximum size of each log file in Megabytes
    ''' </summary>
    Private Const _maxLogSizeMb As Integer = 100

    ''' <summary>
    ''' The maximum count of identic messages to be logged in a row
    ''' </summary>
    Private Const _maxRepetitions As Integer = 5

    ''' <summary>
    ''' The last log n lines to compare for repeated entries.
    ''' </summary>
    Private Shared ReadOnly _lastLogLines As New List(Of String)(_maxRepetitions)

#Region "Constructors"

    ''' <summary>
    ''' Private singleton constructor . Do not allow any instance of this class.
    ''' </summary>
    Private Sub New()
    End Sub

    ''' <summary>
    ''' Static constructor
    ''' </summary>
    Shared Sub New()
        'BackupLogFiles(); <-- do not rotate logs when e.g. SetupTv is started.
        _lastLogLines.Clear()
    End Sub

#End Region

#Region "Properties"
    Private Shared _DebugModeOn As Boolean = True
    Friend Shared Property DebugModeOn() As Boolean
        Get
            Return _DebugModeOn
        End Get
        Set(ByVal value As Boolean)
            _DebugModeOn = value
        End Set
    End Property

    Private Shared _logFileName As String = "CSGuide.log"
    Friend Shared Property LogFileName() As String
        Get
            Return _logFileName
        End Get
        Set(ByVal value As String)
            _logFileName = value
        End Set
    End Property

#End Region

#Region "Public methods"

    ''' <summary>
    ''' Backups the log files.
    ''' </summary>
    Public Shared Sub BackupLogFiles()
        RotateLogs()
        _lastLogLines.Clear()
    End Sub

    ''' <summary>
    ''' Writes the specified exception to the log file
    ''' </summary>
    ''' <param name="ex">The ex.</param>
    Public Shared Sub Write(ByVal ex As Exception)
        [Error]("Exception   :{0}" & vbLf, ex)
    End Sub

    ''' <summary>
    ''' Replaces a password inside the string by stars
    ''' </summary>
    ''' <param name="Logtext">String to replace</param>
    ''' <returns>String without password</returns>
    Public Shared Function SafeString(ByVal Logtext As [String]) As [String]
        Return New Regex("Password=[^;]*;", RegexOptions.IgnoreCase).Replace(Logtext, "Password=***;")
    End Function

    ''' <summary>
    ''' Write a string to the logfile.
    ''' </summary>
    ''' <param name="format">The format of the string.</param>
    ''' <param name="arg">An array containing the actual data of the string.</param>
    Public Shared Sub Write(ByVal format As String, ByVal ParamArray arg As Object())
        ' uncomment the following four lines to help identify the calling method, this
        ' is useful in situations where an unreported exception causes problems
        '		StackTrace stackTrace = new StackTrace();
        '		StackFrame stackFrame = stackTrace.GetFrame(1);
        '		MethodBase methodBase = stackFrame.GetMethod();
        '		WriteFile(LogType.Log, "{0}", methodBase.Name);
        WriteToFile(LogType.Info, format, arg)
    End Sub
    ''' <summary>
    ''' Logs the message to the error file
    ''' </summary>
    ''' <param name="format">The format.</param>
    ''' <param name="arg">The arg.</param>
    Public Shared Sub [Error](ByVal format As String, ByVal ParamArray arg As Object())
        WriteToFile(LogType.Error, format, arg)
    End Sub

    Public Shared Sub Warn(ByVal format As String, ByVal ParamArray arg As Object())
        WriteToFile(LogType.Warn, format, arg)
    End Sub

    ''' <summary>
    ''' Logs the message to the info file
    ''' </summary>
    ''' <param name="format">The format.</param>
    ''' <param name="arg">The arg.</param>
    Public Shared Sub Info(ByVal format As String, ByVal ParamArray arg As Object())
        WriteToFile(LogType.Info, format, arg)
    End Sub

    ''' <summary>
    ''' Logs the message to the debug file
    ''' </summary>
    ''' <param name="format">The format.</param>
    ''' <param name="arg">The arg.</param>
    Public Shared Sub Debug(ByVal format As String, ByVal ParamArray arg As Object())

        If MyLog.DebugModeOn = True Then
            WriteToFile(LogType.Debug, format, arg)
        End If

    End Sub

    ''' <summary>
    ''' Logs the message to the info file
    ''' </summary>
    ''' <param name="format">The format.</param>
    ''' <param name="arg">The arg.</param>
    Public Shared Sub WriteFile(ByVal format As String, ByVal ParamArray arg As Object())
        WriteToFile(LogType.Info, "[Info] " & format, arg)
    End Sub

    '''<summary>
    ''' Returns the path the Application data location
    '''</summary>
    '''<returns>Application data path of TvServer</returns>
    Public Shared Function GetPathName() As String
        Return [String].Format("{0}\Team MediaPortal\MediaPortal", Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData))
    End Function

#End Region

#Region "Private methods"

    Private Shared Function GetFileName(ByVal logType__1 As LogType) As String
        Dim Path As String = GetPathName()
        Return [String].Format("{0}\log\{1}", Path, MyLog.LogFileName)
    End Function

    ''' <summary>
    ''' Since Windows caches API calls to the FileSystem a simple FileInfo.CreationTime will be wrong when replacing files (even after refresh).
    ''' Therefore we set it manually.
    ''' </summary>
    ''' <param name="aFileName"></param>
    Private Shared Sub CreateBlankFile(ByVal aFileName As String)
        Try
            Using sw As StreamWriter = File.CreateText(aFileName)
                sw.Close()
                Try
                    File.SetCreationTime(aFileName, DateTime.Now)
                Catch generatedExceptionName As Exception
                End Try
            End Using
        Catch generatedExceptionName As Exception
        End Try
    End Sub

    ''' <summary>
    ''' Deletes .bak file, moves .log to .bak for every LogType
    ''' </summary>
    Private Shared Sub RotateLogs()
        Try
            Dim physicalLogFiles As New List(Of String)(3)
            ' Get all log types
            For Each logtype As LogType In [Enum].GetValues(GetType(LogType))
                ' Get full path for log
                Dim name As String = GetFileName(logtype)
                ' Since e.g. debug and info might share the same file make sure we only rotate once
                If Not physicalLogFiles.Contains(name) Then
                    physicalLogFiles.Add(name)
                End If
            Next

            For Each logFileName As String In physicalLogFiles
                ' make sure other files will be rotated even if one file fails
                Try
                    Dim bakFileName As String = logFileName.Replace(".log", ".bak")
                    ' Delete outdated log
                    If File.Exists(bakFileName) Then
                        File.Delete(bakFileName)
                    End If
                    ' Rotate current log
                    If File.Exists(logFileName) Then
                        File.Move(logFileName, bakFileName)
                    End If
                    ' Create a new log file with correct timestamps
                    CreateBlankFile(logFileName)
                Catch generatedExceptionName As UnauthorizedAccessException
                Catch generatedExceptionName As ArgumentException
                Catch generatedExceptionName As IOException
                End Try
            Next
            ' Maybe add EventLog here...
        Catch generatedExceptionName As Exception
        End Try
    End Sub

    ''' <summary>
    ''' Compares the cache's last log entries to check whether we have repeating lines that should not be logged
    ''' </summary>
    ''' <param name="aLogLine">A new log line</param>
    ''' <returns>True if the cache only contains the exact lines as given by parameter</returns>
    Private Shared Function IsRepetition(ByVal aLogLine As IComparable(Of String)) As Boolean
        Dim result As Boolean = True
        ' as long as the cache is not full we have no repetitions
        If _lastLogLines.Count = _maxRepetitions Then
            For Each singleLine As String In _lastLogLines
                If aLogLine.CompareTo(singleLine) <> 0 Then
                    result = False
                    Exit For
                End If
            Next
        Else
            result = False
        End If

        Return result
    End Function

    Private Shared Sub CacheLogLine(ByVal aLogLine As String)
        If Not String.IsNullOrEmpty(aLogLine) Then
            If _lastLogLines.Count = _maxRepetitions Then
                _lastLogLines.RemoveAt(0)
            End If

            _lastLogLines.Add(aLogLine)
        End If
    End Sub

    ''' <summary>
    ''' Does pre-logging tasks - like check for rotation, oversize, etc
    ''' </summary>
    ''' <param name="aLogFileName">The file to be checked</param>
    ''' <returns>False if logging must not go on</returns>
    Private Shared Function CheckLogPrepared(ByVal aLogFileName As String) As Boolean
        Dim result As Boolean = True
        Try
            ' If the user or some other event deleted the dir make sure to recreate it.
            Directory.CreateDirectory(Path.GetDirectoryName(aLogFileName))
            If File.Exists(aLogFileName) Then
                Dim checkDate As DateTime = DateTime.Now - _logDaysToKeep
                ' Set the file date to a default which would NOT rotate for the case that FileInfo fetching will fail
                Dim fileDate As DateTime = DateTime.Now
                Try
                    Dim logFi As New FileInfo(aLogFileName)
                    ' The information is retrieved from a cache and might be outdated.
                    logFi.Refresh()
                    fileDate = logFi.CreationTime

                    ' Some log source went out of control here - do not log until out of disk space!
                    If logFi.Length > _maxLogSizeMb * 1000 * 1000 Then
                        result = False
                    End If
                Catch generatedExceptionName As Exception
                End Try
                ' File is older than today - _logDaysToKeep = rotate
                If checkDate.CompareTo(fileDate) > 0 Then
                    BackupLogFiles()
                End If
            End If
        Catch generatedExceptionName As Exception
        End Try
        Return result
    End Function

    ''' <summary>
    ''' Writes the file.
    ''' </summary>
    ''' <param name="logType">the type of logging.</param>
    ''' <param name="format">The format.</param>
    ''' <param name="arg">The arg.</param>
    Private Shared Sub WriteToFile(ByVal logType As LogType, ByVal format As String, ByVal ParamArray arg As Object())
        SyncLock GetType(MyLog)
            Try
                Dim logFileName As String = GetFileName(logType)
                Dim logLine As String = SafeString(String.Format(format, arg))

                If IsRepetition(logLine) Then
                    Return
                End If
                CacheLogLine(logLine)

                If CheckLogPrepared(logFileName) Then
                    Using writer As New StreamWriter(logFileName, True, Encoding.UTF8)
                        writer.BaseStream.Seek(0, SeekOrigin.[End])
                        ' set the file pointer to the end of file
                        writer.WriteLine("{0:yyyy-MM-dd HH:mm:ss.ffffff} [{1}]: {2}", DateTime.Now, logType.ToString, logLine)
                        writer.Close()
                    End Using
                End If
            Catch generatedExceptionName As Exception
            End Try
        End SyncLock
    End Sub

#End Region
End Class
