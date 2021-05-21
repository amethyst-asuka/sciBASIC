﻿
Namespace NlpVec

    ''' <summary>
    ''' the word score of the vector model
    ''' </summary>
    Public Class WordScore
        Implements IComparable(Of WordScore)

        Public name As String
        Public score As Single

        Public Sub New(name As String, score As Single)
            Me.name = name
            Me.score = score
        End Sub

        Public Overrides Function ToString() As String
            Return name & vbTab & score
        End Function

        Public Function CompareTo(o As WordScore) As Integer Implements IComparable(Of WordScore).CompareTo
            If score = o.score Then
                Return 0
            ElseIf score < o.score Then
                Return 1
            Else
                Return -1
            End If
        End Function
    End Class
End Namespace