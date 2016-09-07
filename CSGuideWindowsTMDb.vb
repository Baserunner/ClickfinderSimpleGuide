﻿Imports MediaPortal.GUI.Library
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
        <SkinControlAttribute(60)> Protected FanartBackground As GUIImage = Nothing
        <SkinControlAttribute(61)> Protected FanartBackground2 As GUIImage = Nothing
        <SkinControlAttribute(901)> Protected btnPlot As GUICheckButton = Nothing
        <SkinControlAttribute(902)> Protected btnCast As GUICheckButton = Nothing
        <SkinControlAttribute(20)> Protected tbPlotArea As GUITextScrollUpControl = Nothing
        <SkinControlAttribute(24)> Protected listCast As GUIListControl = Nothing

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

            Return Load(GUIGraphicsContext.Skin + "\ClickfinderSimpleGuideTMDb.xml")
        End Function

        Public Overrides Function GetModuleName() As String
            Return "Clickfinder Simple Guide"
        End Function
#End Region
#Region "Members"

        Private Shared _backdrop As ImageSwapper
        Private Shared _counter As Integer
        Private Shared _filename As String
        Private Shared _movieInfo As CSGuideTMDBCacheItem3
        Private Shared _tmdbClient As TMDbClient

        Private Shared _SelectedListCastItemId As Integer

        Private m_castList As New ArrayList

        Public Shared Property TmdbClient() As TMDbClient
            Get
                Return _tmdbClient
            End Get
            Set(ByVal value As TMDbClient)
                _tmdbClient = value
            End Set
        End Property

        Public Shared Property MovieInfo() As CSGuideTMDBCacheItem3
            Get
                Return _movieInfo
            End Get
            Set(ByVal value As CSGuideTMDBCacheItem3)
                _movieInfo = value
            End Set
        End Property
