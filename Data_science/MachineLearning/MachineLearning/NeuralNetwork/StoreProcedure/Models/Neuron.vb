﻿#Region "Microsoft.VisualBasic::ed8d48b8c84ebd0c77f8a4aec0cbf1a0, Data_science\MachineLearning\MachineLearning\NeuralNetwork\StoreProcedure\Models\Neuron.vb"

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

    '     Class Synapse
    ' 
    '         Properties: [in], [out], delta, w
    ' 
    '         Function: ToString
    ' 
    '     Class NeuronNode
    ' 
    '         Properties: bias, delta, gradient, id
    ' 
    '         Function: ToString
    ' 
    '     Class NeuronLayer
    ' 
    '         Properties: activation, id, neurons
    ' 
    '         Function: getCollection, getSize
    ' 
    '     Class HiddenLayer
    ' 
    '         Properties: activation, layers
    ' 
    '         Function: getCollection, getSize
    ' 
    ' 
    ' /********************************************************************************/

#End Region

Imports System.Xml.Serialization
Imports Microsoft.VisualBasic.ComponentModel.Collection.Generic
Imports Microsoft.VisualBasic.ComponentModel.DataSourceModel.Repository
Imports Microsoft.VisualBasic.Text.Xml.Models

Namespace NeuralNetwork.StoreProcedure

    <XmlType("synapse")> Public Class Synapse

        <XmlAttribute> Public Property [in] As String
        <XmlAttribute> Public Property [out] As String
        ''' <summary>
        ''' 两个神经元之间的连接强度
        ''' </summary>
        ''' <returns></returns>
        <XmlAttribute> Public Property w As Double
        <XmlAttribute> Public Property delta As Double

        ''' <summary>
        ''' 
        ''' </summary>
        ''' <returns></returns>
        Public Overrides Function ToString() As String
            Return $"|{[in]} => {out}| = {w}"
        End Function
    End Class

    ''' <summary>
    ''' 一个神经元节点的数据模型
    ''' </summary>
    <XmlType("neuron")> Public Class NeuronNode : Implements INamedValue

        ''' <summary>
        ''' 当前的这个神经元的唯一标记
        ''' </summary>
        ''' <returns></returns>
        <XmlAttribute> Public Property id As String Implements IKeyedEntity(Of String).Key
        <XmlAttribute> Public Property bias As Double
        <XmlAttribute> Public Property delta As Double
        <XmlAttribute> Public Property gradient As Double

        Public Overrides Function ToString() As String
            Return id
        End Function

    End Class

    ''' <summary>
    ''' Layer对象之中只放置神经元节点的引用唯一编号
    ''' </summary>
    <XmlType("layer")> Public Class NeuronLayer : Inherits ListOf(Of String)
        Implements INamedValue

        <XmlAttribute>
        Public Property id As String Implements IKeyedEntity(Of String).Key
        Public Property activation As ActiveFunction
        <XmlElement("neuron")>
        Public Property neurons As String()

        Protected Overrides Function getSize() As Integer
            Return neurons?.Length
        End Function

        Protected Overrides Function getCollection() As IEnumerable(Of String)
            Return neurons
        End Function
    End Class

    Public Class HiddenLayer : Inherits ListOf(Of NeuronLayer)

        Public Property activation As ActiveFunction
        <XmlElement("layers")>
        Public Property layers As NeuronLayer()

        Protected Overrides Function getSize() As Integer
            Return layers.Length
        End Function

        Protected Overrides Function getCollection() As IEnumerable(Of NeuronLayer)
            Return layers
        End Function
    End Class
End Namespace