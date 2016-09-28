Imports MediaPortal.GUI.Library
Imports TvDatabase
Imports Gentle.Framework

Imports MediaPortal.Configuration
Imports MediaPortal.Dialogs
Imports System.Threading
Imports Gentle.Common
Imports System.Text
Imports System.Text.RegularExpressions
' for Movie Info Start
Imports MediaPortal.Video.Database
Imports MediaPortal.Util
Imports enrichEPG.TvDatabase
Imports TvDatabase.TvBusinessLayer
Imports MediaPortal.GUI.Video
' for Movie Info End
Imports System.Windows.Forms
Imports System.IO
' for TMDB Search
Imports TMDbLib.Client
Imports TMDbLib.Objects.General
Imports System.Xml
Imports System.Diagnostics



Namespace ClickfinderSimpleGuide
    Public Class CSGuideWindow
        Inherits GUIWindow

#Region "Skin Controls"

        <SkinControlAttribute(9)> Protected _DataLoadingAnimation As GUIAnimation = Nothing
        <SkinControlAttribute(11)> Protected _PageProgress As GUIProgressControl = Nothing
        <SkinControlAttribute(37)> Protected _ProgramList As GUIListControl = Nothing
        <SkinControlAttribute(91)> Protected _buttonControlView1 As GUIButtonControl = Nothing
        <SkinControlAttribute(92)> Protected _buttonControlView2 As GUIButtonControl = Nothing
        <SkinControlAttribute(93)> Protected _buttonControlView3 As GUIButtonControl = Nothing
        <SkinControlAttribute(94)> Protected _buttonControlView4 As GUIButtonControl = Nothing
        <SkinControlAttribute(95)> Protected _buttonControlView5 As GUIButtonControl = Nothing
        <SkinControlAttribute(96)> Protected _buttonControlView6 As GUIButtonControl = Nothing
        <SkinControlAttribute(97)> Protected _buttonControlView7 As GUIButtonControl = Nothing
        <SkinControlAttribute(98)> Protected _buttonControlView8 As GUIButtonControl = Nothing
        <SkinControlAttribute(101)> Protected _buttonControlView9 As GUIButtonControl = Nothing
        <SkinControlAttribute(100)> Protected _buttonControlView0 As GUIButtonControl = Nothing
        <SkinControlAttribute(60)> Protected FanartBackground As GUIImage = Nothing
        <SkinControlAttribute(61)> Protected FanartBackground2 As GUIImage = Nothing


#End Region


#Region "Members"

        Friend Shared _ItemsCache As New List(Of TVMovieProgram)
        Private Shared _TMDbCache As New Dictionary(Of String, CSGuideTMDBCacheItem)
        Private _cacheHander As CSGuideCacheHandler
        Private _mClass As String

        Private _ThreadLoadItemsFromDatabase As Threading.Thread
        Private _ThreadFillProgramList As Threading.Thread
        Private _ThreadBuildTMDb As Threading.Thread

        Private _LastFocusedIndex As Integer = 0
        Private _LastFocusedControlID As Integer
        Private Shared _SelectedEPGItemId As Integer

        Private Shared _SqlString As String = String.Empty  'this is the SQL-String used to load the items from the DB        
        ' Private Shared _SqlStringSingleChannel As String = String.Empty
        ' Private Shared _SqlStringMoviesOnly As String = String.Empty
        'Private Shared _SqlStringStarRating As String = String.Empty
        'Private Shared _SqlStringTVMovieBewertung As String = String.Empty
        Private Shared _SqlSubStringMoviesEndTime As String = String.Empty
        Private Shared _StartTime As Date = Nothing
        Private Shared _StartTimePreviousItem As Date = Nothing

        Private Shared _TvGroupFilter As String = String.Empty

        Private Shared _channelNumber As Integer = 1
        Private Shared _viewDisplayName As String = String.Empty
        Private Shared _viewType As String = String.Empty

        Private Shared _chGroupChannelCache As New Dictionary(Of String, IList(Of Integer))
        Private Shared _idProgram As Integer = 0
        Private Shared _IntervalHour As Integer = 29 '29 meint bis 05:00 Uhr morgens den nächsten Tag
        Private Shared _actualViewNumber As Integer
        Private Shared _previousViewNumber As Integer
        Private Shared _HiddenMenuOpen As Boolean = False
        Private Shared _TMDbIsPossible As Boolean = False
        Private Shared _UseTMDb As Boolean = False
        Private Shared _tmdbCacheKey As String
        Private Shared _tmdbClient As TMDbClient

        Private _backdrop As ImageSwapper


#End Region

#Region "Constructors"

        Public Sub New()
        End Sub

        Friend Shared Sub SetViewProperties(ByVal i As Integer)

            _SqlString = CSGuideSettings.View(i).SQL
            _viewType = CSGuideSettings.View(i).Type
            ' Noch korrigieren siehe CSGuideView Function setViewDate
            _StartTime = CSGuideSettings.View(i).StartTime.AddMinutes(CInt(CSGuideSettings.View(i).OffSetMinute))
            ' Bei der View 0 soll die ursprüngliche TV Group bei behalten werden            
            If i <> 0 Then
                _TvGroupFilter = CSGuideSettings.View(i).TvGroup
            End If
            _viewDisplayName = CSGuideSettings.View(i).DisplayName
            _previousViewNumber = _actualViewNumber
            _actualViewNumber = i
            _UseTMDb = CSGuideSettings.View(i).UseTMDb And _TMDbIsPossible

        End Sub

#End Region

#Region "GUI Properties"
        Public Overloads Overrides Property GetID() As Integer
            Get
                Return 730351
            End Get
            Set(ByVal value As Integer)
            End Set
        End Property


        Public Overloads Overrides Function Init() As Boolean
            'Beim initialisieren des Plugin den Screen laden
            Dim mName As String = System.Reflection.MethodInfo.GetCurrentMethod.Name
            _mClass = Me.GetType.Name
            MyLog.Debug(String.Format("[{0}] [{1}]: Called", _mClass, mName))
            Return Load(GUIGraphicsContext.Skin + "\ClickfinderSimpleGuide.xml")
        End Function

        Public Overrides Function GetModuleName() As String
            Return "Clickfinder Simple Guide"
        End Function

        Public Shared Property ActualViewNumber() As Integer
            Get
                Return _actualViewNumber
            End Get
            Set(ByVal value As Integer)
                _actualViewNumber = value
            End Set
        End Property
#End Region

