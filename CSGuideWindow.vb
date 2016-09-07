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
        Implements IMDB.IProgress

#Region "Skin Controls"

        <SkinControlAttribute(9)> Protected _DataLoadingAnimation As GUIAnimation = Nothing
        <SkinControlAttribute(11)> Protected _PageProgress As GUIProgressControl = Nothing
        <SkinControlAttribute(37)> Protected _niceEPGList As GUIListControl = Nothing
        <SkinControlAttribute(91)> Protected _buttonControlView1 As GUIButtonControl = Nothing
        <SkinControlAttribute(92)> Protected _buttonControlView2 As GUIButtonControl = Nothing
        <SkinControlAttribute(93)> Protected _buttonControlView3 As GUIButtonControl = Nothing
        <SkinControlAttribute(94)> Protected _buttonControlView4 As GUIButtonControl = Nothing
        <SkinControlAttribute(95)> Protected _buttonControlView5 As GUIButtonControl = Nothing
        <SkinControlAttribute(96)> Protected _buttonControlView6 As GUIButtonControl = Nothing
        <SkinControlAttribute(97)> Protected _buttonControlView7 As GUIButtonControl = Nothing
        <SkinControlAttribute(98)> Protected _buttonControlView8 As GUIButtonControl = Nothing
        <SkinControlAttribute(100)> Protected _buttonControlView0 As GUIButtonControl = Nothing
        <SkinControlAttribute(60)> Protected FanartBackground As GUIImage = Nothing
        <SkinControlAttribute(61)> Protected FanartBackground2 As GUIImage = Nothing


#End Region


#Region "Members"

        Friend Shared _ItemsCache As New List(Of TVMovieProgram)
        Private Shared _TMDbCache As New Dictionary(Of String, CSGuideTMDBCacheItem3)
        Private _cacheHander As CSGUideCacheHandler
        Friend Shared _CurrentCounter As Integer = 0
        Private _ThreadLoadItemsFromDatabase As Threading.Thread
        Private _ThreadNiceEPGList As Threading.Thread
        Private _ThreadBuildTMDb As Threading.Thread

        Private _LastFocusedIndex As Integer = 0
        Private _LastFocusedControlID As Integer
        Private Shared _SelectedNiceEPGItemId As Integer

        Private Shared m_SqlString As String = String.Empty  'this is the SQL-String used to load the items from the DB        
        Private Shared m_SqlStringSingleChannel As String = String.Empty
        Private Shared m_SqlStringMoviesOnly As String = String.Empty
        Private Shared m_SqlStringStarRating As String = String.Empty
        Private Shared m_SqlStringTVMovieBewertung As String = String.Empty
        Private Shared m_SqlSubStringMoviesEndTime As String = String.Empty
        Private Shared m_StartTime As Date = Nothing
        Private Shared m_StartTimePreviousItem As Date = Nothing
        Private Shared m_EndTime As Date = Nothing
        Private Shared m_MinRuntime As Integer = 0
        Private Shared m_TvGroupFilter As String = String.Empty
        'Private Shared m_RemoveLocalMovies As Boolean
        'Private Shared m_RemoveLocalSeries As Boolean
        Private Shared m_channelNumber As Integer = 1
        Private Shared m_viewDisplayName As String = String.Empty
        Private Shared m_viewType As String = String.Empty
        'Private Shared m_CategorieView As CategorieView = CategorieView.none

        Private Shadows m_chGroupChannelCache As New Dictionary(Of String, IList(Of Integer))
        Private Shared m_idProgram As Integer = 0
        Private Shared m_IntervalHour As Integer = 29 '29 meint bis 05:00 Uhr morgens den nächsten Tag
        Private Shared m_actualViewNumber As Integer
        Private Shared m_previousViewNumber As Integer
        Private Shared m_HiddenMenuOpen As Boolean = False
        Private Shared m_TMDbIsPossible As Boolean = False
        Private Shared m_UseTMDb As Boolean = False
        Private Shared m_tmdbClient As TMDbClient

        Private Shared m_backdrop As ImageSwapper


#End Region

#Region "Constructors"

        Public Sub New()
        End Sub

        Friend Shared Sub SetViewProperties(ByVal i As Integer)

            m_SqlString = CSGuideSettings.View(i).SQL
            m_viewType = CSGuideSettings.View(i).Type
            ' Noch korrigieren siehe CSGuideView Function setViewDate
            m_StartTime = CSGuideSettings.View(i).StartTime.AddMinutes(CInt(CSGuideSettings.View(i).OffSetMinute))
            ' Bei der View 0 soll die ursprüngliche TV Group bei behalten werden            
            If i <> 0 Then
                m_TvGroupFilter = CSGuideSettings.View(i).TvGroup
            End If
            m_viewDisplayName = CSGuideSettings.View(i).DisplayName
            m_previousViewNumber = m_actualViewNumber
            m_actualViewNumber = i
            m_UseTMDb = CSGuideSettings.View(i).UseTMDb

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

            Return Load(GUIGraphicsContext.Skin + "\ClickfinderSimpleGuide.xml")
        End Function

        Public Overrides Function GetModuleName() As String
            Return "Clickfinder Simple Guide"
        End Function

        Public Shared Property ActualViewNumber() As Integer
            Get
                Return m_actualViewNumber
            End Get
            Set(ByVal value As Integer)
                m_actualViewNumber = value
            End Set
        End Property
#End Region

