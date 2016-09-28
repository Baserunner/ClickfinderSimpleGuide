Imports System.IO
Imports System.Xml
Imports System.Text
Imports System.Text.RegularExpressions

Imports MediaPortal.Configuration
Imports MediaPortal.Util

Imports enrichEPG.TvDatabase

Imports Newtonsoft.Json

Imports TMDbLib.Client
Imports TMDbLib.Objects.General
Imports TMDbLib.Objects.Movies
Imports TMDbLib.Objects.Search
Imports System.Threading

Namespace ClickfinderSimpleGuide


    Public Class CSGuideCacheHandler
        Private _cacheFile As String = Path.Combine(Config.GetSubFolder(Config.Dir.Skin, Config.SkinName & "\media\CSGuide\CSGuideTMDbCache.json"))
        Private _lastUpdateFilename As String = Path.Combine(Config.GetSubFolder(Config.Dir.Skin, Config.SkinName & "\media\CSGuide\CSGuideTMDbCache_lastUpdate.json"))
        Private _tmdbClient As TMDbClient
        Private _lastCacheUpdate As New Dictionary(Of String, Date)
        Private _mClass As String
        Private _cacheLimit As Integer = 40


        Public Sub New(ByRef tmdbClient As TMDbClient)
            _mClass = [GetType].Name
            _tmdbClient = tmdbClient
        End Sub

        Public Sub buildCache(ByRef tmdbCache As Dictionary(Of String, CSGuideTMDBCacheItem),
                          ByVal itemsCache As List(Of TVMovieProgram), viewName As String)
            Dim mName As String = System.Reflection.MethodInfo.GetCurrentMethod.Name
            Try
                ' 1. Check if Cache-File already exists
                If File.Exists(_cacheFile) Then
                    tmdbCache = JsonConvert.DeserializeObject(Of Dictionary(Of String, CSGuideTMDBCacheItem))(File.ReadAllText(_cacheFile))
                    ' 2. Remove the old entried
                    removeOldEntries(tmdbCache)
                End If

                ' 3: Check if Cache needs update (should be only updated once a day)
                If cacheNeedsUpdate(viewName) Then
                    ' 4: Update the CacheItems
                    If updateCache(tmdbCache, itemsCache) Then
                        persistCache(tmdbCache)
                    End If
                    '5: Write the lastUpdate-Date in a File            
                    Dim myJasonCache2 As String = JsonConvert.SerializeObject(_lastCacheUpdate, Newtonsoft.Json.Formatting.Indented)
                    File.WriteAllText(_lastUpdateFilename, myJasonCache2)
                End If
            Catch ex As ThreadAbortException
                MyLog.Info(String.Format("[{0}] [{1}]: Cache Builder Thread aborted", _mClass, mName))
            Catch ex As Exception
                MyLog.Error(String.Format("[{0}] [{1}]: Exception err: {2} stack: {3}", _mClass, mName, ex.Message, ex.StackTrace))
            End Try

        End Sub
        Public Sub persistCache(ByRef tmdbCache As Dictionary(Of String, CSGuideTMDBCacheItem))
            Dim mName As String = System.Reflection.MethodInfo.GetCurrentMethod.Name
            Try
                File.WriteAllText(_cacheFile, JsonConvert.SerializeObject(tmdbCache, Newtonsoft.Json.Formatting.Indented))
                MyLog.Info(String.Format("[{0}] [{1}]: Persisting TMDb Cache", _mClass, mName))
            Catch ex As Exception
                MyLog.Error(String.Format("[{0}] [{1}]: Exception err: {2} stack: {3}", _mClass, mName, ex.Message, ex.StackTrace))
            End Try
        End Sub

        Private Function cacheNeedsUpdate(cacheName As String) As Boolean
            Dim mName As String = System.Reflection.MethodInfo.GetCurrentMethod.Name
            If File.Exists(_lastUpdateFilename) Then
                _lastCacheUpdate = JsonConvert.DeserializeObject(Of Dictionary(Of String, Date))(File.ReadAllText(_lastUpdateFilename))
                If Not _lastCacheUpdate.ContainsKey(cacheName) Then
                    _lastCacheUpdate.Add(cacheName, Date.Today)
                    MyLog.Debug(String.Format("[{0}] [{1}]: Cache needs update", _mClass, mName))
                    Return True
                ElseIf _lastCacheUpdate.Item(cacheName).Date < Date.Today Then
                    _lastCacheUpdate.Item(cacheName) = Date.Today
                    MyLog.Debug(String.Format("[{0}] [{1}]: Cache needs update", _mClass, mName))
                    Return True
                Else
                    MyLog.Debug(String.Format("[{0}] [{1}]: No cache update necessary", _mClass, mName))
                    Return False
                End If
            End If
            MyLog.Debug(String.Format("[{0}] [{1}]: Cache needs update", _mClass, mName))
            Return True
        End Function
        Private Function getLastCacheUpdate() As Dictionary(Of String, Date)
            Dim returnDict As Dictionary(Of String, Date) = Nothing
            If File.Exists(_lastUpdateFilename) Then
                returnDict = JsonConvert.DeserializeObject(Of Dictionary(Of String, Date))(File.ReadAllText(_lastUpdateFilename))
            End If
            Return returnDict
        End Function
        Private Sub removeOldEntries(ByRef tmdbCache As Dictionary(Of String, CSGuideTMDBCacheItem))
            Dim mName As String = System.Reflection.MethodInfo.GetCurrentMethod.Name
            Try
                For Each key As String In New List(Of String)(tmdbCache.Keys)
                    If Date.Today > tmdbCache(key).keepUntilDate Then
                        If Utils.FileDelete(tmdbCache(key).Misc.absFanartPath) Then
                            MyLog.Debug(String.Format("[{0}] [{1}]: Deleted {2}", _mClass, mName, tmdbCache(key).Misc.absFanartPath))
                        End If
                        If Utils.FileDelete(tmdbCache(key).Misc.absPosterPath) Then
                            MyLog.Debug(String.Format("[{0}] [{1}]: Deleted {2}", _mClass, mName, tmdbCache(key).Misc.absPosterPath))
                        End If
                        tmdbCache.Remove(key)
                    End If
                Next
                ' to ensure that old entries are deleted
                CSGuideHelper.imageCleaner(Path.Combine(Config.GetSubFolder(Config.Dir.Skin, Config.SkinName & "\media\CSGuide\Actor")), 14)
                CSGuideHelper.imageCleaner(Path.Combine(Config.GetSubFolder(Config.Dir.Skin, Config.SkinName & "\media\CSGuide\Poster")), 14)
                CSGuideHelper.imageCleaner(Path.Combine(Config.GetSubFolder(Config.Dir.Skin, Config.SkinName & "\media\CSGuide\Fanart")), 14)
            Catch ex As Exception
                MyLog.Error(String.Format("[{0}] [{1}]: Exception err: {2} stack: {3}", _mClass, mName, ex.Message, ex.StackTrace))
            End Try
        End Sub


        Public Function updateCache(ByRef tmdbCache As Dictionary(Of String, CSGuideTMDBCacheItem), ByRef itemsCache As List(Of TVMovieProgram)) As Boolean
            Dim mName As String = System.Reflection.MethodInfo.GetCurrentMethod.Name


            'Dim movieFound As SearchMovie = Nothing
            'Dim credit As Credits = Nothing
            Dim updated As Boolean = False
            Dim tvmProgram As TVMovieProgram
            Dim cacheLimitCounter As Integer = 0
            ' Dim movieGerman As Movie = Nothing

            FetchConfig(_tmdbClient)
            Try
                For Each tvmProgram In itemsCache
                    cacheLimitCounter += 1
                    If Not AddItem(tvmProgram, tmdbCache) Then Continue For
                    If cacheLimitCounter > _cacheLimit Then
                        MyLog.Debug(String.Format("[{0}] [{1}]: Cache Limit reached", _mClass, mName))
                        Exit For
                    End If
                    updated = True
                Next
            Catch ex As InvalidOperationException
                MyLog.Debug(String.Format("[{0}] [{1}]: InvalidOperationException", _mClass, mName))
            End Try

            Return updated
        End Function
        Public Function AddItem(ByVal tvmProgram As TVMovieProgram, ByRef tmdbCache As Dictionary(Of String, CSGuideTMDBCacheItem)) As Boolean
            Dim mName As String = System.Reflection.MethodInfo.GetCurrentMethod.Name
            Dim title As String
            Dim year As Integer
            Dim movieFound As SearchMovie = Nothing
            Dim credit As Credits = Nothing
            Dim misc As New CSGuideTMDBCacheItemMisc
            Dim updated As Boolean = False
            Dim keepUntil As Date

            Dim movieGerman As Movie = Nothing
            title = tvmProgram.ReferencedProgram.Title
            year = tvmProgram.ReferencedProgram.OriginalAirDate.Year
            keepUntil = tvmProgram.ReferencedProgram.EndTime.AddDays(1)
            Try
                If tmdbCache.ContainsKey(tvmProgram.ReferencedProgram.Title) Then
                    'Console.WriteLine(title & " (" & year & ") already cached")
                    MyLog.Debug(String.Format("[{0}] [{1}]: {2}({3}) already cached", _mClass, mName, title, year))
                    Return False
                End If
            Catch
            End Try

            Dim results As SearchContainer(Of SearchMovie) = _tmdbClient.SearchMovieAsync(adaptTitleToSearch(title), 0, False, year).Result
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
            MyLog.Debug(String.Format("[{0}] [{1}]: Caching {2}({3})", _mClass, mName, title, year))
            Try
                'tmdbCache.Add(tvmProgram.idProgram, New CSGuideTMDBCacheItem(Date.Today(), movieFound, credit))
                tmdbCache.Add(tvmProgram.ReferencedProgram.Title, New CSGuideTMDBCacheItem(keepUntil, movieGerman, credit, misc))
            Catch
            End Try
            Return True
        End Function

        Private Function adaptTitleToSearch(title As String) As String
            Dim returnString As String

            returnString = Regex.Replace(title, "I\s*$", "1")
            returnString = Regex.Replace(title, "II\s*$", "2")
            returnString = Regex.Replace(title, "III\s*$", "3")
            returnString = Regex.Replace(title, "IV\s*$", "4")
            returnString = Regex.Replace(title, "V\s*$", "5")
            returnString = Regex.Replace(title, "VI\s*$", "6")
            Return returnString
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
                    misc.absFanartPath = Path.Combine(Config.GetSubFolder(Config.Dir.Skin, Config.SkinName & "\media\CSGuide\Fanart\"), imageFilename)
                    misc.relFanartPath = Path.Combine("CSG\Fanart\", imageFilename)
                    misc.fanartURL = _tmdbClient.GetImageUrl("original", movie.BackdropPath).ToString()
                End If
            End If
            If Not movie.PosterPath Is Nothing Then
                artURL = _tmdbClient.GetImageUrl("original", movie.PosterPath).ToString()
                imageFilename = rgx.Match(artURL).Groups(1).Value
                If Not String.IsNullOrEmpty(imageFilename) Then
                    misc.absPosterPath = Path.Combine(Config.GetSubFolder(Config.Dir.Skin, Config.SkinName & "\media\CSGuide\Poster\"), imageFilename)
                    misc.relPosterPath = Path.Combine("CSG\Poster\", imageFilename)
                    misc.posterURL = _tmdbClient.GetImageUrl("original", movie.PosterPath).ToString()
                End If
            End If
            misc.title_string = tvmProgram.ReferencedProgram.Title & CSGuideHelper.GetYearString(tvmProgram)
            Return misc
        End Function

        Private Shared Sub FetchConfig(client As TMDbClient)
            Dim configXml As New FileInfo(Path.Combine(Config.GetSubFolder(Config.Dir.Skin, Config.SkinName & "\media\CSGuide\CSGuideTMDbLibConfig.xml")))

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