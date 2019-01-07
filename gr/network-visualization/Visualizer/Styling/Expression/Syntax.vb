﻿Imports System.Runtime.CompilerServices
Imports Microsoft.VisualBasic.Serialization.JSON

Namespace Styling

    ''' <summary>
    ''' 从graph的属性值到相应的图形属性(节点大小，颜色，字体，形状)的映射操作类型
    ''' </summary>
    Public Enum MapperTypes
        ''' <summary>
        ''' 连续的数值型的映射
        ''' </summary>
        Continuous
        ''' <summary>
        ''' 离散的分类映射
        ''' </summary>
        Discrete
        ''' <summary>
        ''' 直接映射
        ''' </summary>
        Passthrough
    End Enum

    Public Module SyntaxExtensions

        ''' <summary>
        ''' 表达式之中的值不可以有逗号或者括号
        ''' </summary>
        ''' <param name="expression$">
        ''' + 区间映射 map(word, [min, max])
        ''' + 离散映射 map(word, val1=map1, val2=map2, ...)
        ''' </param>
        ''' <returns></returns>
        <Extension>
        Public Function MapExpressionParser(expression As String) As MapExpression
            Dim t$() = expression _
                .GetStackValue("(", ")") _
                .StringSplit("\s*,\s*")
            Dim values$()

            If t.Length = 3 AndAlso t(1).First = "["c AndAlso t(2).Last = "]"c Then
                values = New String() {
                    t(1).Substring(1),
                    t(2).Substring(0, t(2).Length - 1)
                }
                Return New MapExpression With {
                    .propertyName = t(0),
                    .type = MapperTypes.Continuous,
                    .values = values
                }
            Else
                Return New MapExpression With {
                    .propertyName = t(0),
                    .type = MapperTypes.Discrete,
                    .values = t.Skip(1).ToArray
                }
            End If
        End Function

        ''' <summary>
        ''' 因为可能会存在前导或者后置的空格，所以在这里就直接做模式匹配而不是绝对模式匹配了
        ''' </summary>
        ''' <param name="expression"></param>
        ''' <returns></returns>
        <MethodImpl(MethodImplOptions.AggressiveInlining)>
        Public Function IsMapExpression(expression As String) As Boolean
            Return expression.MatchPattern("map\(.+\)", RegexICSng)
        End Function
    End Module

    Public Structure MapExpression

        Dim propertyName As String
        Dim type As MapperTypes
        Dim values As String()

        Public ReadOnly Property AsDictionary As Dictionary(Of String, String)
            <MethodImpl(MethodImplOptions.AggressiveInlining)>
            Get
                Return values _
                    .Select(Function(s) s.GetTagValue("=", trim:=True)) _
                    .ToDictionary(Function(t) t.Name,
                                  Function(t) t.Value)
            End Get
        End Property

        Public Overrides Function ToString() As String
            If type = MapperTypes.Continuous Then
                Return $"Dim '{propertyName}' = [{values.JoinBy(", ")}]"
            Else
                Return $"Dim '{propertyName}' = {Me.AsDictionary.GetJson}"
            End If
        End Function
    End Structure
End Namespace