#Region "GUI Events"

        Protected Overrides Sub OnPageLoad()
            If _ItemsCache.Count = 0 Then
                InitWindow()
            End If
            _backdrop = New ImageSwapper
            _backdrop.PropertyOne = "#Fanart.1"
            _backdrop.PropertyTwo = "#Fanart.2"

            _backdrop.GUIImageOne = FanartBackground
            _backdrop.GUIImageTwo = FanartBackground2

            InitButtons()

            MyBase.OnPageLoad()
            _ThreadFillProgramList = New Thread(AddressOf FillProgramList)
            _ThreadFillProgramList.IsBackground = True
            _ThreadFillProgramList.Start()
            _ProgramList.Focus = True
            GUIWindowManager.NeedRefresh()
        End Sub

        Protected Overrides Sub OnPageDestroy(ByVal new_windowId As Integer)
            Dim mName As String = System.Reflection.MethodInfo.GetCurrentMethod.Name
            'Dim _mClass As String = Me.GetType.Name

            MyLog.Debug(String.Format("[{0}] [{1}]: Destroying ... New WindowID {2}", _mClass, mName, new_windowId))

            AbortRunningThreads()

            MyBase.OnPageDestroy(new_windowId)
            Dispose()
            AllocResources()
        End Sub
        Public Sub InitWindow()
            Dim mName As String = System.Reflection.MethodInfo.GetCurrentMethod.Name

            _mClass = [GetType].Name
            Try
                If (Not String.IsNullOrEmpty(CSGuideSettings.TMDbAPIKey)) Then
                    _tmdbClient = New TMDbClient(CSGuideSettings.TMDbAPIKey)
                    FetchConfig(_tmdbClient)
                    _TMDbIsPossible = True
                    _cacheHander = New CSGuideCacheHandler(_tmdbClient)
                Else
                    MyLog.Info(String.Format("[{0}] [{1}]: No TMDb Info will be fetched", _mClass, mName))
                End If

            Catch ex As Exception
                MyLog.Error(String.Format("[{0}] [{1}]: Problems with the TMDb-API-Key exception err: {2} stack: {3}", _mClass, mName, ex.Message, ex.StackTrace))
                MyLog.Error(String.Format("[{0}] [{1}]: No TMDb Info will be fetched", _mClass, mName))
                _TMDbIsPossible = False
            End Try
            ' needed for the first call of this method
            SetViewProperties(CSGuideSettings.StartView)
            _previousViewNumber = CSGuideSettings.StartView


            'SetViewProperties(m_actualViewNumber)

            MyBase.OnPageLoad()

            GUIWindowManager.NeedRefresh()
            MyLog.Info(String.Format("[{0}] [{1}]: --- Start ---", _mClass, mName))

            GuiLayoutLoading()
            '_LastFocusedControlID = _niceEPGList.GetID
            ' _ItemsCache.Clear()
            ' AbortRunningThreads()

            _ThreadLoadItemsFromDatabase = New Threading.Thread(AddressOf LoadItemsFromDatabase)
            _ThreadLoadItemsFromDatabase.IsBackground = True
            _ThreadLoadItemsFromDatabase.Start()

            If _chGroupChannelCache.Count = 0 Then
                FillChGroupChannelCache()
            End If
        End Sub
        Public Overrides Sub OnAction(ByVal action As MediaPortal.GUI.Library.Action)
            Dim mName As String = System.Reflection.MethodInfo.GetCurrentMethod.Name
            'Dim _mClass As String = Me.GetType.Name

            If GUIWindowManager.ActiveWindow = GetID Then

                'Move up                
                If action.wID = MediaPortal.GUI.Library.Action.ActionType.ACTION_MOVE_UP Then
                    If _ProgramList.SelectedListItem.ItemId = _ProgramList.Item(0).ItemId Then
                        _SelectedEPGItemId = _ProgramList.Item(_ProgramList.Count - 1).ItemId
                        _PageProgress.Percentage = 100
                    Else
                        _SelectedEPGItemId = _ProgramList.Item(_ProgramList.SelectedListItemIndex - 1).ItemId
                        _PageProgress.Percentage = 100 * ((_ProgramList.SelectedListItemIndex - 1) / _ProgramList.Count)
                    End If


                    Try
                        If _ProgramList.IsFocused = True Then
                            SetGUIProperties(TVMovieProgram.Retrieve(_SelectedEPGItemId))
                            _StartTimePreviousItem = TVMovieProgram.Retrieve(_SelectedEPGItemId).ReferencedProgram.StartTime
                        End If

                    Catch ex As Exception
                        MyLog.Error(String.Format("[{0}] [{1}]: Move up : exception err: {2} stack: {3}", _mClass, mName, ex.Message, ex.StackTrace))
                    End Try


                End If

                'Page up                
                If action.wID = MediaPortal.GUI.Library.Action.ActionType.ACTION_PAGE_UP Then
                    If _ProgramList.SelectedListItemIndex - 8 < 0 Then
                        _SelectedEPGItemId = _ProgramList.Item(0).ItemId
                        _PageProgress.Percentage = 0
                    Else
                        _SelectedEPGItemId = _ProgramList.Item(_ProgramList.SelectedListItemIndex - 8).ItemId
                        _PageProgress.Percentage = 100 * ((_ProgramList.SelectedListItemIndex - 8) / _ProgramList.Count)
                    End If



                    Try
                        If _ProgramList.IsFocused = True Then
                            SetGUIProperties(TVMovieProgram.Retrieve(_SelectedEPGItemId))
                            _StartTimePreviousItem = TVMovieProgram.Retrieve(_SelectedEPGItemId).ReferencedProgram.StartTime
                        End If

                    Catch ex As Exception
                        MyLog.Error(String.Format("[{0}] [{1}]: Page up - Err: {2} stack: {3}", _mClass, mName, ex.Message, ex.StackTrace))
                    End Try
                End If

                'Move down                
                If action.wID = MediaPortal.GUI.Library.Action.ActionType.ACTION_MOVE_DOWN Then
                    If _ProgramList.SelectedListItem.ItemId = _ProgramList.Item(_ProgramList.Count - 1).ItemId Then
                        _PageProgress.Percentage = 0
                        _SelectedEPGItemId = _ProgramList.Item(0).ItemId
                    Else
                        _SelectedEPGItemId = _ProgramList.Item(_ProgramList.SelectedListItemIndex + 1).ItemId
                        _PageProgress.Percentage = 100 * ((_ProgramList.SelectedListItemIndex + 1) / (_ProgramList.Count - 1))
                    End If
                    Try
                        If _ProgramList.IsFocused = True Then
                            SetGUIProperties(TVMovieProgram.Retrieve(_SelectedEPGItemId))
                            _StartTimePreviousItem = TVMovieProgram.Retrieve(_SelectedEPGItemId).ReferencedProgram.StartTime
                        End If
                    Catch ex As Exception
                        MyLog.Error(String.Format("[{0}] [{1}]: Move down - Err: {2} stack: {3}", _mClass, mName, ex.Message, ex.StackTrace))

                    End Try
                End If

                'Page down                
                If action.wID = MediaPortal.GUI.Library.Action.ActionType.ACTION_PAGE_DOWN Then
                    If _ProgramList.SelectedListItemIndex + 8 > _ProgramList.Count - 1 Then
                        _SelectedEPGItemId = _ProgramList.Item(_ProgramList.Count - 1).ItemId
                        _PageProgress.Percentage = 100
                    Else
                        _SelectedEPGItemId = _ProgramList.Item(_ProgramList.SelectedListItemIndex + 8).ItemId
                        _PageProgress.Percentage = 100 * ((_ProgramList.SelectedListItemIndex + 8) / _ProgramList.Count)
                    End If

                    Try
                        If _ProgramList.IsFocused = True Then
                            SetGUIProperties(TVMovieProgram.Retrieve(_SelectedEPGItemId))
                            _StartTimePreviousItem = TVMovieProgram.Retrieve(_SelectedEPGItemId).ReferencedProgram.StartTime
                        End If

                    Catch ex As Exception
                        MyLog.Error(String.Format("[{0}] [{1}]: Page down - Err: {2} stack: {3}", _mClass, mName, ex.Message, ex.StackTrace))
                    End Try
                End If

                'Next Item (F8) -> lade den nächsten Tag
                If action.wID = MediaPortal.GUI.Library.Action.ActionType.ACTION_NEXT_ITEM Then
                    _previousViewNumber = _actualViewNumber
                    ' wenn die previous View Single ist, ist schon alles geladen
                    ' das heißt es genügt, wenn der korrekte Item angezeigt wird
                    If CSGuideSettings.View(_previousViewNumber).Type.Equals("Single") Then
                        _StartTimePreviousItem = _StartTimePreviousItem.AddDays(1)
                        SetCorrectListItemIndex()
                        SetGUIProperties(TVMovieProgram.Retrieve(_ProgramList.SelectedListItem.ItemId))
                    Else
                        _StartTime = _StartTime.AddDays(1)



                        _ItemsCache.Clear()
                        AbortRunningThreads()

                        _ThreadLoadItemsFromDatabase = New Thread(AddressOf LoadItemsFromDatabase)

                        _ThreadLoadItemsFromDatabase.IsBackground = True
                        _ThreadLoadItemsFromDatabase.Start()
                    End If
                    Return
                End If

                'Prev. Item (F7) -> einen Tag zurück
                If action.wID = MediaPortal.GUI.Library.Action.ActionType.ACTION_PREV_ITEM Then
                    _previousViewNumber = _actualViewNumber
                    If CSGuideSettings.View(_previousViewNumber).Type.Equals("Single") Then
                        _StartTimePreviousItem = _StartTimePreviousItem.AddDays(-1)
                        SetCorrectListItemIndex()
                        SetGUIProperties(TVMovieProgram.Retrieve(_ProgramList.SelectedListItem.ItemId))
                    Else
                        _StartTime = _StartTime.AddDays(-1)


                        _ItemsCache.Clear()

                        AbortRunningThreads()

                        _ThreadLoadItemsFromDatabase = New Thread(AddressOf LoadItemsFromDatabase)
                        _ThreadLoadItemsFromDatabase.IsBackground = True
                        _ThreadLoadItemsFromDatabase.Start()
                    End If

                    Return
                End If

                'Forward (F6) -> eine Stunde vor
                If action.wID = MediaPortal.GUI.Library.Action.ActionType.ACTION_MUSIC_FORWARD Then
                    _previousViewNumber = _actualViewNumber
                    If CSGuideSettings.View(_previousViewNumber).Type.Equals("Single") Then
                        _StartTimePreviousItem = _StartTimePreviousItem.AddMinutes(60)
                        SetCorrectListItemIndex()
                        SetGUIProperties(TVMovieProgram.Retrieve(_ProgramList.SelectedListItem.ItemId))
                    Else
                        _StartTime = _StartTime.AddMinutes(60)


                        _ItemsCache.Clear()
                        AbortRunningThreads()

                        _ThreadLoadItemsFromDatabase = New Thread(AddressOf LoadItemsFromDatabase)
                        _ThreadLoadItemsFromDatabase.IsBackground = True
                        _ThreadLoadItemsFromDatabase.Start()
                    End If

                    Return
                End If

                'Rewind (F5) -> eine Stunde zurück
                If action.wID = MediaPortal.GUI.Library.Action.ActionType.ACTION_MUSIC_REWIND Then
                    _previousViewNumber = _actualViewNumber
                    If CSGuideSettings.View(_previousViewNumber).Type.Equals("Single") Then
                        _StartTimePreviousItem = _StartTimePreviousItem.AddMinutes(-60)
                        SetCorrectListItemIndex()
                        SetGUIProperties(TVMovieProgram.Retrieve(_ProgramList.SelectedListItem.ItemId))
                    Else

                        _StartTime = _StartTime.AddMinutes(-60)


                        _ItemsCache.Clear()
                        AbortRunningThreads()

                        _ThreadLoadItemsFromDatabase = New Thread(AddressOf LoadItemsFromDatabase)
                        _ThreadLoadItemsFromDatabase.IsBackground = True
                        _ThreadLoadItemsFromDatabase.Start()
                    End If
                    Return
                End If

                '(1) is pressed -> "Now"
                If action.wID = MediaPortal.GUI.Library.Action.ActionType.ACTION_KEY_PRESSED Then
                    If action.m_key IsNot Nothing Then
                        If action.m_key.KeyChar = 49 Then
                            SetViewProperties(1)

                            _ItemsCache.Clear()
                            AbortRunningThreads()

                            _ThreadLoadItemsFromDatabase = New Thread(AddressOf LoadItemsFromDatabase)
                            _ThreadLoadItemsFromDatabase.IsBackground = True
                            _ThreadLoadItemsFromDatabase.Start()
                            Return

                        End If
                    End If
                End If

                '(2) is pressed -> "Prime Time"
                If action.wID = MediaPortal.GUI.Library.Action.ActionType.ACTION_KEY_PRESSED Then
                    If action.m_key IsNot Nothing Then
                        If action.m_key.KeyChar = 50 Then
                            SetViewProperties(2)

                            _ItemsCache.Clear()
                            AbortRunningThreads()

                            _ThreadLoadItemsFromDatabase = New Thread(AddressOf LoadItemsFromDatabase)
                            _ThreadLoadItemsFromDatabase.IsBackground = True
                            _ThreadLoadItemsFromDatabase.Start()
                            Return

                        End If
                    End If
                End If

                '(3) is pressed -> "Late Time"
                If action.wID = MediaPortal.GUI.Library.Action.ActionType.ACTION_KEY_PRESSED Then
                    If action.m_key IsNot Nothing Then
                        If action.m_key.KeyChar = 51 Then
                            SetViewProperties(3)

                            _ItemsCache.Clear()
                            AbortRunningThreads()

                            _ThreadLoadItemsFromDatabase = New Thread(AddressOf LoadItemsFromDatabase)
                            _ThreadLoadItemsFromDatabase.IsBackground = True
                            _ThreadLoadItemsFromDatabase.Start()
                            Return

                        End If
                    End If
                End If

                '(4) is pressed -> "Night" - 00:00 Uhr
                If action.wID = MediaPortal.GUI.Library.Action.ActionType.ACTION_KEY_PRESSED Then
                    If action.m_key IsNot Nothing Then
                        If action.m_key.KeyChar = 52 Then
                            SetViewProperties(4)

                            _ItemsCache.Clear()
                            AbortRunningThreads()

                            _ThreadLoadItemsFromDatabase = New Thread(AddressOf LoadItemsFromDatabase)
                            _ThreadLoadItemsFromDatabase.IsBackground = True
                            _ThreadLoadItemsFromDatabase.Start()
                            Return

                        End If
                    End If
                End If

                '(5) is pressed -> "Movies only" - was kommt heute
                If action.wID = MediaPortal.GUI.Library.Action.ActionType.ACTION_KEY_PRESSED Then
                    If action.m_key IsNot Nothing Then
                        If action.m_key.KeyChar = 53 Then
                            SetViewProperties(5)

                            _ItemsCache.Clear()
                            AbortRunningThreads()

                            _ThreadLoadItemsFromDatabase = New Thread(AddressOf LoadItemsFromDatabase)
                            _ThreadLoadItemsFromDatabase.IsBackground = True
                            _ThreadLoadItemsFromDatabase.Start()
                            Return
                        End If
                    End If
                End If

                '(6) is pressed -> "Movies only" - 7 Tagesvorschau
                If action.wID = MediaPortal.GUI.Library.Action.ActionType.ACTION_KEY_PRESSED Then
                    If action.m_key IsNot Nothing Then
                        If action.m_key.KeyChar = 54 Then
                            SetViewProperties(6)

                            _ItemsCache.Clear()
                            AbortRunningThreads()

                            _ThreadLoadItemsFromDatabase = New Thread(AddressOf LoadItemsFromDatabase)
                            _ThreadLoadItemsFromDatabase.IsBackground = True
                            _ThreadLoadItemsFromDatabase.Start()
                            Return

                        End If
                    End If
                End If

                '(7) is pressed -> "Star-Rating" 
                If action.wID = MediaPortal.GUI.Library.Action.ActionType.ACTION_KEY_PRESSED Then
                    If action.m_key IsNot Nothing Then
                        If action.m_key.KeyChar = 55 Then
                            SetViewProperties(7)

                            _ItemsCache.Clear()
                            AbortRunningThreads()

                            _ThreadLoadItemsFromDatabase = New Thread(AddressOf LoadItemsFromDatabase)
                            _ThreadLoadItemsFromDatabase.IsBackground = True
                            _ThreadLoadItemsFromDatabase.Start()
                            Return

                        End If
                    End If
                End If

                '(8) is pressed -> "TV-Movie Bewertung" 
                If action.wID = MediaPortal.GUI.Library.Action.ActionType.ACTION_KEY_PRESSED Then
                    If action.m_key IsNot Nothing Then
                        If action.m_key.KeyChar = 56 Then
                            SetViewProperties(8)

                            _ItemsCache.Clear()
                            AbortRunningThreads()

                            _ThreadLoadItemsFromDatabase = New Thread(AddressOf LoadItemsFromDatabase)
                            _ThreadLoadItemsFromDatabase.IsBackground = True
                            _ThreadLoadItemsFromDatabase.Start()
                            Return

                        End If
                    End If
                End If

                '(9) is pressed -> "TMDB Info"
                If action.wID = MediaPortal.GUI.Library.Action.ActionType.ACTION_KEY_PRESSED Then
                    If action.m_key IsNot Nothing Then
                        If action.m_key.KeyChar = 57 Then
                            If _ProgramList.IsFocused Then
                                _SelectedEPGItemId = _ProgramList.Item(_ProgramList.SelectedListItemIndex).ItemId
                            End If
                            _idProgram = TVMovieProgram.Retrieve(_SelectedEPGItemId).idProgram
                            _tmdbCacheKey = Program.Retrieve(_SelectedEPGItemId).Title
                            Dim title As String = TVMovieProgram.Retrieve(_SelectedEPGItemId).ReferencedProgram.Title
                            If Not _TMDbCache.ContainsKey(_tmdbCacheKey) Then
                                _cacheHander.AddItem(TVMovieProgram.Retrieve(_SelectedEPGItemId), _TMDbCache)
                                ' Try to persist the Cache, only if the cache build is not running
                                If Not _ThreadBuildTMDb.IsAlive Then
                                    _cacheHander.persistCache(_TMDbCache)
                                End If
                            End If
                            If _TMDbCache.Item(_tmdbCacheKey).movie Is Nothing Then
                                CSGuideHelper.ShowNotify("Kein TMDb Eintrag")
                            Else
                                If Not File.Exists(_TMDbCache.Item(_tmdbCacheKey).Misc.absFanartPath) Then
                                    Utils.DownLoadAndCacheImage(_TMDbCache.Item(_tmdbCacheKey).Misc.fanartURL _
                                                                        , _TMDbCache.Item(_tmdbCacheKey).Misc.absFanartPath)
                                End If
                                CSGuideWindowsTMDb.MovieInfo = _TMDbCache.Item(_tmdbCacheKey)
                                CSGuideWindowsTMDb.TmdbClient = _tmdbClient
                                GUIWindowManager.ActivateWindow(730352, False)
                            End If

                        End If
                    End If
                End If

                '(0) is pressed -> "Single Channel view"
                If action.wID = MediaPortal.GUI.Library.Action.ActionType.ACTION_KEY_PRESSED Then
                    If action.m_key IsNot Nothing Then
                        If action.m_key.KeyChar = 48 Then
                            SetViewProperties(0)

                            'Get the channel number of the program selected
                            '_SelectedNiceEPGItemId = _niceEPGList.Item(_niceEPGList.SelectedListItemIndex).ItemId

                            'Try
                            '    '   If _niceEPGList.IsFocused = True Then
                            _channelNumber = Program.Retrieve(_SelectedEPGItemId).ReferencedChannel.ChannelNumber
                            _idProgram = Program.Retrieve(_SelectedEPGItemId).IdProgram
                            '    '  End If
                            'Catch ex As Exception
                            '    MyLog.Error(String.Format("[{0}] [{1}]: Exception err: {2} stack: {3}", _mClass, mName, ex.Message, ex.StackTrace))
                            'End Try

                            _ItemsCache.Clear()
                            AbortRunningThreads()

                            _ThreadLoadItemsFromDatabase = New Thread(AddressOf LoadItemsFromDatabase)
                            _ThreadLoadItemsFromDatabase.IsBackground = True
                            _ThreadLoadItemsFromDatabase.Start()
                            Return
                        End If
                    End If
                End If

                'Move right: Forward is pressed -> one channel group forward
                If action.wID = MediaPortal.GUI.Library.Action.ActionType.ACTION_MOVE_RIGHT Then
                    If _HiddenMenuOpen Then
                        _HiddenMenuOpen = False
                    Else
                        _previousViewNumber = _actualViewNumber
                        If _viewType.Equals("Overview") Then
                            _TvGroupFilter = GetNextChannelGroup(_TvGroupFilter, 1)

                            _ItemsCache.Clear()
                            AbortRunningThreads()

                            _ThreadLoadItemsFromDatabase = New Thread(AddressOf LoadItemsFromDatabase)
                            _ThreadLoadItemsFromDatabase.IsBackground = True
                            _ThreadLoadItemsFromDatabase.Start()
                            Return
                        Else
                            _channelNumber = GetNextChannelNumber(1)

                            'Try
                            '    If _niceEPGList.IsFocused = True Then

                            '        NiceEPGSetGUIProperties(TVMovieProgram.Retrieve(_SelectedNiceEPGItemId))
                            _StartTimePreviousItem = TVMovieProgram.Retrieve(_SelectedEPGItemId).ReferencedProgram.StartTime
                            '    End If

                            'Catch ex As Exception
                            '    MyLog.Error(String.Format("[{0}] [{1}]: Move right - Err: {2} stack: {3}", _mClass, mName, ex.Message, ex.StackTrace))
                            'End Try

                            _ItemsCache.Clear()
                            AbortRunningThreads()

                            _ThreadLoadItemsFromDatabase = New Thread(AddressOf LoadItemsFromDatabase)
                            _ThreadLoadItemsFromDatabase.IsBackground = True
                            _ThreadLoadItemsFromDatabase.Start()
                            Return
                        End If
                    End If

                End If

                'Move left -> one channel group back
                If action.wID = MediaPortal.GUI.Library.Action.ActionType.ACTION_MOVE_LEFT Then
                    _previousViewNumber = _actualViewNumber

                    If _viewType.Equals("Overview") Then
                        'if "All Channels" AND HiddenMenu Mode then open hidden Menu
                        If Not (_TvGroupFilter.Equals("All Channels") And CSGuideSettings.HiddenMenuMode) Then
                            _TvGroupFilter = GetNextChannelGroup(_TvGroupFilter, 0)

                            _ItemsCache.Clear()
                            AbortRunningThreads()

                            _ThreadLoadItemsFromDatabase = New Thread(AddressOf LoadItemsFromDatabase)
                            _ThreadLoadItemsFromDatabase.IsBackground = True
                            _ThreadLoadItemsFromDatabase.Start()
                            Return
                        Else
                            _HiddenMenuOpen = True
                            If _ProgramList.IsFocused Then _SelectedEPGItemId = _ProgramList.Item(_ProgramList.SelectedListItemIndex).ItemId
                        End If
                    Else
                        'if "Channel Number = 1 AND HiddenMenu Mode the open hidden Menu
                        If Not (_channelNumber = 1 And CSGuideSettings.HiddenMenuMode) Then
                            _channelNumber = GetNextChannelNumber(0)
                            'Try
                            '    If _niceEPGList.IsFocused = True Then
                            SetGUIProperties(TVMovieProgram.Retrieve(_SelectedEPGItemId))
                            _StartTimePreviousItem = TVMovieProgram.Retrieve(_SelectedEPGItemId).ReferencedProgram.StartTime
                            '    End If

                            'Catch ex As Exception
                            '    MyLog.Error(String.Format("[{0}] [{1}]: Move left - Err: {2} stack: {3}", _mClass, mName, ex.Message, ex.StackTrace))
                            'End Try

                            _ItemsCache.Clear()
                            AbortRunningThreads()

                            _ThreadLoadItemsFromDatabase = New Thread(AddressOf LoadItemsFromDatabase)
                            _ThreadLoadItemsFromDatabase.IsBackground = True
                            _ThreadLoadItemsFromDatabase.Start()
                            Return
                        Else
                            _HiddenMenuOpen = True
                            If _ProgramList.IsFocused Then _SelectedEPGItemId = _ProgramList.Item(_ProgramList.SelectedListItemIndex).ItemId
                        End If
                    End If
                End If

                'Play Button (P) -> Start channel
                If action.wID = MediaPortal.GUI.Library.Action.ActionType.ACTION_MUSIC_PLAY Then
                    Try
                        If _ProgramList.IsFocused = True Then CSGuideHelper.StartTv(Program.Retrieve(_ProgramList.SelectedListItem.ItemId))
                    Catch ex As Exception
                        MyLog.Error(String.Format("[{0}] [{1}]: Play Button - Err: {2} stack: {3}", _mClass, mName, ex.Message, ex.StackTrace))

                    End Try
                End If

                'Record Button (R) -> MP TvProgramInfo aufrufen --Über keychar--
                If action.wID = MediaPortal.GUI.Library.Action.ActionType.ACTION_KEY_PRESSED Then
                    If action.m_key IsNot Nothing Then
                        If action.m_key.KeyChar = 114 Then
                            If _ProgramList.IsFocused = True Then CSGuideHelper.LoadTVProgramInfo(Program.Retrieve(_ProgramList.SelectedListItem.ItemId))
                        End If
                    End If
                End If

                'Record Button (R) -> MP TvProgramInfo aufrufen
                If action.wID = MediaPortal.GUI.Library.Action.ActionType.ACTION_RECORD Then
                    If _ProgramList.IsFocused = True Then CSGuideHelper.LoadTVProgramInfo(Program.Retrieve(_ProgramList.SelectedListItem.ItemId))
                End If

                'Menu Button (F9) -> Context Menu open
                If action.wID = MediaPortal.GUI.Library.Action.ActionType.ACTION_CONTEXT_MENU Then

                    If _ProgramList.IsFocused = True Then ShowItemsContextMenu(_ProgramList.SelectedListItem.ItemId)
                End If

                'OSD Info Button (Y) -> Context Menu open (gleiche Fkt. wie Menu Button)
                If action.wID = MediaPortal.GUI.Library.Action.ActionType.ACTION_KEY_PRESSED Then
                    If action.m_key IsNot Nothing Then
                        If action.m_key.KeyChar = 121 Then

                            If _ProgramList.IsFocused = True Then ShowItemsContextMenu(_ProgramList.SelectedListItem.ItemId)
                        End If
                    End If
                End If
                '''' TEST '''''
                'Menu Button (q) -> Context Menu open
                If action.wID = MediaPortal.GUI.Library.Action.ActionType.ACTION_KEY_PRESSED Then
                    If action.m_key IsNot Nothing Then
                        If action.m_key.KeyChar = 113 Then
                            'TEST AERA
                        End If
                    End If
                End If
            End If

            MyBase.OnAction(action)
        End Sub

        Protected Overrides Sub OnClicked(ByVal controlId As Integer,
                                  ByVal control As GUIControl,
                                  ByVal actionType As _
                                  MediaPortal.GUI.Library.Action.ActionType)

            MyBase.OnClicked(controlId, control, actionType)

            If control Is _ProgramList Then
                If actionType = MediaPortal.GUI.Library.Action.ActionType.ACTION_SELECT_ITEM Then
                    If _ProgramList.IsFocused = True Then CSGuideHelper.StartTv(Program.Retrieve(_ProgramList.SelectedListItem.ItemId))
                End If
            End If
            If control Is _buttonControlView1 Then
                SendKeys.Send("1")
                _buttonControlView1.IsFocused = False
                _HiddenMenuOpen = False
            End If
            If control Is _buttonControlView2 Then
                SendKeys.Send("2")
                _buttonControlView2.IsFocused = False
                _HiddenMenuOpen = False
            End If
            If control Is _buttonControlView3 Then
                SendKeys.Send("3")
                _buttonControlView3.IsFocused = False
                _HiddenMenuOpen = False
            End If
            If control Is _buttonControlView4 Then
                SendKeys.Send("4")
                _buttonControlView4.IsFocused = False
                _HiddenMenuOpen = False
            End If
            If control Is _buttonControlView5 Then
                SendKeys.Send("5")
                _buttonControlView5.IsFocused = False
                _HiddenMenuOpen = False
            End If
            If control Is _buttonControlView6 Then
                SendKeys.Send("6")
                _buttonControlView6.IsFocused = False
                _HiddenMenuOpen = False
            End If
            If control Is _buttonControlView7 Then
                SendKeys.Send("7")
                _buttonControlView7.IsFocused = False
                _HiddenMenuOpen = False
            End If
            If control Is _buttonControlView8 Then
                SendKeys.Send("8")
                _buttonControlView8.IsFocused = False
                _HiddenMenuOpen = False
            End If
            If control Is _buttonControlView9 Then
                _SelectedEPGItemId = _ProgramList.Item(_ProgramList.SelectedListItemIndex).ItemId
                SendKeys.Send("9")
                _buttonControlView9.IsFocused = False
                _HiddenMenuOpen = False
            End If
            If control Is _buttonControlView0 Then
                SendKeys.Send("0")
                _buttonControlView0.IsFocused = False
                _HiddenMenuOpen = False
            End If

        End Sub

        Protected Overrides Sub OnPreviousWindow()
            MyLog.Debug("[CSGuideWindow] [OnPreviousWindow]")
            MyBase.OnPreviousWindow()
            _LastFocusedIndex = 0
            _LastFocusedControlID = _ProgramList.GetID
        End Sub
