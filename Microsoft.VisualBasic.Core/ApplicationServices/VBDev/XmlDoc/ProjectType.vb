﻿#Region "Microsoft.VisualBasic::b1a1ea07fb5ce7820363aefac6bc88f8, ..\sciBASIC#\Microsoft.VisualBasic.Core\ApplicationServices\VBDev\XmlDoc\ProjectType.vb"

' Author:
' 
'       asuka (amethyst.asuka@gcmodeller.org)
'       xieguigang (xie.guigang@live.com)
'       xie (genetics@smrucc.org)
' 
' Copyright (c) 2018 GPL3 Licensed
' 
' 
' GNU GENERAL PUBLIC LICENSE (GPL3)
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

#End Region

' Copyright (c) Bendyline LLC. All rights reserved. Licensed under the Apache License, Version 2.0.
'    You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0. 


Imports System.Runtime.CompilerServices
Imports System.Xml
Imports Microsoft.VisualBasic.Text

Namespace ApplicationServices.Development.XmlDoc.Assembly

    ''' <summary>
    ''' A type within a project namespace.
    ''' </summary>
    Public Class ProjectType

        Protected projectNamespace As ProjectNamespace
        Protected fields As Dictionary(Of String, ProjectMember)
        ''' <summary>
        ''' 因为属性存在参数，所以可能会出现重载的情况
        ''' </summary>
        Protected properties As Dictionary(Of String, List(Of ProjectMember))
        ''' <summary>
        ''' 会出现重载函数，所以这里也应该是一个list
        ''' </summary>
        Protected methods As Dictionary(Of String, List(Of ProjectMember))

        Public ReadOnly Property [Namespace]() As ProjectNamespace
            <MethodImpl(MethodImplOptions.AggressiveInlining)>
            Get
                Return Me.projectNamespace
            End Get
        End Property

        Public Property Name As String
        Public Property Summary As String
        Public Property Remarks As String

        Public Sub New(projectNamespace As ProjectNamespace)
            Me.projectNamespace = projectNamespace

            Me.fields = New Dictionary(Of String, ProjectMember)()
            Me.properties = New Dictionary(Of String, List(Of ProjectMember))()
            Me.methods = New Dictionary(Of String, List(Of ProjectMember))()
        End Sub

        Protected Sub New(type As ProjectType)
            projectNamespace = type.projectNamespace
            fields = type.fields
            properties = type.properties
            methods = type.methods
            Name = type.Name
            Summary = type.Summary
            Remarks = type.Remarks
        End Sub

        Friend Sub New(t1 As ProjectType, t2 As ProjectType)
            projectNamespace = t1.projectNamespace
            ' fields = (t1.fields.Values.AsList + t2.fields.Values).GroupBy()
        End Sub

        Public Overrides Function ToString() As String
            Return Name
        End Function

        <MethodImpl(MethodImplOptions.AggressiveInlining)>
        Public Function GetMethods(methodName As String) As List(Of ProjectMember)
            Return getInternal(methods, methodName.ToLower)
        End Function

        Public Function EnsureMethod(methodName As String) As ProjectMember
            Dim pmlist As List(Of ProjectMember) = Me.GetMethods(methodName)
            Dim pm As New ProjectMember(Me) With {
                .Name = methodName
            }

            Call pmlist.Add(pm)

            Return pm
        End Function

        Private Shared Function getInternal(ByRef table As Dictionary(Of String, List(Of ProjectMember)), name$) As List(Of ProjectMember)
            If table.ContainsKey(name) Then
                Return table(name)
            Else
                Dim list As New List(Of ProjectMember)
                table.Add(name, list)
                Return list
            End If
        End Function

        <MethodImpl(MethodImplOptions.AggressiveInlining)>
        Public Function GetProperties(propertyName As String) As List(Of ProjectMember)
            Return getInternal(properties, propertyName.ToLower)
        End Function

        Public Function EnsureProperty(propertyName As String) As ProjectMember
            Dim pmlist As List(Of ProjectMember) = Me.GetProperties(propertyName)
            Dim pm As New ProjectMember(Me) With {
                .Name = propertyName
            }

            Call pmlist.Add(pm)

            Return pm
        End Function

        Public Function GetField(fieldName As String) As ProjectMember
            If Me.fields.ContainsKey(fieldName.ToLower()) Then
                Return Me.fields(fieldName.ToLower())
            End If

            Return Nothing
        End Function

        Public Function EnsureField(fieldName As String) As ProjectMember
            Dim pm As ProjectMember = Me.GetField(fieldName)

            If pm Is Nothing Then
                pm = New ProjectMember(Me) With {
                    .Name = fieldName
                }

                Me.fields.Add(fieldName.ToLower(), pm)
            End If

            Return pm
        End Function

        Public Sub LoadFromNode(xn As XmlNode)
            Dim summaryNode As XmlNode = xn.SelectSingleNode("summary")

            If summaryNode IsNot Nothing Then
                Me.Summary = summaryNode.InnerText.Trim(ASCII.CR, ASCII.LF, " ")
            End If

            summaryNode = xn.SelectSingleNode("remarks")

            If Not summaryNode Is Nothing Then
                Remarks = summaryNode.InnerText.Trim(ASCII.CR, ASCII.LF, " ")
            End If
        End Sub
    End Class
End Namespace
