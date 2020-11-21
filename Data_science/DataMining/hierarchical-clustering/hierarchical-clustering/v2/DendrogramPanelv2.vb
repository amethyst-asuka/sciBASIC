﻿Imports System.Drawing
Imports Microsoft.VisualBasic.Data.ChartPlots.Graphic
Imports Microsoft.VisualBasic.Data.ChartPlots.Graphic.Canvas
Imports Microsoft.VisualBasic.DataMining.HierarchicalClustering.DendrogramVisualize
Imports Microsoft.VisualBasic.Imaging
Imports Microsoft.VisualBasic.Imaging.Drawing2D
Imports Microsoft.VisualBasic.MIME.Markup.HTML.CSS

Public Class DendrogramPanelv2 : Inherits Plot

    Friend ReadOnly hist As Cluster
    Friend ReadOnly layout As Layouts

    Public Sub New(hist As Cluster, theme As Theme)
        MyBase.New(theme)

        Me.hist = hist
    End Sub

    Protected Overrides Sub PlotInternal(ByRef g As IGraphics, canvas As GraphicsRegion)
        Dim plotRegion As Rectangle = canvas.PlotRegion
        ' 每一个样本点都平分一段长度
        Dim unitWidth As Double = plotRegion.Height / hist.Leafs
        Dim scaleX As d3js.scale.LinearScale = d3js.scale.linear().domain({0, hist.DistanceValue}).range(integers:={plotRegion.Left, plotRegion.Right})

        Call plotInternal(hist, unitWidth, g, plotRegion, 0, scaleX, Nothing)
    End Sub

    Private Overloads Sub plotInternal(partition As Cluster,
                                       unitWidth As Double,
                                       ByRef g As IGraphics,
                                       plotRegion As Rectangle,
                                       i As Integer,
                                       scaleX As d3js.scale.LinearScale,
                                       parentPt As PointF)

        Dim orders As Cluster() = partition.Children.OrderBy(Function(a) a.Leafs).ToArray
        Dim x = plotRegion.Right - scaleX(partition.DistanceValue)
        Dim y As Integer

        If partition.isLeaf Then
            y = i * unitWidth + unitWidth / 2
        Else
            ' 连接节点在中间？
            y = i * unitWidth + unitWidth / 2 + (partition.Leafs * unitWidth) / 2
        End If

        Call g.DrawLine(Pens.Blue, parentPt, New PointF(parentPt.X, y))
        Call g.DrawLine(Pens.Blue, New PointF(x, y), New PointF(parentPt.X, y))

        Call g.DrawCircle(New PointF(x, y), 15, Brushes.Red)
        Call g.DrawString(partition.Name, CSSFont.TryParse(CSSFont.PlotLabelNormal), Brushes.Black, New PointF(x, y))

        If partition.isLeaf Then
            Return
        Else
            parentPt = New PointF(x, y)

            Dim n As Integer = 0

            For Each part As Cluster In orders
                Call plotInternal(part, unitWidth, g, plotRegion, i + n, scaleX, parentPt)
                n += part.Leafs
            Next
        End If
    End Sub
End Class