#End Region

#Region "Functions"

        Friend Shared Function GetSqlGroupFilterString(ByVal TvGroupFilter As String) As String
            '#CPGFilter vorbereiten
            Dim groupFilter As New StringBuilder(
                "(SELECT groupmap.idgroup FROM mptvdb.groupmap Inner " &
                "join mptvdb.channelgroup ON groupmap.idgroup = channelgroup.idGroup " &
                "WHERE groupmap.idchannel = program.idchannel And channelgroup.groupName = '#FilterTvGroup')")
            'TvGroup Filter setzen
            groupFilter.Replace("#FilterTvGroup", TvGroupFilter)

            Return groupFilter.ToString

        End Function


        Private Sub SetGUIProperties(ByVal myTVMovieProgram As TVMovieProgram)
            CSGuideHelper.LoadFanart(_backdrop, getFanartFileNameAbsPath(myTVMovieProgram))
            CSGuideHelper.SetProperty("#SettingLastUpdate", CSGuideSettings.TvMovieImportStatus)
            CSGuideHelper.SetProperty("#ChannelGroup", _TvGroupFilter)
            'CSGuideHelper.CSGuideHelper.SetProperty("#TMDbFanArt", getFanartFileName(myTVMovieProgram))

            If _viewType.Equals("Overview") Then
                'CSGuideHelper.SetProperty("#ItemsRightListLabel",
                '                            m_StartTime.ToString("dddd, dd.MM") & " ~" & m_StartTime.ToString("HH:mm"))
                CSGuideHelper.SetProperty("#ItemsRightListLabel", "")
                CSGuideHelper.SetProperty("#EPGView", _viewDisplayName)

                CSGuideHelper.SetProperty("#ChannelName", "")
            End If

            If myTVMovieProgram IsNot Nothing Then
                'CSGuideHelper.SetProperty("#DetailDescription", getAdaptedDescription(myTVMovieProgram.ReferencedProgram.Description))
                CSGuideHelper.SetProperty("#DetailDescription", myTVMovieProgram.ReferencedProgram.Description)
                CSGuideHelper.SetProperty("#DetailImage", CSGuideHelper.Image(myTVMovieProgram))
                CSGuideHelper.SetProperty("#DetailTitle", myTVMovieProgram.ReferencedProgram.Title)
                CSGuideHelper.SetProperty("#ShortDescription", myTVMovieProgram.ShortDescribtion)

                CSGuideHelper.SetProperty("#DetailGenre", myTVMovieProgram.ReferencedProgram.Genre)
                CSGuideHelper.SetProperty("#MovieListTvMovieStar", CSGuideHelper.TvMovieStar(myTVMovieProgram))
                CSGuideHelper.SetProperty("#Duration", DateDiff(DateInterval.Minute, myTVMovieProgram.ReferencedProgram.StartTime, myTVMovieProgram.ReferencedProgram.EndTime) & " min")

                If myTVMovieProgram.Dolby = True Then
                    CSGuideHelper.SetProperty("#DetailAudioImage", "Logos\audio\dolbydigital.png")
                Else
                    CSGuideHelper.SetProperty("#DetailAudioImage", "Logos\audio\stereo.png")
                End If

                If myTVMovieProgram.HDTV = True Then
                    CSGuideHelper.SetProperty("#DetailProgramFormat", "Logos\resolution\720p.png")
                Else
                    CSGuideHelper.SetProperty("#DetailProgramFormat", "Logos\resolution\540.png")
                End If

                CSGuideHelper.SetProperty("#DetailFSK", CSGuideHelper.DetailFSK(myTVMovieProgram))
                CSGuideHelper.SetProperty("#DetailRatingFun", getRatingpercentage(myTVMovieProgram.Fun))
                CSGuideHelper.SetProperty("#DetailRatingAction", getRatingpercentage(myTVMovieProgram.Action))
                CSGuideHelper.SetProperty("#DetailRatingErotic", getRatingpercentage(myTVMovieProgram.Erotic))
                CSGuideHelper.SetProperty("#DetailRatingSuspense", getRatingpercentage(myTVMovieProgram.Tension))
                CSGuideHelper.SetProperty("#DetailRatingLevel", getRatingpercentage(myTVMovieProgram.Requirement))
                CSGuideHelper.SetProperty("#DetailRatingEmotions", getRatingpercentage(myTVMovieProgram.Feelings))
                CSGuideHelper.SetProperty("#SelectedProgramDates", myTVMovieProgram.ReferencedProgram.ReferencedChannel.DisplayName & ": " &
                            myTVMovieProgram.ReferencedProgram.StartTime.ToString("dddd, dd.MM") &
                            " (" &
                            myTVMovieProgram.ReferencedProgram.StartTime.ToString("HH:mm") & " - " &
                            myTVMovieProgram.ReferencedProgram.EndTime.ToString("HH:mm") & ")")
                If _viewType.Equals("Single") Then
                    'CSGuideHelper.SetProperty("#ItemsRightListLabel",
                    '    myTVMovieProgram.ReferencedProgram.StartTime.ToString("dddd, dd.MM") &
                    '    " ~" &
                    'myTVMovieProgram.ReferencedProgram.StartTime.ToString("HH:mm") & " Uhr")
                    CSGuideHelper.SetProperty("#ItemsRightListLabel", "")

                    CSGuideHelper.SetProperty("#EPGView", _viewDisplayName)
                    CSGuideHelper.SetProperty("#ChannelName", myTVMovieProgram.ReferencedProgram.ReferencedChannel.DisplayName)
                End If
            Else
                CSGuideHelper.SetProperty("#DetailDescription", "")
                CSGuideHelper.SetProperty("#DetailImage", "")
                CSGuideHelper.SetProperty("#DetailTitle", "")
                CSGuideHelper.SetProperty("#ShortDescription", "")
                CSGuideHelper.SetProperty("#DetailGenre", "")
                CSGuideHelper.SetProperty("#MovieListTvMovieStar", "")
                CSGuideHelper.SetProperty("#Duration", "")
                CSGuideHelper.SetProperty("#DetailAudioImage", "")
                CSGuideHelper.SetProperty("#DetailAudioImage", "")
                CSGuideHelper.SetProperty("#DetailProgramFormat", "")
                CSGuideHelper.SetProperty("#DetailFSK", "")
                CSGuideHelper.SetProperty("#DetailRatingFun", "0")
                CSGuideHelper.SetProperty("#DetailRatingAction", "0")
                CSGuideHelper.SetProperty("#DetailRatingErotic", "0")
                CSGuideHelper.SetProperty("#DetailRatingSuspense", "0")
                CSGuideHelper.SetProperty("#DetailRatingLevel", "0")
                CSGuideHelper.SetProperty("#DetailRatingEmotions", "0")
                CSGuideHelper.SetProperty("#SelectedProgramDates", "")
                CSGuideHelper.SetProperty("#ChannelName", "Nichts gefunden")
            End If
        End Sub
        Private Function getAdaptedDescription(ByVal programDescription As String) As String
            ' Get rid of "Action=1"\n"Gefühl=2"\n usw - das wird ja schon in den Sternen gezeigt
            ' Probleme mit \n
            Dim pattern As String = "(.*?Wertung:.*?\n)[\w=\d\n]*(.*)"
            Dim rgx As New System.Text.RegularExpressions.Regex(pattern, RegexOptions.Singleline)
            Dim returnString As String = rgx.Replace(programDescription, "$1***************\n$2")
            Return returnString
        End Function
        Private Function getRatingpercentage(ByVal TvMovieRating As Integer) As Integer
            Select Case TvMovieRating
                Case Is = 0
                    Return 0
                Case Is = 1
                    Return 5
                Case Is = 2
                    Return 8
                Case Is = 3
                    Return 10
            End Select
        End Function
        Private Sub FillChGroupChannelCache()
            Dim groupMapList As IList(Of GroupMap)
            Dim channelGroupList As New List(Of Integer)

            groupMapList = GroupMap.ListAll

            ' First fill the list of distinct channel groups             
            For Each _channelGroup In ChannelGroup.ListAll
                channelGroupList.Add(_channelGroup.IdGroup)
            Next

            ' For all channelGroup create a list or corresponding channels
            For Each _channelGroupId In channelGroupList
                Dim _channelList As New List(Of Integer)
                For Each _groupMap In groupMapList
                    If _groupMap.IdGroup = _channelGroupId Then
                        _channelList.Add(Channel.Retrieve(_groupMap.IdChannel).ChannelNumber)
                    End If
                Next
                _channelList.Sort()
                ' The Add the list to a hashtable
                ' For example you receive
                ' m_chGroupChannelCache("AllChannels", [1,2,3,5,8])
                _chGroupChannelCache.Add(ChannelGroup.Retrieve(_channelGroupId).GroupName, _channelList)
            Next
        End Sub
        Private Function GetNextChannelNumber(ByVal order As Integer) As Integer
            ' order = 1 forward, order = 0 backward
            Dim channelNumberList = _chGroupChannelCache(_TvGroupFilter)
            Select Case order
                Case 1
                    For i = 0 To channelNumberList.Count - 1
                        If channelNumberList(i) = _channelNumber Then
                            If i + 1 = channelNumberList.Count Then
                                Return channelNumberList.First
                            Else
                                Return channelNumberList(i + 1)
                            End If
                        End If
                    Next
                Case 0
                    For i = 0 To channelNumberList.Count - 1
                        If channelNumberList(i) = _channelNumber Then
                            If i - 1 < 0 Then
                                Return channelNumberList.Last
                            Else
                                Return channelNumberList(i - 1)
                            End If
                        End If
                    Next
            End Select
            Return channelNumberList.First
        End Function
        Private Function GetNextChannelGroup(ByVal currentChannelGroup As String, order As Integer) As String
            ' order = 1 forward, order = 0 backward
            Dim channelGroupList = _chGroupChannelCache.Keys
            Dim currentGroupIndex As Integer

            For currentGroupIndex = 0 To channelGroupList.Count - 1
                If channelGroupList(currentGroupIndex).Equals(_TvGroupFilter) Then
                    Exit For
                End If
            Next

            Select Case order
                Case 1
                    If (currentGroupIndex = channelGroupList.Count - 1) Then
                        Return channelGroupList.First
                    Else
                        Return channelGroupList(currentGroupIndex + 1)
                    End If
                Case 0
                    If (currentGroupIndex = 0) Then
                        Return channelGroupList.Last
                    Else
                        Return channelGroupList(currentGroupIndex - 1)
                    End If
            End Select
            Return channelGroupList.First
        End Function

        Private Sub LoadItemsFromDatabase()
            Dim mName As String = System.Reflection.MethodInfo.GetCurrentMethod.Name
            ' Dim _mClass As String = Me.GetType.Name
            Try
                Dim timer As Date = Date.Now
                Dim totaltimer As Date = Date.Now

                GuiLayoutLoading()
                _ItemsCache.Clear()
                ' _CurrentCounter = 0

                'SQL String bauen
                Dim SqlStringBuilder As New StringBuilder(_SqlString)
                '_SqlStringBuilder.Replace("#EndTimeSQL", m_SqlSubStringMoviesEndTime)
                SqlStringBuilder.Replace("#StartTime", CSGuideHelper.MySqlDate(_StartTime))
                SqlStringBuilder.Replace("#ChannelNumber", _channelNumber)
                SqlStringBuilder.Replace("#TVGroupFilter", GetSqlGroupFilterString(_TvGroupFilter))
                '_SqlStringBuilder.Replace("#IntervalDay", m_IntervalHour)

                SqlStringBuilder.Replace(" * ", " TVMovieProgram.idProgram, TVMovieProgram.Action, TVMovieProgram.Actors, TVMovieProgram.BildDateiname, TVMovieProgram.Country, TVMovieProgram.Cover, TVMovieProgram.Describtion, TVMovieProgram.Dolby, TVMovieProgram.EpisodeImage, TVMovieProgram.Erotic, TVMovieProgram.FanArt, TVMovieProgram.Feelings, TVMovieProgram.FileName, TVMovieProgram.Fun, TVMovieProgram.HDTV, TVMovieProgram.idEpisode, TVMovieProgram.idMovingPictures, TVMovieProgram.idSeries, TVMovieProgram.idVideo, TVMovieProgram.KurzKritik, TVMovieProgram.local, TVMovieProgram.Regie, TVMovieProgram.Requirement, TVMovieProgram.SeriesPosterImage, TVMovieProgram.ShortDescribtion, TVMovieProgram.Tension, TVMovieProgram.TVMovieBewertung ")
                MyLog.Debug(String.Format("[{0}] [{1}]: Executing: {2} ", _mClass, mName, SqlStringBuilder.ToString))
                Dim _SQLstate As SqlStatement = Broker.GetStatement(SqlStringBuilder.ToString)
                Dim _ItemsOnLoad As List(Of TVMovieProgram) = ObjectFactory.GetCollection(GetType(TVMovieProgram), _SQLstate.Execute())
                MyLog.Info(String.Format("[{0}] [{1}]: Received {2} records in {3}sec", _mClass, mName, _ItemsOnLoad.Count, (DateTime.Now - timer).TotalSeconds))

                If _ItemsOnLoad.Count > 0 Then
                    _ItemsCache = _ItemsOnLoad
                    _ThreadFillProgramList = New Thread(AddressOf FillProgramList)
                    _ThreadFillProgramList.IsBackground = True
                    _ThreadFillProgramList.Start()
                    '' here muss der Cache gefüllt werden
                    If _UseTMDb Then
                        _ThreadBuildTMDb = New Thread(AddressOf UpdateTMDbCache)
                        _ThreadBuildTMDb.IsBackground = True
                        _ThreadBuildTMDb.Start()
                    End If

                Else
                    _DataLoadingAnimation.Visible = False
                    SetGUIProperties(Nothing)
                End If

            Catch ex As ThreadAbortException
                MyLog.Error(String.Format("[{0}] [{1}]: Thread aborted", _mClass, mName))
            Catch ex As GentleException
                MyLog.Error(String.Format("[{0}] [{1}]: GentleException err: {2} stack: {3}", _mClass, mName, ex.Message, ex.StackTrace))
            Catch ex As Exception
                MyLog.Error(String.Format("[{0}] [{1}]: Exception err: {2} stack: {3}", _mClass, mName, ex.Message, ex.StackTrace))
            End Try

        End Sub
        Private Sub UpdateTMDbCache()
            Dim mName As String = System.Reflection.MethodInfo.GetCurrentMethod.Name
            ' Dim _mClass As String = Me.GetType.Name
            Dim stopwatch As Diagnostics.Stopwatch = New Diagnostics.Stopwatch
            stopwatch.Start()
            MyLog.Info(String.Format("[{0}] [{1}]: Updating TMDb-Cache View {2} Start", _mClass, mName, _actualViewNumber))
            _cacheHander.buildCache(_TMDbCache, _ItemsCache, CSGuideSettings.View(_actualViewNumber).DisplayName)
            stopwatch.Stop()
            MyLog.Info(String.Format("[{0}] [{1}]: Updated TMDb-Cache View {2} in {3} ms", _mClass, mName,
                                      _actualViewNumber, stopwatch.ElapsedMilliseconds))
        End Sub
        Private Sub FillProgramList()
            Dim mName As String = System.Reflection.MethodInfo.GetCurrentMethod.Name
            ' Dim _mClass As String = Me.GetType.Name

            Dim timer As Date = Date.Now
            Dim ItemCounter As Integer = 0
            Dim getYearString As String = ""
            Dim getEpisodeString As String = ""

            _ProgramList.Visible = False
            _ProgramList.AllocResources() 'was macht das?
            _ProgramList.Clear()

            Try

                For i = 0 To _ItemsCache.Count - 1

                    Try
                        ItemCounter = ItemCounter + 1
                        Dim _TvMovieProgram As TVMovieProgram = _ItemsCache.Item(i)

                        getYearString = CSGuideHelper.GetYearString(_TvMovieProgram)
                        getEpisodeString = CSGuideHelper.GetEpisodeString(_TvMovieProgram)

                        CSGuideHelper.AddListControlItem(_ProgramList,
                                           _TvMovieProgram.idProgram,
                                           _TvMovieProgram.ReferencedProgram.ReferencedChannel.DisplayName,
                                           _TvMovieProgram.ReferencedProgram.Title & getEpisodeString & getYearString, ,
                                           CSGuideHelper.TimeLabel(_TvMovieProgram),
                                           Config.GetFile(Config.Dir.Thumbs, "tv\logos\") & Replace(_TvMovieProgram.ReferencedProgram.ReferencedChannel.DisplayName, "/", "_") & ".png", ,
                                           CSGuideHelper.RecordingStatus(_TvMovieProgram.ReferencedProgram))

                    Catch ex As ThreadAbortException
                        MyLog.Error(String.Format("[{0}] [{1}]: Thread aborted", _mClass, mName))
                    Catch ex As GentleException
                        MyLog.Error(String.Format("[{0}] [{1}]: GentleException err: {2} stack: {3}", _mClass, mName, ex.Message, ex.StackTrace))
                    Catch ex As Exception
                        MyLog.Error(String.Format("[{0}] [{1}]: Exception err: {2} stack: {3}", _mClass, mName, ex.Message, ex.StackTrace))
                    End Try
                Next

                _DataLoadingAnimation.Visible = False
                _ProgramList.Visible = True

                MyLog.Info(String.Format("[{0}] [{1}]: Thread finished in {2}s", _mClass, mName, (DateTime.Now - timer).TotalSeconds))

                GUIListControl.SelectItemControl(GetID, _LastFocusedControlID, _LastFocusedIndex)
                GUIListControl.FocusControl(GetID, _LastFocusedControlID)

                _PageProgress.Percentage = 100 * ((_ProgramList.SelectedListItemIndex) / _ProgramList.Count)

                If _ProgramList.SelectedListItem Is Nothing Then
                    _ProgramList.SelectedItem = 0
                    MyLog.Debug(String.Format("[{0}] [{1}]: _ProgramList.SelectedListItem is Nothing", _mClass, mName))
                Else
                    SetCorrectListItemIndex()
                    SetGUIProperties(TVMovieProgram.Retrieve(_ProgramList.SelectedListItem.ItemId))
                End If


                _ProgramList.IsFocused = True

                ' GUIWindowManager.NeedRefresh()

            Catch ex As ThreadAbortException
                MyLog.Error(String.Format("[{0}] [{1}]: Thread aborted", _mClass, mName))
            Catch ex As GentleException
                MyLog.Error(String.Format("[{0}] [{1}]: GentleException err: {2} stack: {3}", _mClass, mName, ex.Message, ex.StackTrace))
            Catch ex As Exception
                MyLog.Error(String.Format("[{0}] [{1}]: Exception err: {2} stack: {3}", _mClass, mName, ex.Message, ex.StackTrace))
            End Try
        End Sub

        'ProgresBar paralell anzeigen
        Private Sub ShowLeftProgressBar()
            _DataLoadingAnimation.Visible = True
        End Sub

        Private Sub AbortRunningThreads()
            Dim mName As String = System.Reflection.MethodInfo.GetCurrentMethod.Name

            Try
                If _ThreadLoadItemsFromDatabase.IsAlive = True Then
                    _ThreadLoadItemsFromDatabase.Abort()
                    MyLog.Debug(String.Format("[{0}] [{1}]: ThreadLoadItemsFromDatabase aborted", _mClass, mName))
                End If
                If _ThreadFillProgramList.IsAlive = True Then
                    _ThreadFillProgramList.Abort()
                    MyLog.Debug(String.Format("[{0}] [{1}]: ThreadFillEPGList aborted", _mClass, mName))
                End If
                If _UseTMDb Then ' to avoid exception
                    If _ThreadBuildTMDb.IsAlive = True Then
                        _ThreadBuildTMDb.Abort()
                        MyLog.Debug(String.Format("[{0}] [{1}]: ThreadBuildTMDb aborted", _mClass, mName))
                    End If
                End If

            Catch ex As Exception
                MyLog.Error(String.Format("[{0}] [{1}]: Exception err: {2} stack: {3}", _mClass, mName, ex.Message, ex.StackTrace))
            End Try
        End Sub

        Private Sub SetCorrectListItemIndex()
            ' ToDo
            ' what it should do
            ' Improve Navigation experience 
            ' (1) In case one program is slected in the Overview and you press 0 the focus should be on the same program
            ' (2) In case you are in single channel view the program selected in the next single channel view should be on the same (or similar start date)



            ' First start searching the prgram id - this is possible if previous EPGView is not SingleChannel
            If (CSGuideSettings.View(_previousViewNumber).Type <> "Single") Then

                If _idProgram <> 0 Then
                        For i = 0 To _ProgramList.Count - 1
                            If _ProgramList.Item(i).ItemId = _idProgram Then
                                _ProgramList.SelectedListItemIndex = i
                                _StartTimePreviousItem = TVMovieProgram.Retrieve(_ProgramList.Item(i).ItemId).ReferencedProgram.StartTime
                                Exit Sub
                            End If
                        Next
                    End If

            ElseIf (_viewType.Equals("Single")) And (_StartTimePreviousItem <> Nothing) Then
                Try
                    For i = 0 To _ProgramList.Count - 1
                        Dim _TvMovieProgram As TVMovieProgram = TVMovieProgram.Retrieve(_ProgramList.Item(i).ItemId)
                        If _TvMovieProgram.ReferencedProgram.StartTime >= _StartTimePreviousItem Then
                            If _TvMovieProgram.ReferencedProgram.StartTime = _StartTimePreviousItem Then
                                _ProgramList.SelectedListItemIndex = i
                            Else
                                _ProgramList.SelectedListItemIndex = i - 1
                            End If
                            _StartTimePreviousItem = TVMovieProgram.Retrieve(_ProgramList.Item(_ProgramList.SelectedListItemIndex).ItemId).ReferencedProgram.StartTime
                            Exit Sub
                        End If
                    Next
                Catch ex As Exception
                End Try
            Else
                _ProgramList.SelectedListItemIndex = 0
            End If
            _SelectedEPGItemId = _ProgramList.Item(_ProgramList.SelectedListItemIndex).ItemId
        End Sub

        Private Function getFanartFileNameAbsPath(ByVal myTVMovieProgram As TVMovieProgram) As String
            If Not myTVMovieProgram Is Nothing Then
                If _TMDbCache.ContainsKey(myTVMovieProgram.ReferencedProgram.Title) Then
                    Return getFanartFileName(myTVMovieProgram)
                End If
            End If
            Return Path.Combine(Config.GetSubFolder(Config.Dir.Skin, Config.SkinName & "\media\hover_ClickfinderSimpleGuide.png"))
        End Function
        Private Function getFanartFileName(ByVal myTVMovieProgram As TVMovieProgram) As String
            Dim mName As String = System.Reflection.MethodInfo.GetCurrentMethod.Name
            ' Dim _mClass As String = Me.GetType.Name

            Dim cacheKey As String = myTVMovieProgram.ReferencedProgram.Title
            Dim backgroundImage As String = "hover_ClickfinderSimpleGuide.png"

            Try
                If _TMDbCache.ContainsKey(cacheKey) Then
                    Dim absFanartPath As String = _TMDbCache.Item(cacheKey).Misc.absFanartPath
                    Dim fanartURL As String = _TMDbCache.Item(cacheKey).Misc.fanartURL

                    If (Not String.IsNullOrEmpty(absFanartPath)) And (Not String.IsNullOrEmpty(fanartURL)) Then
                        If Not File.Exists(absFanartPath) Then
                            Utils.DownLoadAndCacheImage(fanartURL, absFanartPath)
                        End If
                        Return absFanartPath
                    End If
                End If
            Catch ex As Exception
                MyLog.Error(String.Format("[{0}] [{1}]: Err: {2} stack: {3}", _mClass, mName, ex.Message, ex.StackTrace))
            End Try
            Return Path.Combine(Config.GetSubFolder(Config.Dir.Skin, Config.SkinName & "\media\" & backgroundImage))
        End Function

        Private Shared Sub FetchConfig(client As TMDbClient)
            Dim configXml As New FileInfo(Path.Combine(Config.GetSubFolder(Config.Dir.Skin, Config.SkinName & "\media\CSGuide\CSGuideTMDbLibConfig.xml")))

            Console.WriteLine("Config file: " & configXml.FullName & ", Exists: " & configXml.Exists)

            If configXml.Exists AndAlso configXml.LastWriteTimeUtc >= DateTime.UtcNow.AddHours(-1) Then
                Console.WriteLine("Using stored config")
                Dim xml As String = File.ReadAllText(configXml.FullName, Encoding.Unicode)

                Dim xmlDoc As New XmlDocument()
                xmlDoc.LoadXml(xml)

                client.SetConfig(Serializer.Deserialize(Of TMDbConfig)(xmlDoc))
            Else
                Console.WriteLine("Getting new config")
                client.GetConfig()

                Console.WriteLine("Storing config")
                Dim xmlDoc As XmlDocument = Serializer.Serialize(client.Config)
                File.WriteAllText(configXml.FullName, xmlDoc.OuterXml, Encoding.Unicode)
            End If
        End Sub



#End Region
#Region "Hidden Menu"

        Private Sub InitButtons()
            _buttonControlView1.Label = "(1): " & CSGuideSettings.View(1).DisplayName
            _buttonControlView2.Label = "(2): " & CSGuideSettings.View(2).DisplayName
            _buttonControlView3.Label = "(3): " & CSGuideSettings.View(3).DisplayName
            _buttonControlView4.Label = "(4): " & CSGuideSettings.View(4).DisplayName
            _buttonControlView5.Label = "(5): " & CSGuideSettings.View(5).DisplayName
            _buttonControlView6.Label = "(6): " & CSGuideSettings.View(6).DisplayName
            _buttonControlView7.Label = "(7): " & CSGuideSettings.View(7).DisplayName
            _buttonControlView8.Label = "(8): " & CSGuideSettings.View(8).DisplayName
            _buttonControlView9.Label = "(9): TMDb Info"
            _buttonControlView0.Label = "(0): " & CSGuideSettings.View(0).DisplayName
        End Sub
#End Region
#Region "MediaPortal Funktionen / Dialogs"
        Private Sub ShowItemsContextMenu(ByVal idProgram As Integer)
            Dim dlgContext As GUIDialogMenu = CType(GUIWindowManager.GetWindow(CType(GUIWindow.Window.WINDOW_DIALOG_MENU, Integer)), GUIDialogMenu)
            dlgContext.Reset()

            Dim _Program As Program = Program.Retrieve(idProgram)

            'ContextMenu Layout
            dlgContext.SetHeading("Clickfinder Simple Guide - Help")
            dlgContext.ShowQuickNumbers = False

            'ID 1
            Dim line1 As New GUIListItem
            line1.Label = "(1): " & CSGuideSettings.View(1).DisplayName
            dlgContext.Add(line1)
            line1.Dispose()
            'ID 2
            Dim line2 As New GUIListItem
            line2.Label = "(2): " & CSGuideSettings.View(2).DisplayName
            dlgContext.Add(line2)
            line2.Dispose()
            'ID 3
            Dim line3 As New GUIListItem
            line3.Label = "(3): " & CSGuideSettings.View(3).DisplayName
            dlgContext.Add(line3)
            line3.Dispose()
            'ID 4
            Dim line4 As New GUIListItem
            line4.Label = "(4): " & CSGuideSettings.View(4).DisplayName
            dlgContext.Add(line4)
            line4.Dispose()
            'ID 5
            Dim line5 As New GUIListItem
            line5.Label = "(5): " & CSGuideSettings.View(5).DisplayName
            dlgContext.Add(line5)
            line5.Dispose()
            'ID 6
            Dim line6 As New GUIListItem
            line6.Label = "(6): " & CSGuideSettings.View(6).DisplayName
            dlgContext.Add(line6)
            line6.Dispose()
            'ID 7
            Dim line23 As New GUIListItem
            line23.Label = "(7): " & CSGuideSettings.View(7).DisplayName
            dlgContext.Add(line23)
            line23.Dispose()
            'ID 8
            Dim line24 As New GUIListItem
            line24.Label = "(8): " & CSGuideSettings.View(8).DisplayName
            dlgContext.Add(line24)
            line24.Dispose()
            'ID 9
            Dim line7 As New GUIListItem
            line7.Label = "(9): TMDB Info"
            dlgContext.Add(line7)
            line7.Dispose()
            'ID 10
            Dim line20 As New GUIListItem
            line20.Label = "(0): " & CSGuideSettings.View(0).DisplayName
            dlgContext.Add(line20)
            line20.Dispose()
            'ID 11
            Dim line12 As New GUIListItem
            line12.Label = "Next Item (F8): Load next day"
            dlgContext.Add(line12)
            line12.Dispose()
            'ID 12
            Dim line13 As New GUIListItem
            line13.Label = "Previous Item (F7): Load day before"
            dlgContext.Add(line13)
            line13.Dispose()
            'ID 13
            Dim line14 As New GUIListItem
            line14.Label = "Forward (F6): Load one hour forward"
            dlgContext.Add(line14)
            line14.Dispose()
            'ID 14
            Dim line15 As New GUIListItem
            line15.Label = "Rewind (F5): Load one hour back"
            dlgContext.Add(line15)
            line15.Dispose()
            'ID 15
            Dim line8 As New GUIListItem
            line8.Label = "(Pfeil hoch): Navigiere hoch"
            dlgContext.Add(line8)
            line8.Dispose()
            'ID 16
            Dim line81 As New GUIListItem
            line81.Label = "(Pfeil runter): Navigiere runter"
            dlgContext.Add(line81)
            line81.Dispose()
            'ID 17
            Dim line9 As New GUIListItem
            line9.Label = "(Page up): Navigiere Page up"
            dlgContext.Add(line9)
            line9.Dispose()
            'ID 18
            Dim line91 As New GUIListItem
            line91.Label = "(Page down): Navigiere Page down"
            dlgContext.Add(line91)
            line91.Dispose()
            'ID 19
            Dim line10 As New GUIListItem
            Select Case CSGuideSettings.View(_actualViewNumber).Type
                Case "Overview"
                    line10.Label = "(Pfeil rechts): Eine Kanalgruppe vorwärts"
                Case "Single"
                    line10.Label = "(Pfeil rechts): Ein Kanal vorwärts"
            End Select
            dlgContext.Add(line10)
            line10.Dispose()
            'ID 20
            Dim line11 As New GUIListItem
            Select Case CSGuideSettings.View(_actualViewNumber).Type
                Case "Overview"
                    line11.Label = "(Pfeil links): Eine Kanalgruppe rückwärts"
                Case "Single"
                    line11.Label = "(Pfeil links): Ein Kanal rückwärts"
            End Select
            dlgContext.Add(line11)
            line11.Dispose()
            'ID 21
            Dim line16 As New GUIListItem
            line16.Label = "Play Button (P): Start channel"
            dlgContext.Add(line16)
            line16.Dispose()
            'ID 22
            Dim line17 As New GUIListItem
            line17.Label = "Record Button (R): TvProgramInfo"
            dlgContext.Add(line17)
            line17.Dispose()
            'ID 23
            Dim line18 As New GUIListItem
            line18.Label = "Menu Button (F9): Zeigt die Hilfe"
            dlgContext.Add(line18)
            line18.Dispose()
            'ID 24
            Dim line19 As New GUIListItem
            line19.Label = "OSD Info Button (Y): Zeigt die Hilfe"
            dlgContext.Add(line19)
            line19.Dispose()

            dlgContext.DoModal(GUIWindowManager.ActiveWindow)

            ' To be completed https://msdn.microsoft.com/de-de/library/system.windows.forms.sendkeys.send%28v=vs.110%29.aspx?cs-save-lang=1&cs-lang=vb#code-snippet-2
            Select Case dlgContext.SelectedId

                Case Is = 1
                    SendKeys.Send("1")
                Case Is = 2
                    SendKeys.Send("2")
                Case Is = 3
                    SendKeys.Send("3")
                Case Is = 4
                    SendKeys.Send("4")
                Case Is = 5
                    SendKeys.Send("5")
                Case Is = 6
                    SendKeys.Send("6")
                Case Is = 7
                    SendKeys.Send("7")
                Case Is = 8
                    SendKeys.Send("8")
                Case Is = 9
                    SendKeys.Send("9")
                Case Is = 10
                    SendKeys.Send("0")
                Case Is = 11
                    SendKeys.Send("{F8}")
                Case Is = 12
                    SendKeys.Send("{F7}")
                Case Is = 13
                    SendKeys.Send("{F6}")
                Case Is = 14
                    SendKeys.Send("{F5}")
                Case Is = 15
                    SendKeys.Send("{UP}")
                Case Is = 16
                    SendKeys.Send("{DOWN}")
                Case Is = 17
                    SendKeys.Send("{PGUP}")
                Case Is = 18
                    SendKeys.Send("{PGDN}")
                Case Is = 19
                    SendKeys.Send("{RIGHT}")
                Case Is = 20
                    SendKeys.Send("{LEFT}")
                Case Is = 21
                    SendKeys.Send("P")
                Case Is = 22
                    SendKeys.Send("R")
                Case Is = 23
                    SendKeys.Send("{F9}")
                Case Is = 24
                    SendKeys.Send("Y")

            End Select

            dlgContext.Dispose()
            dlgContext.AllocResources()

        End Sub

        Private Sub GuiLayoutLoading()

            _ProgramList.Visible = False

            Dim _ProgressBarThread As New Threading.Thread(AddressOf ShowLeftProgressBar)
            _ProgressBarThread.Start()

            CSGuideHelper.SetProperty("#CurrentPageLabel", "Lade")

            ' set to something - the skin only checks whether filled or not
            If CSGuideSettings.HiddenMenuMode Then
                CSGuideHelper.SetProperty("#ShowHiddenMenu", "Show")
            Else
                CSGuideHelper.SetProperty("#ShowHiddenMenu", "")
            End If
        End Sub


#End Region



    End Class
End Namespace