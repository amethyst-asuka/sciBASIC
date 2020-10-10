﻿''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
'
'	PdfFileWriter
'	PDF File Write C# Class Library.
'
'	PdfPage
'	PDF page class. An indirect PDF object.
'
'	Uzi Granot
'	Version: 1.0
'	Date: April 1, 2013
'	Copyright (C) 2013-2019 Uzi Granot. All Rights Reserved
'
'	PdfFileWriter C# class library and TestPdfFileWriter test/demo
'  application are free software.
'	They is distributed under the Code Project Open License (CPOL).
'	The document PdfFileWriterReadmeAndLicense.pdf contained within
'	the distribution specify the license agreement and other
'	conditions and notes. You must read this document and agree
'	with the conditions specified in order to use this software.
'
'	For version history please refer to PdfDocument.cs
'
''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''

Imports System.Collections.Generic
Imports System.Text


    ''' <summary>
    ''' PDF page class
    ''' </summary>
    ''' <remarks>
    ''' PDF page class represent one page in the document.
    ''' </remarks>
    Public Class PdfPage
        Inherits PdfObject

        Friend Width As Double      ' in points
        Friend Height As Double     ' in points
        Friend ContentsArray As List(Of PdfContents)

        '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
        ''' <summary>
        ''' Default constructor
        ''' </summary>
        ''' <param name="Document">Parent PDF document object</param>
        ''' <remarks>
        ''' Page size is taken from PdfDocument
        ''' </remarks>
        '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
        Public Sub New(ByVal Document As PdfDocument)
            MyBase.New(Document, ObjectType.Dictionary, "/Page")
            Width = Document.PageSize.Width
            Height = Document.PageSize.Height
            ConstructorHelper()
            Return
        End Sub

        '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
        ''' <summary>
        ''' Constructor
        ''' </summary>
        ''' <param name="Document">Parent PDF document object</param>
        ''' <param name="PageSize">Paper size for this page</param>
        ''' <remarks>
        ''' PageSize override the default page size
        ''' </remarks>
        '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''

        Public Sub New(ByVal Document As PdfDocument, ByVal PageSize As SizeD)
            MyBase.New(Document, ObjectType.Dictionary, "/Page")
            Width = ScaleFactor * PageSize.Width
            Height = ScaleFactor * PageSize.Height
            ConstructorHelper()
            Return
        End Sub

        '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
        ''' <summary>
        ''' Constructor
        ''' </summary>
        ''' <param name="Document">Parent PDF document object</param>
        ''' <param name="PaperType">Paper type</param>
        ''' <param name="Landscape">If Lanscape is true, width and height are swapped.</param>
        ''' <remarks>
        ''' PaperType and orientation override the default page size.
        ''' </remarks>
        '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
        Public Sub New(ByVal Document As PdfDocument, ByVal PaperType As PaperType, ByVal Landscape As Boolean)
            MyBase.New(Document, ObjectType.Dictionary, "/Page")
            ' get standard paper size
            Width = PdfDocument.PaperTypeSize(PaperType).Width
            Height = PdfDocument.PaperTypeSize(PaperType).Height

            ' for landscape swap width and height
            If Landscape Then
                Dim Temp = Width
                Width = Height
                Height = Temp
            End If

            ' exit
            ConstructorHelper()
            Return
        End Sub

        '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
        ''' <summary>
        ''' Constructor
        ''' </summary>
        ''' <param name="Document">Parent PDF document object</param>
        ''' <param name="Width">Page width</param>
        ''' <param name="Height">Page height</param>
        ''' <remarks>
        ''' Width and Height override the default page size
        ''' </remarks>
        '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
        Public Sub New(ByVal Document As PdfDocument, ByVal Width As Double, ByVal Height As Double)
            MyBase.New(Document, ObjectType.Dictionary, "/Page")
            Me.Width = ScaleFactor * Width
            Me.Height = ScaleFactor * Height
            ConstructorHelper()
            Return
        End Sub

        ''' <summary>
        ''' Clone Constructor
        ''' </summary>
        ''' <param name="Page">Existing page object</param>
        Public Sub New(ByVal Page As PdfPage)
            MyBase.New(Page.Document, ObjectType.Dictionary, "/Page")
            Width = Page.Width
            Height = Page.Height
            ConstructorHelper()
            Return
        End Sub

        '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
        ' Constructor common method
        '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''

        Private Sub ConstructorHelper()
            ' add page to parent array of pages
            Document.PageArray.Add(Me)

            ' link page to parent
            Dictionary.AddIndirectReference("/Parent", Document.PagesObject)

            ' add page size in points
            Dictionary.AddFormat("/MediaBox", "[0 0 {0} {1}]", Round(Width), Round(Height))

            ' exit
            Return
        End Sub

        ''' <summary>
        ''' Page size
        ''' </summary>
        ''' <returns>Page size</returns>
        ''' <remarks>Page size in user units of measure. If Width is less than height
        ''' orientation is portrait. Otherwise orientation is landscape.</remarks>
        Public Function PageSize() As SizeD
            Return New SizeD(Width / ScaleFactor, Height / ScaleFactor)
        End Function

        '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
        ''' <summary>
        ''' Add existing contents to page
        ''' </summary>
        ''' <param name="Contents">Contents object</param>
        '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
        Public Sub AddContents(ByVal Contents As PdfContents)
            ' set page contents flag
            Contents.PageContents = True

            ' add content to content array
            If ContentsArray Is Nothing Then ContentsArray = New List(Of PdfContents)()
            ContentsArray.Add(Contents)

            ' exit
            Return
        End Sub

        ''' <summary>
        ''' Gets the current contents of this page
        ''' </summary>
        ''' <returns>Page's current contents</returns>
        Public Function GetCurrentContents() As PdfContents
            Return If(ContentsArray Is Nothing OrElse ContentsArray.Count = 0, Nothing, ContentsArray(ContentsArray.Count - 1))
        End Function

        ''' <summary>
        ''' Add annotation action
        ''' </summary>
        ''' <param name="AnnotRect">Annotation rectangle</param>
        ''' <param name="AnnotAction">Annotation action derived class</param>
        ''' <returns>PdfAnnotation object</returns>
        Public Function AddAnnotation(ByVal AnnotRect As PdfRectangle, ByVal AnnotAction As AnnotAction) As PdfAnnotation
            If AnnotAction.GetType() Is GetType(AnnotLinkAction) Then
                Return AddLinkAction(CType(AnnotAction, AnnotLinkAction).LocMarkerName, AnnotRect)
            End If

            Return New PdfAnnotation(Me, AnnotRect, AnnotAction)
        End Function

        Friend Sub AddAnnotInternal(ByVal AnnotRect As PdfRectangle, ByVal AnnotAction As AnnotAction)
            If AnnotAction.GetType() Is GetType(AnnotLinkAction) Then
                AddLinkAction(CType(AnnotAction, AnnotLinkAction).LocMarkerName, AnnotRect)
            Else
                If AnnotAction.GetType() Is GetType(AnnotFileAttachment) Then CType(AnnotAction, AnnotFileAttachment).Icon = FileAttachIcon.NoIcon
                Dim null As New PdfAnnotation(Me, AnnotRect, AnnotAction)
            End If

            Return
        End Sub

        '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
        ''' <summary>
        ''' Add weblink to this page
        ''' </summary>
        ''' <param name="LeftAbsPos">Left position of weblink area</param>
        ''' <param name="BottomAbsPos">Bottom position of weblink area</param>
        ''' <param name="RightAbsPos">Right position of weblink area</param>
        ''' <param name="TopAbsPos">Top position of weblink area</param>
        ''' <param name="WebLinkStr">Hyperlink string</param>
        ''' <returns>PdfAnnotation object</returns>
        ''' <remarks>
        ''' <para>
        ''' 	The four position arguments are in relation to the
        ''' 	bottom left corner of the paper.
        ''' 	If web link is empty, ignore this call.
        ''' </para>
        ''' <para>
        ''' For more information go to <a href="http://www.codeproject.com/Articles/570682/PDF-File-Writer-Csharp-Class-Library-Version#WeblinkSupport">2.7 Web Link Support</a>
        ''' </para>
        ''' </remarks>
        '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
        Public Function AddWebLink(ByVal LeftAbsPos As Double, ByVal BottomAbsPos As Double, ByVal RightAbsPos As Double, ByVal TopAbsPos As Double, ByVal WebLinkStr As String) As PdfAnnotation
            If String.IsNullOrWhiteSpace(WebLinkStr) Then Return Nothing
            Return AddWebLink(New PdfRectangle(LeftAbsPos, BottomAbsPos, RightAbsPos, TopAbsPos), WebLinkStr)
        End Function

        '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
        ''' <summary>
        ''' Add weblink to this page
        ''' </summary>
        ''' <param name="AnnotRect">Weblink area</param>
        ''' <param name="WebLinkStr">Hyperlink string</param>
        ''' <returns>PdfAnnotation object</returns>
        ''' <remarks>
        ''' <para>
        ''' 	The four position arguments are in relation to the
        ''' 	bottom left corner of the paper.
        ''' 	If web link is empty, ignore this call.
        ''' </para>
        ''' <para>
        ''' For more information go to <a href="http://www.codeproject.com/Articles/570682/PDF-File-Writer-Csharp-Class-Library-Version#WeblinkSupport">2.7 Web Link Support</a>
        ''' </para>
        ''' </remarks>
        '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
        Public Function AddWebLink(ByVal AnnotRect As PdfRectangle, ByVal WebLinkStr As String) As PdfAnnotation
            If String.IsNullOrWhiteSpace(WebLinkStr) Then Return Nothing
            Return New PdfAnnotation(Me, AnnotRect, New AnnotWebLink(WebLinkStr))
        End Function

        ''' <summary>
        ''' Add location marker
        ''' </summary>
        ''' <param name="LocMarkerName">Location marker name</param>
        ''' <param name="Scope">Location marker scope</param>
        ''' <param name="FitArg">PDF reader display control</param>
        ''' <param name="SideArg">Optional dimensions for FitArg control</param>
        Public Sub AddLocationMarker(ByVal LocMarkerName As String, ByVal Scope As LocMarkerScope, ByVal FitArg As DestFit, ParamArray SideArg As Double())
            LocationMarker.Create(LocMarkerName, Me, Scope, FitArg, SideArg)
            Return
        End Sub

        ''' <summary>
        ''' Add go to action
        ''' </summary>
        ''' <param name="LocMarkerName">Destination name</param>
        ''' <param name="AnnotRect">Annotation rectangle</param>
        ''' <returns>PdfAnnotation object</returns>
        Public Function AddLinkAction(ByVal LocMarkerName As String, ByVal AnnotRect As PdfRectangle) As PdfAnnotation
            Return New PdfAnnotation(Me, AnnotRect, New AnnotLinkAction(LocMarkerName))
        End Function

        ''' <summary>
        ''' Add rendering screen action to page
        ''' </summary>
        ''' <param name="AnnotRect">Annotation rectangle</param>
        ''' <param name="DisplayMedia">Display media object</param>
        ''' <returns>PdfAnnotation</returns>
        Public Function AddScreenAction(ByVal AnnotRect As PdfRectangle, ByVal DisplayMedia As PdfDisplayMedia) As PdfAnnotation
            Return New PdfAnnotation(Me, AnnotRect, New AnnotDisplayMedia(DisplayMedia))
        End Function

        ''' <summary>
        ''' Add annotation file attachement with icon
        ''' </summary>
        ''' <param name="AnnotRect">Annotation rectangle</param>
        ''' <param name="EmbeddedFile">Embedded file</param>
        ''' <param name="Icon">Icon</param>
        ''' <returns>PdfAnnotation</returns>
        ''' <remarks>The AnnotRect is the icon rectangle area. To access the file
        ''' the user has to right click on the icon.</remarks>
        Public Function AddFileAttachment(ByVal AnnotRect As PdfRectangle, ByVal EmbeddedFile As PdfEmbeddedFile, ByVal Icon As FileAttachIcon) As PdfAnnotation
            Return New PdfAnnotation(Me, AnnotRect, New AnnotFileAttachment(EmbeddedFile, Icon))
        End Function

        ''' <summary>
        ''' Add annotation file attachement with no icon
        ''' </summary>
        ''' <param name="AnnotRect">Annotation rectangle</param>
        ''' <param name="EmbeddedFile">Embedded file</param>
        ''' <returns>PdfAnnotation</returns>
        ''' <remarks>The AnnotRect is any area on the page. When the user right click this
        ''' area a floating menu will be displayed.</remarks>
        Public Function AddFileAttachment(ByVal AnnotRect As PdfRectangle, ByVal EmbeddedFile As PdfEmbeddedFile) As PdfAnnotation
            Return New PdfAnnotation(Me, AnnotRect, New AnnotFileAttachment(EmbeddedFile, FileAttachIcon.NoIcon))
        End Function

        ''' <summary>
        ''' Add sticky note to this page
        ''' </summary>
        ''' <param name="AbsLeft">Icon page absolute left position</param>
        ''' <param name="AbsTop">Icon page absolute top position</param>
        ''' <param name="Note">Sticky note text string</param>
        ''' <param name="Icon">Sticky note icon enumeration</param>
        ''' <returns>PdfAnnotation</returns>
        Public Function AddStickyNote(ByVal AbsLeft As Double, ByVal AbsTop As Double, ByVal Note As String, ByVal Icon As StickyNoteIcon) As PdfAnnotation
            Return New PdfAnnotation(Me, New PdfRectangle(AbsLeft, AbsTop, AbsLeft, AbsTop), New AnnotStickyNote(Note, Icon))
        End Function

        '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
        ' Write object to PDF file
        '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''

        Friend Overrides Sub WriteObjectToPdfFile()
            ' we have at least one contents object
            If ContentsArray IsNot Nothing Then
                ' page has one contents object
                If ContentsArray.Count = 1 Then
                    Dictionary.AddFormat("/Contents", "[{0} 0 R]", ContentsArray(0).ObjectNumber)

                    ' page is made of multiple contents
                    Dictionary.Add("/Resources", BuildResourcesDictionary(ContentsArray(0).ResObjects, True))
                Else
                    ' contents dictionary entry
                    Dim ContentsStr As StringBuilder = New StringBuilder("[")

                    ' build contents dictionary entry
                    For Each Contents In ContentsArray
                        ContentsStr.AppendFormat("{0} 0 R ", Contents.ObjectNumber)
                    Next

                    ' add terminating bracket
                    ContentsStr.Length -= 1
                    ContentsStr.Append("]"c)
                    Dictionary.Add("/Contents", ContentsStr.ToString())

                    ' resources array of all contents objects
                    Dim ResObjects As List(Of PdfObject) = New List(Of PdfObject)()

                    ' loop for all contents objects
                    For Each Contents In ContentsArray
                        ' make sure we have resources
                        If Contents.ResObjects IsNot Nothing Then
                            ' loop for resources within this contents object
                            For Each ResObject In Contents.ResObjects
                                ' check if we have it already
                                Dim Ptr = ResObjects.BinarySearch(ResObject)
                                If Ptr < 0 Then ResObjects.Insert(Not Ptr, ResObject)
                            Next
                        End If
                    Next

                    ' save to dictionary
                    Dictionary.Add("/Resources", BuildResourcesDictionary(ResObjects, True))
                End If
            End If

            ' call PdfObject routine
            MyBase.WriteObjectToPdfFile()

            ' exit
            Return
        End Sub
    End Class

