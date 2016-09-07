Imports System.IO
Imports System.Xml
Imports System.Text

Imports MediaPortal.Configuration

Imports enrichEPG.TvDatabase

Imports Newtonsoft.Json

Imports TMDbLib.Client
Imports TMDbLib.Objects.General
Imports TMDbLib.Objects.Movies
Imports TMDbLib.Objects.Search
Namespace ClickfinderSimpleGuide


    Public Class CSGUideCacheHandler
        Private _cacheFile As String = Path.Combine(Config.GetSubFolder(Config.Dir.Skin, Config.SkinName & "\media\CSG\CSGuideTMDbCache.xml"))
        Private _lastUpdateFilename As String = Path.Combine(Config.GetSubFolder(Config.Dir.Skin, Config.SkinName & "\media\CSG\CSGuideTMDbCache_lastUpdate.txt"))
        Private _tmdbClient As TMDbClient
        Private _lastCacheUpdate As New Dictionary(Of String, Date)
        Private _cacheUpdateDict As New Dictionary(Of String, Date)

        Public Sub New(ByRef tmdbClient As TMDbClient)
            _tmdbClient = tmdbClient
            _cacheUpdateDict.Item("TMDbCache") = Date.Now
        End Sub

        Public Sub buildCache(ByRef tmdbCache As Dictionary(Of String, CSGuideTMDBCacheItem3),
                          ByVal itemsCache As List(Of TVMovieProgram), ByVal oldestEntryDate As Date)

            ' 1. Check if Cache-File already exists
            If File.Exists(_cacheFile) Then
                tmdbCache = JsonConvert.DeserializeObject(Of Dictionary(Of String, CSGuideTMDBCacheItem3))(File.ReadAllText(_cacheFile))
                ' 2. Remove the old entried
                removeOldEntries(oldestEntryDate, tmdbCache)
            End If

            ' 3: Check if Cache needs update (should be only updated once a day)
            If cacheNeedsUpdate("TMDbCache") Then
                ' 4: Update the CacheItems
                If updateCache(tmdbCache, itemsCache) Then
                    Dim myJasonCache As String = JsonConvert.SerializeObject(tmdbCache, Newtonsoft.Json.Formatting.Indented)
                    File.WriteAllText(_cacheFile, myJasonCache)
                End If
                '5: Write the lastUpdate-Date in a File            
                Dim myJasonCache2 As String = JsonConvert.SerializeObject(_cacheUpdateDict, Newtonsoft.Json.Formatting.Indented)
                File.WriteAllText(_lastUpdateFilename, myJasonCache2)
            End If


        End Sub
        Private Function cacheNeedsUpdate(cacheName As String) As Boolean

            If File.Exists(_lastUpdateFilename) Then
                _lastCacheUpdate = JsonConvert.DeserializeObject(Of Dictionary(Of String, Date))(File.ReadAllText(_lastUpdateFilename))
                If _lastCacheUpdate.Item(cacheName).Date < Date.Today Then
                    Return True
                Else
                    Return False
                End If
            End If
            Return True
        End Function
        Private Sub removeOldEntries(oldestEntryDate As Date, ByRef tmdbCache As Dictionary(Of String, CSGuideTMDBCacheItem3))
            For Each key As String In New List(Of String)(tmdbCache.Keys)
                If oldestEntryDate > tmdbCache(key).updateDate Then
                    tmdbCache.Remove(key)
                End If
            Next
        End Sub
        Public Function updateCache(ByRef tmdbCache As Dictionary(Of String, CSGuideTMDBCacheItem3), ByRef itemsCache As List(Of TVMovieProgram)) As Boolean
            Dim _mName As String = System.Reflection.MethodInfo.GetCurrentMethod.Name
            Dim _mClass As String = Me.GetType.Name

            Dim movieFound As SearchMovie = Nothing
            Dim credit As Credits = Nothing
            Dim updated As Boolean = False
            Dim tvmProgram As TVMovieProgram
            Dim movieGerman As Movie = Nothing

            FetchConfig(_tmdbClient)

            For Each tvmProgram In itemsCache
                If Not AddItem(tvmProgram, tmdbCache) Then Continue For
                updated = True
            Next
            Return updated
        End Function
        Public Function AddItem(ByVal tvmProgram As TVMovieProgram, ByRef tmdbCache As Dictionary(Of String, CSGuideTMDBCacheItem3)) As Boolean
            Dim _mName As String = System.Reflection.MethodInfo.GetCurrentMethod.Name
            Dim _mClass As String = Me.GetType.Name
            Dim title As String
            Dim year As Integer
            Dim movieFound As SearchMovie = Nothing
            Dim credit As Credits = Nothing
            Dim misc As New CSGuideTMDBCacheItemMisc
            Dim updated As Boolean = False

            Dim movieGerman As Movie = Nothing
            title = tvmProgram.ReferencedProgram.Title
            year = tvmProgram.ReferencedProgram.OriginalAirDate.Year
            Try
                If tmdbCache.ContainsKey(tvmProgram.idProgram) Then
                    'Console.WriteLine(title & " (" & year & ") already cached")
                    MyLog.Debug(String.Format("[{0}] [{1}]: {2}({3}) already cached", _mClass, _mName, title, year))
                    Return False
                End If
            Catch
            End Try

            Dim results As SearchContainer(Of SearchMovie) = _tmdbClient.SearchMovieAsync(title, 0, False, year).Result
            If results IsNot Nothing AndAlso results.Results.Count <> 0 Then
                movieFound = results.Results.First()
                movieGerman = _tmdbClient.GetMovieAsync(movieFound.Id, "de").Result
                credit = _tmdbClient.GetMovieCreditsAsync(movieFound.Id).Result
                misc = getMiscPart(movieGerman, tvmProgram)
            Else
                movieFound = Nothing
                credit = Nothing
            End If
            'Console.WriteLine("Caching " & title & " (" & year & "), Key: " & tvmProgram.idProgram)
            MyLog.Debug(String.Format("[{0}] [{1}]: Caching {2}({3})", _mClass, _mName, title, year))
            Try
                'tmdbCache.Add(tvmProgram.idProgram, New CSGuideTMDBCacheItem(Date.Today(), movieFound, credit))
                tmdbCache.Add(tvmProgram.idProgram, New CSGuideTMDBCacheItem3(Date.Today(), movieGerman, credit, misc))
            Catch
            End Try
            Return True
        End Function

        Private Function getMiscPart(ByVal movie As Movie, ByVal tvmProgram As TVMovieProgram) As CSGuideTMDBCacheItemMisc
            Dim filePaths As New Dictionary(Of String, String)
            Dim imageFilename As String
            Dim artURL As String
            Dim rgx As New System.Text.RegularExpressions.Regex(".*\/(.*)$")
            Dim misc As New CSGuideTMDBCacheItemMisc

            If Not movie.BackdropPath Is Nothing Then
                artURL = _tmdbClient.GetImageUrl("original", movie.BackdropPath).ToString()
                imageFilename = rgx.Match(artURL).Groups(1).Value
                If Not String.IsNullOrEmpty(imageFilename) Then
                    misc.absFanartPath = Path.Combine(Config.GetSubFolder(Config.Dir.Skin, Config.SkinName & "\Media\CSG\Fanart\"), imageFilename)
                    misc.relFanartPath = Path.Combine("CSG\Fanart\", imageFilename)
                    misc.fanartURL = _tmdbClient.GetImageUrl("original", movie.BackdropPath).ToString()
                End If
            End If
            If Not movie.PosterPath Is Nothing Then
                artURL = _tmdbClient.GetImageUrl("original", movie.PosterPath).ToString()
                imageFilename = rgx.Match(artURL).Groups(1).Value
                If Not String.IsNullOrEmpty(imageFilename) Then
                    misc.absPosterPath = Path.Combine(Config.GetSubFolder(Config.Dir.Skin, Config.SkinName & "\Media\CSG\Poster\"), imageFilename)
                    misc.relPosterPath = Path.Combine("CSG\Poster\", imageFilename)
                    misc.posterURL = _tmdbClient.GetImageUrl("original", movie.PosterPath).ToString()
                End If
            End If
            misc.title_string = tvmProgram.ReferencedProgram.Title & CSGuideHelper.GetYearString(tvmProgram)
            Return misc
        End Function

        Private Shared Sub FetchConfig(client As TMDbClient)
            Dim configXml As New FileInfo(Path.Combine(Config.GetSubFolder(Config.Dir.Skin, Config.SkinName & "\media\CSG\CSGuideTMDbLibConfig.xml")))

            Console.WriteLine("Config file: " & configXml.FullName & ", Exists: " & configXml.Exists)

            If configXml.Exists AndAlso configXml.LastWriteTimeUtc >= DateTime.UtcNow.AddHours(-1) Then
                ' Console.WriteLine("Using stored config")
                Dim xml As String = File.ReadAllText(configXml.FullName, Encoding.Unicode)

                Dim xmlDoc As New XmlDocument()
                xmlDoc.LoadXml(xml)

                client.SetConfig(Serializer.Deserialize(Of TMDbConfig)(xmlDoc))
            Else
                ' Console.WriteLine("Getting new config")
                client.GetConfig()

                ' Console.WriteLine("Storing config")
                Dim xmlDoc As XmlDocument = Serializer.Serialize(client.Config)
                File.WriteAllText(configXml.FullName, xmlDoc.OuterXml, Encoding.Unicode)
            End If
        End Sub
    End Class
End Namespace