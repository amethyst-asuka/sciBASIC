﻿Imports System.Drawing
Imports Microsoft.VisualBasic.Data.visualize.Network.Graph
Imports stdNum = System.Math

Namespace Layouts.ForceDirected

    Public Class EdgeWeightedPlanner : Inherits Planner

        ReadOnly absW As New Dictionary(Of String, Double)
        ReadOnly mass As New MassHandler

        Public Sub New(g As NetworkGraph, maxW As Double,
                       Optional ejectFactor As Integer = 6,
                       Optional condenseFactor As Integer = 3,
                       Optional maxtx As Integer = 4,
                       Optional maxty As Integer = 3,
                       Optional dist_threshold As String = "30,250",
                       Optional size As String = "1000,1000",
                       Optional avoidRegions() As RectangleF = Nothing)

            MyBase.New(g, ejectFactor, condenseFactor, maxtx, maxty, dist_threshold, size, avoidRegions)

            Dim allW As Double() = g.graphEdges _
                .Select(Function(e) e.weight) _
                .Where(Function(w) w <> 0.0) _
                .Select(AddressOf stdNum.Abs) _
                .ToArray
            Dim mineW As Double = allW.Min
            Dim maxeW As Double = allW.Max

            For Each edge As Edge In g.graphEdges
                If edge.weight = 0.0 Then
                    edge.weight = mineW
                End If

                absW(edge.ID) = (stdNum.Abs(edge.weight) - mineW) / (maxeW - mineW) * maxW + 1
            Next
        End Sub

        ''' <summary>
        ''' 边连接的权重越大，吸引力越大
        ''' </summary>
        Protected Overrides Sub runAttraction()
            Dim u, v As Node
            Dim distX, distY, dist As Double
            Dim dx, dy As Double

            For Each edge As Edge In g.graphEdges
                u = edge.U
                v = edge.V
                distX = u.data.initialPostion.x - v.data.initialPostion.x
                distY = u.data.initialPostion.y - v.data.initialPostion.y
                dist = stdNum.Sqrt(distX * distX + distY * distY)
                dx = distX * dist / k * condenseFactor
                dy = distY * dist / k * condenseFactor

                ' 如果是相同的分组，则吸引力很大
                If (dist < dist_thresh.Min) Then
                    dx = 0
                    dy = 0
                Else
                    dx *= absW(edge.ID)
                    dy *= absW(edge.ID)
                End If

                With mass.DeltaMass(u, v, dx, dy)
                    mDxMap(u.label) = mDxMap(u.label) - .X
                    mDyMap(u.label) = mDyMap(u.label) - .Y
                End With

                With mass.DeltaMass(v, u, dx, dy)
                    mDxMap(v.label) = mDxMap(v.label) + .X
                    mDyMap(v.label) = mDyMap(v.label) + .Y
                End With
            Next
        End Sub

        ''' <summary>
        ''' 边连接的权重越大，排斥力越小
        ''' </summary>
        Protected Overrides Sub runRepulsive()
            Dim distX, distY, dist As Double
            Dim ejectFactor = Me.ejectFactor
            Dim dx, dy As Double

            For Each u As Node In g.vertex
                For Each v As Node In g.vertex.Where(Function(ui) Not ui Is u)
                    distX = u.data.initialPostion.x - v.data.initialPostion.x
                    distY = u.data.initialPostion.y - v.data.initialPostion.y
                    dist = stdNum.Sqrt(distX * distX + distY * distY)

                    If dist > 0 AndAlso dist < dist_thresh.Max Then
                        dx = (distX / dist) * (k * k / dist) * ejectFactor
                        dy = (distY / dist) * (k * k / dist) * ejectFactor

                        Dim link As Edge = g.GetEdges(u, v).FirstOrDefault

                        If link Is Nothing Then
                            If (dist < dist_thresh.Min) Then
                                dx *= 2
                                dy *= 2
                            End If
                        Else
                            ' 是相同的分组，则排斥力很小
                            If (dist < dist_thresh.Min) Then
                            Else
                                dx /= absW(link.ID)
                                dy /= absW(link.ID)
                            End If
                        End If

                        With mass.DeltaMass(u, v, dx, dy)
                            mDxMap(u.label) = mDxMap(u.label) + .X
                            mDyMap(u.label) = mDyMap(u.label) + .Y
                        End With
                    End If
                Next
            Next
        End Sub
    End Class
End Namespace