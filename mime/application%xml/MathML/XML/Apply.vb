﻿#Region "Microsoft.VisualBasic::920759a2b3cc9ff083af085f97622086, mime\application%xml\MathML\XML\Apply.vb"

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

    '     Class Apply
    ' 
    '         Properties: [operator], apply, cn, divide, plus
    '                     power, times
    ' 
    ' 
    ' /********************************************************************************/

#End Region

Imports System.Xml.Serialization

Namespace MathML

    Public Class Apply : Inherits symbols

        Public Property divide As mathOperator
        Public Property times As mathOperator
        Public Property plus As mathOperator
        Public Property power As mathOperator

        Public Property cn As constant

        <XmlElement("apply")>
        Public Property apply As Apply()

        Public ReadOnly Property [operator] As String
            Get
                If Not divide Is Nothing Then
                    Return "/"
                ElseIf Not times Is Nothing Then
                    Return "*"
                ElseIf Not plus Is Nothing Then
                    Return "+"
                ElseIf Not power Is Nothing Then
                    Return "^"
                Else
                    Return "-"
                End If
            End Get
        End Property
    End Class

End Namespace
