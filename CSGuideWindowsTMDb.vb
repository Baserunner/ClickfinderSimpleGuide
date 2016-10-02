Imports MediaPortal.GUI.Library
Imports MediaPortal.Util
Imports MediaPortal.Configuration
Imports TMDbLib.Objects.Movies
Imports TMDbLib.Objects.People
Imports TMDbLib.Client
Imports System.IO



Namespace ClickfinderSimpleGuide
    Public Class CSGuideWindowsTMDb
        Inherits GUIWindow

#Region "Skin Controls"
        <SkinControlAttribute(60)> Protected _FanartBackground As GUIImage = Nothing
        <SkinControlAttribute(61)> Protected _FanartBackground2 As GUIImage = Nothing
        <SkinControlAttribute(901)> Protected _btnPlot As GUICheckButton = Nothing
        <SkinControlAttribute(902)> Protected _btnCast As GUICheckButton = Nothing
        <SkinControlAttribute(20)> Protected _tbPlotArea As GUITextScrollUpControl = Nothing
        <SkinControlAttribute(24)> Protected _listCast As GUIListControl = Nothing

#End Region
#Region "GUI Properties"
        Public Overloads Overrides Property GetID() As Integer
            Get
                Return 730352
            End Get
            Set(ByVal value As Integer)
            End Set
        End Property


        Public Overloads Overrides Function Init() As Boolean
            'Beim initialisieren des Plugin den Screen laden
            Dim mName As String = System.Reflection.MethodInfo.GetCurrentMethod.Name
            _mClass = Me.GetType.Name
            MyLog.Debug(String.Format("[{0}] [{1}]: Called", _mClass, mName))
            Return Load(GUIGraphicsContext.Skin + "\ClickfinderSimpleGuideTMDb.xml")
        End Function

        Public Overrides Function GetModuleName() As String
            Return "Clickfinder Simple Guide"
        End Function
#End Region
#Region "Members"

        Private _backdrop As ImageSwapper
        Private _counter As Integer
        Private _filename As String
        Private Shared _movieInfo As CSGuideTMDBCacheItem
        Private Shared _tmdbClient As TMDbClient
        Private _SelectedListCastItemId As Integer
        Private _castList As New ArrayList
        Private _mClass As String

        Public Shared Property TmdbClient() As TMDbClient
            Get
                Return _tmdbClient
            End Get
            Set(ByVal value As TMDbClient)
                _tmdbClient = value
            End Set
        End Property

        Public Shared Property MovieInfo() As CSGuideTMDBCacheItem
            Get
                Return _movieInfo
            End Get
            Set(ByVal value As CSGuideTMDBCacheItem)
                _movieInfo = value
            End Set
        End Property
