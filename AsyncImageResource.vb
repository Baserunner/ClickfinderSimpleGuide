Imports System.Data
Imports System.Threading
Imports System.Drawing
Imports System.Runtime.InteropServices
Imports System.Reflection
Imports MediaPortal.GUI.Library
Imports System.IO

Namespace ClickfinderSimpleGuide
    Public Delegate Sub AsyncImageLoadComplete(image As AsyncImageResource)

    Public Class AsyncImageResource
        Private loadingLock As New [Object]()
        Private pendingToken As Integer = 0
        Private threadsWaiting As Integer = 0
        Private warned As Boolean = False


        ''' <summary>
        ''' This event is triggered when a new image file has been successfully loaded
        ''' into memory.
        ''' </summary>
        Public Event ImageLoadingComplete As AsyncImageLoadComplete

        ''' <summary>
        ''' True if this resources will actively load into memory when assigned a file.
        ''' </summary>
        Public Property Active() As Boolean
            Get
                Return _active
            End Get

            Set
                If _active = Value Then
                    Return
                End If

                _active = Value

                Dim newThread As New Thread(New ThreadStart(AddressOf activeWorker))
                newThread.Name = "Cornerstone"
                newThread.Start()
            End Set
        End Property
        Private _active As Boolean = True

        ''' <summary>
        ''' If multiple changes to the Filename property are made in rapid succession, this delay
        ''' will be used to prevent uneccesary loading operations. Most useful for large images that
        ''' take a non-trivial amount of time to load from memory.
        ''' </summary>
        Public Property Delay() As Integer
            Get
                Return _delay
            End Get
            Set
                _delay = Value
            End Set
        End Property
        Private _delay As Integer = 250

        Private Sub activeWorker()
            SyncLock loadingLock
                If _active Then
                    ' load the resource
                    _identifier = loadResourceSafe(_filename)

                    ' notify any listeners a resource has been loaded
                    RaiseEvent ImageLoadingComplete(Me)
                Else
                    unloadResource(_filename)
                    _identifier = Nothing
                End If
            End SyncLock
        End Sub


        ''' <summary>
        ''' This MediaPortal property will automatically be set with the renderable identifier
        ''' once the resource has been loaded. Appropriate for a texture field of a GUIImage 
        ''' control.
        ''' </summary>
        Public Property [Property]() As String
            Get
                Return _property
            End Get
            Set
                _property = Value

                writeProperty()
            End Set
        End Property
        Private _property As String = Nothing

        Private Sub writeProperty()
            If _active AndAlso _property IsNot Nothing AndAlso _identifier IsNot Nothing Then
                GUIPropertyManager.SetProperty(_property, _identifier)
            Else
                If _property IsNot Nothing Then
                    GUIPropertyManager.SetProperty(_property, "-")
                End If
            End If
        End Sub


        ''' <summary>
        ''' The identifier used by the MediaPortal GUITextureManager to identify this resource.
        ''' This changes when a new file has been assigned, if you need to know when this changes
        ''' use the ImageLoadingComplete event.
        ''' </summary>
        Public ReadOnly Property Identifier() As String
            Get
                Return _identifier
            End Get
        End Property
        Private _identifier As String = Nothing


        ''' <summary>
        ''' The filename of the image backing this resource. Reassign to change textures.
        ''' </summary>
        Public Property Filename() As String
            Get
                Return _filename
            End Get

            Set
                Dim newThread As New Thread(New ParameterizedThreadStart(AddressOf setFilenameWorker))
                newThread.Name = "Cornerstone"
                newThread.Start(Value)
            End Set
        End Property
        Private _filename As String = Nothing

        ' Unloads the previous file and sets a new filename. 
        Private Sub setFilenameWorker(newFilenameObj As Object)
            Dim localToken As Integer = System.Threading.Interlocked.Increment(pendingToken)
            Dim oldFilename As String = _filename

            ' check if another thread has locked for loading
            Dim loading As Boolean = Monitor.TryEnter(loadingLock)
            If loading Then
                Monitor.[Exit](loadingLock)
            End If

            ' if a loading action is in progress or another thread is waiting, we wait too
            If loading OrElse threadsWaiting > 0 Then
                threadsWaiting += 1
                For i As Integer = 0 To 4
                    Thread.Sleep(_delay / 5)
                    If localToken < pendingToken Then
                        Return
                    End If
                Next
                threadsWaiting -= 1
            End If

            SyncLock loadingLock
                If localToken < pendingToken Then
                    Return
                End If

                ' type cast and clean our filename
                Dim newFilename As String = TryCast(newFilenameObj, String)
                If newFilename IsNot Nothing AndAlso newFilename.Trim().Length = 0 Then
                    newFilename = Nothing
                ElseIf newFilename IsNot Nothing Then
                    newFilename = newFilename.Trim()
                End If

                ' if we are not active we should not be assigning a filename
                If Not Active Then
                    newFilename = Nothing
                End If

                ' if there is no change, quit
                If _filename IsNot Nothing AndAlso _filename.Equals(newFilename) Then
                    RaiseEvent ImageLoadingComplete(Me)

                    Return
                End If

                Dim newIdentifier As String = loadResourceSafe(newFilename)

                ' check if we have a new loading action pending, if so just quit
                If localToken < pendingToken Then
                    unloadResource(newIdentifier)
                    Return
                End If

                ' update MediaPortal about the image change                
                _identifier = newIdentifier
                _filename = newFilename
                writeProperty()

                ' notify any listeners a resource has been loaded
                RaiseEvent ImageLoadingComplete(Me)
            End SyncLock

            ' wait a few seconds in case we want to quickly reload the previous resource
            ' if it's not reassigned, unload from memory.
            Thread.Sleep(5000)
            SyncLock loadingLock
                If _filename <> oldFilename Then
                    unloadResource(oldFilename)
                End If
            End SyncLock
        End Sub


        ''' <summary>
        ''' Loads the given file into memory and registers it with MediaPortal.
        ''' </summary>
        ''' <param name="filename">The image file to be loaded.</param>
        Private Function loadResource(filename As String) As Boolean
            If Not _active OrElse filename Is Nothing OrElse Not File.Exists(filename) Then
                Return False
            End If

            Try
                If GUITextureManager.Load(filename, 0, 0, 0, True) > 0 Then
                    Return True
                End If
            Catch
            End Try

            Return False
        End Function

        Private Function loadResourceSafe(filename As String) As String
            If filename Is Nothing OrElse filename.Trim().Length = 0 Then
                Return Nothing
            End If

            ' try to load with new persistent load feature
            Try
                If loadResource(filename) Then
                    Return filename
                End If
            Catch generatedExceptionName As MissingMethodException
                If Not warned Then
                    warned = True
                End If
            End Try

            ' if not available load image ourselves and pass to MediaPortal. Much slower but this still
            ' gives us asynchronous loading. 
            Dim image As Image = LoadImageFastFromFile(filename)

            If image IsNot Nothing Then
                If GUITextureManager.LoadFromMemory(image, getIdentifier(filename), 0, 0, 0) > 0 Then
                    Return getIdentifier(filename)
                End If
            End If
            Return Nothing
        End Function

        Private Function getIdentifier(filename As String) As String
            Return "[Trakt:" & filename.GetHashCode() & "]"
        End Function

        ''' <summary>
        ''' If previously loaded, unloads the resource from memory and removes it 
        ''' from the MediaPortal GUITextureManager.
        ''' </summary>
        Private Sub unloadResource(filename As String)

            If filename Is Nothing Then
                Return
            End If

            ' double duty since we dont know if we loaded via new fast way or old
            ' slow way
            GUITextureManager.ReleaseTexture(getIdentifier(filename))
            GUITextureManager.ReleaseTexture(filename)
        End Sub


        <DllImport("gdiplus.dll", CharSet:=CharSet.Unicode)>
        Private Shared Function GdipLoadImageFromFile(filename As String, ByRef image As IntPtr) As Integer
        End Function

        ' Loads an Image from a File by invoking GDI Plus instead of using build-in 
        ' .NET methods, or falls back to Image.FromFile. GDI Plus should be faster.
        Public Shared Function LoadImageFastFromFile(filename As String) As Image

            Dim imagePtr As IntPtr = IntPtr.Zero
            Dim image As Image = Nothing

            Try
                If GdipLoadImageFromFile(filename, imagePtr) <> 0 Then
                    image = LoadImageSafe(filename)
                Else

                    image = DirectCast(GetType(Bitmap).InvokeMember("FromGDIplus", BindingFlags.NonPublic Or BindingFlags.[Static] Or BindingFlags.InvokeMethod, Nothing, Nothing, New Object() {imagePtr}), Image)
                End If
            Catch e As Exception
                MyLog.Warn(String.Format("[AsyncImageResource] Failed to load image {0}: {1}", filename, e.Message))
                image = Nothing
            End Try

            Return image

        End Function

        ''' <summary>
        ''' Method to safely load an image from a file without leaving the file open       
        ''' </summary> 
        Public Shared Function LoadImageSafe(filePath As String) As Image
            Try
                Dim fi As New FileInfo(filePath)

                If Not fi.Exists Then
                    Throw New FileNotFoundException("Cannot find image")
                End If
                If fi.Length = 0 Then
                    Throw New FileNotFoundException("Zero length image file")
                End If

                ' Image.FromFile is known to leave files open, so we use a stream instead to read it
                Using fs As New FileStream(filePath, FileMode.Open, FileAccess.Read)
                    If Not fs.CanRead Then
                        Throw New FileLoadException("Cannot read file stream")
                    End If

                    If fs.Length = 0 Then
                        Throw New FileLoadException("File stream zero length")
                    End If

                    Using original As Image = Image.FromStream(fs)
                        ' Make a copy of the file in memory, then release the one GDI+ gave us
                        ' thus ensuring that all file handles are closed properly (which GDI+ doesn’t do for us in a timely fashion)
                        Dim width As Integer = original.Width
                        Dim height As Integer = original.Height
                        If width = 0 Then
                            Throw New DataException("Bad image dimension width=0")
                        End If
                        If height = 0 Then
                            Throw New DataException("Bad image dimension height=0")
                        End If

                        Dim copy As New Bitmap(width, height)
                        Using graphics__1 As Graphics = Graphics.FromImage(copy)
                            graphics__1.DrawImage(original, 0, 0, copy.Width, copy.Height)
                        End Using
                        Return copy
                    End Using
                End Using
            Catch ex As Exception
                ex.Data.Add("FileName", filePath)
                Throw
            End Try
        End Function
    End Class
End Namespace

