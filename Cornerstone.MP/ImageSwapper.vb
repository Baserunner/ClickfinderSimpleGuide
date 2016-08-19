
Imports MediaPortal.GUI.Library


Namespace ClickfinderSimpleGuide
    ''' <summary>
    ''' This class takes two GUIImage objects so that you can treat them as one. When you assign
    ''' a new image to this object using the Filename property, the currently active image is 
    ''' hidden and the second is made visibile (with the new image file displayed). This allows
    ''' for animations on image change, such as a fading transition.
    ''' 
    ''' This class also uses the AsyncImageResource class for asynchronus image loading, 
    ''' dramtically improving GUI performance. It also takes advantage of the Delay feature of
    ''' the AsyncImageResource to prevent unneccisary loads when rapid image changes are made.
    ''' </summary>
    Public Class ImageSwapper
        Private imagesNeedSwapping As Boolean = False
        Private loadingLock As New Object()

        ''' <summary>
        ''' Image loading only occurs when set to true. If false all resources will be unloaded
        ''' and all GUIImage objects set to invisible. Setting Active to false also clears the
        ''' Filename property.
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
                _imageResource.Active = _active

                ' if we are inactive be sure both properties are cleared
                If Not Active Then
                    _imageResource.[Property] = _propertyTwo
                    _imageResource.[Property] = _propertyOne
                End If
            End Set
        End Property
        Private _active As Boolean = True

        ''' <summary>
        ''' The filename of the image backing this resource. Reassign to change textures.
        ''' </summary>
        Public Property Filename() As String
            Get
                Return _filename
            End Get

            Set
                SyncLock loadingLock
                    If Not Active Then
                        Value = Nothing
                    End If

                    If (Value IsNot Nothing AndAlso Value.Equals(_filename)) OrElse _guiImageOne Is Nothing Then
                        Return
                    End If

                    ' if we have a second backdrop image object, alternate between the two
                    If _guiImageTwo IsNot Nothing AndAlso imagesNeedSwapping Then
                        If _imageResource.[Property].Equals(_propertyOne) Then
                            _imageResource.[Property] = _propertyTwo
                        Else
                            _imageResource.[Property] = _propertyOne
                        End If

                        imagesNeedSwapping = False
                    End If

                    ' update resource with new file
                    _filename = Value
                    If _loadingImage IsNot Nothing Then
                        _loadingImage.Visible = True
                    End If
                    _imageResource.Filename = _filename
                End SyncLock
            End Set
        End Property
        Private _filename As String = Nothing

        ''' <summary>
        ''' First GUIImage used for the visibilty toggle behavior. If set to NULL the ImageSwapper
        ''' behaves as if inactive.
        ''' </summary>
        Public Property GUIImageOne() As GUIImage
            Get
                Return _guiImageOne
            End Get
            Set
                If _guiImageOne Is Value Then
                    Return
                End If

                _guiImageOne = Value
                If _guiImageOne IsNot Nothing Then
                    _guiImageOne.FileName = _propertyOne
                    _filename = Nothing
                End If
            End Set
        End Property
        Private _guiImageOne As GUIImage

        ''' <summary>
        ''' Second GUIImage used for the visibility toggle behavior. If set to NULL no toggling
        ''' occurs and only GUIImageOne is used. This provides backwards compatibility if a skin
        ''' does not implement the second GUIImage control.
        ''' </summary>
        Public Property GUIImageTwo() As GUIImage
            Get
                Return _guiImageTwo
            End Get
            Set
                If _guiImageTwo Is Value Then
                    Return
                End If

                _guiImageTwo = Value
                If _guiImageTwo IsNot Nothing Then
                    _guiImageTwo.FileName = _propertyTwo
                    _filename = Nothing
                End If
            End Set
        End Property
        Private _guiImageTwo As GUIImage

        ''' <summary>
        ''' If set, this image object will be set to visible during the load process and will
        ''' be set to hidden when the next image has completed loading.
        ''' </summary>
        Public Property LoadingImage() As GUIImage
            Get
                Return _loadingImage
            End Get
            Set
                _loadingImage = Value
            End Set
        End Property
        Private _loadingImage As GUIImage

        ''' <summary>
        ''' The property assigned to the first GUIImage. Assigning this property to the texture
        ''' field of another GUIImage object will result in the image being loaded there. This
        ''' can also be useful for backwards compatibility.
        ''' </summary>
        Public Property PropertyOne() As String
            Get
                Return _propertyOne
            End Get
            Set
                If _imageResource.[Property].Equals(_propertyOne) Then
                    _imageResource.[Property] = Value
                End If

                _propertyOne = Value
            End Set
        End Property
        Private _propertyOne As String = "#Cornerstone.ImageSwapper1"

        ''' <summary>
        ''' The property field used for the second GUIImage.
        ''' </summary>
        Public Property PropertyTwo() As String
            Get
                Return _propertyTwo
            End Get
            Set
                If _imageResource.[Property].Equals(_propertyTwo) Then
                    _imageResource.[Property] = Value
                End If

                _propertyTwo = Value
            End Set
        End Property
        Private _propertyTwo As String = "#Cornerstone.ImageSwapper2"

        ''' <summary>
        ''' The AsyncImageResource backing this object. All image loading and unloading is done
        ''' in the background by this object.
        ''' </summary>
        Public ReadOnly Property ImageResource() As AsyncImageResource
            Get
                Return _imageResource
            End Get
        End Property
        Private _imageResource As AsyncImageResource


        Public Sub New()
            _imageResource = New AsyncImageResource()
            _imageResource.[Property] = _propertyOne
            AddHandler _imageResource.ImageLoadingComplete, New AsyncImageLoadComplete(AddressOf imageResource_ImageLoadingComplete)
            '_imageResource.ImageLoadingComplete += New AsyncImageLoadComplete(AddressOf imageResource_ImageLoadingComplete)
        End Sub

        ' Once image loading is complete this method is called and the visibility of the
        ' two GUIImages is swapped.
        Private Sub imageResource_ImageLoadingComplete(image As AsyncImageResource)
            SyncLock loadingLock
                If _guiImageOne Is Nothing Then
                    Return
                End If

                If _filename Is Nothing Then
                    If _guiImageOne IsNot Nothing Then
                        _guiImageOne.Visible = False
                    End If
                    If _guiImageTwo IsNot Nothing Then
                        _guiImageTwo.Visible = False
                    End If
                    Return
                End If

                _guiImageOne.ResetAnimations()
                If _guiImageTwo IsNot Nothing Then
                    _guiImageTwo.ResetAnimations()
                End If

                ' if we have a second backdrop image object, alternate between the two
                If _guiImageTwo IsNot Nothing Then
                    If _imageResource.[Property].Equals(_propertyOne) Then
                        _guiImageOne.Visible = _active
                        _guiImageTwo.Visible = False
                    Else
                        _guiImageOne.Visible = False
                        _guiImageTwo.Visible = _active
                    End If

                    imagesNeedSwapping = True
                Else


                    ' if no 2nd backdrop control, just update normally
                    _guiImageOne.Visible = _active
                End If

                If _loadingImage IsNot Nothing Then
                    _loadingImage.Visible = False
                End If
            End SyncLock
        End Sub

    End Class
End Namespace

