Imports TMDbLib.Objects.Movies

Public Class CSGuideTMDBCacheItem
    Private _movie As Movie
    Private _credit As Credits
    Private _misc As CSGuideTMDBCacheItemMisc
    Private _keepUntilDate As Date

    Public Property Misc() As CSGuideTMDBCacheItemMisc
        Get
            Return _misc
        End Get
        Set(ByVal value As CSGuideTMDBCacheItemMisc)
            _misc = value
        End Set
    End Property

    Public Property keepUntilDate() As Date
        Get
            Return _keepUntilDate
        End Get
        Set(ByVal value As Date)
            _keepUntilDate = value
        End Set
    End Property
    Public Property movie() As Movie
        Get
            Return _movie
        End Get
        Set(ByVal value As Movie)
            _movie = value
        End Set
    End Property

    Public Property credit() As Credits
        Get
            Return _credit
        End Get
        Set(ByVal value As Credits)
            _credit = value
        End Set
    End Property
    Public Sub New(ByVal keepUntilDate As Date, ByVal movie As Movie, ByVal credit As Credits, ByVal misc As CSGuideTMDBCacheItemMisc)
        _keepUntilDate = keepUntilDate
        _movie = movie
        _credit = credit
        _misc = misc

    End Sub
End Class
