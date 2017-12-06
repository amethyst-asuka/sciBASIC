﻿Imports System.IO
Imports System.Runtime.CompilerServices
Imports Microsoft.VisualBasic.Language

''' <summary>
''' shortest_common_superstring
''' </summary>
Public Module SCS

    <Extension>
    Public Sub TableView(fragments As IEnumerable(Of String), SCS$, ByRef print As TextWriter, Optional empty As Char = "."c)
        Call print.WriteLine(SCS)

        For Each str As String In fragments
            Dim start% = InStr(SCS, str) - 1
            Dim ends% = start + str.Length
            Dim lefts = SCS.Length - ends
            Dim view$ = New String(empty, start) & str & New String(empty, lefts)

            Call print.WriteLine(view)
        Next

        Call print.Flush()
    End Sub

    ''' <summary>
    ''' Solve using Greedy. Forf all string find the max common prefix/suffix. Merge those two strings
    ''' and continue it.
    ''' </summary>
    ''' <remarks>
    ''' https://github.com/aakash01/codebase/blob/60394bf92eb09410c07eec1c4d3c81cf0fc72a70/src/com/aakash/practice/interviewbit_may2017/dynamic_programming/ShortestCommonSuperString.java
    ''' </remarks>
    Public Function ShortestCommonSuperString(Seqs As List(Of String)) As String
        Dim l As Integer = Seqs.Count

        Do While l > 1
            Dim currMax As Integer = Integer.MinValue
            Dim finalStr As String = Nothing
            Dim p As Integer = -1, q As Integer = -1

            For j As Integer = 0 To l - 1
                For k As Integer = j + 1 To l - 1
                    Dim str As String = Seqs(j)
                    Dim b As String = Seqs(k)

                    If str.Contains(b) Then
                        If b.Length > currMax Then
                            finalStr = str
                            currMax = b.Length
                            p = j
                            q = k
                        End If
                    ElseIf b.Contains(str) Then
                        If str.Length > currMax Then
                            finalStr = b
                            currMax = str.Length
                            p = j
                            q = k
                        End If
                    Else
                        ' find max common prefix and suffix
                        Dim maxPrefixMatch = MaxPrefixLength(str, b)
                        If maxPrefixMatch > currMax Then
                            finalStr = str + b.Substring(maxPrefixMatch)
                            currMax = maxPrefixMatch
                            p = j
                            q = k
                        End If

                        Dim maxSuffixMatch = MaxPrefixLength(b, str)
                        If maxSuffixMatch > currMax Then
                            finalStr = b + str.Substring(maxSuffixMatch)
                            currMax = maxSuffixMatch
                            p = j
                            q = k
                        End If
                    End If
                Next
            Next

            l -= 1
            Seqs(p) = finalStr
            Seqs(q) = Seqs(l)
        Loop

        Return Seqs.First
    End Function

    Private Function MaxPrefixLength(a As String, b As String) As Integer
        Dim max As Integer = 0
        Dim prefix$

        For i As Integer = 0 To b.Length - 1
            prefix = b.Substring(0, i + 1)

            If a.EndsWith(prefix) Then
                max = i + 1
            End If
        Next

        Return max
    End Function
End Module
