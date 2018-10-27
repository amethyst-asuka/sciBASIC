﻿#Region "Microsoft.VisualBasic::fce8a9ea7c599c8100764ec6c4800e82, Microsoft.VisualBasic.Core\ComponentModel\ValuePair\TagData\FactorValue.vb"

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

    '     Class FactorValue
    ' 
    '         Properties: Factor, Value
    ' 
    '     Class FactorString
    ' 
    '         Properties: Factor, text
    ' 
    ' 
    ' /********************************************************************************/

#End Region

Namespace ComponentModel.TagData

    Public Class FactorValue(Of T As {Structure, IComparable(Of T)}, V)

        Public Property Factor As T
        Public Property Value As V

    End Class

    Public Class FactorString(Of T As {Structure, IComparable(Of T)})

        Public Property Factor As T
        Public Property text As String

    End Class
End Namespace
