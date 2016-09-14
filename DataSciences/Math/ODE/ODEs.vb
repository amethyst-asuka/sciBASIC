﻿Imports System.Reflection
Imports Microsoft.VisualBasic.ComponentModel.DataSourceModel
Imports Microsoft.VisualBasic.Language
Imports Microsoft.VisualBasic.Linq
Imports Microsoft.VisualBasic.Mathematical.BasicR

''' <summary>
''' Solving ODEs in R language, as example for test this class:
''' 
''' ```R
''' func &lt;- function(t, x, parms) {
'''    with(as.list(c(parms, x)), {
'''    
'''       dP &lt;- a * P      - b * C * P
'''       dC &lt;- b * P * C  - c * C
'''      
'''       list(c(dP, dC))
'''    })
''' }
'''
''' y0    &lt;- c(P = 2, C = 1)
''' parms &lt;- c(a = 0.1, b = 0.1, c = 0.1)
''' out   &lt;- ode(y = y0, times = 0:100, func, parms = parms)
''' 
''' head(out)
''' plot(out)
''' ```
''' </summary>
Public MustInherit Class ODEs

    Dim K1, K2, K3, K4 As Vector
    Dim vars As var()

    Sub New()
        Dim type As TypeInfo = Me.GetType
        Dim fields = type.DeclaredFields _
            .Where(Function(x) x.FieldType.Equals(var.type)) _
            .ToArray

        vars = New var(fields.Length - 1) {}

        For Each f As SeqValue(Of FieldInfo) In fields.SeqIterator
            Dim x As New var() With {
                .Name = f.obj.Name,
                .Index = f.i
            }
            vars(f.i) = x

            Call f.obj.SetValue(Me, x)
        Next
    End Sub

    ''' <summary>
    ''' 
    ''' </summary>
    ''' <param name="dxn">x初值</param>
    ''' <param name="dyn">初值y(n)</param>
    ''' <param name="dh">步长</param>
    ''' <param name="dynext">下一步的值y(n+1)</param>
    Private Sub __rungeKutta(dxn As Double,
                             ByRef dyn As Vector,
                             dh As Double,
                             ByRef dynext As Vector)
        ODEs(dxn, dyn, K1)                             ' 求解K1
        ODEs(dxn + dh / 2, dyn + dh / 2 * K1, K2)      ' 求解K2
        ODEs(dxn + dh / 2, dyn + dh / 2 * K2, K3)      ' 求解K3
        ODEs(dxn + dh, dyn + dh * K3, K4)              ' 求解K4

        dynext = dyn + (K1 + K2 + K3 + K4) * dh / 6.0  ' 求解下一步的值y(n+1)
    End Sub

    ''' <summary>
    ''' 初值
    ''' </summary>
    ''' <returns></returns>
    Protected MustOverride Function y0() As var()

    Public Function Solve(n As Integer, a As Double, b As Double) As NamedValue(Of Double())()
        Dim dh As Double = (b - a) / n  ' 步长
        Dim dx As Double = a
        Dim y0 As Double() = Me.y0 _
            .OrderBy(Function(o) o.Index) _
            .Select(Function(o) o.value) _
            .ToArray
        Dim darrayn As New Vector(y0)
        Dim darraynext As New Vector(y0.Length) ' //下一步的值,最好初始化

        K1 = New Vector(y0.Length)
        K2 = New Vector(y0.Length)
        K3 = New Vector(y0.Length)
        K4 = New Vector(y0.Length)

        Dim y As List(Of Double)() = New List(Of Double)(vars.Length - 1) {}
        Dim x As New List(Of Double)

        For i As Integer = 0 To n
            __rungeKutta(dx, darrayn, dh, darraynext)
            x += dx
            dx += dh
            darrayn = darraynext

            For Each var In vars
                y(var) += darrayn(var.Index)
            Next
        Next

        Dim out = LinqAPI.MakeList(Of NamedValue(Of Double())) <=
 _
            From var As var
            In vars
            Select New NamedValue(Of Double()) With {
                .Name = var.Name,
                .x = y(var)
            }
        Return out + New NamedValue(Of Double()) With {
            .x = x,
            .Name = NameOf(x)
        }
    End Function

    ''' <summary>
    ''' 在这里计算具体的微分方程组
    ''' </summary>
    ''' <param name="dx"></param>
    ''' <param name="dy"></param>
    Protected MustOverride Sub __odes(dx As Double, ByRef dy As Vector)

    Private Sub ODEs(dx As Double, y As Vector, ByRef k As Vector)
        For Each x In vars       ' 更新设置y的值
            x.value = y(x.Index)
        Next

        Call __odes(dx, dy:=k)
    End Sub
End Class

Public Delegate Sub [Function](dx As Double, y As Vector, ByRef k As Vector)