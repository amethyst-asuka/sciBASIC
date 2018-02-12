﻿Imports System.Runtime.CompilerServices
Imports System.Text
Imports Microsoft.VisualBasic.ComponentModel.DataSourceModel
Imports Microsoft.VisualBasic.Emit.Marshal
Imports Microsoft.VisualBasic.Language
Imports Microsoft.VisualBasic.Language.Values
Imports Microsoft.VisualBasic.Scripting.SymbolBuilder
Imports Microsoft.VisualBasic.Text

Namespace ApplicationServices.Development

    Public Module VBCodeSignature

        Const AccessPattern$ = "((Public )|(Private )|(Friend )|(Protected )|(Shadows )|(Shared )|(Overrides )|(Overloads )|(Overridable )|(MustOverrides )|(NotInheritable )|(MustInherit ))*"
        Const TypePatterns$ = "^\s*" & AccessPattern & "((Class)|(Module)|(Structure)|(Enum)|(Delegate)|(Interface))\s+" & VBLanguage.IdentiferPattern
        Const PropertyPatterns$ = "^\s+" & AccessPattern & "\s*((ReadOnly )|(WriteOnly )|(Default ))*\s*Property\s+" & VBLanguage.IdentiferPattern
        Const MethodPatterns$ = "^\s+" & AccessPattern & "\s*((Sub )|(Function )|(Iterator )|(Operator ))+\s*" & VBLanguage.IdentiferPattern
        Const OperatorPatterns$ = "^\s+" & AccessPattern & "\s*Operator\s+(([<]|[>]|\=|\+|\-|\*|/|\^|\\)+|(" & VBLanguage.IdentiferPattern & "))"
        Const ClosePatterns$ = "^\s+End\s((Sub)|(Function)|(Class)|(Structure)|(Enum)|(Interface)|(Operator)|(Module))"
        Const CloseTypePatterns$ = "^\s*End\s((Class)|(Structure)|(Enum)|(Interface)|(Module))"
        Const IndentsPattern$ = "^\s+"

        <MethodImpl(MethodImplOptions.AggressiveInlining)>
        <Extension> Public Function SummaryModules(vb As String) As String
            Dim vblines As Pointer(Of String) = vb.lTokens
            Dim summary As New StringBuilder

            Do While Not vblines.EndRead
                summary.AppendLine(vblines.SummaryInternal(vb))
            Loop

            Return summary.ToString
        End Function

        <Extension>
        Private Function SummaryInternal(vblines As Pointer(Of String), vb$) As String
            Dim line$
            Dim tokens As Value(Of String) = ""
            Dim list As List(Of String)
            Dim type$
            Dim name$
            Dim indents$
            Dim properties As New List(Of NamedValue(Of String))
            Dim methods As New List(Of NamedValue(Of String))
            Dim operators As New List(Of NamedValue(Of String))
            Dim container As New NamedValue(Of String)
            Dim innerModules As New StringBuilder

            Do While Not vblines.EndRead
                line = ++vblines

                If Not (tokens = line.Match(TypePatterns, RegexICMul)).StringEmpty Then
                    list = tokens.Split(" "c).AsList
                    type = list(-2)
                    name = list(-1)
                    indents = line.Match(IndentsPattern, RegexICMul)

                    If type = "Enum" Then
                        Dim members = vb _
                            .Match("Enum\s+" & name & ".+?End Enum", RegexICSng) _
                            .lTokens _
                            .Where(Function(s) s.IsPattern("\s+" & VBLanguage.IdentiferPattern & "\s*([=].+?)?\s*")) _
                            .Select(AddressOf Trim) _
                            .Where(Function(s) Not s.StringEmpty) _
                            .ToArray

                        Dim enumType As New StringBuilder
                        Dim memberList = members.memberList

                        enumType.AppendLine(indents & type & " " & name)
                        enumType.AppendLine()

                        For Each line In memberList
                            enumType.AppendLine(indents & "    " & line)
                        Next

                        If container.IsEmpty Then
                            Return enumType.ToString
                        Else
                            innerModules.AppendLine(enumType.ToString)
                        End If
                    Else
                        If container.IsEmpty Then
                            container = New NamedValue(Of String)(name, type, indents.Trim(ASCII.CR, ASCII.LF))
                        Else
                            ' 下一层堆栈
                            innerModules.AppendLine((vblines - 1).SummaryInternal(vb))
                        End If
                    End If
                End If
                If Not (tokens = line.Match(PropertyPatterns, RegexICMul)).StringEmpty Then
                    list = tokens.Split(" "c).AsList
                    type = list(-2)
                    name = list(-1)
                    indents = line.Match(IndentsPattern, RegexICMul)

                    properties += New NamedValue(Of String)(name, type, indents)
                End If
                If Not (tokens = line.Match(MethodPatterns, RegexICMul)).StringEmpty Then
                    list = tokens.Split(" "c).AsList
                    type = list(-2)
                    name = list(-1)
                    indents = line.Match(IndentsPattern, RegexICMul)

                    If type = "Operator" Then
                        operators += New NamedValue(Of String)(name, type, indents)
                    Else
                        methods += New NamedValue(Of String)(name, type, indents)
                    End If
                End If
                If Not (tokens = line.Match(OperatorPatterns, RegexICMul)).StringEmpty Then
                    list = tokens.Split(" "c).AsList
                    type = list(-2)
                    name = list(-1)
                    indents = line.Match(IndentsPattern, RegexICMul)

                    If type = "Operator" Then
                        operators += New NamedValue(Of String)(name, type, indents)
                    Else
                        methods += New NamedValue(Of String)(name, type, indents)
                    End If
                End If
                If Not (tokens = line.Match(CloseTypePatterns, RegexICMul)).StringEmpty Then
                    Return container.typeSummary(properties, methods, operators, innerModules)
                End If
            Loop

            If Not container.IsEmpty Then
                Return container.typeSummary(properties, methods, operators, innerModules)
            ElseIf Not innerModules.Length = 0 Then
                Return innerModules.ToString
            Else
                Return ""
            End If
        End Function

        <Extension>
        Private Function typeSummary(container As NamedValue(Of String),
                                     properties As List(Of NamedValue(Of String)),
                                     methods As List(Of NamedValue(Of String)),
                                     operators As List(Of NamedValue(Of String)),
                                     innerModules As StringBuilder) As String

            Dim vbType As New StringBuilder
            Dim members As New List(Of String)
            Dim prefix$
            Dim lines$()

            vbType.AppendLine(container.Description & container.Value & " " & container.Name)
            vbType.AppendLine()

            If Not properties.IsNullOrEmpty Then
                prefix = container.Description & "    Properties: "
                lines = properties.Keys.memberList
                members += prefix & lines(Scan0)
                members += lines _
                    .Skip(1) _
                    .Select(Function(l) New String(" "c, prefix.Length) & l) _
                    .JoinBy(ASCII.LF)

                If Not methods.IsNullOrEmpty Then
                    members += ""
                End If
            End If
            If Not methods.IsNullOrEmpty Then
                Dim types = methods _
                    .GroupBy(Function(m) m.Value) _
                    .ToDictionary(Function(t) t.Key,
                                  Function(l) l.Keys.memberList)

                If types.ContainsKey("Function") Then
                    prefix = container.Description & $"    Function: "
                    members += prefix & types!Function.First
                    members += types!Function _
                        .Skip(1) _
                        .Select(Function(l) New String(" "c, prefix.Length) & l) _
                        .JoinBy(ASCII.LF)

                    If types.Count > 1 Then
                        members += ""
                    End If
                End If
                If types.ContainsKey("Sub") Then
                    prefix = container.Description & $"    Sub: "
                    members += prefix & types!Sub.First
                    members += types!Sub _
                        .Skip(1) _
                        .Select(Function(l) New String(" "c, prefix.Length) & l) _
                        .JoinBy(ASCII.LF)

                    If Not operators.IsNullOrEmpty Then
                        members += ""
                    End If
                End If
            End If
            If Not operators.IsNullOrEmpty Then
                prefix = container.Description & "    Operators: "
                lines = operators.Keys.memberList
                members += prefix & lines(Scan0)
                members += lines _
                    .Skip(1) _
                    .Select(Function(l) New String(" "c, prefix.Length) & l) _
                    .JoinBy(ASCII.LF)
            End If

            vbType.AppendLine(members.JoinBy(ASCII.LF))

            If innerModules.Length > 0 Then
                vbType.AppendLine(innerModules.ToString)
            End If

            Return vbType.ToString
        End Function

        <MethodImpl(MethodImplOptions.AggressiveInlining)>
        <Extension> Private Function memberList(names As IEnumerable(Of String)) As String()
            Return names _
                .GroupBy(Function(pName) pName) _
                .OrderBy(Function(pName) pName.Key) _
                .Select(Function(overload)
                            If overload.Count = 1 Then
                                Return overload.Key
                            Else
                                Return $"(+{overload.Count} Overloads) " & overload.Key
                            End If
                        End Function) _
                .Split(5) _
                .Select(Function(part) part.JoinBy(", ")) _
                .ToArray
        End Function
    End Module
End Namespace