﻿#Region "Microsoft.VisualBasic::eb85e2658caab5610e1efd29fb139c60, Data_science\Mathematica\Math\Math\Scripting\Expression\Expression\BinaryExpression.vb"

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

    '     Class BinaryExpression
    ' 
    '         Properties: [operator], left, right
    ' 
    '         Constructor: (+1 Overloads) Sub New
    '         Function: Evaluate, ToString
    ' 
    ' 
    ' /********************************************************************************/

#End Region

Namespace Scripting.MathExpression.Impl

    Public Class BinaryExpression : Inherits Expression

        Public ReadOnly Property left As Expression
        Public ReadOnly Property right As Expression
        Public ReadOnly Property [operator] As Char

        Sub New(left As Expression, right As Expression, op As Char)
            Me.left = left
            Me.right = right
            Me.operator = op
        End Sub

        Public Overrides Function Evaluate(env As ExpressionEngine) As Double
            Dim left As Double = Me.left.Evaluate(env)
            Dim right As Double = Me.right.Evaluate(env)
            Dim result As Double = Arithmetic.Evaluate(left, right, [operator])

            Return result
        End Function

        Public Overrides Function ToString() As String
            Return $"({left} {[operator]} {right})"
        End Function
    End Class
End Namespace