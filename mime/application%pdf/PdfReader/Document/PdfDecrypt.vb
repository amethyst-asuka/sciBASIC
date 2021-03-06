﻿#Region "Microsoft.VisualBasic::f76c043490b1ebc321bd2637655313c6, mime\application%pdf\PdfReader\Document\PdfDecrypt.vb"

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

    '     Class PdfDecrypt
    ' 
    '         Constructor: (+1 Overloads) Sub New
    '         Function: CreateDecrypt
    ' 
    ' 
    ' /********************************************************************************/

#End Region

Imports System

Namespace PdfReader
    Public MustInherit Class PdfDecrypt
        Inherits PdfObject

        Public Sub New(ByVal parent As PdfObject)
            MyBase.New(parent)
        End Sub

        Public MustOverride Function DecodeString(ByVal str As PdfString) As String
        Public MustOverride Function DecodeStringAsBytes(ByVal str As PdfString) As Byte()
        Public MustOverride Function DecodeStream(ByVal stream As PdfStream) As String
        Public MustOverride Function DecodeStreamAsBytes(ByVal stream As PdfStream) As Byte()

        Public Shared Function CreateDecrypt(ByVal doc As PdfDocument, ByVal trailer As PdfDictionary) As PdfDecrypt
            Dim ret As PdfDecrypt = New PdfDecryptNone(doc)

            ' Check for optional encryption reference
            Dim encryptRef = trailer.OptionalValue(Of PdfObjectReference)("Encrypt")

            If encryptRef IsNot Nothing Then
                Dim encryptDict = doc.IndirectObjects.OptionalValue(Of PdfDictionary)(encryptRef)
                Dim filter = encryptDict.MandatoryValue(Of PdfName)("Filter")
                Dim v = encryptDict.OptionalValue(Of PdfInteger)("V")

                ' We only implement the simple Standard, Version 1 scheme
                If Equals(filter.Value, "Standard") AndAlso v IsNot Nothing AndAlso v.Value = 1 Then
                    ret = New PdfDecryptStandard(doc, trailer, encryptDict)
                Else
                    Throw New ApplicationException("Can only decrypt the standard handler with version 1.")
                End If
            End If

            Return ret
        End Function
    End Class
End Namespace