#End Region
#Region "GUI Events"

        Protected Overrides Sub OnPageLoad()
            _backdrop = New ImageSwapper
            _counter = 0
            _backdrop.PropertyOne = "#Fanart.1"
            _backdrop.PropertyTwo = "#Fanart.2"

            _backdrop.GUIImageOne = FanartBackground
            _backdrop.GUIImageTwo = FanartBackground2
            LoadFanart(_backdrop, _movieInfo.Misc.absFanartPath)
            'Init the GUI controls
            btnPlot.Selected = True
            listCast.IsVisible = False
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
            Dim _mName As String = System.Reflection.MethodInfo.GetCurrentMethod.Name
            Dim _mClass As String = Me.GetType.Name
            Dim _filename As String = Nothing

            If GUIWindowManager.ActiveWindow = GetID Then

                'Move down                
                If action.wID = MediaPortal.GUI.Library.Action.ActionType.ACTION_MOVE_DOWN Then
                    If listCast.SelectedListItem.ItemId = listCast.Item(listCast.Count - 1).ItemId Then
                        _SelectedListCastItemId = listCast.Item(0).ItemId
                    Else
                        _SelectedListCastItemId = listCast.Item(listCast.SelectedListItemIndex + 1).ItemId
                    End If
                    Try
                        If listCast.IsFocused = True Then
                            SetActorGUIProperties()
                        End If
                    Catch ex As Exception
                        MyLog.Error(String.Format("[{0}] [{1}]: Move down - Err: {2} stack: {3}", _mClass, _mName, ex.Message, ex.StackTrace))
                    End Try
                End If

                'Move up                
                If action.wID = MediaPortal.GUI.Library.Action.ActionType.ACTION_MOVE_UP Then
                    If listCast.SelectedListItem.ItemId = listCast.Item(0).ItemId Then
                        _SelectedListCastItemId = listCast.Item(listCast.Count - 1).ItemId
                    Else
                        _SelectedListCastItemId = listCast.Item(listCast.SelectedListItemIndex - 1).ItemId
                    End If
                    Try
                        If listCast.IsFocused = True Then
                            SetActorGUIProperties()
                        End If
                    Catch ex As Exception
                        MyLog.Error(String.Format("[{0}] [{1}]: Move up - Err: {2} stack: {3}", _mClass, _mName, ex.Message, ex.StackTrace))
                    End Try
                End If
                'Page down                
                If action.wID = MediaPortal.GUI.Library.Action.ActionType.ACTION_PAGE_DOWN Then
                    If listCast.SelectedListItem.ItemId + 10 > listCast.Item(listCast.Count - 1).ItemId Then
                        _SelectedListCastItemId = listCast.Item(listCast.Count - 1).ItemId
                    Else
                        _SelectedListCastItemId = listCast.Item(listCast.SelectedListItemIndex + 10).ItemId
                    End If
                    Try
                        If listCast.IsFocused = True Then
                            SetActorGUIProperties()
                        End If
                    Catch ex As Exception
                        MyLog.Error(String.Format("[{0}] [{1}]: Page down - Err: {2} stack: {3}", _mClass, _mName, ex.Message, ex.StackTrace))
                    End Try
                End If

                'Page up                
                If action.wID = MediaPortal.GUI.Library.Action.ActionType.ACTION_PAGE_UP Then
                    If listCast.SelectedListItemIndex - 10 < 0 Then
                        _SelectedListCastItemId = listCast.Item(0).ItemId
                    Else
                        _SelectedListCastItemId = listCast.Item(listCast.SelectedListItemIndex - 10).ItemId
                    End If
                    Try
                        If listCast.IsFocused = True Then
                            SetActorGUIProperties()
                        End If
                    Catch ex As Exception
                        MyLog.Error(String.Format("[{0}] [{1}]: Page up - Err: {2} stack: {3}", _mClass, _mName, ex.Message, ex.StackTrace))
                    End Try
                End If
            End If
            MyBase.OnAction(action)
        End Sub


        Protected Overrides Sub OnPreviousWindow()
            Dim _mName As String = System.Reflection.MethodInfo.GetCurrentMethod.Name
            Dim _mClass As String = Me.GetType.Name
            MyLog.Debug(String.Format("[{0}] [{1}]: Calling myBase.OnPreviousWindow", _mClass, _mName))
            MyBase.OnPreviousWindow()
        End Sub

        Protected Overrides Sub OnClicked(controlId As Integer, control As GUIControl, actionType As Action.ActionType)
            MyBase.OnClicked(controlId, control, actionType)

            ' Plot button
            If controlId = 901 Then

                btnCast.Selected = False
                If tbPlotArea IsNot Nothing Then tbPlotArea.IsVisible = True
                If listCast IsNot Nothing Then listCast.IsVisible = False
                SetPlotGUIProperties()

            End If

            ' Cast button
            If controlId = 902 Then
                btnPlot.Selected = False
                If tbPlotArea IsNot Nothing Then tbPlotArea.IsVisible = False
                If listCast IsNot Nothing Then
                    listCast.IsVisible = True
                    btnCast.Focus = False
                    listCast.Focus = True
                    _SelectedListCastItemId = listCast.Item(0).ItemId
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
            CSGuideHelper.SetProperty("#budget", String.Format("{0:N0}", _movieInfo.movie.Budget))
            CSGuideHelper.SetProperty("#revenue", String.Format("{0:N0}", _movieInfo.movie.Revenue))
            CSGuideHelper.SetProperty("#runtime", _movieInfo.movie.Runtime & " min")
            CSGuideHelper.SetProperty("#orginalTitle", _movieInfo.movie.OriginalTitle)
            CSGuideHelper.SetProperty("#popularity", _movieInfo.movie.Popularity)
            CSGuideHelper.SetProperty("#votes", _movieInfo.movie.VoteCount)
            CSGuideHelper.SetProperty("#releaseDate", _movieInfo.movie.ReleaseDate)
            CSGuideHelper.SetProperty("#rating", _movieInfo.movie.VoteAverage)

        End Sub

        Private Sub SetActorGUIProperties()
            Dim person As Person = _tmdbClient.GetPersonAsync(_SelectedListCastItemId).Result
            Dim myURL As String = _tmdbClient.GetImageUrl("original", person.ProfilePath).ToString()
            Utils.DownLoadAndCacheImage(myURL, getAbsActorThumbPath(myURL))
            CSGuideHelper.SetProperty("#thumb", getAbsActorThumbPath(myURL))
            CSGuideHelper.SetProperty("#birthday", person.Birthday)
            CSGuideHelper.SetProperty("#placeOfBirth", person.PlaceOfBirth)
            CSGuideHelper.SetProperty("#biography", person.Biography)
        End Sub
        Private Function getAbsActorThumbPath(url As String) As String
            Dim rgx As New System.Text.RegularExpressions.Regex(".*\/(.*)$")
            Return Path.Combine(Config.GetSubFolder(Config.Dir.Skin, Config.SkinName & "\Media\CSG\Actor\"), rgx.Match(url).Groups(1).Value)
        End Function
        Private Sub SetCastListItems()

            Try
                m_castList.Clear()

                If m_castList IsNot Nothing Then
                    m_castList.Clear()
                Else
                    Return
                End If

                For Each myCast In _movieInfo.credit.Cast
                    m_castList.Add(myCast)
                Next

                If m_castList.Count = 0 Then
                    Return
                End If

                For Each cast As Cast In m_castList
                    'temp = actor.Split(splitter)
                    Dim item As New GUIListItem()
                    item.ItemId = cast.Id
                    item.Label = cast.Name + " (" + cast.Character + ")"
                    item.Label2 = cast.Name
                    item.Label3 = cast.Id
                    listCast.Add(item)
                Next

                If listCast.Count > 0 Then
                    listCast.SelectedListItemIndex = 0
                End If
            Catch ex As Exception
                Log.[Error]("GUIVideoInfo exception SetActorGUIListItems: {0}", ex.Message)
            End Try
        End Sub
    End Class

End Namespace

