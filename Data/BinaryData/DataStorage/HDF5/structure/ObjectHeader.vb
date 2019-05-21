﻿#Region "Microsoft.VisualBasic::6a130d643fec10050ebabc4db0c44dea, mime\application%netcdf\HDF5\structure\ObjectHeader.vb"

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

    ' 	Class ObjectHeader
    ' 
    ' 	    Properties: address, headerMessages, objectHeaderSize, objectReferenceCount, totalNumberOfHeaderMessages
    '                  version
    ' 
    ' 	    Constructor: (+1 Overloads) Sub New
    ' 
    ' 	    Function: readVersion1
    ' 
    ' 	    Sub: printValues, readVersion2
    ' 
    ' 
    ' /********************************************************************************/

#End Region

'
' * Mostly copied from NETCDF4 source code.
' * refer : http://www.unidata.ucar.edu
' * 
' * Modified by iychoi@email.arizona.edu
' 


Imports System.IO
Imports BinaryReader = Microsoft.VisualBasic.Data.IO.HDF5.IO.BinaryReader

Namespace HDF5.[Structure]

    Public Class ObjectHeader : Inherits HDF5Ptr

        Public Overridable ReadOnly Property version As Integer
        Public Overridable ReadOnly Property totalNumberOfHeaderMessages As Integer
        Public Overridable ReadOnly Property objectReferenceCount As Integer
        Public Overridable ReadOnly Property objectHeaderSize As Integer
        Public Overridable ReadOnly Property headerMessages As New List(Of ObjectHeaderMessage)

        Public Sub New([in] As BinaryReader, sb As Superblock, address As Long)
            Call MyBase.New(address)

            [in].offset = address

            Me.version = [in].readByte()

            If Me.version = 1 Then

                [in].skipBytes(1)

                Me.totalNumberOfHeaderMessages = [in].readShort()
                Me.objectReferenceCount = [in].readInt()
                Me.objectHeaderSize = [in].readInt()

                [in].skipBytes(4)

                readVersion1([in], sb, [in].offset, Me.totalNumberOfHeaderMessages, Long.MaxValue)
            Else
                readVersion2([in], sb, [in].offset)
			End If
		End Sub

        Private Function readVersion1([in] As BinaryReader, sb As Superblock, address As Long, readMessages As Integer, maxBytes As Long) As Integer
            Dim count As Integer = 0
            Dim byteRead As Integer = 0
            ' read messages
            Dim messageOffset As Long = address

            [in].offset = address

            While count < readMessages AndAlso byteRead < maxBytes
				Dim msg As New ObjectHeaderMessage([in], sb, messageOffset)

				messageOffset += msg.headerLength + msg.sizeOfHeaderMessageData
				byteRead += msg.headerLength + msg.sizeOfHeaderMessageData

				count += 1

				If msg.headerMessageType Is ObjectHeaderMessageType.ObjectHeaderContinuation Then
					' CONTINUE
					Dim cmsg As ContinueMessage = msg.continueMessage
					Dim continuationBlockFilePos As Long = cmsg.offset

					count += readVersion1([in], sb, continuationBlockFilePos, readMessages - count, cmsg.length)
				ElseIf msg.headerMessageType IsNot ObjectHeaderMessageType.NIL Then
                    ' NOT NIL
                    Me.headerMessages.Add(msg)
                End If
			End While
			Return count
		End Function

        Private Sub readVersion2([in] As BinaryReader, sb As Superblock, address As Long)
			Throw New IOException("version not implented")
		End Sub

        Public Overridable Sub printValues()
            Console.WriteLine("ObjectHeader >>>")
            Console.WriteLine("address : " & Me.m_address)
            Console.WriteLine("version : " & Me.version)
            Console.WriteLine("number of messages : " & Me.totalNumberOfHeaderMessages)
            Console.WriteLine("object reference count : " & Me.objectReferenceCount)
            Console.WriteLine("object header size : " & Me.objectHeaderSize)

            For i As Integer = 0 To Me.headerMessages.Count - 1
                Me.headerMessages(i).printValues()
            Next

            Console.WriteLine("ObjectHeader <<<")
        End Sub
    End Class

End Namespace
