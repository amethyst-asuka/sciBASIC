Imports System.Runtime.CompilerServices
Imports Microsoft.VisualBasic.DataMining.ComponentModel
Imports Microsoft.VisualBasic.Language
Imports Microsoft.VisualBasic.Serialization.JSON

Namespace DecisionTree

    ''' <summary>
    ''' A row in data table.(分类用的对象实例)
    ''' </summary>
    ''' <remarks>
    ''' 属性向量<see cref="Entity.entityVector"/>的最后一个值总是用来表示<see cref="Entity.decisions"/>结果值
    ''' </remarks>
    Public Class Entity : Inherits EntityBase(Of String)
        Implements ICloneable

        ''' <summary>
        ''' 分类结果
        ''' </summary>
        ''' <returns></returns>
        Public ReadOnly Property decisions As String
            Get
                Return entityVector(Length - 1)
            End Get
        End Property

        Public Overrides Function ToString() As String
            Return $"{decisions} ~ {entityVector.Take(Length - 1).GetJson}"
        End Function

        Public Function Clone() As Object Implements ICloneable.Clone
            Return New Entity With {
                .entityVector = entityVector.ToArray
            }
        End Function
    End Class

    Public Class DataTable

        ''' <summary>
        ''' Factor titles of <see cref="Entity.entityVector"/>
        ''' </summary>
        ''' <returns></returns>
        Public Property headers As String()

        ''' <summary>
        ''' 分类结果的显示标题
        ''' </summary>
        ''' <returns></returns>
        Public ReadOnly Property decisions As String
            <MethodImpl(MethodImplOptions.AggressiveInlining)>
            Get
                Return headers.Last
            End Get
        End Property

        ''' <summary>
        ''' Training set data
        ''' </summary>
        ''' <returns></returns>
        Public Property rows As Entity()

        ''' <summary>
        ''' Get the property fields count
        ''' </summary>
        ''' <returns></returns>
        Public ReadOnly Property columns As Integer
            Get
                Return headers.Length
            End Get
        End Property

        Default Public ReadOnly Property GetRow(index As Integer) As Entity
            <MethodImpl(MethodImplOptions.AggressiveInlining)>
            Get
                Return rows(index)
            End Get
        End Property

        Public Sub RemoveColumn(index As Integer)
            headers = headers.Delete(index)
            rows.DoEach(Sub(r) r.entityVector = r.entityVector.Delete(index))
        End Sub

        Public Overrides Function ToString() As String
            Return headers.GetJson
        End Function

        Public Shared Function [Imports](table As System.Data.DataTable) As DataTable
            Dim headers = Iterator Function() As IEnumerable(Of String)
                              For i As Integer = 0 To table.Columns.Count - 1
                                  Yield table.Columns(i).ToString
                              Next
                          End Function().ToArray
            Dim data As Entity() = Iterator Function() As IEnumerable(Of Entity)
                                       Dim row As New List(Of String)

                                       For i As Integer = 0 To table.Rows.Count - 1
                                           For j As Integer = 0 To table.Columns.Count - 1
                                               row += table.Rows(i)(j).ToString
                                           Next

                                           Yield New Entity With {
                                               .entityVector = row.ToArray
                                           }

                                           row *= 0
                                       Next
                                   End Function().ToArray

            Return New DataTable With {
                .headers = headers,
                .rows = data
            }
        End Function

    End Class

    Public Class ClassifyResult

        Public Property result As String
        Public Property explains As New List(Of String)

        Public Overrides Function ToString() As String
            Dim reason As String = explains _
                .Take(explains.Count - 1) _
                .Split(2) _
                .Select(Function(exp) $"({exp(Scan0)} Is '{exp(1)}')") _
                .JoinBy(" And ")

            Return $"{result} As [ {reason} ]"
        End Function
    End Class
End Namespace