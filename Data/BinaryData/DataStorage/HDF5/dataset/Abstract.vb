
'*****************************************************************************
' This file is part of jHDF. A pure Java library for accessing HDF5 files.
' 
' http://jhdf.io
' 
' Copyright 2019 James Mudd
' 
' MIT License see 'LICENSE' file
' *****************************************************************************

Imports Microsoft.VisualBasic.Data.IO.HDF5.struct
Imports Microsoft.VisualBasic.Data.IO.HDF5.type

Namespace HDF5.dataset

    Public MustInherit Class Hdf5Dataset

        Public Property dataType As DataType
        Public Property dataSpace As DataspaceMessage

        Public Overridable ReadOnly Property scalar() As Boolean
            Get
                Return dataSpace.dimensionLength.Length = 0
            End Get
        End Property

        Public MustOverride Function data(sb As Superblock) As Object

        'Dim type As DataType = dataType
        'Dim bb As ByteBuffer = dataBuffer

        'If TypeOf type Is VariableLength Then
        '    Return VariableLengthDatasetReader.readDataset(DirectCast(type, VariableLength), bb, dimensions, hdfFc)
        'Else
        '    Return DatasetReader.readDataset(type, bb, dimensions)
        'End If

        Public Overrides Function ToString() As String
            Return $"{Me.GetType.Name} {Scripting.ToString(dataSpace)} {Scripting.ToString(dataType)}"
        End Function
    End Class

End Namespace
