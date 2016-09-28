Imports System
Imports System.IO

Imports MediaPortal.Profile
Imports MediaPortal.Configuration
Imports MediaPortal.GUI.Library
Imports MediaPortal.Util
Imports MediaPortal.Utils

Imports Gentle.Framework
Imports System.Drawing
Imports System.Threading
Imports System.Windows.Forms
Imports TvDatabase
Imports TvDatabase.TvBusinessLayer
Imports ClickfinderSimpleGuide.ClickfinderSimpleGuide
Imports System.Text.RegularExpressions


Public Class CSGuideSetup

    Private IsValidationError As Boolean = False
    Private currentTabIndex As Integer = 0


    Private Sub Setup_Load(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MyBase.Load

        MyLog.Info("*** [Setup] [Setup_Load] **** Starting ****")
        CSGuideSettings.init()
        Init_Controls()



    End Sub


    Private Sub Init_Controls()
        Gen_Label1.Text = "Clickfinder Simple Guide Version: "
        Gen_Label1A.Text = ClickfinderSimpleGuide.CSGuideHelper.Version
        Gen_Label2.Text = "TVMovie++ Plugin Version:"
        Gen_Label3.Text = "TVMovie++ Plugin Enabled:"
        Gen_Label4.Text = "TVMovie++ Last Update: "

        If String.IsNullOrEmpty(CSGuideSettings.TVMovieVersion) Then
            Gen_Label2A.Text = "*** TVMovie++ Plugin Version not found ***"
            Gen_Label2A.ForeColor = Color.Red
        Else
            Gen_Label2A.Text = CSGuideSettings.TVMovieVersion
            Gen_Label2A.ForeColor = Color.Green
        End If

        Gen_Label3A.Text = StrConv(CSGuideSettings.TVMovieIsEnabled, VbStrConv.ProperCase)
        If StrConv(CSGuideSettings.TVMovieIsEnabled, VbStrConv.ProperCase).Equals("False") Then
            Gen_Label3A.ForeColor = Color.Red
        Else
            Gen_Label3A.ForeColor = Color.Green
        End If


        If String.IsNullOrEmpty(CSGuideSettings.TVMovieLastUpdate) Then
            Gen_Label4A.Text = "*** No Last Update Date Set ***"
            Gen_Label4A.ForeColor = Color.Red
        Else
            Gen_Label4A.Text = CSGuideSettings.TVMovieLastUpdate
            Gen_Label4A.ForeColor = Color.Green
        End If
        Gen_TextBox2.Text = CSGuideSettings.TVMovieDatabasePath


        Gen_ComboBox1.Text = "View " & CSGuideSettings.StartView
        Gen_TextBox1.Text = CSGuideSettings.ClickfinderImagePath
        'Gen_TextBox3.Text = CSGuideSettings.TMDbAPIKey

        If CSGuideSettings.DebugMode Then
            Gen_CheckBox1.Checked = True
        Else
            Gen_CheckBox1.Checked = False
        End If
        If CSGuideSettings.HiddenMenuMode Then
            Gen_CheckBox2.Checked = True
        Else
            Gen_CheckBox2.Checked = False
        End If
        V0_TextBox1.Text = CSGuideSettings.View(0).DisplayName
        V0_TextBox2.Text = CSGuideSettings.View(0).TimeString
        V0_TextBox3.Text = CSGuideSettings.View(0).SQL
        V0_TextBox4.Text = CSGuideSettings.View(0).OffSetMinute
        V0_ComboBox1.Text = CSGuideSettings.View(0).TvGroup
        V0_ComboBox2.Text = CSGuideSettings.View(0).Type


        V1_TextBox1.Text = CSGuideSettings.View(1).DisplayName
        V1_TextBox2.Text = CSGuideSettings.View(1).TimeString
        V1_TextBox3.Text = CSGuideSettings.View(1).SQL
        V1_TextBox4.Text = CSGuideSettings.View(1).OffSetMinute
        V1_ComboBox1.Text = CSGuideSettings.View(1).TvGroup
        V1_ComboBox2.Text = CSGuideSettings.View(1).Type
        V1_CheckBox1.Checked = CSGuideSettings.View(1).UseTMDb

        V2_TextBox1.Text = CSGuideSettings.View(2).DisplayName
        V2_TextBox2.Text = CSGuideSettings.View(2).TimeString
        V2_TextBox3.Text = CSGuideSettings.View(2).SQL
        V2_TextBox4.Text = CSGuideSettings.View(2).OffSetMinute
        V2_ComboBox1.Text = CSGuideSettings.View(2).TvGroup
        V2_ComboBox2.Text = CSGuideSettings.View(2).Type
        V2_CheckBox1.Checked = CSGuideSettings.View(2).UseTMDb

        V3_TextBox1.Text = CSGuideSettings.View(3).DisplayName
        V3_TextBox2.Text = CSGuideSettings.View(3).TimeString
        V3_TextBox3.Text = CSGuideSettings.View(3).SQL
        V3_TextBox4.Text = CSGuideSettings.View(3).OffSetMinute
        V3_ComboBox1.Text = CSGuideSettings.View(3).TvGroup
        V3_ComboBox2.Text = CSGuideSettings.View(3).Type
        V3_CheckBox1.Checked = CSGuideSettings.View(3).UseTMDb

        V4_TextBox1.Text = CSGuideSettings.View(4).DisplayName
        V4_TextBox2.Text = CSGuideSettings.View(4).TimeString
        V4_TextBox3.Text = CSGuideSettings.View(4).SQL
        V4_TextBox4.Text = CSGuideSettings.View(4).OffSetMinute
        V4_ComboBox1.Text = CSGuideSettings.View(4).TvGroup
        V4_ComboBox2.Text = CSGuideSettings.View(4).Type
        V4_CheckBox1.Checked = CSGuideSettings.View(4).UseTMDb

        V5_TextBox1.Text = CSGuideSettings.View(5).DisplayName
        V5_TextBox2.Text = CSGuideSettings.View(5).TimeString
        V5_TextBox3.Text = CSGuideSettings.View(5).SQL
        V5_TextBox4.Text = CSGuideSettings.View(5).OffSetMinute
        V5_ComboBox1.Text = CSGuideSettings.View(5).TvGroup
        V5_ComboBox2.Text = CSGuideSettings.View(5).Type
        V5_CheckBox1.Checked = CSGuideSettings.View(5).UseTMDb

        V6_TextBox1.Text = CSGuideSettings.View(6).DisplayName
        V6_TextBox2.Text = CSGuideSettings.View(6).TimeString
        V6_TextBox3.Text = CSGuideSettings.View(6).SQL
        V6_TextBox4.Text = CSGuideSettings.View(6).OffSetMinute
        V6_ComboBox1.Text = CSGuideSettings.View(6).TvGroup
        V6_ComboBox2.Text = CSGuideSettings.View(6).Type
        V6_CheckBox1.Checked = CSGuideSettings.View(6).UseTMDb

        V7_TextBox1.Text = CSGuideSettings.View(7).DisplayName
        V7_TextBox2.Text = CSGuideSettings.View(7).TimeString
        V7_TextBox3.Text = CSGuideSettings.View(7).SQL
        V7_TextBox4.Text = CSGuideSettings.View(7).OffSetMinute
        V7_ComboBox1.Text = CSGuideSettings.View(7).TvGroup
        V7_ComboBox2.Text = CSGuideSettings.View(7).Type
        V7_CheckBox1.Checked = CSGuideSettings.View(7).UseTMDb

        V8_TextBox1.Text = CSGuideSettings.View(8).DisplayName
        V8_TextBox2.Text = CSGuideSettings.View(8).TimeString
        V8_TextBox3.Text = CSGuideSettings.View(8).SQL
        V8_TextBox4.Text = CSGuideSettings.View(8).OffSetMinute
        V8_ComboBox1.Text = CSGuideSettings.View(8).TvGroup
        V8_ComboBox2.Text = CSGuideSettings.View(8).Type
        V8_CheckBox1.Checked = CSGuideSettings.View(8).UseTMDb

        For Each _channelGroup In ChannelGroup.ListAll
            V0_ComboBox1.Items.Add(_channelGroup.GroupName)
            V1_ComboBox1.Items.Add(_channelGroup.GroupName)
            V2_ComboBox1.Items.Add(_channelGroup.GroupName)
            V3_ComboBox1.Items.Add(_channelGroup.GroupName)
            V4_ComboBox1.Items.Add(_channelGroup.GroupName)
            V5_ComboBox1.Items.Add(_channelGroup.GroupName)
            V6_ComboBox1.Items.Add(_channelGroup.GroupName)
            V7_ComboBox1.Items.Add(_channelGroup.GroupName)
            V8_ComboBox1.Items.Add(_channelGroup.GroupName)
        Next
    End Sub

    Private Sub Button_Save_Click(sender As Object, e As EventArgs) Handles Button_Save.Click

        If IsValidationError Then
            MessageBox.Show("Bitte Einträge überprüfen", "Validation Error", MessageBoxButtons.OK,
                             MessageBoxIcon.Exclamation,
                             MessageBoxDefaultButton.Button1)
            Exit Sub
        End If

        Dim _lastCharAsString As String = Gen_ComboBox1.Text.Last()
        CSGuideSettings.StartView = CInt(_lastCharAsString)
        CSGuideSettings.ClickfinderImagePath = Gen_TextBox1.Text
        'CSGuideSettings.TMDbAPIKey = Gen_TextBox3.Text

        If Gen_CheckBox1.Checked Then
            CSGuideSettings.DebugMode = True
        Else
            CSGuideSettings.DebugMode = False
        End If

        If Gen_CheckBox2.Checked Then
            CSGuideSettings.HiddenMenuMode = True
        Else
            CSGuideSettings.HiddenMenuMode = False
        End If

        CSGuideSettings.View(0).DisplayName = V0_TextBox1.Text
        CSGuideSettings.View(0).TimeString = V0_TextBox2.Text
        CSGuideSettings.View(0).SQL = V0_TextBox3.Text
        CSGuideSettings.View(0).OffSetMinute = V0_TextBox4.Text
        CSGuideSettings.View(0).TvGroup = V0_ComboBox1.Text
        CSGuideSettings.View(0).Type = V0_ComboBox2.Text

        CSGuideSettings.View(1).DisplayName = V1_TextBox1.Text
        CSGuideSettings.View(1).TimeString = V1_TextBox2.Text
        CSGuideSettings.View(1).SQL = V1_TextBox3.Text
        CSGuideSettings.View(1).OffSetMinute = V1_TextBox4.Text
        CSGuideSettings.View(1).TvGroup = V1_ComboBox1.Text
        CSGuideSettings.View(1).Type = V1_ComboBox2.Text
        CSGuideSettings.View(1).UseTMDb = V1_CheckBox1.Checked

        CSGuideSettings.View(2).DisplayName = V2_TextBox1.Text
        CSGuideSettings.View(2).TimeString = V2_TextBox2.Text
        CSGuideSettings.View(2).SQL = V2_TextBox3.Text
        CSGuideSettings.View(2).OffSetMinute = V2_TextBox4.Text
        CSGuideSettings.View(2).TvGroup = V2_ComboBox1.Text
        CSGuideSettings.View(2).Type = V2_ComboBox2.Text
        CSGuideSettings.View(2).UseTMDb = V2_CheckBox1.Checked

        CSGuideSettings.View(3).DisplayName = V3_TextBox1.Text
        CSGuideSettings.View(3).TimeString = V3_TextBox2.Text
        CSGuideSettings.View(3).SQL = V3_TextBox3.Text
        CSGuideSettings.View(3).OffSetMinute = V3_TextBox4.Text
        CSGuideSettings.View(3).TvGroup = V3_ComboBox1.Text
        CSGuideSettings.View(3).Type = V3_ComboBox2.Text
        CSGuideSettings.View(3).UseTMDb = V3_CheckBox1.Checked

        CSGuideSettings.View(4).DisplayName = V4_TextBox1.Text
        CSGuideSettings.View(4).TimeString = V4_TextBox2.Text
        CSGuideSettings.View(4).SQL = V4_TextBox3.Text
        CSGuideSettings.View(4).OffSetMinute = V4_TextBox4.Text
        CSGuideSettings.View(4).TvGroup = V4_ComboBox1.Text
        CSGuideSettings.View(4).Type = V4_ComboBox2.Text
        CSGuideSettings.View(4).UseTMDb = V4_CheckBox1.Checked

        CSGuideSettings.View(5).DisplayName = V5_TextBox1.Text
        CSGuideSettings.View(5).TimeString = V5_TextBox2.Text
        CSGuideSettings.View(5).SQL = V5_TextBox3.Text
        CSGuideSettings.View(5).OffSetMinute = V5_TextBox4.Text
        CSGuideSettings.View(5).TvGroup = V5_ComboBox1.Text
        CSGuideSettings.View(5).Type = V5_ComboBox2.Text
        CSGuideSettings.View(5).UseTMDb = V5_CheckBox1.Checked

        CSGuideSettings.View(6).DisplayName = V6_TextBox1.Text
        CSGuideSettings.View(6).TimeString = V6_TextBox2.Text
        CSGuideSettings.View(6).SQL = V6_TextBox3.Text
        CSGuideSettings.View(6).OffSetMinute = V6_TextBox4.Text
        CSGuideSettings.View(6).TvGroup = V6_ComboBox1.Text
        CSGuideSettings.View(6).Type = V6_ComboBox2.Text
        CSGuideSettings.View(6).UseTMDb = V6_CheckBox1.Checked

        CSGuideSettings.View(7).DisplayName = V7_TextBox1.Text
        CSGuideSettings.View(7).TimeString = V7_TextBox2.Text
        CSGuideSettings.View(7).SQL = V7_TextBox3.Text
        CSGuideSettings.View(7).OffSetMinute = V7_TextBox4.Text
        CSGuideSettings.View(7).TvGroup = V7_ComboBox1.Text
        CSGuideSettings.View(7).Type = V7_ComboBox2.Text
        CSGuideSettings.View(7).UseTMDb = V7_CheckBox1.Checked

        CSGuideSettings.View(8).DisplayName = V8_TextBox1.Text
        CSGuideSettings.View(8).TimeString = V8_TextBox2.Text
        CSGuideSettings.View(8).SQL = V8_TextBox3.Text
        CSGuideSettings.View(8).OffSetMinute = V8_TextBox4.Text
        CSGuideSettings.View(8).TvGroup = V8_ComboBox1.Text
        CSGuideSettings.View(8).Type = V8_ComboBox2.Text
        CSGuideSettings.View(8).UseTMDb = V8_CheckBox1.Checked

        If CSGuideSettings.saveToXml() Then
            MsgBox("Successfully saved the Configuration",
               MsgBoxStyle.OkOnly Or MsgBoxStyle.Information,
               "Saving Config")
        Else
            MsgBox("Problems saving the Configuration - Check the Log",
                MsgBoxStyle.OkOnly Or MsgBoxStyle.Exclamation,
                "Saving Config")
        End If

    End Sub

    Private Sub Button2_Click(sender As Object, e As EventArgs) Handles Button2.Click
        Me.Close()
    End Sub

    Private Sub V1_TextBox2_Leave(sender As Object, e As System.EventArgs) Handles V1_TextBox2.Leave
        Dim r As Regex = New Regex("^\d\d\:\d\d$")
        Dim m As Match = r.Match(V1_TextBox2.Text)
        currentTabIndex = TabControl1.SelectedIndex
        If m.Success Or V1_TextBox2.Text.Equals("Now") Then
            V1_TextBox2.ForeColor = Color.Black
            V1_TextBox2.BackColor = Color.White
            ErrorProvider1.SetError(V1_TextBox2, String.Empty)
            IsValidationError = False
        Else
            ErrorProvider1.SetError(V1_TextBox2, "Erlaubt sind ""hh:mm"" und ""Now""")
            IsValidationError = True
            V1_TextBox2.Focus()
            V1_TextBox2.Select(0, V1_TextBox2.Text.Length)
            V1_TextBox2.ForeColor = Color.White
            V1_TextBox2.BackColor = Color.Red
        End If

    End Sub

    Private Sub V1_TextBox4_Leave(sender As Object, e As System.EventArgs) Handles V1_TextBox4.Leave
        Dim r As Regex = New Regex("^\-{0,1}\d+$")
        Dim m As Match = r.Match(V1_TextBox4.Text)
        If m.Success Then
            V1_TextBox4.ForeColor = Color.Black
            V1_TextBox4.BackColor = Color.White
            ErrorProvider2.SetError(V1_TextBox4, String.Empty)
            IsValidationError = False
        Else
            ErrorProvider2.SetError(V1_TextBox4, "Erlaubt ist eine positive oder negative ganze Zahl")
            IsValidationError = True
            V1_TextBox4.Focus()
            V1_TextBox4.Select(0, V1_TextBox4.Text.Length)
            V1_TextBox4.ForeColor = Color.White
            V1_TextBox4.BackColor = Color.Red
        End If

    End Sub

    Private Sub TabControl1_Deselecting(sender As Object, e As TabControlCancelEventArgs) _
         Handles TabControl1.Deselecting

        ' Don't allow Tab Change in case there is a validation error
        If IsValidationError Then
            e.Cancel = True
        End If
    End Sub
    Private Sub Gen_Button1_Click(sender As Object, e As EventArgs) Handles Gen_Button1.Click
        'If Not String.IsNullOrEmpty(Gen_TextBox2.Text) Then
        '    Try
        '        FolderBrowserDialog1.RootFolder = Path.GetDirectoryName(Gen_TextBox2.Text)
        '    Catch
        '        FolderBrowserDialog1.RootFolder = ""
        '    End Try
        'End If        
        If FolderBrowserDialog1.ShowDialog() = System.Windows.Forms.DialogResult.OK Then
            Gen_TextBox1.Text = FolderBrowserDialog1.SelectedPath
        End If

    End Sub
    Private Sub Gen_Button2_Click(sender As Object, e As EventArgs) Handles Gen_Button2.Click
        OpenFileDialog1.InitialDirectory = Path.GetDirectoryName(Gen_TextBox2.Text)
        OpenFileDialog1.FileName = Path.GetFileName(Gen_TextBox2.Text)
        If OpenFileDialog1.ShowDialog() = System.Windows.Forms.DialogResult.OK Then
            Gen_TextBox2.Text = OpenFileDialog1.FileName
        End If
    End Sub

    Private Sub Button3_Click(sender As Object, e As EventArgs) Handles Button3.Click
        CSGuideSettings.setDefault()
        Init_Controls()
    End Sub

    Private Sub V1_Button1_Click(sender As Object, e As EventArgs) Handles V1_Button1.Click
        If V1_Button1.Text.Equals("UnLocked") Then
            V1_Button1.Text = "Locked"
            V1_Button1.BackColor = Color.Red
            V1_Button1.ForeColor = Color.White
            V1_TextBox3.ReadOnly = True
        Else
            V1_Button1.Text = "UnLocked"
            V1_Button1.BackColor = Color.Lime
            V1_Button1.ForeColor = Color.Black
            V1_TextBox3.ReadOnly = False
        End If

    End Sub

    Private Sub V2_Button1_Click(sender As Object, e As EventArgs) Handles V2_Button1.Click
        If V2_Button1.Text.Equals("UnLocked") Then
            V2_Button1.Text = "Locked"
            V2_Button1.BackColor = Color.Red
            V2_Button1.ForeColor = Color.White
            V2_TextBox3.ReadOnly = True
        Else
            V2_Button1.Text = "UnLocked"
            V2_Button1.BackColor = Color.Lime
            V2_Button1.ForeColor = Color.Black
            V2_TextBox3.ReadOnly = False
        End If
    End Sub

    Private Sub V3_Button1_Click(sender As Object, e As EventArgs) Handles V3_Button1.Click
        If V3_Button1.Text.Equals("UnLocked") Then
            V3_Button1.Text = "Locked"
            V3_Button1.BackColor = Color.Red
            V3_Button1.ForeColor = Color.White
            V3_TextBox3.ReadOnly = True
        Else
            V3_Button1.Text = "UnLocked"
            V3_Button1.BackColor = Color.Lime
            V3_Button1.ForeColor = Color.Black
            V3_TextBox3.ReadOnly = False
        End If
    End Sub

    Private Sub V4_Button1_Click(sender As Object, e As EventArgs) Handles V4_Button1.Click
        If V4_Button1.Text.Equals("UnLocked") Then
            V4_Button1.Text = "Locked"
            V4_Button1.BackColor = Color.Red
            V4_Button1.ForeColor = Color.White
            V4_TextBox3.ReadOnly = True
        Else
            V4_Button1.Text = "UnLocked"
            V4_Button1.BackColor = Color.Lime
            V4_Button1.ForeColor = Color.Black
            V4_TextBox3.ReadOnly = False
        End If
    End Sub

    Private Sub V5_Button1_Click(sender As Object, e As EventArgs) Handles V5_Button1.Click
        If V5_Button1.Text.Equals("UnLocked") Then
            V5_Button1.Text = "Locked"
            V5_Button1.BackColor = Color.Red
            V5_Button1.ForeColor = Color.White
            V5_TextBox3.ReadOnly = True
        Else
            V5_Button1.Text = "UnLocked"
            V5_Button1.BackColor = Color.Lime
            V5_Button1.ForeColor = Color.Black
            V5_TextBox3.ReadOnly = False
        End If
    End Sub

    Private Sub V6_Button1_Click(sender As Object, e As EventArgs) Handles V6_Button1.Click
        If V6_Button1.Text.Equals("UnLocked") Then
            V6_Button1.Text = "Locked"
            V6_Button1.BackColor = Color.Red
            V6_Button1.ForeColor = Color.White
            V6_TextBox3.ReadOnly = True
        Else
            V6_Button1.Text = "UnLocked"
            V6_Button1.BackColor = Color.Lime
            V6_Button1.ForeColor = Color.Black
            V6_TextBox3.ReadOnly = False
        End If
    End Sub

    Private Sub V7_Button1_Click(sender As Object, e As EventArgs) Handles V7_Button1.Click
        If V7_Button1.Text.Equals("UnLocked") Then
            V7_Button1.Text = "Locked"
            V7_Button1.BackColor = Color.Red
            V7_Button1.ForeColor = Color.White
            V7_TextBox3.ReadOnly = True
        Else
            V7_Button1.Text = "UnLocked"
            V7_Button1.BackColor = Color.Lime
            V7_Button1.ForeColor = Color.Black
            V7_TextBox3.ReadOnly = False
        End If
    End Sub

    Private Sub V8_Button1_Click(sender As Object, e As EventArgs) Handles V8_Button1.Click
        If V8_Button1.Text.Equals("UnLocked") Then
            V8_Button1.Text = "Locked"
            V8_Button1.BackColor = Color.Red
            V8_Button1.ForeColor = Color.White
            V8_TextBox3.ReadOnly = True
        Else
            V8_Button1.Text = "UnLocked"
            V8_Button1.BackColor = Color.Lime
            V8_Button1.ForeColor = Color.Black
            V8_TextBox3.ReadOnly = False
        End If
    End Sub

    Private Sub LinkLabel1_LinkClicked(sender As Object, e As LinkLabelLinkClickedEventArgs) Handles LinkLabel1.LinkClicked
        'LinkLabel1.Links.Add(New LinkLabel.Link().LinkData = "http://www.ewe-software.de/download.html#tvmovieclickfinder")
        System.Diagnostics.Process.Start("http://www.ewe-software.de/download.html#tvmovieclickfinder")
        'System.Diagnostics.Process.Start(e.Link.LinkData.ToString())
    End Sub

    Private Sub Gen_Button3_Click(sender As Object, e As EventArgs) Handles Gen_Button3.Click
        Dim cacheFile As String = Path.Combine(Config.GetSubFolder(Config.Dir.Skin, Config.SkinName & "\media\CSGuide\CSGuideTMDbCache.json"))
        Dim lastUpdateFilename As String = Path.Combine(Config.GetSubFolder(Config.Dir.Skin, Config.SkinName & "\media\CSGuide\CSGuideTMDbCache_lastUpdate.json"))
        Utils.FileDelete(cacheFile)
        Utils.FileDelete(lastUpdateFilename)
        CSGuideHelper.imageCleaner(Path.Combine(Config.GetSubFolder(Config.Dir.Skin, Config.SkinName & "\media\CSGuide\Actor")))
        CSGuideHelper.imageCleaner(Path.Combine(Config.GetSubFolder(Config.Dir.Skin, Config.SkinName & "\media\CSGuide\Poster")))
        CSGuideHelper.imageCleaner(Path.Combine(Config.GetSubFolder(Config.Dir.Skin, Config.SkinName & "\media\CSGuide\Fanart")))
    End Sub
End Class