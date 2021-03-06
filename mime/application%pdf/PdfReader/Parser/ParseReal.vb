﻿#Region "Microsoft.VisualBasic::e9ec99e8382633aa47f71f0e34ea779e, mime\application%pdf\PdfReader\Parser\ParseReal.vb"

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

    '     Class ParseReal
    ' 
    '         Properties: Value
    ' 
    '         Constructor: (+2 Overloads) Sub New
    ' 
    ' 
    ' /********************************************************************************/

#End Region

Namespace PdfReader
    Public Class ParseReal
        Inherits ParseObjectBase

        Private _Value As Single

        Public Sub New(ByVal token As TokenReal)
            Me.New(token.Value)
        End Sub

        Public Sub New(ByVal value As Single)
            Me.Value = value
        End Sub

        Public Property Value As Single
            Get
                Return _Value
            End Get
            Private Set(ByVal value As Single)
                _Value = value
            End Set
        End Property
    End Class
End Namespace
