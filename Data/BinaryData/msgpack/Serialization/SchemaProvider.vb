﻿#Region "Microsoft.VisualBasic::a952b0569dd164aaa4438e0c924225cb, Data\BinaryData\msgpack\Serialization\SchemaProvider.vb"

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

    '     Class SchemaProvider
    ' 
    ' 
    ' 
    ' 
    ' /********************************************************************************/

#End Region

Imports System.Reflection
Imports Microsoft.VisualBasic.ComponentModel.DataSourceModel

Namespace Serialization

    ''' <summary>
    ''' 
    ''' </summary>
    ''' <typeparam name="T"></typeparam>
    ''' <remarks>
    ''' 这个模块是为了处理元素类型定义信息和序列化代码调用模块之间没有实际的引用关系的情况
    ''' 例如模块A没有引用messagepack模块，则没有办法添加<see cref="MessagePackMemberAttribute"/>
    ''' 来完成序列化，则这个时候会需要通过这个模块来提供这样的映射关系
    ''' </remarks>
    Public MustInherit Class SchemaProvider(Of T)

        Shared ReadOnly slotList As Dictionary(Of String, PropertyInfo) = DataFramework.Schema(Of T)(
            flag:=PropertyAccess.ReadWrite,
            nonIndex:=True,
            primitive:=False,
            binds:=PublicProperty
        )

        ''' <summary>
        ''' provides a schema table for base object for generates 
        ''' a sequence of <see cref="MessagePackMemberAttribute"/>
        ''' </summary>
        ''' <returns></returns>
        Protected Friend MustOverride Iterator Function GetObjectSchema() As IEnumerable(Of (obj As Type, schema As Dictionary(Of String, NilImplication)))

    End Class
End Namespace
