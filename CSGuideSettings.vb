Imports MediaPortal.Profile
Imports MediaPortal.Configuration
Imports TvDatabase
Imports System.Text.RegularExpressions
Imports System.IO



Namespace ClickfinderSimpleGuide
    Public Class CSGuideSettings
        Private Shared m_startView As Integer
        Private Shared m_view(9) As CSGuideView
        Private Shared m_debugMode As Boolean
        Private Shared m_clickfinderImagePath As String
        Private Shared m_TvMovieImportStatus As String
        Private Shared _layer As New TvBusinessLayer
        Private Shared m_TVMovieVersion As String
        Private Shared m_clickfinderPath As String
        Private Shared m_TVMovieIsEnabled As String
        Private Shared m_TVMovieLastUpdate As String
        Private Shared m_configFileName As String = "ClickfinderSimpleGuide.xml"


        Friend Shared Property TVMovieLastUpdate() As String
            Get
                Return m_TVMovieLastUpdate
            End Get
            Set(ByVal value As String)
                m_TVMovieLastUpdate = value
            End Set
        End Property



        Friend Shared Property TVMovieIsEnabled() As String
            Get
                Return m_TVMovieIsEnabled
            End Get
            Set(ByVal value As String)
                m_TVMovieIsEnabled = value
            End Set
        End Property

        Friend Shared Property TVMovieDatabasePath As String
            Get
                Return m_clickfinderPath
            End Get
            Set(ByVal value As String)
                m_clickfinderPath = value
            End Set
        End Property
        Friend Shared Property TVMovieVersion() As String
            Get
                Return m_TVMovieVersion
            End Get
            Set(ByVal value As String)
                m_TVMovieVersion = value
            End Set
        End Property

        Friend Shared Property TvMovieImportStatus() As String
            Get
                Return m_TvMovieImportStatus
            End Get
            Set(ByVal value As String)
                m_TvMovieImportStatus = value
            End Set
        End Property

        Friend Shared Property ClickfinderImagePath() As String
            Get
                Return m_clickfinderImagePath
            End Get
            Set(ByVal value As String)
                m_clickfinderImagePath = value
            End Set
        End Property
        Friend Shared Property DebugMode() As Boolean
            Get
                Return m_debugMode
            End Get
            Set(ByVal value As Boolean)
                m_debugMode = value
            End Set
        End Property

        Friend Shared Property View(ByVal i As Integer) As CSGuideView
            Get
                Return m_view(i)
            End Get
            Set(ByVal value As CSGuideView)
                m_view(i) = value
            End Set
        End Property


        Friend Shared Property StartView() As Integer
            Get
                Return m_startView
            End Get
            Set(ByVal value As Integer)
                m_startView = value
            End Set
        End Property
        Friend Shared Sub init()
            setTVMovieSettings()
            loadFromXmlOrSetDefault()

        End Sub

        Private Shared Sub setTVMovieSettings()
            Dim _mName As String = System.Reflection.MethodInfo.GetCurrentMethod.Name
            Dim _mClass As String = "CSGuideSettings"
            Try
                m_TvMovieImportStatus = getTvMovieImportStatus()
                m_TVMovieVersion = _layer.GetSetting("TvMovieVersion", "").Value
                m_clickfinderPath = _layer.GetSetting("TvMoviedatabasepath", "").Value
                m_TVMovieIsEnabled = _layer.GetSetting("TvMovieEnabled", "false").Value
                m_TVMovieLastUpdate = _layer.GetSetting("TvMovieLastUpdate", "").Value

            Catch ex As Exception
                MyLog.Error(String.Format("[{0}] [{1}]: Move up : exception err: {2} stack: {3}", _mClass, _mName, ex.Message, ex.StackTrace))                
            End Try
        End Sub

        Friend Shared Sub loadFromXmlOrSetDefault()
            Dim _mName As String = System.Reflection.MethodInfo.GetCurrentMethod.Name
            Dim _mClass As String = "CSGuideSettings"
            Dim _path As String = Config.GetFile(Config.Dir.Config, m_configFileName)

            If System.IO.File.Exists(_path) Then
                Dim _mySettings As New Settings(_path)
                m_startView = _mySettings.GetValueAsInt("General", "StartView", "0")
                m_debugMode = _mySettings.GetValueAsBool("General", "DebugMode", True)
                m_clickfinderImagePath = _mySettings.GetValueAsString("General", "ClickfinderImagePath", "")

                For i = 0 To 8
                    Dim _view As CSGuideView = New CSGuideView(
                                          _mySettings.GetValue("Views", "View" & i & "Name"),
                                          _mySettings.GetValue("Views", "View" & i & "Type"),
                                          _mySettings.GetValueAsString("Views", "View" & i & "SQL", ""),
                                          _mySettings.GetValue("Views", "View" & i & "StartTime"),
                                          _mySettings.GetValue("Views", "View" & i & "TvGroup"),
                                          _mySettings.GetValue("Views", "View" & i & "DisplayName"),
                                          _mySettings.GetValue("Views", "View" & i & "StartTimeOffset"))
                    m_view(i) = _view
                Next
            Else
                setDefault()
                MyLog.Info(String.Format("[{0}] [{1}]: New Configuration - Loading the Defaults", _mClass, _mName))                
            End If
        End Sub

        Friend Shared Function saveToXml() As Boolean
            Dim _mName As String = System.Reflection.MethodInfo.GetCurrentMethod.Name
            Dim _mClass As String = "CSGuideSettings"

            Dim _path As String = Config.GetFile(Config.Dir.Config, m_configFileName)
            MyLog.Info(String.Format("[{0}] [{1}]: Saving to {2}", _mClass, _mName, _path))
            Try
                Dim _mySettings As New Settings(_path)
                _mySettings.SetValue("General", "StartView", m_startView)
                _mySettings.SetValueAsBool("General", "DebugMode", m_debugMode)
                _mySettings.SetValue("General", "ClickfinderImagePath", m_clickfinderImagePath)

                For i = 0 To 8
                    _mySettings.SetValue("Views", "View" & i & "Name", m_view(i).Name)
                    _mySettings.SetValue("Views", "View" & i & "Type", m_view(i).Type)
                    _mySettings.SetValue("Views", "View" & i & "SQL", m_view(i).SQL)
                    _mySettings.SetValue("Views", "View" & i & "StartTime", m_view(i).TimeString)
                    _mySettings.SetValue("Views", "View" & i & "TvGroup", m_view(i).TvGroup)
                    _mySettings.SetValue("Views", "View" & i & "DisplayName", m_view(i).DisplayName)
                    _mySettings.SetValue("Views", "View" & i & "StartTimeOffset", m_view(i).OffSetMinute)
                Next
                Return True
            Catch ex As Exception
                MyLog.Error(String.Format("[{0}] [{1}]: Exception err: {2} stack: {3}", _mClass, _mName, ex.Message, ex.StackTrace))
                Return False
            End Try
        End Function

        Friend Shared Sub logAll()
            Dim _mName As String = System.Reflection.MethodInfo.GetCurrentMethod.Name
            Dim _mClass As String = "CSGuideSettings"

            MyLog.Debug(String.Format("[{0}] [{1}] ClickfinderPath = {2}", _mClass, _mName, m_clickfinderPath))
            MyLog.Debug(String.Format("[{0}] [{1}] ClickfinderImagePath = {2}", _mClass, _mName, m_clickfinderImagePath))
            MyLog.Debug(String.Format("[{0}] [{1}] TvMovieImportStatus = {2}", _mClass, _mName, m_TvMovieImportStatus))
            MyLog.Debug(String.Format("[{0}] [{1}] TVMovieVersion = {2}", _mClass, _mName, m_TVMovieVersion))
            MyLog.Debug(String.Format("[{0}] [{1}] TVMovieIsEnabled = {2}", _mClass, _mName, m_TVMovieIsEnabled))
            MyLog.Debug(String.Format("[{0}] [{1}] TVMovieLastUpdate = {2}", _mClass, _mName, m_TVMovieLastUpdate))
            MyLog.Debug(String.Format("[{0}] [{1}] ------------------------", _mClass, _mName))
            MyLog.Debug(String.Format("[{0}] [{1}] StartView = {2}", _mClass, _mName, m_startView))
            MyLog.Debug(String.Format("[{0}] [{1}] DebugMode = {2}", _mClass, _mName, m_debugMode))
            MyLog.Debug(String.Format("[{0}] [{1}] ------------------------", _mClass, _mName))
            For i = 0 To 8
                MyLog.Debug(String.Format("[{0}] [{1}] View" & i & "Name = {2}", _mClass, _mName, m_view(i).Name))
                MyLog.Debug(String.Format("[{0}] [{1}] View" & i & "Type = {2}", _mClass, _mName, m_view(i).Type))
                MyLog.Debug(String.Format("[{0}] [{1}] View" & i & "SQL = {2}", _mClass, _mName, m_view(i).SQL))
                MyLog.Debug(String.Format("[{0}] [{1}] View" & i & "StartTime = {2}", _mClass, _mName, m_view(i).TimeString))
                MyLog.Debug(String.Format("[{0}] [{1}] View" & i & "TvGroup = {2}", _mClass, _mName, m_view(i).TvGroup))
                MyLog.Debug(String.Format("[{0}] [{1}] View" & i & "DisplayName = {2}", _mClass, _mName, m_view(i).DisplayName))
                MyLog.Debug(String.Format("[{0}] [{1}] View" & i & "StartTimeOffset = {2}", _mClass, _mName, m_view(i).OffSetMinute))
            Next
        End Sub

        Friend Shared Function getTvMovieImportStatus() As String
            Dim _importRunning As Boolean
            Dim _lastUpdate As String
            Dim _mName As String = System.Reflection.MethodInfo.GetCurrentMethod.Name
            Dim _mClass As String = "CSGuideSettings"

            Try
                _importRunning = _layer.GetSetting("TvMovieImportIsRunning", "false").Value
                If _importRunning Then
                    Return "TV-Movie Import is Running"
                Else
                    _lastUpdate = _layer.GetSetting("TvMovieLastUpdate", "").Value
                    Dim r As Regex = New Regex("^(\d\d\.\d\d\.).*?\s(\d\d\:\d\d)")
                    Dim m As Match = r.Match(_lastUpdate)

                    Return "Letzter TV-Movie Update am " & m.Groups(1).Value & " um " & m.Groups(2).Value & " Uhr"
                End If

            Catch ex As Exception
                MyLog.Error(String.Format("[{0}] [{1}]: Exception err: {2} stack: {3}", _mClass, _mName, ex.Message, ex.StackTrace))
            End Try
            Return ""
        End Function

        Friend Shared Sub setDefault()
            Dim _view As CSGuideView


            m_startView = 1
            m_debugMode = False
            m_clickfinderImagePath = Path.GetDirectoryName(m_clickfinderPath) & "\Hyperlinks"

            _view = New CSGuideView(
                                  "Single",
                                  "Single",
                                  "Select * from program INNER JOIN TvMovieProgram ON program.idprogram = TvMovieProgram.idProgram inner join channel on program.idChannel = channel.idChannel where startTime >= #StartTime and channel.channelNumber=#ChannelNumber order by startTime",
                                  "Now",
                                  "All Channels",
                                  "Single Channel",
                                  "-90")
            m_view(0) = _view
            _view = New CSGuideView(
                                    "Now",
                                    "Overview",
                                    "Select * from program INNER JOIN TvMovieProgram ON program.idprogram = TvMovieProgram.idProgram inner join channel on program.idChannel = channel.idChannel where #TVGroupFilter and program.idProgram in (Select tt.idProgram from  ( Select t.chN, t.idP as idProgram, min(t.diff) from (Select channel.channelNumber as chN, program.idprogram as idP, abs(timediff(startTime,#StartTime)) as diff from program INNER JOIN TvMovieProgram ON program.idprogram = TvMovieProgram.idProgram inner join channel on program.idChannel = channel.idChannel where (DATE(startTime) = DATE(#StartTime) or DATE(startTime) = DATE(DATE_SUB(#StartTime, INTERVAL 1 day))) and startTime <= #StartTime and channel.isTv=1 order by diff ) as t group by t.chN) as tt) order by channel.channelNumber",
                                    "Now",
                                    "All Channels",
                                    "Jetzt",
                                    "0")
            m_view(1) = _view

            _view = New CSGuideView(
            "Prime",
            "Overview",
            "Select * from program INNER JOIN TvMovieProgram ON program.idprogram = TvMovieProgram.idProgram inner join channel on program.idChannel = channel.idChannel where #TVGroupFilter and program.idProgram in (Select tt.idProgram from  ( Select t.chN, t.idP as idProgram, min(t.diff) from (Select channel.channelNumber as chN, program.idprogram as idP, abs(timediff(startTime,#StartTime)) as diff from program INNER JOIN TvMovieProgram ON program.idprogram = TvMovieProgram.idProgram inner join channel on program.idChannel = channel.idChannel where (DATE(startTime) = DATE(#StartTime) or DATE(startTime) = DATE(DATE_SUB(#StartTime, INTERVAL 1 day))) and startTime <= #StartTime and channel.isTv=1 order by diff ) as t group by t.chN) as tt) order by channel.channelNumber",
            "20:15",
            "All Channels",
            "Prime Time",
            "0")
            m_view(2) = _view

            _view = New CSGuideView(
            "Late",
            "Overview",
            "Select * from program INNER JOIN TvMovieProgram ON program.idprogram = TvMovieProgram.idProgram inner join channel on program.idChannel = channel.idChannel where #TVGroupFilter and program.idProgram in (Select tt.idProgram from  ( Select t.chN, t.idP as idProgram, min(t.diff) from (Select channel.channelNumber as chN, program.idprogram as idP, abs(timediff(startTime,#StartTime)) as diff from program INNER JOIN TvMovieProgram ON program.idprogram = TvMovieProgram.idProgram inner join channel on program.idChannel = channel.idChannel where (DATE(startTime) = DATE(#StartTime) or DATE(startTime) = DATE(DATE_SUB(#StartTime, INTERVAL 1 day))) and startTime <= #StartTime and channel.isTv=1 order by diff ) as t group by t.chN) as tt) order by channel.channelNumber",
            "22:15",
            "All Channels",
           "Late",
           "0")
            m_view(3) = _view

            _view = New CSGuideView(
            "Night",
            "Overview",
            "Select * from program INNER JOIN TvMovieProgram ON program.idprogram = TvMovieProgram.idProgram inner join channel on program.idChannel = channel.idChannel where #TVGroupFilter and program.idProgram in (Select tt.idProgram from  ( Select t.chN, t.idP as idProgram, min(t.diff) from (Select channel.channelNumber as chN, program.idprogram as idP, abs(timediff(startTime,#StartTime)) as diff from program INNER JOIN TvMovieProgram ON program.idprogram = TvMovieProgram.idProgram inner join channel on program.idChannel = channel.idChannel where (DATE(startTime) = DATE(#StartTime) or DATE(startTime) = DATE(DATE_SUB(#StartTime, INTERVAL 1 day))) and startTime <= #StartTime and channel.isTv=1 order by diff ) as t group by t.chN) as tt) order by channel.channelNumber",
            "00:00",
            "All Channels",
            "01:00 Nachts",
            "1500")
            m_view(4) = _view

            _view = New CSGuideView(
           "Movie",
           "Overview",
           "Select * from program INNER JOIN TvMovieProgram ON program.idprogram = TvMovieProgram.idProgram WHERE #TVGroupFilter AND TIMESTAMPDIFF(MINUTE,program.starttime, program.endtime) > 10 AND startTime >= #StartTime AND startTime <= date_add(date(#StartTime), interval 29 hour) AND starRating >= 1 AND (genre NOT LIKE '%Serie' OR genre NOT LIKE '%Reihe' OR genre NOT LIKE '%Sitcom%' OR genre NOT LIKE '%Zeichentrick%') GROUP BY program.title, program.episodeName ORDER BY TvMovieProgram.TVMovieBewertung DESC, startTime ASC, starRating DESC, title ASC",
           "Now",
           "All Channels",
           "Filme",
           "0")
            m_view(5) = _view

            _view = New CSGuideView(
           "MoviePreview",
           "Overview",
           "Select * from program INNER JOIN TvMovieProgram ON program.idprogram = TvMovieProgram.idProgram WHERE #TVGroupFilter AND TIMESTAMPDIFF(MINUTE,program.starttime, program.endtime) > 10 AND startTime >= #StartTime AND startTime <= date_add(date(#StartTime), interval 173 hour) AND starRating >= 1 AND (genre NOT LIKE '%Serie' OR genre NOT LIKE '%Reihe' OR genre NOT LIKE '%Sitcom%' OR genre NOT LIKE '%Zeichentrick%') GROUP BY program.title, program.episodeName ORDER BY TvMovieProgram.TVMovieBewertung DESC, startTime ASC, starRating DESC, title ASC",
           "Now",
           "All Channels",
           "Movie-Vorschau",
           "0")
            m_view(6) = _view

            _view = New CSGuideView(
            "StarRating",
            "Overview",
            "Select * from program INNER JOIN TvMovieProgram ON program.idprogram = TvMovieProgram.idProgram WHERE #TVGroupFilter AND TIMESTAMPDIFF(MINUTE,program.starttime, program.endtime) > 10 AND startTime >= #StartTime AND startTime <= date_add(date(#StartTime), interval 29 hour) AND  starRating >= 7 GROUP BY program.title, program.episodeName ORDER BY starRating DESC, title ASC",
            "Now",
            "All Channels",
            "Star-Rating",
            "0")
            m_view(7) = _view

            _view = New CSGuideView(
            "Tages-Tipps",
            "Overview",
            "Select * from program INNER JOIN TvMovieProgram ON program.idprogram = TvMovieProgram.idProgram WHERE #TVGroupFilter AND TIMESTAMPDIFF(MINUTE,program.starttime, program.endtime) >10 AND startTime >= #StartTime AND description like '%Tages-Tipp%' GROUP BY program.title order by startTime ASC, title ASC",
            "Now",
            "All Channels",
            "Tages-Tipps!",
            "0")
            m_view(8) = _view

        End Sub

    End Class

End Namespace
