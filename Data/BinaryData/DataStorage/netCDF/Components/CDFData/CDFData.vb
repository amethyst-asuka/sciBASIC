﻿#Region "Microsoft.VisualBasic::f155c75f0a0d91c1d729221c7ce6218b, Data\BinaryData\DataStorage\netCDF\Components\CDFData\CDFData.vb"

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

    '     Class CDFData
    ' 
    '         Properties: genericValue, ICDFDataVector_length
    ' 
    '         Function: GetBuffer, ToString
    ' 
    ' 
    ' /********************************************************************************/

#End Region

Imports System.Text
Imports Microsoft.VisualBasic.Language
Imports Microsoft.VisualBasic.Language.Vectorization
Imports Microsoft.VisualBasic.Linq
Imports Microsoft.VisualBasic.Net.Http

Namespace netCDF.Components

    ''' <summary>
    '''  存储在CDF文件之中的数据的统一接口模块
    ''' </summary>
    Public MustInherit Class CDFData(Of T) : Inherits Vector(Of T)
        Implements ICDFDataVector

        Public MustOverride ReadOnly Property cdfDataType As CDFDataTypes Implements ICDFDataVector.cdfDataType

        Public ReadOnly Property genericValue As Array Implements ICDFDataVector.genericValue
            Get
                Return buffer
            End Get
        End Property

        Private ReadOnly Property ICDFDataVector_length As Integer Implements ICDFDataVector.length
            Get
                Return buffer.Length
            End Get
        End Property

        Default Public Overloads Property Item(i As Integer) As T
            Get
                Return buffer(i)
            End Get
            Set(value As T)
                buffer(i) = value
            End Set
        End Property

        Default Public Overloads Property Item(i As i32) As T
            Get
                Return buffer(i)
            End Get
            Set(value As T)
                buffer(i) = value
            End Set
        End Property

        Public Overrides Function ToString() As String
            Dim stringify$

            Select Case cdfDataType
                Case CDFDataTypes.BYTE : stringify = DirectCast(genericValue, Byte()).ToBase64String
                Case CDFDataTypes.CHAR : stringify = DirectCast(genericValue, Char()).CharString
                Case CDFDataTypes.DOUBLE : stringify = DirectCast(genericValue, Double()).Select(Function(d) d.ToString("G3")).JoinBy(",")
                Case CDFDataTypes.FLOAT : stringify = DirectCast(genericValue, Single()).Select(Function(d) d.ToString("G3")).JoinBy(",")
                Case CDFDataTypes.INT : stringify = DirectCast(genericValue, Integer()).JoinBy(",")
                Case CDFDataTypes.SHORT : stringify = DirectCast(genericValue, Short()).JoinBy(",")
                Case CDFDataTypes.LONG : stringify = DirectCast(genericValue, Long()).JoinBy(",")
                Case CDFDataTypes.BOOLEAN : stringify = DirectCast(genericValue, Boolean()).Select(Function(b) If(b, 1, 0)).JoinBy(",")
                Case Else
                    Return "invalid!"
            End Select

            If (stringify.Length > 50) Then
                stringify = stringify.Substring(0, 50)
            End If
            If (cdfDataType <> CDFDataTypes.undefined) Then
                stringify &= $" (length: ${Me.Length})"
            End If

            Return $"[{cdfDataType}] {stringify}"
        End Function

        Public Function GetBuffer(encoding As Encoding) As Byte() Implements ICDFDataVector.GetBuffer
            Dim chunks As Byte()()

            Select Case cdfDataType
                Case CDFDataTypes.BYTE : Return DirectCast(CObj(Me), bytes).Array
                Case CDFDataTypes.BOOLEAN : Return DirectCast(CObj(Me), flags).Array.Select(Function(b) CByte(If(b, 1, 0))).ToArray
                Case CDFDataTypes.CHAR : Return encoding.GetBytes(DirectCast(CObj(Me), chars).CharString)
                Case CDFDataTypes.DOUBLE
                    chunks = DirectCast(CObj(Me), doubles).Array _
                        .Select(AddressOf BitConverter.GetBytes) _
                        .ToArray
                Case CDFDataTypes.FLOAT
                    chunks = DirectCast(CObj(Me), floats).Array _
                        .Select(AddressOf BitConverter.GetBytes) _
                        .ToArray
                Case CDFDataTypes.INT
                    chunks = DirectCast(CObj(Me), integers).Array _
                        .Select(AddressOf BitConverter.GetBytes) _
                        .ToArray
                Case CDFDataTypes.SHORT
                    chunks = DirectCast(CObj(Me), shorts).Array _
                        .Select(AddressOf BitConverter.GetBytes) _
                        .ToArray
                Case CDFDataTypes.LONG
                    chunks = DirectCast(CObj(Me), longs).Array _
                        .Select(AddressOf BitConverter.GetBytes) _
                        .ToArray
                Case Else
                    Throw New NotImplementedException(cdfDataType.Description)
            End Select

            If BitConverter.IsLittleEndian Then
                Return chunks _
                    .Select(Function(c)
                                System.Array.Reverse(c)
                                Return c
                            End Function) _
                    .IteratesALL _
                    .ToArray
            Else
                Return chunks.IteratesALL.ToArray
            End If
        End Function
    End Class
End Namespace
