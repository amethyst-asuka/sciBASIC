﻿#Region "Microsoft.VisualBasic::b343077c87065c0937e0399023a225d0, Data\BinaryData\BinaryData\SQLite3\Schema\Schema.vb"

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

'     Class Schema
' 
'         Properties: columns
' 
'         Constructor: (+1 Overloads) Sub New
'         Function: ParseColumns, ToString
' 
' 
' /********************************************************************************/

#End Region

Imports System.Runtime.CompilerServices
Imports Microsoft.VisualBasic.ComponentModel.DataSourceModel
Imports Microsoft.VisualBasic.Serialization.JSON

Namespace ManagedSqlite.Core.SQLSchema

    Public Class Schema

        Public Property columns As NamedValue(Of String)()
        Public Property tableName As String

        <MethodImpl(MethodImplOptions.AggressiveInlining)>
        Sub New(sql$, removeNameEscape As Boolean)
            Me.columns = ParseColumns(sql, removeNameEscape).ToArray
        End Sub

        Private Iterator Function ParseColumns(sql$, removeNameEscape As Boolean) As IEnumerable(Of NamedValue(Of String))
            Dim tokens As Token() = New SQLParser(sql).GetTokens.ToArray
            Dim [nameOf] = Function(text As String())
                               If removeNameEscape Then
                                   Return text(Scan0).GetStackValue("[", "]")
                               Else
                                   Return text(Scan0)
                               End If
                           End Function

            If Not (tokens(Scan0).isKeyword("create") AndAlso tokens(1).isKeyword("table")) Then
                Throw New InvalidProgramException("Only 'CREATE TABLE' expression is allowed!")
            End If

            Me.tableName = tokens(2).text

            tokens = tokens.Skip(4).Take(tokens.Length - 5).ToArray

            Dim type As String
            Dim name As String
            Dim blocks = tokens.SplitByTopLevelDelimiter(TokenTypes.comma)

            For Each block As Token() In blocks _
                .Where(Function(b)
                           Return Not b.Length = 1 AndAlso Not b(Scan0).name = TokenTypes.comma
                       End Function)

                name = block(Scan0).text.GetStackValue("""", """")
                type = block.ElementAtOrNull(1)?.text

                If type.ToLower = "[varchar]" Then
                    If tokens.Length > 2 AndAlso tokens(2).text.IsPattern("\(\s*\d+\s*\)") Then
                        type = type.GetStackValue("[", "]") & tokens(2).text
                    Else
                        type = type.GetStackValue("[", "]")
                    End If
                ElseIf type.ToLower = "not" AndAlso tokens(2).text.ToLower = "null" Then
                    type = "blob"
                End If

                Yield New NamedValue(Of String) With {
                    .Name = name,
                    .Value = type
                }
            Next
        End Function

        Public Overrides Function ToString() As String
            Return columns.Keys.GetJson
        End Function
    End Class
End Namespace
