﻿#Region "Microsoft.VisualBasic::1d421efd76f6bcd295a8d5a39d3c5207, mime\application%pdf\PdfReader\Document\PdfArray.vb"

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

    '     Class PdfArray
    ' 
    '         Properties: Objects, ParseArray
    ' 
    '         Constructor: (+1 Overloads) Sub New
    ' 
    '         Function: GetAllTextContent, GetWords
    ' 
    '         Sub: Visit
    ' 
    ' 
    ' /********************************************************************************/

#End Region

Imports System.Text

Namespace PdfReader

    ''' <summary>
    ''' PDF的实际内容存储的位置
    ''' </summary>
    Public Class PdfArray : Inherits PdfObject

        Private _wrapped As List(Of PdfObject)

        Public Sub New(ByVal parent As PdfObject, ByVal array As ParseArray)
            MyBase.New(parent, array)
        End Sub

        Public Overrides Sub Visit(ByVal visitor As IPdfObjectVisitor)
            visitor.Visit(Me)
        End Sub

        Public ReadOnly Property ParseArray As ParseArray
            Get
                Return TryCast(ParseObject, ParseArray)
            End Get
        End Property

        Public ReadOnly Property Objects As List(Of PdfObject)
            Get

                If _wrapped Is Nothing Then
                    _wrapped = New List(Of PdfObject)()

                    For Each obj In ParseArray.Objects
                        _wrapped.Add(WrapObject(obj))
                    Next
                End If

                Return _wrapped
            End Get
        End Property

        Public Iterator Function GetWords() As IEnumerable(Of ParseString)
            For Each obj As PdfObject In Objects
                If TypeOf obj.ParseObject Is ParseString Then
                    Yield DirectCast(obj.ParseObject, ParseString)
                End If
            Next
        End Function

        ''' <summary>
        ''' show text content
        ''' </summary>
        ''' <returns></returns>
        Public Function GetAllTextContent() As String
            Dim sb As StringBuilder = New StringBuilder()

            For Each word As ParseString In GetWords()
                sb.Append(word.Value)
            Next

            Return sb.ToString()
        End Function
    End Class
End Namespace

