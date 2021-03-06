﻿#Region "Microsoft.VisualBasic::2589894d8f2bd706be8f10b439a7585c, Microsoft.VisualBasic.Core\src\ComponentModel\Algorithm\QuickSortFunction.vb"

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

    '     Class QuickSortFunction
    ' 
    '         Constructor: (+1 Overloads) Sub New
    ' 
    '         Function: (+2 Overloads) QuickSort
    ' 
    '         Sub: QuickSort
    ' 
    ' 
    ' /********************************************************************************/

#End Region

Namespace ComponentModel.Algorithm

#If netcore5 = 1 Or NET_48 = 1 Then

    ''' <summary>
    ''' 快速排序基本上被认为是相同数量级的所有排序算法中，平均性能最好的。
    ''' </summary>
    Public Class QuickSortFunction(Of K, T)

        ReadOnly compares As Comparison(Of K)

        Sub New(compares As Comparison(Of K))
            Me.compares = compares
        End Sub

        Public Function QuickSort(src As IEnumerable(Of (K, T))) As (K, T)()
            Dim input As (K, T)() = src.ToArray
            Call QuickSort(input, Scan0, input.Length - 1)
            Return input
        End Function

        Public Function QuickSort(list As IEnumerable(Of T), key As Func(Of T, K)) As T()
            Dim input As (K, T)() = list.Select(Function(p) (key(p), p)).ToArray
            Call QuickSort(input, Scan0, input.Length - 1)
            Return input.Select(Function(a) a.Item2).ToArray
        End Function

        Private Sub QuickSort(src As (key As K, T)(), begin As Integer, [end] As Integer)
            If begin < [end] Then
                Dim t = src(begin)
                Dim i = begin
                Dim j = [end]

                While (i < j)
                    While (i < j AndAlso compares(src(j).key, t.key) > 0)
                        j -= 1
                    End While
                    If (i < j) Then
                        src(i) = src(j)
                        i += 1
                    End If
                    While (i < j AndAlso compares(src(i).key, t.key) < 0)
                        i += 1
                    End While
                    If i < j Then
                        src(j) = src(i)
                        j -= 1
                    End If
                End While

                src(i) = t

                Call QuickSort(src, begin, i - 1)
                Call QuickSort(src, i + 1, [end])
            End If
        End Sub
    End Class
    
#End If
End Namespace
