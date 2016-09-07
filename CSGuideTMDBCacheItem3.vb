Imports TMDbLib.Objects.Movies

Public Class CSGuideTMDBCacheItem3
    Private m_movie As Movie
    Private m_credit As Credits
    Private m_misc As CSGuideTMDBCacheItemMisc
    Private m_updateDate As Date

    Public Property Misc() As CSGuideTMDBCacheItemMisc
        Get
            Return m_misc
        End Get
        Set(ByVal value As CSGuideTMDBCacheItemMisc)
            m_misc = value
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
    Public Sub New(ByVal updateDate As Date, ByVal movie As Movie, ByVal credit As Credits, ByVal misc As CSGuideTMDBCacheItemMisc)
        m_updateDate = updateDate
        m_movie = movie
        m_credit = credit
        m_misc = misc

    End Sub
End Class
Public Class Class1

End Class