#End Region
#Region "GUI Events"

        Protected Overrides Sub OnPageLoad()
            _mClass = Me.GetType.Name
            _backdrop = New ImageSwapper
            _counter = 0
            _backdrop.PropertyOne = "#Fanart.1"
            _backdrop.PropertyTwo = "#Fanart.2"

            _backdrop.GUIImageOne = _FanartBackground
            _backdrop.GUIImageTwo = _FanartBackground2
            LoadFanart(_backdrop, _movieInfo.Misc.absFanartPath)
            'Init the GUI controls
            _btnPlot.Selected = True
            _listCast.IsVisible = False
            '
            SetPlotGUIProperties()
            SetCastListItems()
        End Sub

        Protected Overrides Sub OnPageDestroy(ByVal new_windowId As Integer)

            MyBase.OnPageDestroy(new_windowId)
            Dispose()
            AllocResources()
        End Sub

        Public Overrides Sub OnAction(ByVal action As MediaPortal.GUI.Library.Action)
            Dim mName As String = System.Reflection.MethodInfo.GetCurrentMethod.Name
            Dim filename As String = Nothing

            If GUIWindowManager.ActiveWindow = GetID Then

                'Move down                
                If action.wID = MediaPortal.GUI.Library.Action.ActionType.ACTION_MOVE_DOWN Then
                    If _listCast.SelectedListItem.ItemId = _listCast.Item(_listCast.Count - 1).ItemId Then
                        _SelectedListCastItemId = _listCast.Item(0).ItemId
                    Else
                        _SelectedListCastItemId = _listCast.Item(_listCast.SelectedListItemIndex + 1).ItemId
                    End If
                    Try
                        If _listCast.IsFocused = True Then
                            SetActorGUIProperties()
                        End If
                    Catch ex As Exception
                        MyLog.Error(String.Format("[{0}] [{1}]: Move down - Err: {2} stack: {3}", _mClass, mName, ex.Message, ex.StackTrace))
                    End Try
                End If

                'Move up                
                If action.wID = MediaPortal.GUI.Library.Action.ActionType.ACTION_MOVE_UP Then
                    If _listCast.SelectedListItem.ItemId = _listCast.Item(0).ItemId Then
                        _SelectedListCastItemId = _listCast.Item(_listCast.Count - 1).ItemId
                    Else
                        _SelectedListCastItemId = _listCast.Item(_listCast.SelectedListItemIndex - 1).ItemId
                    End If
                    Try
                        If _listCast.IsFocused = True Then
                            SetActorGUIProperties()
                        End If
                    Catch ex As Exception
                        MyLog.Error(String.Format("[{0}] [{1}]: Move up - Err: {2} stack: {3}", _mClass, mName, ex.Message, ex.StackTrace))
                    End Try
                End If
                'Page down                
                If action.wID = MediaPortal.GUI.Library.Action.ActionType.ACTION_PAGE_DOWN Then
                    If _listCast.SelectedListItem.ItemId + 10 > _listCast.Item(_listCast.Count - 1).ItemId Then
                        _SelectedListCastItemId = _listCast.Item(_listCast.Count - 1).ItemId
                    Else
                        _SelectedListCastItemId = _listCast.Item(_listCast.SelectedListItemIndex + 10).ItemId
                    End If
                    Try
                        If _listCast.IsFocused = True Then
                            SetActorGUIProperties()
                        End If
                    Catch ex As Exception
                        MyLog.Error(String.Format("[{0}] [{1}]: Page down - Err: {2} stack: {3}", _mClass, mName, ex.Message, ex.StackTrace))
                    End Try
                End If

                'Page up                
                If action.wID = MediaPortal.GUI.Library.Action.ActionType.ACTION_PAGE_UP Then
                    If _listCast.SelectedListItemIndex - 10 < 0 Then
                        _SelectedListCastItemId = _listCast.Item(0).ItemId
                    Else
                        _SelectedListCastItemId = _listCast.Item(_listCast.SelectedListItemIndex - 10).ItemId
                    End If
                    Try
                        If _listCast.IsFocused = True Then
                            SetActorGUIProperties()
                        End If
                    Catch ex As Exception
                        MyLog.Error(String.Format("[{0}] [{1}]: Page up - Err: {2} stack: {3}", _mClass, mName, ex.Message, ex.StackTrace))
                    End Try
                End If
            End If
            MyBase.OnAction(action)
        End Sub


        Protected Overrides Sub OnPreviousWindow()
            Dim mName As String = System.Reflection.MethodInfo.GetCurrentMethod.Name
            MyLog.Debug(String.Format("[{0}] [{1}]: Calling myBase.OnPreviousWindow", _mClass, mName))
            MyBase.OnPreviousWindow()
        End Sub

        Protected Overrides Sub OnClicked(controlId As Integer, control As GUIControl, actionType As Action.ActionType)
            MyBase.OnClicked(controlId, control, actionType)

            ' Plot button
            If controlId = 901 Then

                _btnCast.Selected = False
                If _tbPlotArea IsNot Nothing Then _tbPlotArea.IsVisible = True
                If _listCast IsNot Nothing Then _listCast.IsVisible = False
                SetPlotGUIProperties()

            End If

            ' Cast button
            If controlId = 902 Then
                _btnPlot.Selected = False
                If _tbPlotArea IsNot Nothing Then _tbPlotArea.IsVisible = False
                If _listCast IsNot Nothing Then
                    _listCast.IsVisible = True
                    _btnCast.Focus = False
                    _listCast.Focus = True
                    _SelectedListCastItemId = _listCast.Item(0).ItemId
                    SetActorGUIProperties()
                End If
            End If
        End Sub