#Region "GUI Events"

        Protected Overrides Sub OnPageLoad()
            Dim _mName As String = System.Reflection.MethodInfo.GetCurrentMethod.Name
            Dim _mClass As String = Me.GetType.Name

            Try
                If (Not String.IsNullOrEmpty(CSGuideSettings.TMDbAPIKey)) Then
                    m_tmdbClient = New TMDbClient(CSGuideSettings.TMDbAPIKey)
                    FetchConfig(m_tmdbClient)
                    m_TMDbIsPossible = True
                    _cacheHander = New CSGUideCacheHandler(m_tmdbClient)
                Else
                    MyLog.Info(String.Format("[{0}] [{1}]: No TMDb Info will be fetched", _mClass, _mName))
                End If

            Catch ex As Exception
                MyLog.Error(String.Format("[{0}] [{1}]: Problems with the TMDb-API-Key exception err: {2} stack: {3}", _mClass, _mName, ex.Message, ex.StackTrace))
                MyLog.Error(String.Format("[{0}] [{1}]: No TMDb Info will be fetched", _mClass, _mName))
                m_TMDbIsPossible = False
            End Try
            ' needed for the first call of this method
            m_UseTMDb = m_UseTMDb And m_TMDbIsPossible

            m_backdrop = New ImageSwapper
            m_backdrop.PropertyOne = "#Fanart.1"
            m_backdrop.PropertyTwo = "#Fanart.2"

            m_backdrop.GUIImageOne = FanartBackground
            m_backdrop.GUIImageTwo = FanartBackground2

            _niceEPGList.Focus = True

            'SetViewProperties(m_actualViewNumber)
            InitButtons()
            MyBase.OnPageLoad()

            GUIWindowManager.NeedRefresh()
            MyLog.Info(String.Format("[{0}] [{1}]: --- Start ---", _mClass, _mName))

            GuiLayoutLoading()
            '_LastFocusedControlID = _niceEPGList.GetID
            _ItemsCache.Clear()
            AbortRunningThreads()

            _ThreadLoadItemsFromDatabase = New Threading.Thread(AddressOf LoadItemsFromDatabase)
            _ThreadLoadItemsFromDatabase.IsBackground = True
            _ThreadLoadItemsFromDatabase.Start()

            If m_chGroupChannelCache.Count = 0 Then
                FillChGroupChannelCache()
            End If
        End Sub

        Protected Overrides Sub OnPageDestroy(ByVal new_windowId As Integer)
            Dim _mName As String = System.Reflection.MethodInfo.GetCurrentMethod.Name
            Dim _mClass As String = Me.GetType.Name

            MyLog.Debug(String.Format("[{0}] [{1}]: Destroying ... New WindowID {2}", _mClass, _mName, new_windowId))
            RememberLastFocusedItem()
            AbortRunningThreads()
            MyBase.OnPageDestroy(new_windowId)
            Dispose()
            AllocResources()
        End Sub

        Public Overrides Sub OnAction(ByVal action As MediaPortal.GUI.Library.Action)
            Dim _mName As String = System.Reflection.MethodInfo.GetCurrentMethod.Name
            Dim _mClass As String = Me.GetType.Name

            If GUIWindowManager.ActiveWindow = GetID Then

                'Move up                
                If action.wID = MediaPortal.GUI.Library.Action.ActionType.ACTION_MOVE_UP Then
                    If _niceEPGList.SelectedListItem.ItemId = _niceEPGList.Item(0).ItemId Then
                        _SelectedNiceEPGItemId = _niceEPGList.Item(_niceEPGList.Count - 1).ItemId
                        _PageProgress.Percentage = 100
                    Else
                        _SelectedNiceEPGItemId = _niceEPGList.Item(_niceEPGList.SelectedListItemIndex - 1).ItemId
                        _PageProgress.Percentage = 100 * ((_niceEPGList.SelectedListItemIndex - 1) / _niceEPGList.Count)
                    End If
                    RememberLastFocusedItem()

                    Try
                        If _niceEPGList.IsFocused = True Then
                            NiceEPGSetGUIProperties(TVMovieProgram.Retrieve(_SelectedNiceEPGItemId))
                            m_StartTimePreviousItem = TVMovieProgram.Retrieve(_SelectedNiceEPGItemId).ReferencedProgram.StartTime
                        End If

                    Catch ex As Exception
                        MyLog.Error(String.Format("[{0}] [{1}]: Move up : exception err: {2} stack: {3}", _mClass, _mName, ex.Message, ex.StackTrace))
                    End Try


                End If

                'Page up                
                If action.wID = MediaPortal.GUI.Library.Action.ActionType.ACTION_PAGE_UP Then
                    If _niceEPGList.SelectedListItemIndex - 8 < 0 Then
                        _SelectedNiceEPGItemId = _niceEPGList.Item(0).ItemId
                        _PageProgress.Percentage = 0
                    Else
                        _SelectedNiceEPGItemId = _niceEPGList.Item(_niceEPGList.SelectedListItemIndex - 8).ItemId
                        _PageProgress.Percentage = 100 * ((_niceEPGList.SelectedListItemIndex - 8) / _niceEPGList.Count)
                    End If

                    RememberLastFocusedItem()

                    Try
                        If _niceEPGList.IsFocused = True Then
                            NiceEPGSetGUIProperties(TVMovieProgram.Retrieve(_SelectedNiceEPGItemId))
                            m_StartTimePreviousItem = TVMovieProgram.Retrieve(_SelectedNiceEPGItemId).ReferencedProgram.StartTime
                        End If

                    Catch ex As Exception
                        MyLog.Error(String.Format("[{0}] [{1}]: Page up - Err: {2} stack: {3}", _mClass, _mName, ex.Message, ex.StackTrace))
                    End Try
                End If

                'Move down                
                If action.wID = MediaPortal.GUI.Library.Action.ActionType.ACTION_MOVE_DOWN Then
                    If _niceEPGList.SelectedListItem.ItemId = _niceEPGList.Item(_niceEPGList.Count - 1).ItemId Then
                        _PageProgress.Percentage = 0
                        _SelectedNiceEPGItemId = _niceEPGList.Item(0).ItemId
                    Else
                        _SelectedNiceEPGItemId = _niceEPGList.Item(_niceEPGList.SelectedListItemIndex + 1).ItemId
                        _PageProgress.Percentage = 100 * ((_niceEPGList.SelectedListItemIndex + 1) / (_niceEPGList.Count - 1))
                    End If

                    RememberLastFocusedItem()

                    Try
                        If _niceEPGList.IsFocused = True Then
                            NiceEPGSetGUIProperties(TVMovieProgram.Retrieve(_SelectedNiceEPGItemId))
                            m_StartTimePreviousItem = TVMovieProgram.Retrieve(_SelectedNiceEPGItemId).ReferencedProgram.StartTime
                        End If
                    Catch ex As Exception
                        MyLog.Error(String.Format("[{0}] [{1}]: Move down - Err: {2} stack: {3}", _mClass, _mName, ex.Message, ex.StackTrace))

                    End Try
                End If

                'Page down                
                If action.wID = MediaPortal.GUI.Library.Action.ActionType.ACTION_PAGE_DOWN Then
                    If _niceEPGList.SelectedListItemIndex + 8 > _niceEPGList.Count - 1 Then
                        _SelectedNiceEPGItemId = _niceEPGList.Item(_niceEPGList.Count - 1).ItemId
                        _PageProgress.Percentage = 100
                    Else
                        _SelectedNiceEPGItemId = _niceEPGList.Item(_niceEPGList.SelectedListItemIndex + 8).ItemId
                        _PageProgress.Percentage = 100 * ((_niceEPGList.SelectedListItemIndex + 8) / _niceEPGList.Count)
                    End If

                    RememberLastFocusedItem()

                    Try
                        If _niceEPGList.IsFocused = True Then
                            NiceEPGSetGUIProperties(TVMovieProgram.Retrieve(_SelectedNiceEPGItemId))
                            m_StartTimePreviousItem = TVMovieProgram.Retrieve(_SelectedNiceEPGItemId).ReferencedProgram.StartTime
                        End If

                    Catch ex As Exception
                        MyLog.Error(String.Format("[{0}] [{1}]: Page down - Err: {2} stack: {3}", _mClass, _mName, ex.Message, ex.StackTrace))
                    End Try
                End If

                'Next Item (F8) -> lade den nächsten Tag
                If action.wID = MediaPortal.GUI.Library.Action.ActionType.ACTION_NEXT_ITEM Then
                    m_previousViewNumber = m_actualViewNumber
                    ' wenn die previous View Single ist, ist schon alles geladen
                    ' das heißt es genügt, wenn der korrekte Item angezeigt wird
                    If CSGuideSettings.View(m_previousViewNumber).Type.Equals("Single") Then
                        m_StartTimePreviousItem = m_StartTimePreviousItem.AddDays(1)
                        SetCorrectListItemIndex()
                        NiceEPGSetGUIProperties(TVMovieProgram.Retrieve(_niceEPGList.SelectedListItem.ItemId))
                    Else
                        m_StartTime = m_StartTime.AddDays(1)

                        RememberLastFocusedItem()

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
                    m_previousViewNumber = m_actualViewNumber
                    If CSGuideSettings.View(m_previousViewNumber).Type.Equals("Single") Then
                        m_StartTimePreviousItem = m_StartTimePreviousItem.AddDays(-1)
                        SetCorrectListItemIndex()
                        NiceEPGSetGUIProperties(TVMovieProgram.Retrieve(_niceEPGList.SelectedListItem.ItemId))
                    Else
                        m_StartTime = m_StartTime.AddDays(-1)
                        RememberLastFocusedItem()

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
                    m_previousViewNumber = m_actualViewNumber
                    If CSGuideSettings.View(m_previousViewNumber).Type.Equals("Single") Then
                        m_StartTimePreviousItem = m_StartTimePreviousItem.AddMinutes(60)
                        SetCorrectListItemIndex()
                        NiceEPGSetGUIProperties(TVMovieProgram.Retrieve(_niceEPGList.SelectedListItem.ItemId))
                    Else
                        m_StartTime = m_StartTime.AddMinutes(60)
                        RememberLastFocusedItem()

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
                    m_previousViewNumber = m_actualViewNumber
                    If CSGuideSettings.View(m_previousViewNumber).Type.Equals("Single") Then
                        m_StartTimePreviousItem = m_StartTimePreviousItem.AddMinutes(-60)
                        SetCorrectListItemIndex()
                        NiceEPGSetGUIProperties(TVMovieProgram.Retrieve(_niceEPGList.SelectedListItem.ItemId))
                    Else

                        m_StartTime = m_StartTime.AddMinutes(-60)
                        RememberLastFocusedItem()

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
                            'Get the channel number of the program selected
                            _SelectedNiceEPGItemId = _niceEPGList.Item(_niceEPGList.SelectedListItemIndex).ItemId
                            Try
                                If _niceEPGList.IsFocused = True Then
                                    RememberLastFocusedItem()
                                    OnGetIMDBInfo(TVMovieProgram.Retrieve(_SelectedNiceEPGItemId))
                                End If
                            Catch ex As Exception
                                MyLog.Error("[Move (9) is pressed]: exception err: {0} stack: {1}", ex.Message, ex.StackTrace)
                            End Try
                            Return

                        End If
                    End If
                End If

                '(0) is pressed -> "Single Channel view"
                If action.wID = MediaPortal.GUI.Library.Action.ActionType.ACTION_KEY_PRESSED Then
                    If action.m_key IsNot Nothing Then
                        If action.m_key.KeyChar = 48 Then
                            SetViewProperties(0)

                            'Get the channel number of the program selected
                            _SelectedNiceEPGItemId = _niceEPGList.Item(_niceEPGList.SelectedListItemIndex).ItemId

                            Try
                                '   If _niceEPGList.IsFocused = True Then
                                m_channelNumber = Program.Retrieve(_SelectedNiceEPGItemId).ReferencedChannel.ChannelNumber
                                m_idProgram = Program.Retrieve(_SelectedNiceEPGItemId).IdProgram
                                '  End If
                            Catch ex As Exception
                                MyLog.Error(String.Format("[{0}] [{1}]: Exception err: {2} stack: {3}", _mClass, _mName, ex.Message, ex.StackTrace))
                            End Try

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
                    If m_HiddenMenuOpen Then
                        m_HiddenMenuOpen = False
                    Else
                        m_previousViewNumber = m_actualViewNumber
                        If m_viewType.Equals("Overview") Then
                            m_TvGroupFilter = GetNextChannelGroup(m_TvGroupFilter, 1)

                            _ItemsCache.Clear()
                            AbortRunningThreads()

                            _ThreadLoadItemsFromDatabase = New Thread(AddressOf LoadItemsFromDatabase)
                            _ThreadLoadItemsFromDatabase.IsBackground = True
                            _ThreadLoadItemsFromDatabase.Start()
                            Return
                        Else
                            m_channelNumber = GetNextChannelNumber(1)

                            Try
                                If _niceEPGList.IsFocused = True Then
                                    NiceEPGSetGUIProperties(TVMovieProgram.Retrieve(_SelectedNiceEPGItemId))
                                    m_StartTimePreviousItem = TVMovieProgram.Retrieve(_SelectedNiceEPGItemId).ReferencedProgram.StartTime
                                End If

                            Catch ex As Exception
                                MyLog.Error(String.Format("[{0}] [{1}]: Move right - Err: {2} stack: {3}", _mClass, _mName, ex.Message, ex.StackTrace))
                            End Try

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
                    m_previousViewNumber = m_actualViewNumber
                    If m_viewType.Equals("Overview") Then
                        'if "All Channels" AND HiddenMenu Mode then open hidden Menu
                        If Not (m_TvGroupFilter.Equals("All Channels") And CSGuideSettings.HiddenMenuMode) Then
                            m_TvGroupFilter = GetNextChannelGroup(m_TvGroupFilter, 0)

                            _ItemsCache.Clear()
                            AbortRunningThreads()

                            _ThreadLoadItemsFromDatabase = New Thread(AddressOf LoadItemsFromDatabase)
                            _ThreadLoadItemsFromDatabase.IsBackground = True
                            _ThreadLoadItemsFromDatabase.Start()
                            Return
                        Else
                            m_HiddenMenuOpen = True
                        End If
                    Else
                        'if "Channel Number = 1 AND HiddenMenu Mode the open hidden Menu
                        If Not (m_channelNumber = 1 And CSGuideSettings.HiddenMenuMode) Then
                            m_channelNumber = GetNextChannelNumber(0)
                            Try
                                If _niceEPGList.IsFocused = True Then
                                    NiceEPGSetGUIProperties(TVMovieProgram.Retrieve(_SelectedNiceEPGItemId))
                                    m_StartTimePreviousItem = TVMovieProgram.Retrieve(_SelectedNiceEPGItemId).ReferencedProgram.StartTime
                                End If

                            Catch ex As Exception
                                MyLog.Error(String.Format("[{0}] [{1}]: Move left - Err: {2} stack: {3}", _mClass, _mName, ex.Message, ex.StackTrace))
                            End Try

                            _ItemsCache.Clear()
                            AbortRunningThreads()

                            _ThreadLoadItemsFromDatabase = New Thread(AddressOf LoadItemsFromDatabase)
                            _ThreadLoadItemsFromDatabase.IsBackground = True
                            _ThreadLoadItemsFromDatabase.Start()
                            Return
                        Else
                            m_HiddenMenuOpen = True
                        End If
                    End If
                End If

                'Play Button (P) -> Start channel
                If action.wID = MediaPortal.GUI.Library.Action.ActionType.ACTION_MUSIC_PLAY Then
                    Try
                        If _niceEPGList.IsFocused = True Then CSGuideHelper.StartTv(Program.Retrieve(_niceEPGList.SelectedListItem.ItemId))
                    Catch ex As Exception
                        MyLog.Error(String.Format("[{0}] [{1}]: Play Button - Err: {2} stack: {3}", _mClass, _mName, ex.Message, ex.StackTrace))

                    End Try
                End If

                'Record Button (R) -> MP TvProgramInfo aufrufen --Über keychar--
                If action.wID = MediaPortal.GUI.Library.Action.ActionType.ACTION_KEY_PRESSED Then
                    If action.m_key IsNot Nothing Then
                        If action.m_key.KeyChar = 114 Then
                            If _niceEPGList.IsFocused = True Then CSGuideHelper.LoadTVProgramInfo(Program.Retrieve(_niceEPGList.SelectedListItem.ItemId))
                        End If
                    End If
                End If

                'Record Button (R) -> MP TvProgramInfo aufrufen
                If action.wID = MediaPortal.GUI.Library.Action.ActionType.ACTION_RECORD Then
                    If _niceEPGList.IsFocused = True Then CSGuideHelper.LoadTVProgramInfo(Program.Retrieve(_niceEPGList.SelectedListItem.ItemId))
                End If

                'Menu Button (F9) -> Context Menu open
                If action.wID = MediaPortal.GUI.Library.Action.ActionType.ACTION_CONTEXT_MENU Then
                    RememberLastFocusedItem()
                    If _niceEPGList.IsFocused = True Then ShowItemsContextMenu(_niceEPGList.SelectedListItem.ItemId)
                End If

                'OSD Info Button (Y) -> Context Menu open (gleiche Fkt. wie Menu Button)
                If action.wID = MediaPortal.GUI.Library.Action.ActionType.ACTION_KEY_PRESSED Then
                    If action.m_key IsNot Nothing Then
                        If action.m_key.KeyChar = 121 Then
                            RememberLastFocusedItem()
                            If _niceEPGList.IsFocused = True Then ShowItemsContextMenu(_niceEPGList.SelectedListItem.ItemId)
                        End If
                    End If
                End If
                '''' TEST '''''
                'Menu Button (q) -> Context Menu open
                If action.wID = MediaPortal.GUI.Library.Action.ActionType.ACTION_KEY_PRESSED Then
                    If action.m_key IsNot Nothing Then
                        If action.m_key.KeyChar = 113 Then
                            m_idProgram = TVMovieProgram.Retrieve(_SelectedNiceEPGItemId).idProgram
                            If Not _TMDbCache.ContainsKey(m_idProgram) Then
                                _cacheHander.AddItem(TVMovieProgram.Retrieve(_SelectedNiceEPGItemId), _TMDbCache)
                                ' to change ...
                                Utils.DownLoadAndCacheImage(_TMDbCache.Item(m_idProgram).Misc.fanartURL _
                                                            , _TMDbCache.Item(m_idProgram).Misc.absFanartPath)
                            End If
                            CSGuideWindowsTMDb.MovieInfo = _TMDbCache.Item(m_idProgram)
                                CSGuideWindowsTMDb.TmdbClient = m_tmdbClient
                                RememberLastFocusedItem()
                                GUIWindowManager.ActivateWindow(730352, False)
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

            If control Is _niceEPGList Then
                If actionType = MediaPortal.GUI.Library.Action.ActionType.ACTION_SELECT_ITEM Then
                    Action_SelectItem()
                End If
            End If
            If control Is _buttonControlView1 Then
                SendKeys.Send("1")
                m_HiddenMenuOpen = False
            End If
            If control Is _buttonControlView2 Then
                SendKeys.Send("2")
                m_HiddenMenuOpen = False
            End If
            If control Is _buttonControlView3 Then
                SendKeys.Send("3")
                m_HiddenMenuOpen = False
            End If
            If control Is _buttonControlView4 Then
                SendKeys.Send("4")
                m_HiddenMenuOpen = False
            End If
            If control Is _buttonControlView5 Then
                SendKeys.Send("5")
                m_HiddenMenuOpen = False
            End If
            If control Is _buttonControlView6 Then
                SendKeys.Send("6")
                m_HiddenMenuOpen = False
            End If
            If control Is _buttonControlView7 Then
                SendKeys.Send("7")
                m_HiddenMenuOpen = False
            End If
            If control Is _buttonControlView8 Then
                SendKeys.Send("8")
                m_HiddenMenuOpen = False
            End If
            If control Is _buttonControlView0 Then
                SendKeys.Send("0")
                m_HiddenMenuOpen = False
            End If

        End Sub

        Private Sub Action_SelectItem()
            RememberLastFocusedItem()
            If _niceEPGList.IsFocused = True Then CSGuideHelper.StartTv(Program.Retrieve(_niceEPGList.SelectedListItem.ItemId))
        End Sub

        Protected Overrides Sub OnPreviousWindow()
            MyLog.Debug("[CSGuideWindow] [OnPreviousWindow]")
            MyBase.OnPreviousWindow()
            _LastFocusedIndex = 0
            _LastFocusedControlID = _niceEPGList.GetID
        End Sub
