﻿#Region "Microsoft.VisualBasic::584a68f1ece99345b38e0642bb306175, Microsoft.VisualBasic.Core\Serialization\RawStream.vb"

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

'     Interface ISerializable
' 
'         Function: Serialize
' 
'     Class RawStream
' 
'         Constructor: (+2 Overloads) Sub New
'         Function: GetRawStream
' 
' 
' /********************************************************************************/

#End Region

Imports System.IO
Imports System.Runtime.CompilerServices
Imports System.Runtime.InteropServices
Imports System.Text
Imports Microsoft.VisualBasic.Linq
Imports Microsoft.VisualBasic.Parallel
Imports Microsoft.VisualBasic.Scripting.Runtime
Imports Microsoft.VisualBasic.Text

Namespace Serialization

    ''' <summary>
    ''' 支持序列化的对象，则这个对象可以被应用于<see cref="RequestStream"/>数据载体的网络传输操作过程
    ''' </summary>
    Public Interface ISerializable
        ''' <summary>
        ''' Transform this .NET object into a raw stream object for the network data transfer. 
        ''' </summary>
        ''' <returns></returns>
        Function Serialize() As Byte()
    End Interface

    Public Delegate Function ReadObject(Of T)(bytes As Byte(), offset As Integer) As T

    ''' <summary>
    ''' 原始串流的基本模型，这个流对象应该具备有两个基本的方法：
    ''' 1. 从原始的字节流之中反序列化构造出自身的构造函数
    ''' 2. 将自身序列化为字节流的<see cref="ISerializable.Serialize()"/>序列化方法
    ''' </summary>
    <Serializable> Public MustInherit Class RawStream : Implements ISerializable

        ''' <summary>
        ''' You should overrides this constructor to generate a stream object.(必须要有一个这个构造函数来执行反序列化)
        ''' </summary>
        ''' <param name="rawStream"></param>
        Sub New(rawStream As Byte())

        End Sub

        Public Sub New()
        End Sub

        ''' <summary>
        ''' <see cref="ISerializable.Serialize"/>序列化方法
        ''' </summary>
        ''' <returns></returns>
        Public MustOverride Function Serialize() As Byte() Implements ISerializable.Serialize

        ''' <summary>
        ''' 按照类型的定义进行反序列化操作
        ''' </summary>
        ''' <typeparam name="TRawStream"></typeparam>
        ''' <param name="rawStream"></param>
        ''' <returns></returns>
        ''' 
        <MethodImpl(MethodImplOptions.AggressiveInlining)>
        Public Shared Function GetRawStream(Of TRawStream As RawStream)(rawStream As Byte()) As TRawStream
            Return Activator.CreateInstance(GetType(TRawStream), {rawStream})
        End Function

        Protected Shared ReadOnly _rawStreamType As Type = GetType(Byte())

        Public Const INT64 As Integer = 8
        ''' <summary>
        ''' Single/Integer
        ''' </summary>
        Public Const INT32 As Integer = 4
        ''' <summary>
        ''' System.Double
        ''' </summary>
        Public Const DblFloat As Integer = 8
        Public Const ShortInt As Integer = 2
        Public Const SingleFloat As Integer = 4
        Public Const DecimalInt As Integer = 12

        Public Shared Function GetData(raw As Stream, code As TypeCode, Optional encoding As Encodings = Encodings.UTF8) As Array
            Dim type As Type = code.CreatePrimitiveType
            Dim bytes As Byte() = New Byte(raw.Length - 1) {}

            Call raw.Read(bytes, Scan0, bytes.Length)

            Select Case code
                Case TypeCode.Boolean
                    Dim flags As Boolean() = New Boolean(bytes.Length - 1) {}

                    For i As Integer = 0 To bytes.Length - 1
                        flags(i) = bytes(i) <> 0
                    Next

                    Return flags
                Case TypeCode.Byte
                    Return bytes
                Case TypeCode.Char
                    Return encoding.CodePage.GetString(bytes).ToArray
                Case TypeCode.Double
                    Return readInternal(bytes, AddressOf BitConverter.ToDouble)
                Case TypeCode.Single
                    Return readInternal(bytes, AddressOf BitConverter.ToSingle)
                Case TypeCode.String

                Case TypeCode.Int64
                    Return readInternal(bytes, AddressOf BitConverter.ToInt64)
                Case TypeCode.Int16
                    Return readInternal(bytes, AddressOf BitConverter.ToInt16)
                Case TypeCode.Int32
                    Return readInternal(bytes, AddressOf BitConverter.ToInt32)
                Case Else
                    Throw New NotImplementedException(code.ToString)
            End Select
        End Function

        Private Shared Function readInternal(Of T)(bytes As Byte(), read As ReadObject(Of T)) As T()
            Dim sizeof As Integer = Marshal.SizeOf(GetType(T))
            Dim objs As T() = New T(bytes.Length / sizeof - 1) {}

            For i As Integer = 0 To objs.Length - 1
                objs(i) = read(bytes, i * sizeof)
            Next

            Return objs
        End Function

        <MethodImpl(MethodImplOptions.AggressiveInlining)>
        Public Shared Function GetBytes(vector As Array, Optional encoding As Encodings = Encodings.UTF8) As Byte()
            Return BytesInternal(vector, encoding).IteratesALL.ToArray
        End Function

        Private Shared Function BytesInternal(vector As Array, encoding As Encodings) As IEnumerable(Of Byte())
            If TypeOf vector Is Integer() Then
                Return DirectCast(vector, Integer()).Select(Function(s) BitConverter.GetBytes(s))
            ElseIf TypeOf vector Is Long() Then
                Return DirectCast(vector, Long()).Select(Function(s) BitConverter.GetBytes(s))
            ElseIf TypeOf vector Is Double() Then
                Return DirectCast(vector, Double()).Select(Function(s) BitConverter.GetBytes(s))
            ElseIf TypeOf vector Is Single() Then
                Return DirectCast(vector, Single()).Select(Function(s) BitConverter.GetBytes(s))
            ElseIf TypeOf vector Is Boolean() Then
                Return DirectCast(vector, Boolean()).Select(Function(b) {If(b, CByte(1), CByte(0))})
            ElseIf TypeOf vector Is Byte() Then
                Return {DirectCast(vector, Byte())}
            ElseIf TypeOf vector Is String() Then
                Dim codepage As Encoding = encoding.CodePage

                Return DirectCast(vector, String()) _
                    .Select(Function(str)
                                Return codepage.GetBytes(str).JoinIterates({0})
                            End Function)
            Else
                Throw New NotImplementedException(vector.GetType.FullName)
            End If
        End Function
    End Class
End Namespace