#End Region


        Public Shared Sub LoadFanart(backdrop As ImageSwapper, filename As String)
            ' Activate Backdrop in Image Swapper
            If Not backdrop.Active Then
                backdrop.Active = True
            End If

            If String.IsNullOrEmpty(filename) OrElse Not System.IO.File.Exists(filename) Then
                filename = String.Empty
            End If

            ' Assign Fanart filename to Image Loader
            ' Will display fanart in backdrop or reset to default background
            backdrop.Filename = filename
        End Sub

        Private Sub SetPlotGUIProperties()
            Dim stringHelper = ""
            Utils.DownLoadAndCacheImage(_movieInfo.Misc.posterURL, _movieInfo.Misc.absPosterPath)
            CSGuideHelper.SetProperty("#thumb", _movieInfo.Misc.absPosterPath)
            CSGuideHelper.SetProperty("#title", _movieInfo.Misc.title_string)
            CSGuideHelper.SetProperty("#plot", _movieInfo.movie.Overview)
            CSGuideHelper.SetProperty("#tagline", _movieInfo.movie.Tagline)
            For Each genre In _movieInfo.movie.Genres
                If String.IsNullOrEmpty(stringHelper) Then
                    stringHelper = genre.Name
                Else
                    stringHelper = stringHelper & "|" & genre.Name
                End If
            Next

            CSGuideHelper.SetProperty("#genre", stringHelper)
            stringHelper = ""
            CSGuideHelper.SetProperty("#director", _movieInfo.credit.Crew.First.Name)
            For Each country In _movieInfo.movie.ProductionCountries
                If String.IsNullOrEmpty(stringHelper) Then
                    stringHelper = country.Name
                Else
                    stringHelper = stringHelper & "|" & country.Name
                End If
            Next
            CSGuideHelper.SetProperty("#country", stringHelper)
            stringHelper = ""
            If _movieInfo.movie.Budget = 0 Then
                CSGuideHelper.SetProperty("#budget", "unbekannt")
            Else
                CSGuideHelper.SetProperty("#budget", String.Format("{0:N0}", _movieInfo.movie.Budget))
            End If

            If _movieInfo.movie.Revenue = 0 Then
                CSGuideHelper.SetProperty("#revenue", "unbekannt")
            Else
                CSGuideHelper.SetProperty("#revenue", String.Format("{0:N0}", _movieInfo.movie.Revenue))
            End If

            CSGuideHelper.SetProperty("#runtime", _movieInfo.movie.Runtime & " min")
            CSGuideHelper.SetProperty("#orginalTitle", _movieInfo.movie.OriginalTitle)
            CSGuideHelper.SetProperty("#popularity", _movieInfo.movie.Popularity)
            CSGuideHelper.SetProperty("#votes", "(" & _movieInfo.movie.VoteCount & " Votes)")
            CSGuideHelper.SetProperty("#releaseDate", _movieInfo.movie.ReleaseDate)
            CSGuideHelper.SetProperty("#rating", _movieInfo.movie.VoteAverage)

        End Sub

        Private Sub SetActorGUIProperties()
            Dim mName As String = System.Reflection.MethodInfo.GetCurrentMethod.Name
            Try
                Dim person As Person = _tmdbClient.GetPersonAsync(_SelectedListCastItemId).Result
                CSGuideHelper.SetProperty("#thumb", "")
                CSGuideHelper.SetProperty("#birthday", "unbekannt")
                CSGuideHelper.SetProperty("#placeOfBirth", "unbekannt")
                CSGuideHelper.SetProperty("#biography", "")
                If Not person Is Nothing Then
                    Dim myURL As String = _tmdbClient.GetImageUrl("original", person.ProfilePath).ToString()
                    Utils.DownLoadAndCacheImage(myURL, getAbsActorThumbPath(myURL))
                    CSGuideHelper.SetProperty("#thumb", getAbsActorThumbPath(myURL))
                    If Not person.Birthday Is Nothing Then CSGuideHelper.SetProperty("#birthday", person.Birthday)
                    If Not person.PlaceOfBirth Is Nothing Then CSGuideHelper.SetProperty("#placeOfBirth", person.PlaceOfBirth)
                    If Not person.Biography Is Nothing Then CSGuideHelper.SetProperty("#biography", person.Biography)
                End If
            Catch ex As Exception
                MyLog.Error(String.Format("[{0}] [{1}]: Cannot get information from Person {2}", _mClass, mName, _SelectedListCastItemId))
            End Try

        End Sub
        Private Function getAbsActorThumbPath(url As String) As String
            Dim rgx As New System.Text.RegularExpressions.Regex(".*\/(.*)$")
            Return Path.Combine(Config.GetSubFolder(Config.Dir.Skin, Config.SkinName & "\media\CSGuide\Actor\"), rgx.Match(url).Groups(1).Value)
        End Function
        Private Sub SetCastListItems()

            Try
                _castList.Clear()

                If _castList IsNot Nothing Then
                    _castList.Clear()
                Else
                    Return
                End If

                For Each myCast In _movieInfo.credit.Cast
                    _castList.Add(myCast)
                Next

                If _castList.Count = 0 Then
                    Return
                End If

                For Each cast As Cast In _castList
                    'temp = actor.Split(splitter)
                    Dim item As New GUIListItem()
                    item.ItemId = cast.Id
                    item.Label = cast.Name + " (" + cast.Character + ")"
                    item.Label2 = cast.Name
                    item.Label3 = cast.Id
                    _listCast.Add(item)
                Next

                If _listCast.Count > 0 Then
                    _listCast.SelectedListItemIndex = 0
                End If
            Catch ex As Exception
                Log.[Error]("GUIVideoInfo exception SetActorGUIListItems: {0}", ex.Message)
            End Try
        End Sub
    End Class

End Namespace

