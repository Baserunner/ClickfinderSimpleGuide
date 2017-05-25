Imports MediaPortal.GUI.Library
Imports TvDatabase
Imports TvPlugin
Imports MediaPortal.Dialogs
Imports MediaPortal.GUI.Library.GUIWindow

Imports MediaPortal.Configuration
Imports MediaPortal.Player
Imports System.Text.RegularExpressions
Imports enrichEPG.TvDatabase

Imports System.IO
Imports System.Text

Namespace ClickfinderSimpleGuide
    Public Class CSGuideHelper
#Region "Members"
        Private Shared _PlayedFile As TVMovieProgram
#End Region

#Region "Properties"
        Public Shared ReadOnly Property Version() As String
            Get
                Return "v0.9.5.0"
            End Get
        End Property
#End Region

        Friend Shared Sub AddListControlItem(ByVal Listcontrol As GUIListControl,
                                             ByVal idProgram As Integer,
                                             ByVal ChannelName As String,
                                             ByVal titelLabel As String,
                                             Optional ByVal timeLabel As String = "",
                                             Optional ByVal infoLabel As String = "",
                                             Optional ByVal ImagePath As String = "",
                                             Optional ByVal MinRunTime As Integer = 0,
                                             Optional ByVal isRecording As String = "",
                                             Optional ByVal tmpInfo As String = "",
                                             Optional ByVal tmpInfo2 As String = "")
            Try

                Dim lItem As New GUIListItem

                lItem.Label = titelLabel
                lItem.Label2 = timeLabel
                lItem.Label3 = infoLabel
                lItem.ItemId = idProgram
                lItem.Path = ChannelName
                lItem.IconImage = ImagePath
                lItem.Duration = MinRunTime
                lItem.PinImage = isRecording
                lItem.TVTag = tmpInfo
                lItem.ThumbnailImage = tmpInfo2

                GUIControl.AddListItemControl(GUIWindowManager.ActiveWindow, Listcontrol.GetID, lItem)


                lItem.Dispose()

            Catch ex As Exception
                MyLog.Error("[CSGuideHelper] [AddListControlItem]: exception err: {0} stack: {1}", ex.Message, ex.StackTrace)
            End Try

        End Sub

        Friend Shared Function MySqlDate(ByVal Datum As Date) As String
            Try
                If Gentle.Framework.Broker.ProviderName = "MySQL" Then
                    Return "'" & Datum.Year & "-" & Format(Datum.Month, "00") & "-" & Format(Datum.Day, "00") & " " & Format(Datum.Hour, "00") & ":" & Format(Datum.Minute, "00") & ":00'"
                Else
                    Return "'" & Datum.Year & Format(Datum.Month, "00") & Format(Datum.Day, "00") & " " & Format(Datum.Hour, "00") & ":" & Format(Datum.Minute, "00") & ":" & Format(Datum.Second, "00") & "'"
                End If

            Catch ex As Exception
                MyLog.Error("[CSGuideHelper] [MySqlDate]: exception err: {0} stack: {1}", ex.Message, ex.StackTrace)
                Return ""
            End Try
        End Function

        Friend Shared Sub LoadTVProgramInfo(ByVal Program As Program)

            Try
                TvPlugin.TVProgramInfo.CurrentProgram = Program
                GUIWindowManager.ActivateWindow(CInt(Window.WINDOW_TV_PROGRAM_INFO))
            Catch ex As Exception
                MyLog.Error("[CSGuideHelper] [LoadTVProgramInfo]: exception err: {0} stack: {1}", ex.Message, ex.StackTrace)
            End Try

        End Sub
        'MP Notify für Sendung setzen
        Friend Shared Sub SetNotify(ByVal Program As Program)
            Try
                Dim Erinnerung As Program = Program.Retrieve(Program.IdProgram)
                Erinnerung.Notify = True
                Erinnerung.Persist()
                TvNotifyManager.OnNotifiesChanged()

                MPDialogOK("Erinnerung:", Erinnerung.Title, Erinnerung.StartTime & " - " & Erinnerung.EndTime, Erinnerung.ReferencedChannel.DisplayName)

            Catch ex As Exception
                MyLog.Error("[CSGuideHelper] [SetNotify]: exception err: {0} stack: {1}", ex.Message, ex.StackTrace)
                ShowNotify(ex.Message)
            End Try
        End Sub
        'MP Tv Kanal einschalten
        Friend Shared Sub StartTv(ByVal program As Program)
            Try
                Dim _TvMovieprogram As TVMovieProgram = TVMovieProgram.Retrieve(program.IdProgram)

                If Not String.IsNullOrEmpty(_TvMovieprogram.FileName) Then
                    _PlayedFile = _TvMovieprogram

                    g_Player.Play(_TvMovieprogram.FileName)
                    g_Player.ShowFullScreenWindowVideoDefault()

                Else
                    Dim changeChannel As Channel = DirectCast(program.ReferencedChannel, Channel)
                    MediaPortal.GUI.Library.GUIWindowManager.ActivateWindow(CInt(MediaPortal.GUI.Library.GUIWindow.Window.WINDOW_TVFULLSCREEN))
                    TVHome.ViewChannelAndCheck(changeChannel)
                End If

            Catch ex As Exception
                MyLog.Error("[CSGuideHelper] [StartTv]: exception err: {0} stack: {1}", ex.Message, ex.StackTrace)
                ShowNotify(ex.Message)
            End Try
        End Sub

        'MediaPortal Dialoge
        Friend Shared Sub MPDialogOK(ByVal Heading As String, ByVal StringLine1 As String, Optional ByVal StringLine2 As String = "", Optional ByVal StringLine3 As String = "")
            Try
                Dim dlg As GUIDialogOK = CType(GUIWindowManager.GetWindow(CType(GUIWindow.Window.WINDOW_DIALOG_OK, Integer)), GUIDialogOK)

                dlg.SetHeading(Heading)
                dlg.SetLine(1, StringLine1)
                dlg.SetLine(2, StringLine2)
                dlg.SetLine(3, StringLine3)
                dlg.DoModal(GUIWindowManager.ActiveWindow)
                dlg.Dispose()
                dlg.AllocResources()
            Catch ex As Exception
                MyLog.Error("[CSGuideHelper]: [StartTv]: exception err: {0} stack: {1}", ex.Message, ex.StackTrace)
            End Try

        End Sub

        Friend Shared Sub CheckConnectionState()
            Try
                'TvServer nicht verbunden / online
                If CSGuideHelper.TvServerConnected = False Then
                    ShowNotify("Kein Connect zum TV-Server")
                    Return
                End If

                'Clickfinder DB nicht gefunden
                If IO.Directory.Exists(CSGuideSettings.ClickfinderImagePath) = False Then
                    ShowNotify("""Clickfinder Image Pfad"" nicht gefunden")
                    Return
                End If

                'TvMovie EGP IMport++ plugin nicht aktiviert / installiert
                If CSGuideSettings.TVMovieIsEnabled = False Then
                    ShowNotify("TV Movie++ Plugin nicht Enabled")
                    Return
                End If
            Catch ex As Exception
                MyLog.Error("[CSGuideHelper] [CheckConnectionState]: exception err: {0} stack: {1}", ex.Message, ex.StackTrace)
            End Try
        End Sub

        Public Shared Sub ShowNotify(ByVal Message As String)
            Try
                Dim dlgContext As GUIDialogOK = CType(GUIWindowManager.GetWindow(CType(GUIWindow.Window.WINDOW_DIALOG_OK, Integer)), GUIDialogOK)
                dlgContext.Reset()
                dlgContext.SetHeading("Information")
                dlgContext.SetLine(1, Message)

                dlgContext.DoModal(GUIWindowManager.ActiveWindow)
                'GUIWindowManager.CloseCurrentWindow()

                dlgContext.Dispose()
                dlgContext.AllocResources()
            Catch ex As Exception
                MyLog.Error("[CSGuideHelper] [ShowNotify]: exception err: {0} stack: {1}", ex.Message, ex.StackTrace)
            End Try
        End Sub


        Friend Shared Function TvServerConnected() As Boolean
            'Prüfen ob TvServer online ist
            Try
                Dim _server As IList(Of Server) = Server.ListAll
                MyLog.Debug("[CSGuideHelper] [TvServerConnected]: TvServer found: {0}", _server(0).HostName)
                Return True
            Catch ex As Exception
                MyLog.Error("[CSGuideHelper] [TvServerConnected]: Server not found")
                Return False
            End Try
        End Function

        Friend Shared Function GetEpisodeString(ByVal myTVMovieProgram As TVMovieProgram) As String
            Dim mName As String = System.Reflection.MethodInfo.GetCurrentMethod.Name
            Dim episodeString As String = String.Empty
            Try
                If Not String.IsNullOrEmpty(myTVMovieProgram.ReferencedProgram.SeriesNum) Then
                    episodeString = " [" & myTVMovieProgram.ReferencedProgram.SeriesNum & "." &
                                    myTVMovieProgram.ReferencedProgram.EpisodeNum & " " &
                                    myTVMovieProgram.ReferencedProgram.EpisodeName & "]"
                End If
            Catch ex As Exception
                MyLog.Error(String.Format("[CSGuideHelper] [{0}]: Err: {1} stack: {2}", mName, ex.Message, ex.StackTrace))
            End Try
            Return episodeString
        End Function

        Friend Shared Function GetYearString(ByVal myTVMovieProgram As TVMovieProgram) As String
            Dim mName As String = System.Reflection.MethodInfo.GetCurrentMethod.Name
            ' Dim _mClass As String = GetType.ToString
            Dim yearString As String = String.Empty
            Try
                If Not myTVMovieProgram.ReferencedProgram.OriginalAirDate < New Date(1900, 1, 1) Then
                    If Not myTVMovieProgram.Country Is Nothing Then
                        If Not myTVMovieProgram.Country.Equals("") Then
                            yearString = " (" & myTVMovieProgram.Country & " " & myTVMovieProgram.ReferencedProgram.OriginalAirDate.Year & ")"
                        End If
                    End If
                Else
                    Dim _description = myTVMovieProgram.ReferencedProgram.Description
                    If _description.Length > 100 Then
                        Dim _pattern As String = ".*?Aus:\s+(?<year_string>.*?\s\d\d\d\d)"
                        Dim _match = Regex.Match(_description.Substring(0, 100), _pattern)
                        If _match.Success Then
                            yearString = " (" & _match.Groups("year_string").Value & ")"
                        End If
                    End If
                End If
            Catch ex As Exception
                MyLog.Error(String.Format("[CSGuideHelper] [{0}]: Err: {1} stack: {2}", mName, ex.Message, ex.StackTrace))

            End Try

            Return yearString

        End Function

        Friend Shared Sub Notify(ByVal message As String, Optional ByVal image As String = "")
            Try
                Dim dlgContext As GUIDialogNotify = CType(GUIWindowManager.GetWindow(CType(GUIWindow.Window.WINDOW_DIALOG_NOTIFY, Integer)), GUIDialogNotify)
                dlgContext.Reset()

                dlgContext.SetHeading("Clickfinder ProgramGuide")
                If String.IsNullOrEmpty(image) Then
                    dlgContext.SetImage(Config.GetFile(Config.Dir.Thumbs, "Clickfinder ProgramGuide\ClickfinderPG_logo.png"))
                Else
                    dlgContext.SetImage(image)
                End If

                dlgContext.SetText(message)
                dlgContext.TimeOut = 5

                dlgContext.DoModal(GUIWindowManager.ActiveWindow)
                dlgContext.Dispose()
                dlgContext.AllocResources()
            Catch ex As Exception
                MyLog.Error("[CSGuideHelper] [Notify]: exception err: {0} stack: {1}", ex.Message, ex.StackTrace)
            End Try
        End Sub
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
        Public Shared Sub imageCleaner(ByVal fileDir As String, Optional ByVal daysOld As Integer = -1)
            Dim mName As String = System.Reflection.MethodInfo.GetCurrentMethod.Name
            Dim directory As New IO.DirectoryInfo(fileDir)
            Dim deleteCounter As Integer = 0
            Dim numberOfFilesInDir As Integer = 0

            ' MyLog.Debug(String.Format("[CSGuideHelper] [{0}]: Started", mName, deleteCounter, fileDir))
            Try
                numberOfFilesInDir = directory.GetFiles().Count
                If numberOfFilesInDir <= 40000 Then
                    For Each file As IO.FileInfo In directory.GetFiles("*.jpg")
                        If (Now - file.CreationTime).Days > daysOld Then
                            file.Delete()
                            deleteCounter = deleteCounter + 1
                        End If
                    Next
                Else
                    MyLog.Warn(String.Format("[CSGuideHelper] [{0}]: There are {1} files in {2} - Did not clean up, too many ... ", mName, numberOfFilesInDir, fileDir))
                End If


                MyLog.Debug(String.Format("[CSGuideHelper] [{0}]: Deleted {1} files in {2}", mName, deleteCounter, fileDir))
            Catch ex As Exception
                MyLog.Error("[CSGuideHelper] [{0}]: exception err: {1} stack: {2}", mName, ex.Message, ex.StackTrace)
            End Try
            ' MyLog.Debug(String.Format("[CSGuideHelper] [{0}]: Ended", mName, deleteCounter, fileDir))
        End Sub

        Public Shared Sub checkSkinFolder()
            Dim mName As String = System.Reflection.MethodInfo.GetCurrentMethod.Name
            Dim myPath As String
            'Dim tmDbLibConfig As String
            'Dim tmDbLibConfigFile As String

            Try
                myPath = Path.Combine(Config.GetSubFolder(Config.Dir.Skin, Config.SkinName & "\media\CSGuide\Fanart\"))

                If (Not Directory.Exists(myPath)) Then
                    Directory.CreateDirectory(myPath)
                End If
                myPath = Path.Combine(Config.GetSubFolder(Config.Dir.Skin, Config.SkinName & "\media\CSGuide\Poster\"))
                If (Not Directory.Exists(myPath)) Then
                    Directory.CreateDirectory(myPath)
                End If
                myPath = Path.Combine(Config.GetSubFolder(Config.Dir.Skin, Config.SkinName & "\media\CSGuide\Actor\"))
                If (Not Directory.Exists(myPath)) Then
                    Directory.CreateDirectory(myPath)
                End If

                'tmDbLibConfigFile = Path.Combine(Config.GetSubFolder(Config.Dir.Skin, Config.SkinName & "\media\CSGuide\CSGuideTMDbLibConfig.xml"))
                'If (Not File.Exists(tmDbLibConfigFile)) Then
                '    tmDbLibConfig = "<?xml version=""1.0"" encoding=""utf-8""?><TMDbConfig><Images><BaseUrl>http://image.tmdb.org/t/p/</BaseUrl><SecureBaseUrl>https://image.tmdb.org/t/p/</SecureBaseUrl><PosterSizes><string>w92</string><string>w154</string><string>w185</string><string>w342</string><string>w500</string><string>w780</string><string>original</string></PosterSizes><BackdropSizes><string>w300</string><string>w780</string><string>w1280</string><string>original</string></BackdropSizes><ProfileSizes><string>w45</string><string>w185</string><string>h632</string><string>original</string></ProfileSizes><LogoSizes><string>w45</string><string>w92</string><string>w154</string><string>w185</string><string>w300</string><string>w500</string><string>original</string></LogoSizes></Images><ChangeKeys><string>adult</string><string>air_date</string><string>also_known_as</string><string>alternative_titles</string><string>biography</string><string>birthday</string><string>budget</string><string>cast</string><string>certifications</string><string>character_names</string><string>created_by</string><string>crew</string><string>deathday</string><string>episode</string><string>episode_number</string><string>episode_run_time</string><string>freebase_id</string><string>freebase_mid</string><string>general</string><string>genres</string><string>guest_stars</string><string>homepage</string><string>images</string><string>imdb_id</string><string>languages</string><string>name</string><string>network</string><string>origin_country</string><string>original_name</string><string>original_title</string><string>overview</string><string>parts</string><string>place_of_birth</string><string>plot_keywords</string><string>production_code</string><string>production_companies</string><string>production_countries</string><string>releases</string><string>revenue</string><string>runtime</string><string>season</string><string>season_number</string><string>season_regular</string><string>spoken_languages</string><string>status</string><string>tagline</string><string>title</string><string>translations</string><string>tvdb_id</string><string>tvrage_id</string><string>type</string><string>video</string><string>videos</string></ChangeKeys></TMDbConfig>"
                '    File.WriteAllText(tmDbLibConfigFile, tmDbLibConfig, Encoding.UTF8)
                'End If
                MyLog.Debug("[CSGuideHelper] [{0}]: Checked media directories: OK", mName)
            Catch ex As Exception
                MyLog.Error("[CSGuideHelper] [{0}]: exception err: {1} stack: {2}", mName, ex.Message, ex.StackTrace)
            End Try

        End Sub

        Friend Shared ReadOnly Property ratingStar(ByVal Program As Program) As Integer
            Get
                If Program.StarRating > 0 Then
                    Return Program.StarRating
                Else
                    Return 0
                End If
            End Get
        End Property
        Friend Shared ReadOnly Property TvMovieStar(ByVal TvMovieProgram As TVMovieProgram) As String
            Get
                If TvMovieProgram.ReferencedProgram.Description.Contains("Tages-Tipp") Then
                    Return "ClickfinderSG_Tagestipp.png"               
                ElseIf TvMovieProgram.TVMovieBewertung > 0 Then
                    Return "ClickfinderSG_R" & TvMovieProgram.TVMovieBewertung & ".png"
                Else
                    Return ""
                End If
            End Get
        End Property
        Friend Shared ReadOnly Property TimeLabel(ByVal TvMovieProgram As TVMovieProgram) As String
            Get
                Dim _percent100 As Long
                Dim _percentX As Long

                _percent100 = DateDiff(DateInterval.Minute, TvMovieProgram.ReferencedProgram.StartTime, TvMovieProgram.ReferencedProgram.EndTime)
                _percentX = DateDiff(DateInterval.Minute, TvMovieProgram.ReferencedProgram.StartTime, Date.Now)
                Return TvMovieProgram.ReferencedProgram.ReferencedChannel.ChannelNumber & ": " & TvMovieProgram.ReferencedProgram.ReferencedChannel.DisplayName & " " &
                                CStr(Format(TvMovieProgram.ReferencedProgram.StartTime.Hour, "00") & ":" &
                                            Format(TvMovieProgram.ReferencedProgram.StartTime.Minute, "00") & "-" &
                                            Format(TvMovieProgram.ReferencedProgram.EndTime.Hour, "00") & ":" &
                                            Format(TvMovieProgram.ReferencedProgram.EndTime.Minute, "00")
                                            )
            End Get
        End Property
        Friend Shared Sub SetProperty(ByVal [property] As String, ByVal value As String)
            If [property] Is Nothing Then
                Return
            End If

            'If the value is empty always add a space
            'otherwise the property will keep 
            'displaying it's previous value
            If [String].IsNullOrEmpty(value) Then
                value = " "
            End If

            GUIPropertyManager.SetProperty([property], value)
        End Sub

        Friend Shared ReadOnly Property Image(ByVal TvMovieProgram As TVMovieProgram) As String
            Get

                Dim _TvSeriesPoster As String = Config.GetFile(Config.Dir.Thumbs, "MPTVSeriesBanners\") & TvMovieProgram.SeriesPosterImage
                Dim _Cover As String = Config.GetFile(Config.Dir.Thumbs, "") & TvMovieProgram.Cover
                Dim _ChannelLogo As String = Config.GetFile(Config.Dir.Thumbs, "tv\logos\") & Replace(TvMovieProgram.ReferencedProgram.ReferencedChannel.DisplayName, "/", "_") & ".png"
                Dim _ClickfinderImage As String = CSGuideSettings.ClickfinderImagePath & "\" & TvMovieProgram.BildDateiname

                'Image zunächst auf SenderLogo festlegen
                Select Case TvMovieProgram.idSeries
                    Case Is = 0
                        If TvMovieProgram.local = True Then
                            If Not String.IsNullOrEmpty(TvMovieProgram.Cover) Then
                                'Movie/Video existiert lokal -> Cover zeigen
                                Return _Cover
                            Else
                                'Kein Cover existiert -> SenderLogo / Clickfinder Bild anzeigen
                                If Not String.IsNullOrEmpty(TvMovieProgram.BildDateiname) Then
                                    Return _ClickfinderImage
                                Else
                                    Return _ChannelLogo
                                End If

                            End If
                        Else
                            'Keine Serie/Movie/Video -> SenderLogo / Clickfinder Bild anzeigen
                            If Not String.IsNullOrEmpty(TvMovieProgram.BildDateiname) Then
                                Return _ClickfinderImage
                            Else
                                Return _ChannelLogo
                            End If
                        End If
                    Case Is > 0
                        If Not String.IsNullOrEmpty(TvMovieProgram.SeriesPosterImage) Then
                            If Not String.IsNullOrEmpty(TvMovieProgram.SeriesPosterImage) Then
                                'Serie erkannt -> Series Poster anzeigen
                                Return _TvSeriesPoster
                            Else
                                'Kein Cover existiert -> SenderLogo / Clickfinder Bild anzeigen
                                If Not String.IsNullOrEmpty(TvMovieProgram.BildDateiname) Then
                                    Return _ClickfinderImage
                                Else
                                    Return _ChannelLogo
                                End If
                            End If
                        Else
                            'Kein Cover existiert -> SenderLogo / Clickfinder Bild anzeigen
                            If Not String.IsNullOrEmpty(TvMovieProgram.BildDateiname) Then
                                Return _ClickfinderImage
                            Else
                                Return _ChannelLogo
                            End If
                        End If
                    Case Else
                        MyLog.Warn("[CSGuideHelper] [Image]: No Image found")
                        Return ""

                End Select
            End Get
        End Property

        Friend Shared ReadOnly Property RecordingStatus(ByVal program As Program) As String
            Get
                If program.IsRecording = True Or
                    program.IsRecordingManual = True Or
                    program.IsRecordingOnce = True Or
                    program.IsRecordingOncePending = True Then
                    Return "tvguide_record_button.png"
                ElseIf program.IsRecordingSeries = True Or program.IsRecordingSeriesPending = True Then
                    Return "tvguide_recordserie_button.png"
                Else
                    Return String.Empty
                End If

            End Get
        End Property


        Friend Shared ReadOnly Property DetailFSK(ByVal TvMovieProgram As TVMovieProgram) As String
            Get
                If TvMovieProgram.TVMovieBewertung > 0 And TvMovieProgram.TVMovieBewertung < 6 Then
                    Select Case TvMovieProgram.ReferencedProgram.ParentalRating
                        Case Is = 0
                            Return "Logos\ClickfinderSG\fsk0.png"
                        Case Is < 12
                            Return "Logos\ClickfinderSG\fsk6.png"
                        Case Is < 18
                            Return "Logos\ClickfinderSG\fsk12.png"
                        Case Is = 18
                            Return "Logos\ClickfinderSG\fsk18.png"
                        Case Else
                            Return String.Empty
                    End Select
                Else
                    Return String.Empty
                End If
            End Get

        End Property
    End Class
End Namespace
