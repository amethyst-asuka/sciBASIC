﻿#Region "Microsoft.VisualBasic::6343850558179f8e7f8a518bf587ae4c, mime\application%pdf\PdfReader\Document\PdfPages.vb"

    ' Author:
    ' 
    '       asuka (amethyst.asuka@gcmodeller.org)
    '       xie (genetics@smrucc.org)
    '       xieguigang (xie.guigang@live.com)
    ' 
    ' Copyright (c) 2018 GPL3 Licensed
    ' 
    ' 
    ' GNU GENERAL PUBLIC LICENSE (GPL3)
    ' 
    ' 
    ' This program is free software: you can redistribute it and/or modify
    ' it under the terms of the GNU General Public License as published by
    ' the Free Software Foundation, either version 3 of the License, or
    ' (at your option) any later version.
    ' 
    ' This program is distributed in the hope that it will be useful,
    ' but WITHOUT ANY WARRANTY; without even the implied warranty of
    ' MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    ' GNU General Public License for more details.
    ' 
    ' You should have received a copy of the GNU General Public License
    ' along with this program. If not, see <http://www.gnu.org/licenses/>.



    ' /********************************************************************************/

    ' Summaries:

    '     Class PdfPages
    ' 
    '         Properties: Children
    ' 
    '         Constructor: (+1 Overloads) Sub New
    '         Sub: FindLeafPages, Visit
    ' 
    ' 
    ' /********************************************************************************/

#End Region

Imports System
Imports System.Collections.Generic

Namespace PdfReader
    Public Class PdfPages
        Inherits PdfPageInherit

        Private _Children As System.Collections.Generic.List(Of PdfReader.PdfPageInherit)

        Public Sub New(ByVal dictionary As PdfDictionary)
            MyBase.New(dictionary.Parent, dictionary.ParseDictionary)
            Children = New List(Of PdfPageInherit)()

            For Each reference As PdfObjectReference In MandatoryValue(Of PdfArray)("Kids").Objects
                Dim childDictionary = Document.IndirectObjects.MandatoryValue(Of PdfDictionary)(reference)
                Dim type = childDictionary.MandatoryValue(Of PdfName)("Type").Value

                If Equals(type, "Page") Then
                    Children.Add(New PdfPage(childDictionary))
                ElseIf Equals(type, "Pages") Then
                    Children.Add(New PdfPages(childDictionary))
                Else
                    Throw New ArgumentException($"Unrecognized dictionary type references from page tree '{type}'.")
                End If
            Next
        End Sub

        Public Overrides Sub Visit(ByVal visitor As IPdfObjectVisitor)
            visitor.Visit(Me)
        End Sub

        Public Overrides Sub FindLeafPages(ByVal pages As List(Of PdfPage))
            For Each child In Children
                child.FindLeafPages(pages)
            Next
        End Sub

        Public Property Children As List(Of PdfPageInherit)
            Get
                Return _Children
            End Get
            Private Set(ByVal value As List(Of PdfPageInherit))
                _Children = value
            End Set
        End Property
    End Class
End Namespace