#End Region

#Region "Functions"

        Friend Shared Function GetSqlGroupFilterString(ByVal TvGroupFilter As String) As String
            '#CPGFilter vorbereiten
            Dim _groupFilter As New StringBuilder(
                "(SELECT groupmap.idgroup FROM mptvdb.groupmap Inner " &
                "join mptvdb.channelgroup ON groupmap.idgroup = channelgroup.idGroup " &
                "WHERE groupmap.idchannel = program.idchannel And channelgroup.groupName = '#FilterTvGroup')")
                                'TvGroup Filter setzen
                                _groupFilter.Replace("#FilterTvGroup", TvGroupFilter)

            Return _groupFilter.ToString

        End Function


        Private Sub NiceEPGSetGUIProperties(ByVal myTVMovieProgram As TVMovieProgram)
            CSGuideHelper.LoadFanart(m_backdrop, getFanartFileNameAbsPath(myTVMovieProgram))
            CSGuideHelper.SetProperty("#SettingLastUpdate", CSGuideSettings.TvMovieImportStatus)
            CSGuideHelper.SetProperty("#ChannelGroup", m_TvGroupFilter)
            'CSGuideHelper.CSGuideHelper.SetProperty("#TMDbFanArt", getFanartFileName(myTVMovieProgram))

            If m_viewType.Equals("Overview") Then
                'CSGuideHelper.SetProperty("#ItemsRightListLabel",
                '                            m_StartTime.ToString("dddd, dd.MM") & " ~" & m_StartTime.ToString("HH:mm"))
                CSGuideHelper.SetProperty("#ItemsRightListLabel", "")
                CSGuideHelper.SetProperty("#EPGView", m_viewDisplayName)

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
                If m_viewType.Equals("Single") Then
                    'CSGuideHelper.SetProperty("#ItemsRightListLabel",
                    '    myTVMovieProgram.ReferencedProgram.StartTime.ToString("dddd, dd.MM") &
                    '    " ~" &
                    'myTVMovieProgram.ReferencedProgram.StartTime.ToString("HH:mm") & " Uhr")
                    CSGuideHelper.SetProperty("#ItemsRightListLabel", "")

                    CSGuideHelper.SetProperty("#EPGView", m_viewDisplayName)
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
            Dim _pattern As String = "(.*?Wertung:.*?\n)[\w=\d\n]*(.*)"
            Dim _rgx As New System.Text.RegularExpressions.Regex(_pattern, RegexOptions.Singleline)
            Dim _returnString As String = _rgx.Replace(programDescription, "$1***************\n$2")
            Return _returnString
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
            Dim _groupMapList As IList(Of GroupMap)
            Dim _channelGroupList As New List(Of Integer)

            _groupMapList = GroupMap.ListAll

            ' First fill the list of distinct channel groups             
            For Each _channelGroup In ChannelGroup.ListAll
                _channelGroupList.Add(_channelGroup.IdGroup)
            Next

            ' For all channelGroup create a list or corresponding channels
            For Each _channelGroupId In _channelGroupList
                Dim _channelList As New List(Of Integer)
                For Each _groupMap In _groupMapList
                    If _groupMap.IdGroup = _channelGroupId Then
                        _channelList.Add(Channel.Retrieve(_groupMap.IdChannel).ChannelNumber)
                    End If
                Next
                _channelList.Sort()
                ' The Add the list to a hashtable
                ' For example you receive
                ' m_chGroupChannelCache("AllChannels", [1,2,3,5,8])
                m_chGroupChannelCache.Add(ChannelGroup.Retrieve(_channelGroupId).GroupName, _channelList)
            Next
        End Sub
        Private Function GetNextChannelNumber(ByVal order As Integer) As Integer
            ' order = 1 forward, order = 0 backward
            Dim _channelNumberList = m_chGroupChannelCache(m_TvGroupFilter)
            Select Case order
                Case 1
                    For i = 0 To _channelNumberList.Count - 1
                        If _channelNumberList(i) = m_channelNumber Then
                            If i + 1 = _channelNumberList.Count Then
                                Return _channelNumberList.First
                            Else
                                Return _channelNumberList(i + 1)
                            End If
                        End If
                    Next
                Case 0
                    For i = 0 To _channelNumberList.Count - 1
                        If _channelNumberList(i) = m_channelNumber Then
                            If i - 1 < 0 Then
                                Return _channelNumberList.Last
                            Else
                                Return _channelNumberList(i - 1)
                            End If
                        End If
                    Next
            End Select
            Return _channelNumberList.First
        End Function
        Private Function GetNextChannelGroup(ByVal currentChannelGroup As String, order As Integer) As String
            ' order = 1 forward, order = 0 backward
            Dim _channelGroupList = m_chGroupChannelCache.Keys
            Dim _currentGroupIndex As Integer

            For _currentGroupIndex = 0 To _channelGroupList.Count - 1
                If _channelGroupList(_currentGroupIndex).Equals(m_TvGroupFilter) Then
                    Exit For
                End If
            Next

            Select Case order
                Case 1
                    If (_currentGroupIndex = _channelGroupList.Count - 1) Then
                        Return _channelGroupList.First
                    Else
                        Return _channelGroupList(_currentGroupIndex + 1)
                    End If
                Case 0
                    If (_currentGroupIndex = 0) Then
                        Return _channelGroupList.Last
                    Else
                        Return _channelGroupList(_currentGroupIndex - 1)
                    End If
            End Select
            Return _channelGroupList.First
        End Function

        Private Sub LoadItemsFromDatabase()
            Dim _mName As String = System.Reflection.MethodInfo.GetCurrentMethod.Name
            Dim _mClass As String = Me.GetType.Name
            Try
                Dim _timer As Date = Date.Now
                Dim _totaltimer As Date = Date.Now

                GuiLayoutLoading()
                _ItemsCache.Clear()
                _CurrentCounter = 0

                'SQL String bauen
                Dim _SqlStringBuilder As New StringBuilder(m_SqlString)
                '_SqlStringBuilder.Replace("#EndTimeSQL", m_SqlSubStringMoviesEndTime)
                _SqlStringBuilder.Replace("#StartTime", CSGuideHelper.MySqlDate(m_StartTime))
                _SqlStringBuilder.Replace("#ChannelNumber", m_channelNumber)
                _SqlStringBuilder.Replace("#TVGroupFilter", GetSqlGroupFilterString(m_TvGroupFilter))
                '_SqlStringBuilder.Replace("#IntervalDay", m_IntervalHour)

                _SqlStringBuilder.Replace(" * ", " TVMovieProgram.idProgram, TVMovieProgram.Action, TVMovieProgram.Actors, TVMovieProgram.BildDateiname, TVMovieProgram.Country, TVMovieProgram.Cover, TVMovieProgram.Describtion, TVMovieProgram.Dolby, TVMovieProgram.EpisodeImage, TVMovieProgram.Erotic, TVMovieProgram.FanArt, TVMovieProgram.Feelings, TVMovieProgram.FileName, TVMovieProgram.Fun, TVMovieProgram.HDTV, TVMovieProgram.idEpisode, TVMovieProgram.idMovingPictures, TVMovieProgram.idSeries, TVMovieProgram.idVideo, TVMovieProgram.KurzKritik, TVMovieProgram.local, TVMovieProgram.Regie, TVMovieProgram.Requirement, TVMovieProgram.SeriesPosterImage, TVMovieProgram.ShortDescribtion, TVMovieProgram.Tension, TVMovieProgram.TVMovieBewertung ")
                MyLog.Debug(String.Format("[{0}] [{1}]: Executing: {2} ", _mClass, _mName, _SqlStringBuilder.ToString))
                Dim _SQLstate As SqlStatement = Broker.GetStatement(_SqlStringBuilder.ToString)
                Dim _ItemsOnLoad As List(Of TVMovieProgram) = ObjectFactory.GetCollection(GetType(TVMovieProgram), _SQLstate.Execute())
                MyLog.Info(String.Format("[{0}] [{1}]: Received {2} records in {3}sec", _mClass, _mName, _ItemsOnLoad.Count, (DateTime.Now - _timer).TotalSeconds))

                If _ItemsOnLoad.Count > 0 Then
                    _ItemsCache = _ItemsOnLoad
                    _ThreadNiceEPGList = New Thread(AddressOf FillniceEPGList)
                    _ThreadNiceEPGList.IsBackground = True
                    _ThreadNiceEPGList.Start()
                    '' here muss der Cache gefüllt werden
                    If m_UseTMDb Then
                        _ThreadBuildTMDb = New Thread(AddressOf UpdateTMDbCache)
                        _ThreadBuildTMDb.IsBackground = True
                        _ThreadBuildTMDb.Start()
                    End If

                Else
                    _DataLoadingAnimation.Visible = False
                    NiceEPGSetGUIProperties(Nothing)
                End If

            Catch ex As ThreadAbortException
                MyLog.Error(String.Format("[{0}] [{1}]: Thread aborted", _mClass, _mName))
            Catch ex As GentleException
                MyLog.Error(String.Format("[{0}] [{1}]: GentleException err: {2} stack: {3}", _mClass, _mName, ex.Message, ex.StackTrace))
            Catch ex As Exception
                MyLog.Error(String.Format("[{0}] [{1}]: Exception err: {2} stack: {3}", _mClass, _mName, ex.Message, ex.StackTrace))
            End Try

        End Sub
        Private Sub UpdateTMDbCache()
            Dim _mName As String = System.Reflection.MethodInfo.GetCurrentMethod.Name
            Dim _mClass As String = Me.GetType.Name
            Dim stopwatch As Diagnostics.Stopwatch = New Diagnostics.Stopwatch
            stopwatch.Start()
            MyLog.Debug(String.Format("[{0}] [{1}]: Updating TMDb-Cache for View {2} Start", _mClass, _mName, m_actualViewNumber))
            _cacheHander.buildCache(_TMDbCache, _ItemsCache, CDate("03.09.2016"))
            stopwatch.Stop()
            MyLog.Debug(String.Format("[{0}] [{1}]: Updated TMDb-Cache ({2}) Items in {3} ms", _mClass, _mName _
                                      , _TMDbCache.Count, stopwatch.ElapsedMilliseconds))
        End Sub
        Private Sub FillniceEPGList()
            Dim _mName As String = System.Reflection.MethodInfo.GetCurrentMethod.Name
            Dim _mClass As String = Me.GetType.Name

            Dim _timer As Date = Date.Now
            Dim _ItemCounter As Integer = 0
            Dim _getYearString As String = ""

            _niceEPGList.Visible = False
            _niceEPGList.AllocResources() 'was macht das?
            _niceEPGList.Clear()

            Try

                For i = 0 To _ItemsCache.Count - 1

                    Try
                        _ItemCounter = _ItemCounter + 1
                        Dim _TvMovieProgram As TVMovieProgram = _ItemsCache.Item(i)

                        _getYearString = CSGuideHelper.GetYearString(_TvMovieProgram)

                        CSGuideHelper.AddListControlItem(_niceEPGList,
                                           _TvMovieProgram.idProgram,
                                           _TvMovieProgram.ReferencedProgram.ReferencedChannel.DisplayName,
                                           _TvMovieProgram.ReferencedProgram.Title & _getYearString, ,
                                           CSGuideHelper.TimeLabel(_TvMovieProgram),
                                           Config.GetFile(Config.Dir.Thumbs, "tv\logos\") & Replace(_TvMovieProgram.ReferencedProgram.ReferencedChannel.DisplayName, "/", "_") & ".png", ,
                                           CSGuideHelper.RecordingStatus(_TvMovieProgram.ReferencedProgram))

                    Catch ex As ThreadAbortException
                        MyLog.Error(String.Format("[{0}] [{1}]: Thread aborted", _mClass, _mName))
                    Catch ex As GentleException
                        MyLog.Error(String.Format("[{0}] [{1}]: GentleException err: {2} stack: {3}", _mClass, _mName, ex.Message, ex.StackTrace))
                    Catch ex As Exception
                        MyLog.Error(String.Format("[{0}] [{1}]: Exception err: {2} stack: {3}", _mClass, _mName, ex.Message, ex.StackTrace))
                    End Try
                Next

                _DataLoadingAnimation.Visible = False
                _niceEPGList.Visible = True

                MyLog.Info(String.Format("[{0}] [{1}]: Thread finished in {2}s", _mClass, _mName, (DateTime.Now - _timer).TotalSeconds))

                GUIListControl.SelectItemControl(GetID, _LastFocusedControlID, _LastFocusedIndex)
                GUIListControl.FocusControl(GetID, _LastFocusedControlID)

                _PageProgress.Percentage = 100 * ((_niceEPGList.SelectedListItemIndex) / _niceEPGList.Count)

                If _niceEPGList.SelectedListItem Is Nothing Then
                    _niceEPGList.SelectedItem = 0
                    MyLog.Debug(String.Format("[{0}] [{1}]: _niceEPGList.SelectedListItem is Nothing", _mClass, _mName))
                End If

                SetCorrectListItemIndex()
                NiceEPGSetGUIProperties(TVMovieProgram.Retrieve(_niceEPGList.SelectedListItem.ItemId))

                'GUIWindowManager.NeedRefresh()

            Catch ex As ThreadAbortException
                MyLog.Error(String.Format("[{0}] [{1}]: Thread aborted", _mClass, _mName))
            Catch ex As GentleException
                MyLog.Error(String.Format("[{0}] [{1}]: GentleException err: {2} stack: {3}", _mClass, _mName, ex.Message, ex.StackTrace))
            Catch ex As Exception
                MyLog.Error(String.Format("[{0}] [{1}]: Exception err: {2} stack: {3}", _mClass, _mName, ex.Message, ex.StackTrace))
            End Try
        End Sub

        'ProgresBar paralell anzeigen
        Private Sub ShowLeftProgressBar()
            _DataLoadingAnimation.Visible = True
        End Sub

        Private Sub AbortRunningThreads()
            Dim _mName As String = System.Reflection.MethodInfo.GetCurrentMethod.Name
            Dim _mClass As String = Me.GetType.Name

            Try
                If _ThreadLoadItemsFromDatabase.IsAlive = True Then
                    _ThreadLoadItemsFromDatabase.Abort()
                    MyLog.Debug(String.Format("[{0}] [{1}]: ThreadLoadItemsFromDatabase aborted", _mClass, _mName))
                End If
                If _ThreadNiceEPGList.IsAlive = True Then
                    _ThreadNiceEPGList.Abort()
                    MyLog.Debug(String.Format("[{0}] [{1}]: ThreadFillEPGList aborted", _mClass, _mName))
                End If
            Catch ex As Exception
                MyLog.Error(String.Format("[{0}] [{1}]: Exception err: {2} stack: {3}", _mClass, _mName, ex.Message, ex.StackTrace))
            End Try
        End Sub

        Private Sub SetCorrectListItemIndex()
            ' ToDo
            ' what it should do
            ' Improve Navigation experience 
            ' (1) In case one program is slected in the Overview and you press 0 the focus should be on the same program
            ' (2) In case you are in single channel view the program selected in the next single channel view should be on the same (or similar start date)


            ' First start searching the prgram id - this is possible if previous EPGView is not SingleChannel
            If (CSGuideSettings.View(m_previousViewNumber).Type <> "Single") Then
                If m_idProgram <> 0 Then
                    For i = 0 To _niceEPGList.Count - 1
                        If _niceEPGList.Item(i).ItemId = m_idProgram Then
                            _niceEPGList.SelectedListItemIndex = i
                            m_StartTimePreviousItem = TVMovieProgram.Retrieve(_niceEPGList.Item(i).ItemId).ReferencedProgram.StartTime
                            Exit Sub
                        End If
                    Next
                End If
                ' To Delete Start
                _niceEPGList.SelectedListItemIndex = 0
                ' To Delete End
            ElseIf (m_viewType.Equals("Single")) And (m_StartTimePreviousItem <> Nothing) Then
                Try
                    For i = 0 To _niceEPGList.Count - 1
                        Dim _TvMovieProgram As TVMovieProgram = TVMovieProgram.Retrieve(_niceEPGList.Item(i).ItemId)
                        If _TvMovieProgram.ReferencedProgram.StartTime >= m_StartTimePreviousItem Then
                            If _TvMovieProgram.ReferencedProgram.StartTime = m_StartTimePreviousItem Then
                                _niceEPGList.SelectedListItemIndex = i
                            Else
                                _niceEPGList.SelectedListItemIndex = i - 1
                            End If
                            m_StartTimePreviousItem = TVMovieProgram.Retrieve(_niceEPGList.Item(_niceEPGList.SelectedListItemIndex).ItemId).ReferencedProgram.StartTime
                            Exit Sub
                        End If
                    Next
                Catch ex As Exception
                End Try
            Else
                _niceEPGList.SelectedListItemIndex = 0
            End If

        End Sub
        Private Sub RememberLastFocusedItem()

            'If _niceEPGList.IsFocused Then
            '    _LastFocusedIndex = _niceEPGList.SelectedListItemIndex
            '    _LastFocusedControlID = _niceEPGList.GetID
            'Else
            '    _LastFocusedIndex = 0
            '    _LastFocusedControlID = _niceEPGList.GetID
            'End If

        End Sub
        Private Function getFanartFileNameAbsPath(ByVal myTVMovieProgram As TVMovieProgram) As String
            If _TMDbCache.ContainsKey(myTVMovieProgram.idProgram) Then
                Return getFanartFileName3(myTVMovieProgram)
            Else
                Return Path.Combine(Config.GetSubFolder(Config.Dir.Skin, Config.SkinName & "\media\hover_ClickfinderSimpleGuide.png"))
            End If
        End Function
        Private Function getFanartFileName3(ByVal myTVMovieProgram As TVMovieProgram) As String
            Dim _mName As String = System.Reflection.MethodInfo.GetCurrentMethod.Name
            Dim _mClass As String = Me.GetType.Name

            Dim programID As Integer = myTVMovieProgram.idProgram
            Dim backgroundImage As String = "hover_ClickfinderSimpleGuide.png"

            Try
                If _TMDbCache.ContainsKey(programID) Then
                    Dim absFanartPath As String = _TMDbCache.Item(programID).Misc.absFanartPath
                    Dim fanartURL As String = _TMDbCache.Item(programID).Misc.fanartURL

                    If (Not String.IsNullOrEmpty(absFanartPath)) And (Not String.IsNullOrEmpty(fanartURL)) Then
                        Utils.DownLoadAndCacheImage(fanartURL, absFanartPath)
                        Return absFanartPath
                    End If
                End If
            Catch ex As Exception
                MyLog.Error(String.Format("[{0}] [{1}]: Err: {2} stack: {3}", _mClass, _mName, ex.Message, ex.StackTrace))
            End Try
            Return Path.Combine(Config.GetSubFolder(Config.Dir.Skin, Config.SkinName & "\media\" & backgroundImage))
        End Function

        Private Function getFanartFileName2(ByVal myTVMovieProgram As TVMovieProgram) As String
            Dim _mName As String = System.Reflection.MethodInfo.GetCurrentMethod.Name
            Dim _mClass As String = Me.GetType.Name

            Dim programID As Integer = myTVMovieProgram.idProgram
            Dim backgroundImage As String = "hover_ClickfinderSimpleGuide.png"
            Try
                If _TMDbCache.ContainsKey(programID) Then
                    If Not IsNothing(_TMDbCache.Item(programID).movie) Then
                        If Not _TMDbCache.Item(programID).movie.BackdropPath Is Nothing Then
                            Dim _fanArtURL = m_tmdbClient.GetImageUrl("original", _TMDbCache.Item(programID).movie.BackdropPath).ToString()
                            Dim _rgx As New System.Text.RegularExpressions.Regex(".*\/(.*)$")
                            Dim _imageFilename As String = _rgx.Match(_fanArtURL).Groups(1).Value
                            If Not String.IsNullOrEmpty(_imageFilename) Then
                                Dim _absImagePath As String = Path.Combine(Config.GetSubFolder(Config.Dir.Skin, Config.SkinName & "\Media\CSG\Fanart\"), _imageFilename)
                                Dim _relImagePath As String = Path.Combine("CSG\Fanart\", _imageFilename)
                                Utils.DownLoadAndCacheImage(_fanArtURL, _absImagePath)
                                backgroundImage = _relImagePath
                            End If
                        End If
                    End If
                End If
            Catch ex As Exception
                MyLog.Error(String.Format("[{0}] [{1}]: Err: {2} stack: {3}", _mClass, _mName, ex.Message, ex.StackTrace))
            End Try
            Return backgroundImage
        End Function


        Private Shared Sub FetchConfig(client As TMDbClient)
            Dim configXml As New FileInfo("C:\ProgramData\Team MediaPortal\MediaPortal TV Server\enrichEPG\config.xml")

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
            _buttonControlView0.Label = "(0): " & CSGuideSettings.View(0).DisplayName
        End Sub
#End Region
#Region "MediaPortal Funktionen / Dialogs"
        Private Sub ShowItemsContextMenu(ByVal idProgram As Integer)
            MyLog.Info("")
            MyLog.Info("[NiceEPGGuiWindow] [ShowItemsContextMenu]: open")
            MyLog.Info("")
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
            'ID 23
            Dim line23 As New GUIListItem
            line23.Label = "(7): " & CSGuideSettings.View(7).DisplayName
            dlgContext.Add(line23)
            line23.Dispose()
            'ID 24
            Dim line24 As New GUIListItem
            line24.Label = "(8): " & CSGuideSettings.View(8).DisplayName
            dlgContext.Add(line24)
            line24.Dispose()
            'ID 7
            Dim line7 As New GUIListItem
            line7.Label = "(9): TMDB/IMDB Info"
            dlgContext.Add(line7)
            line7.Dispose()
            'ID 8
            Dim line20 As New GUIListItem
            line20.Label = "(0): " & CSGuideSettings.View(0).DisplayName
            dlgContext.Add(line20)
            line20.Dispose()
            'ID 9
            Dim line12 As New GUIListItem
            line12.Label = "Next Item (F8): Load next day"
            dlgContext.Add(line12)
            line12.Dispose()
            'ID 10
            Dim line13 As New GUIListItem
            line13.Label = "Previous Item (F7): Load day before"
            dlgContext.Add(line13)
            line13.Dispose()
            'ID 11
            Dim line14 As New GUIListItem
            line14.Label = "Forward (F6): Load one hour forward"
            dlgContext.Add(line14)
            line14.Dispose()
            'ID 12
            Dim line15 As New GUIListItem
            line15.Label = "Rewind (F5): Load one hour back"
            dlgContext.Add(line15)
            line15.Dispose()
            'ID 13
            Dim line8 As New GUIListItem
            line8.Label = "(Pfeil hoch): Navigiere hoch"
            dlgContext.Add(line8)
            line8.Dispose()
            'ID 14
            Dim line81 As New GUIListItem
            line81.Label = "(Pfeil runter): Navigiere runter"
            dlgContext.Add(line81)
            line81.Dispose()
            'ID 15
            Dim line9 As New GUIListItem
            line9.Label = "(Page up): Navigiere Page up"
            dlgContext.Add(line9)
            line9.Dispose()
            'ID 16
            Dim line91 As New GUIListItem
            line91.Label = "(Page down): Navigiere Page down"
            dlgContext.Add(line91)
            line91.Dispose()
            'ID 17
            Dim line10 As New GUIListItem
            Select Case CSGuideSettings.View(m_actualViewNumber).Type
                Case "Overview"
                    line10.Label = "(Pfeil rechts): Eine Kanalgruppe vorwärts"
                Case "Single"
                    line10.Label = "(Pfeil rechts): Ein Kanal vorwärts"
            End Select
            dlgContext.Add(line10)
            line10.Dispose()
            'ID 18
            Dim line11 As New GUIListItem
            Select Case CSGuideSettings.View(m_actualViewNumber).Type
                Case "Overview"
                    line11.Label = "(Pfeil links): Eine Kanalgruppe rückwärts"
                Case "Single"
                    line11.Label = "(Pfeil links): Ein Kanal rückwärts"
            End Select
            dlgContext.Add(line11)
            line11.Dispose()
            'ID 19
            Dim line16 As New GUIListItem
            line16.Label = "Play Button (P): Start channel"
            dlgContext.Add(line16)
            line16.Dispose()
            'ID 20
            Dim line17 As New GUIListItem
            line17.Label = "Record Button (R): TvProgramInfo"
            dlgContext.Add(line17)
            line17.Dispose()
            'ID 21
            Dim line18 As New GUIListItem
            line18.Label = "Menu Button (F9): Zeigt die Hilfe"
            dlgContext.Add(line18)
            line18.Dispose()
            'ID 22
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
                    SendKeys.Send("9")
                Case Is = 8
                    SendKeys.Send("0")
                Case Is = 9
                    SendKeys.Send("{F8}")
                Case Is = 10
                    SendKeys.Send("{F7}")
                Case Is = 11
                    SendKeys.Send("{F6}")
                Case Is = 12
                    SendKeys.Send("{F5}")
                Case Is = 13
                    SendKeys.Send("{UP}")
                Case Is = 14
                    SendKeys.Send("{DOWN}")
                Case Is = 15
                    SendKeys.Send("{PGUP}")
                Case Is = 16
                    SendKeys.Send("{PGDN}")
                Case Is = 17
                    SendKeys.Send("{RIGHT}")
                Case Is = 18
                    SendKeys.Send("{LEFT}")
                Case Is = 19
                    SendKeys.Send("P")
                Case Is = 20
                    SendKeys.Send("R")
                Case Is = 21
                    SendKeys.Send("{F9}")
                Case Is = 22
                    SendKeys.Send("Y")
                Case Is = 23
                    SendKeys.Send("7")
                Case Is = 24
                    SendKeys.Send("8")

            End Select

            dlgContext.Dispose()
            dlgContext.AllocResources()

        End Sub

        Private Sub GuiLayoutLoading()

            _niceEPGList.Visible = False

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

#Region "MovieInfo"

        Protected Function GetKeyboard(ByRef strLine As String) As Boolean
            Dim keyboard As VirtualKeyboard = DirectCast(GUIWindowManager.GetWindow(CInt(Window.WINDOW_VIRTUAL_KEYBOARD)), VirtualKeyboard)
            If keyboard Is Nothing Then
                Return False
            End If
            keyboard.Reset()
            keyboard.Text = strLine
            keyboard.DoModal(GetID)
            If keyboard.IsConfirmed Then
                strLine = keyboard.Text
                Return True
            End If
            Return False
        End Function

        Private Sub OnGetIMDBInfo(ByVal myTVMovieProgram As TVMovieProgram)
            Dim _movieDetails As IMDBMovie = New IMDBMovie()
            Dim _currentProgram As Program = myTVMovieProgram.ReferencedProgram
            _movieDetails.SearchString = myTVMovieProgram.ReferencedProgram.Title

            If IMDBFetcher.GetInfoFromIMDB(Me, _movieDetails, False, False) Then
                Dim dbLayer As New TvBusinessLayer()

                Dim progs As IList(Of Program) = dbLayer.GetProgramExists(Channel.Retrieve(_currentProgram.IdChannel), _currentProgram.StartTime, _currentProgram.EndTime)
                If progs IsNot Nothing AndAlso progs.Count > 0 Then
                    Dim prog As Program = DirectCast(progs(0), Program)
                    prog.Description = _movieDetails.Plot
                    ' prog.Genre = movieDetails.Genre;
                    prog.StarRating = CInt(_movieDetails.Rating)
                    prog.Persist()
                End If
                Dim videoInfo As GUIVideoInfo = DirectCast(GUIWindowManager.GetWindow(CInt(Window.WINDOW_VIDEO_INFO)), GUIVideoInfo)
                videoInfo.AllocResources()
                videoInfo.Movie = _movieDetails
                Dim btnPlay As GUIButtonControl = DirectCast(videoInfo.GetControl(2), GUIButtonControl)
                If btnPlay IsNot Nothing Then
                    btnPlay.Visible = False
                End If
                Dim btnCast As GUICheckButton = DirectCast(videoInfo.GetControl(4), GUICheckButton)
                If btnCast IsNot Nothing Then
                    btnCast.Visible = False
                End If
                Dim btnWatched As GUICheckButton = DirectCast(videoInfo.GetControl(6), GUICheckButton)
                If btnWatched IsNot Nothing Then
                    btnWatched.Visible = False
                End If
                GUIWindowManager.ActivateWindow(CInt(Window.WINDOW_VIDEO_INFO))
            Else
                Log.Info("IMDB Fetcher: Nothing found")
            End If
        End Sub

        Public Function OnDisableCancel(fetcher As IMDBFetcher) As Boolean Implements MediaPortal.Video.Database.IMDB.IProgress.OnDisableCancel
            Dim pDlgProgress As GUIDialogProgress = DirectCast(GUIWindowManager.GetWindow(CInt(Window.WINDOW_DIALOG_PROGRESS)), GUIDialogProgress)
            If pDlgProgress.IsInstance(fetcher) Then
                pDlgProgress.DisableCancel(True)
            End If
            Return True
        End Function

        Public Sub OnProgress(line1 As String, line2 As String, line3 As String, percent As Integer) Implements IMDB.IProgress.OnProgress
            If Not GUIWindowManager.IsRouted Then
                Return
            End If
            Dim pDlgProgress As GUIDialogProgress = DirectCast(GUIWindowManager.GetWindow(CInt(Window.WINDOW_DIALOG_PROGRESS)), GUIDialogProgress)
            pDlgProgress.ShowProgressBar(True)
            pDlgProgress.SetLine(1, line1)
            pDlgProgress.SetLine(2, line2)
            If percent > 0 Then
                pDlgProgress.SetPercentage(percent)
            End If
            pDlgProgress.Progress()
        End Sub

        Public Function OnSearchStarting(fetcher As IMDBFetcher) As Boolean Implements MediaPortal.Video.Database.IMDB.IProgress.OnSearchStarting
            Dim pDlgProgress As GUIDialogProgress = DirectCast(GUIWindowManager.GetWindow(CInt(Window.WINDOW_DIALOG_PROGRESS)), GUIDialogProgress)
            ' show dialog that we're busy querying www.imdb.com
            pDlgProgress.Reset()
            pDlgProgress.SetHeading(GUILocalizeStrings.[Get](197))
            pDlgProgress.SetLine(1, fetcher.MovieName)
            pDlgProgress.SetLine(2, String.Empty)
            pDlgProgress.SetObject(fetcher)
            pDlgProgress.StartModal(GUIWindowManager.ActiveWindow)
            Return True
        End Function

        Public Function OnSearchStarted(fetcher As IMDBFetcher) As Boolean Implements MediaPortal.Video.Database.IMDB.IProgress.OnSearchStarted
            Dim pDlgProgress As GUIDialogProgress = DirectCast(GUIWindowManager.GetWindow(CInt(Window.WINDOW_DIALOG_PROGRESS)), GUIDialogProgress)
            pDlgProgress.SetObject(fetcher)
            pDlgProgress.DoModal(GUIWindowManager.ActiveWindow)
            If pDlgProgress.IsCanceled Then
                Return False
            End If
            Return True
        End Function

        Public Function OnSearchEnd(fetcher As IMDBFetcher) As Boolean Implements MediaPortal.Video.Database.IMDB.IProgress.OnSearchEnd
            Dim pDlgProgress As GUIDialogProgress = DirectCast(GUIWindowManager.GetWindow(CInt(Window.WINDOW_DIALOG_PROGRESS)), GUIDialogProgress)
            If (pDlgProgress IsNot Nothing) AndAlso (pDlgProgress.IsInstance(fetcher)) Then
                pDlgProgress.Close()
            End If
            Return True
        End Function

        Public Function OnMovieNotFound(fetcher As IMDBFetcher) As Boolean Implements MediaPortal.Video.Database.IMDB.IProgress.OnMovieNotFound
            Log.Info("IMDB Fetcher: OnMovieNotFound")
            ' show dialog...
            Dim pDlgOK As GUIDialogOK = DirectCast(GUIWindowManager.GetWindow(CInt(Window.WINDOW_DIALOG_OK)), GUIDialogOK)
            pDlgOK.SetHeading(195)
            pDlgOK.SetLine(1, fetcher.MovieName)
            pDlgOK.SetLine(2, String.Empty)
            pDlgOK.DoModal(GUIWindowManager.ActiveWindow)
            Return True
        End Function

        Public Function OnDetailsStarting(fetcher As IMDBFetcher) As Boolean Implements MediaPortal.Video.Database.IMDB.IProgress.OnDetailsStarting
            Dim pDlgProgress As GUIDialogProgress = DirectCast(GUIWindowManager.GetWindow(CInt(Window.WINDOW_DIALOG_PROGRESS)), GUIDialogProgress)
            ' show dialog that we're downloading the movie info
            pDlgProgress.Reset()
            pDlgProgress.SetHeading(GUILocalizeStrings.[Get](198))
            'pDlgProgress.SetLine(0, strMovieName);
            pDlgProgress.SetLine(1, fetcher.MovieName)
            pDlgProgress.SetLine(2, String.Empty)
            pDlgProgress.SetObject(fetcher)
            pDlgProgress.StartModal(GUIWindowManager.ActiveWindow)
            Return True
        End Function

        Public Function OnDetailsStarted(fetcher As IMDBFetcher) As Boolean Implements MediaPortal.Video.Database.IMDB.IProgress.OnDetailsStarted
            Dim pDlgProgress As GUIDialogProgress = DirectCast(GUIWindowManager.GetWindow(CInt(Window.WINDOW_DIALOG_PROGRESS)), GUIDialogProgress)
            pDlgProgress.SetObject(fetcher)
            pDlgProgress.DoModal(GUIWindowManager.ActiveWindow)
            If pDlgProgress.IsCanceled Then
                Return False
            End If
            Return True
        End Function

        Public Function OnDetailsEnd(fetcher As IMDBFetcher) As Boolean Implements MediaPortal.Video.Database.IMDB.IProgress.OnDetailsEnd
            Dim pDlgProgress As GUIDialogProgress = DirectCast(GUIWindowManager.GetWindow(CInt(Window.WINDOW_DIALOG_PROGRESS)), GUIDialogProgress)
            If (pDlgProgress IsNot Nothing) AndAlso (pDlgProgress.IsInstance(fetcher)) Then
                pDlgProgress.Close()
            End If
            Return True
        End Function

        Public Function OnActorsStarting(fetcher As IMDBFetcher) As Boolean Implements MediaPortal.Video.Database.IMDB.IProgress.OnActorsStarting
            ' won't occure
            Return True
        End Function

        Public Function OnActorsStarted(fetcher As IMDBFetcher) As Boolean Implements MediaPortal.Video.Database.IMDB.IProgress.OnActorsStarted
            ' won't occure
            Return True
        End Function

        Public Function OnActorsEnd(fetcher As IMDBFetcher) As Boolean Implements MediaPortal.Video.Database.IMDB.IProgress.OnActorsEnd
            ' won't occure
            Return True
        End Function

        Public Function OnActorInfoStarting(fetcher As IMDBFetcher) As Boolean Implements MediaPortal.Video.Database.IMDB.IProgress.OnActorInfoStarting
            ' won't occure
            Return True
        End Function

        Public Function OnSelectActor(fetcher As IMDBFetcher, ByRef selected As Integer) As Boolean Implements MediaPortal.Video.Database.IMDB.IProgress.OnSelectActor
            ' won't occure
            selected = 0
            Return True
        End Function

        Public Function OnDetailsNotFound(fetcher As IMDBFetcher) As Boolean Implements MediaPortal.Video.Database.IMDB.IProgress.OnDetailsNotFound
            Log.Info("IMDB Fetcher: OnDetailsNotFound")
            ' show dialog...
            Dim pDlgOK As GUIDialogOK = DirectCast(GUIWindowManager.GetWindow(CInt(Window.WINDOW_DIALOG_OK)), GUIDialogOK)
            ' show dialog...
            pDlgOK.SetHeading(195)
            pDlgOK.SetLine(1, fetcher.MovieName)
            pDlgOK.SetLine(2, String.Empty)
            pDlgOK.DoModal(GUIWindowManager.ActiveWindow)
            Return False
        End Function

        Public Function OnRequestMovieTitle(fetcher As IMDBFetcher, ByRef movieName As String) As Boolean Implements MediaPortal.Video.Database.IMDB.IProgress.OnRequestMovieTitle
            movieName = fetcher.MovieName
            If GetKeyboard(movieName) Then
                If movieName = String.Empty Then
                    Return False
                End If
                Return True
            End If
            movieName = String.Empty
            Return False
        End Function

        Public Function OnSelectMovie(fetcher As IMDBFetcher, ByRef selectedMovie As Integer) As Boolean Implements MediaPortal.Video.Database.IMDB.IProgress.OnSelectMovie

            Dim pDlgSelect As GUIDialogSelect = DirectCast(GUIWindowManager.GetWindow(CInt(Window.WINDOW_DIALOG_SELECT)), GUIDialogSelect)
            ' more then 1 movie found
            ' ask user to select 1
            pDlgSelect.Reset()
            pDlgSelect.SetHeading(196)
            'select movie
            For i As Integer = 0 To fetcher.Count - 1
                pDlgSelect.Add(fetcher(i).Title)
            Next
            pDlgSelect.EnableButton(True)
            pDlgSelect.SetButtonLabel(413)
            ' manual
            pDlgSelect.DoModal(GUIWindowManager.ActiveWindow)

            ' and wait till user selects one
            selectedMovie = pDlgSelect.SelectedLabel
            If pDlgSelect.IsButtonPressed Then
                Return True
            End If
            If selectedMovie = -1 Then
                Return False
            End If
            Return True
        End Function

        Public Function OnScanStart(total As Integer) As Boolean Implements MediaPortal.Video.Database.IMDB.IProgress.OnScanStart
            ' won't occure
            Return True
        End Function

        Public Function OnScanEnd() As Boolean Implements MediaPortal.Video.Database.IMDB.IProgress.OnScanEnd
            ' won't occure
            Return True
        End Function

        Public Function OnScanIterating(count As Integer) As Boolean Implements MediaPortal.Video.Database.IMDB.IProgress.OnScanIterating
            ' won't occure
            Return True
        End Function

        Public Function OnScanIterated(count As Integer) As Boolean Implements MediaPortal.Video.Database.IMDB.IProgress.OnScanIterated
            ' won't occure
            Return True
        End Function

#End Region

    End Class
End Namespace