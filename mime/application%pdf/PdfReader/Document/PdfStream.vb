﻿#Region "Microsoft.VisualBasic::0441d5eb9caa47a8f885c6f0f400e6c0, mime\application%pdf\PdfReader\Document\PdfStream.vb"

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

    '     Class PdfStream
    ' 
    '         Properties: Dictionary, HasFilter, ParseStream, Value, ValueAsBytes
    ' 
    '         Constructor: (+1 Overloads) Sub New
    '         Sub: Visit
    ' 
    ' 
    ' /********************************************************************************/

#End Region


Namespace PdfReader
    Public Class PdfStream
        Inherits PdfObject

        Private _dictionary As PdfDictionary

        Public Sub New(ByVal parent As PdfObject, ByVal stream As ParseStream)
            MyBase.New(parent, stream)
        End Sub

        Public Overrides Sub Visit(ByVal visitor As IPdfObjectVisitor)
            visitor.Visit(Me)
        End Sub

        Public ReadOnly Property ParseStream As ParseStream
            Get
                Return TryCast(ParseObject, ParseStream)
            End Get
        End Property

        Public ReadOnly Property HasFilter As Boolean
            Get
                Return ParseStream.HasFilter
            End Get
        End Property

        Public ReadOnly Property Dictionary As PdfDictionary
            Get
                If _dictionary Is Nothing Then _dictionary = New PdfDictionary(Me, ParseStream.Dictionary)
                Return _dictionary
            End Get
        End Property

        Public ReadOnly Property Value As String
            Get
                Return Decrypt.DecodeStream(Me)
            End Get
        End Property

        Public ReadOnly Property ValueAsBytes As Byte()
            Get
                Return Decrypt.DecodeStreamAsBytes(Me)
            End Get
        End Property
    End Class
End Namespace

