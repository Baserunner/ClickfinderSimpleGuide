Imports TMDbLib.Objects.Search
Imports TMDbLib.Objects.Movies

Public Class CSGuideTMDBCacheItem
    Private m_movie As SearchMovie
    Private m_credit As Credits

    Private m_updateDate As Date
    Public Property updateDate() As Date
        Get
            Return m_updateDate
        End Get
        Set(ByVal value As Date)
            m_updateDate = value
        End Set
    End Property
    Public Property movie() As SearchMovie
        Get
            Return m_movie
        End Get
        Set(ByVal value As SearchMovie)
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
    Public Sub New(ByVal updateDate As Date, ByVal movie As SearchMovie, ByVal credit As Credits)
        m_updateDate = updateDate
        m_movie = movie
        m_credit = credit

    End Sub
End Class
