Imports MediaPortal.Profile
Imports MediaPortal.Configuration

'Imports System.Threading
'Imports System.Globalization

'Imports System.Reflection


Namespace ClickfinderSimpleGuide
    Public Class CSGuideView
        Private m_viewName As String
        Private m_viewType As String
        Private m_viewSQL As String
        Private m_viewStartTimeDate As Date
        Private m_viewTvGroup As String
        Private m_viewDisplayName As String
        Private m_viewStartTime As String
        Private m_viewStartTimeOffSet As String
        Private m_useTMDb As Boolean

        Public Property UseTMDb() As Boolean
            Get
                Return m_useTMDb
            End Get
            Set(ByVal value As Boolean)
                m_useTMDb = value
            End Set
        End Property

        Public Property OffSetMinute() As String
            Get
                Return m_viewStartTimeOffSet
            End Get
            Set(ByVal value As String)
                m_viewStartTimeOffSet = value
            End Set
        End Property

        Public Property TimeString() As String
            Get
                Return m_viewStartTime
            End Get
            Set(ByVal value As String)
                m_viewStartTime = value
            End Set
        End Property


        Public Property DisplayName() As String
            Get
                Return m_viewDisplayName
            End Get
            Set(ByVal value As String)
                m_viewDisplayName = value
            End Set
        End Property


        Public Property TvGroup() As String
            Get
                Return m_viewTvGroup
            End Get
            Set(ByVal value As String)
                m_viewTvGroup = value
            End Set
        End Property


        Public Sub New(ByVal myName As String,
                    ByVal myType As String,
                    ByVal mySQL As String,
                    ByVal myStartTime As String,
                    ByVal myTvGroup As String,
                    ByVal myDisplayName As String,
                    ByVal myStartTimeOffSet As String,
                    ByVal myUseTMDb As Boolean
                       )

            m_viewName = myName
            m_viewType = myType
            m_viewSQL = mySQL
            m_viewStartTime = myStartTime
            m_viewTvGroup = myTvGroup
            m_viewDisplayName = myDisplayName
            m_viewStartTimeOffSet = myStartTimeOffSet
            m_useTMDb = myUseTMDb

        End Sub


        Public Property Name() As String
            Get
                Return m_viewName
            End Get
            Set(ByVal value As String)
                m_viewName = value
            End Set
        End Property

        Public Property Type() As String
            Get
                Return m_viewType
            End Get
            Set(ByVal value As String)
                m_viewType = value
            End Set
        End Property

        Public Property SQL() As String
            Get
                Return m_viewSQL
            End Get
            Set(ByVal value As String)
                m_viewSQL = value
            End Set
        End Property

        Public Property StartTime() As Date
            Get
                Return setViewDate(m_viewStartTime, Convert.ToInt32(m_viewStartTimeOffSet))
            End Get
            Set(ByVal value As Date)
                m_viewStartTimeDate = value
            End Set
        End Property

        Private Function setViewDate(ByVal myTime As String, ByVal myOffset As String) As Date
            If myTime.Equals("Now") Then
                Return Date.Now.AddMinutes(Convert.ToInt32(myOffset))
            Else
                Dim hour_minute() As String = Split(myTime, ":")
                Return Date.Today.AddHours(Convert.ToDouble(hour_minute(0))).AddMinutes(Convert.ToDouble(hour_minute(1))).AddMinutes(myOffset)
            End If
        End Function
    End Class
End Namespace