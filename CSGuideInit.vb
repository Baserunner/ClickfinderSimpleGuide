Imports System
Imports System.IO
Imports System.Windows.Forms
Imports System.Collections.Generic
Imports System.Globalization
Imports System.Runtime.CompilerServices


Imports MediaPortal.GUI.Library
Imports MediaPortal.Dialogs
Imports MediaPortal.Profile
Imports MediaPortal.Configuration
Imports MediaPortal.Utils
Imports MediaPortal.Util
Imports TvDatabase
Imports MediaPortal.Player

Imports Gentle.Common
Imports TvPlugin

Imports MediaPortal.Database
'Imports SQLite.NET

Imports System.Threading

Imports Action = MediaPortal.GUI.Library.Action
Imports Gentle.Framework
'Imports System.Drawing
'Imports MediaPortal.Playlists

'Imports System.Timers

'Imports enrichEPG.TvDatabase
Imports System.Text
'Imports enrichEPG.Database

Namespace ClickfinderSimpleGuide
    <PluginIcons("ClickfinderSimpleGuide.CSG_Setup.png", "ClickfinderSimpleGuide.CSG_Setup_Disabled.png")> _
    Public Class CSGuideInit

        Inherits GUIWindow
        Implements ISetupForm

#Region "Skin Controls"

#End Region

#Region "Members"
        Private _stateTimer As System.Timers.Timer
        Friend Shared _OverlayStartupLoaded As Boolean = False
#End Region

#Region "iSetupFormImplementation"

        Public Overloads Overrides Property GetID() As Integer
            Get
                Return 730350
            End Get
            Set(ByVal value As Integer)
            End Set
        End Property
        Public Function PluginName() As String Implements MediaPortal.GUI.Library.ISetupForm.PluginName
            Return "Clickfinder Simple Guide"
        End Function
        Public Function Description() As String Implements MediaPortal.GUI.Library.ISetupForm.Description
            Return "EPG plugin based on Scroungers Clickfinder Program Guide."
        End Function
        Public Function Author() As String Implements MediaPortal.GUI.Library.ISetupForm.Author
            Return "Baserunner"
        End Function
        Public Sub ShowPlugin() Implements MediaPortal.GUI.Library.ISetupForm.ShowPlugin
            Dim setup As New CSGuideSetup
            setup.ShowDialog()
        End Sub
        Public Function CanEnable() As Boolean Implements MediaPortal.GUI.Library.ISetupForm.CanEnable
            Return True
        End Function
        Public Function GetWindowId() As Integer _
        Implements MediaPortal.GUI.Library.ISetupForm.GetWindowId

            Return 730350

        End Function
        Public Function DefaultEnabled() As Boolean Implements MediaPortal.GUI.Library.ISetupForm.DefaultEnabled
            Return True
        End Function
        Public Function HasSetup() As Boolean Implements MediaPortal.GUI.Library.ISetupForm.HasSetup
            Return True
        End Function
        Public Function GetHome(ByRef strButtonText As String, ByRef strButtonImage As String, ByRef strButtonImageFocus As String, ByRef strPictureImage As String) As Boolean Implements MediaPortal.GUI.Library.ISetupForm.GetHome
            Dim _layer As New TvBusinessLayer
            strButtonText = "Clickfinder Simple Guide"

            strButtonImage = String.Empty
            strButtonImageFocus = String.Empty
            strPictureImage = String.Empty

            Return True
        End Function
        Public Overloads Overrides Function Init() As Boolean
            'Beim initialisieren des Plugin den Screen laden
            'AddHandler GUIGraphicsContext.form.KeyDown, AddressOf MyBase.FormKeyDown
            Return Load(GUIGraphicsContext.Skin + "\ClickfinderSimpleGuideBoot.xml")
        End Function

#End Region


