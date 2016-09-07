Imports TMDbLib.Objects.Movies

Public Class CSGuideTMDBCacheItem2
    Private m_movie As Movie
    Private m_credit As Credits
    Private m_filePaths As Dictionary(Of String, String)
    Private m_updateDate As Date

    Public Property FilePaths() As Dictionary(Of String, String)
        Get
            Return m_filePaths
        End Get
        Set(ByVal value As Dictionary(Of String, String))
            m_filePaths = value
        End Set
    End Property
    Public Property updateDate() As Date
        Get
            Return m_updateDate
        End Get
        Set(ByVal value As Date)
            m_updateDate = value
        End Set
    End Property
    Public Property movie() As Movie
        Get
            Return m_movie
        End Get
        Set(ByVal value As Movie)
            m_movie = value
        End Set
    End Property

    Public Property credit() As Credits
        Get
            Return m_credit
        End Get
        Set(ByVal value As Credits)
            m_credit = value
        End Set
    End Property
    Public Sub New(ByVal updateDate As Date, ByVal movie As Movie, ByVal credit As Credits, ByVal filePaths As Dictionary(Of String, String))
        m_updateDate = updateDate
        m_movie = movie
        m_credit = credit
        m_filePaths = filePaths

    End Sub
End Class
