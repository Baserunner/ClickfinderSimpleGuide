Imports System.IO
Imports System.Xml
Imports System.Xml.Serialization

Public NotInheritable Class Serializer
    Private Sub New()
    End Sub
    Private Shared _serializerNamespaces As XmlSerializerNamespaces

    Shared Sub New()
        _serializerNamespaces = New XmlSerializerNamespaces()
        _serializerNamespaces.Add("", "")
    End Sub

    ''' <summary>
    ''' Serialize an object to an XMLDocument using the built-in xml serializer.
    ''' </summary>
    ''' <typeparam name="T">Type of object to serialize</typeparam>
    ''' <param name="obj">Object to serialize</param>
    ''' <param name="alternateNamespaces"> </param>
    ''' <returns>XmlDocument containing the serialized data</returns>
    Public Shared Function Serialize(Of T)(obj As T, Optional alternateNamespaces As XmlSerializerNamespaces = Nothing) As XmlDocument
        Dim s As New XmlSerializer(obj.[GetType]())

        Using stream As New MemoryStream()
            Using sw As New StreamWriter(stream)
                s.Serialize(sw, obj, If(alternateNamespaces, _serializerNamespaces))

                stream.Position = 0
                stream.Flush()

                Dim doc As New XmlDocument()
                doc.Load(stream)
                Return doc
            End Using
        End Using
    End Function

    ''' <summary>
    ''' Deserialize the XmlDocument given, into an object of type T.
    ''' T must have a parameterless constructor.
    ''' </summary>
    ''' <typeparam name="T">Type of the object to deserialize</typeparam>
    ''' <param name="doc">The document to deserialize</param>
    ''' <returns>A fresh object containing the information from the document</returns>
    Public Shared Function Deserialize(Of T)(doc As XmlDocument) As T
        ' Use awesomeness of Activator
        Dim tmp As T = Activator.CreateInstance(Of T)()
        Dim serializer As New XmlSerializer(tmp.[GetType]())
        Dim objectToSerialize As T = DirectCast(serializer.Deserialize(New XmlNodeReader(doc.DocumentElement)), T)
        Return objectToSerialize
    End Function
End Class