#Region "GUI Events"
        Public Overrides Sub PreInit()
            Dim _mName As String = System.Reflection.MethodInfo.GetCurrentMethod.Name
            Dim _mClass As String = Me.GetType.Name

            CSGuideSettings.init()
            enrichEPG.MySettings.SetSettings(Config.GetFile(Config.Dir.Database, ""), False, False, False,
                                             enrichEPG.MySettings.LogPath.Client, "enrichEPG.log", , , , True, , )
            Try
                MyLog.DebugModeOn = CSGuideSettings.DebugMode
                MyLog.BackupLogFiles()

                MyLog.Info(String.Format("[{0}] [{1}] *** Starting *** ", _mClass, _mName))
                MyLog.Info("[NiceEPG] [PreInit] *** DebugMode is " & CSGuideSettings.DebugMode & " *** ")
                If CSGuideSettings.DebugMode Then
                    CSGuideSettings.logAll()
                Else
                    MyLog.Info(String.Format("[{0}] [{1}] ***  DebugMode is " & CSGuideSettings.DebugMode & " *** ", _mClass, _mName))
                End If

                If CSGuideHelper.TvServerConnected = False Then
                    MyLog.Warn(String.Format("[{0}] [{1}]: TvServer not online", _mClass, _mName))
                ElseIf IO.Directory.Exists(CSGuideSettings.ClickfinderImagePath) = False Then
                    MyLog.Warn(String.Format("[{0}] [{1}]: ClickfinderImagePath nicht gefunden", _mClass, _mName))
                ElseIf CSGuideSettings.TVMovieIsEnabled = False Then
                    MyLog.Warn(String.Format("[{0}] [{1}]: TV Movie++ Plugin nicht Enabled", _mClass, _mName))
                End If

                MyBase.PreInit()

            Catch ex As Exception
                MyLog.Error(String.Format("[{0}] [{1}]: Exception err:" & ex.Message & " stack:" & ex.StackTrace, _mClass, _mName))
            End Try

        End Sub
        Protected Overrides Sub OnPageLoad()
            Dim _mName As String = System.Reflection.MethodInfo.GetCurrentMethod.Name
            Dim _mClass As String = Me.GetType.Name

            MyBase.OnPageLoad()
            GUIWindowManager.NeedRefresh()

            Try
                CSGuideHelper.CheckConnectionState()
                MyLog.Debug(String.Format("[{0}] [{1}]: Activating Window 730351", _mClass, _mName))
                GUIWindowManager.ActivateWindow(730351, True)

            Catch ex As Exception
                MyLog.Error(String.Format("[{0}] [{1}]: Exception err:" & ex.Message & " stack:" & ex.StackTrace, _mClass, _mName))
            End Try

        End Sub
        Public Overrides Sub OnAction(ByVal Action As MediaPortal.GUI.Library.Action)
            MyBase.OnAction(Action)
        End Sub
        Protected Overrides Sub OnPageDestroy(ByVal new_windowId As Integer)
            Dim _mName As String = System.Reflection.MethodInfo.GetCurrentMethod.Name
            Dim _mClass As String = Me.GetType.Name

            MyLog.Debug(String.Format("[{0}] [{1}]: New Window ID: " & new_windowId, _mClass, _mName))
            GC.Collect()
            MyBase.OnPageDestroy(new_windowId)

        End Sub
        Protected Overrides Sub Finalize()
            Dim _mName As String = System.Reflection.MethodInfo.GetCurrentMethod.Name
            Dim _mClass As String = Me.GetType.Name

            MyLog.Debug(String.Format("[{0}] [{1}]: Finalyzing... ", _mClass, _mName))
            MyBase.Finalize()
        End Sub
        Protected Overrides Sub OnClicked(ByVal controlId As Integer, _
                                          ByVal control As GUIControl, _
                                          ByVal actionType As  _
                                          MediaPortal.GUI.Library.Action.ActionType)

            MyBase.OnClicked(controlId, control, actionType)

        End Sub
#End Region

    End Class
End Namespace