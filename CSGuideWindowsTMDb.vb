Imports MediaPortal.GUI.Library


Namespace ClickfinderSimpleGuide
    Public Class CSGuideWindowsTMDb
        Inherits GUIWindow

#Region "Skin Controls"
        <SkinControlAttribute(60)> Protected FanartBackground As GUIImage = Nothing
        <SkinControlAttribute(61)> Protected FanartBackground2 As GUIImage = Nothing
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

        Private Shared m_backdrop As ImageSwapper
        Private Shared m_counter As Integer
        Private Shared m_filename As String

        Public Shared Property Filename() As String
            Get
                Return m_filename
            End Get
            Set(ByVal value As String)
                m_filename = value
            End Set
        End Property
#End Region
#Region "GUI Events"

        Protected Overrides Sub OnPageLoad()
            m_backdrop = New ImageSwapper
            m_counter = 0
            m_backdrop.PropertyOne = "#Fanart.1"
            m_backdrop.PropertyTwo = "#Fanart.2"

            m_backdrop.GUIImageOne = FanartBackground
            m_backdrop.GUIImageTwo = FanartBackground2
            LoadFanart(m_backdrop, m_filename)

        End Sub

        Protected Overrides Sub OnPageDestroy(ByVal new_windowId As Integer)

            MyBase.OnPageDestroy(new_windowId)
            Dispose()
            AllocResources()
        End Sub

        Public Overrides Sub OnAction(ByVal action As MediaPortal.GUI.Library.Action)
            Dim _mName As String = System.Reflection.MethodInfo.GetCurrentMethod.Name
            Dim _mClass As String = Me.GetType.Name
            'Dim _filename As String = Nothing

            If GUIWindowManager.ActiveWindow = GetID Then

                '(1) is pressed -> "Change Image"
                If action.wID = MediaPortal.GUI.Library.Action.ActionType.ACTION_KEY_PRESSED Then
                    If action.m_key IsNot Nothing Then
                        If action.m_key.KeyChar = 49 Then
                            'If m_counter Mod 2 = 0 Then
                            '    _filename = "C:\ProgramData\Team MediaPortal\MediaPortal\skin\Titan\Media\CSG\Fanart\3aWLsYLqRa8cmijBpJU3CDEmhoI.jpg"
                            'Else
                            '    _filename = "C:\ProgramData\Team MediaPortal\MediaPortal\skin\Titan\Media\CSG\Fanart\AkJCEvyQXoel61gxqd3jyV8QRsW.jpg"
                            'End If
                            'm_counter = m_counter + 1
                            LoadFanart(m_backdrop, m_filename)
                        End If
                    End If
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

    End Class

End Namespace

