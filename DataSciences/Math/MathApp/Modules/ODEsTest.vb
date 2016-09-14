﻿Imports Microsoft.VisualBasic.DocumentFormat.Csv
Imports Microsoft.VisualBasic.Language
Imports Microsoft.VisualBasic.Linq
Imports Microsoft.VisualBasic.Mathematical.BasicR
Imports Microsoft.VisualBasic.Mathematical.diffEq

Class ODEsTest : Inherits ODEs

    Const a = 0.1, b = 0.1, c = 0.1

    Dim P As var
    Dim yC As var

    Protected Overrides Sub __odes(dx As Double, ByRef dy As Vector)
        dy(P) = a * P - b * yC * P
        dy(yC) = b * P * yC - c * yC
    End Sub

    Protected Overrides Function y0() As var()
        Return {P = 2, yC = 1}
    End Function
End Class
