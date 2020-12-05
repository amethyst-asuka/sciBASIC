﻿Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Drawing.Text
Imports System.IO
Imports Microsoft.VisualBasic.Data.IO.MessagePack
Imports Microsoft.VisualBasic.DataMining.UMAP

Namespace Tester
    Friend Class Program
        Private Shared Sub Main()
            ' Note: The MNIST data here consist of normalized vectors (so the CosineForNormalizedVectors distance function can be safely used)
            Dim data = MsgPackSerializer.Deserialize(Of LabelledVector())(File.ReadAllBytes("MNIST-LabelledVectorArray-60000x100.msgpack"))
            data = data.Take(10_000).ToArray()
            Dim timer = Stopwatch.StartNew()
            Dim umap = New Umap(distance:=AddressOf DistanceFunctions.CosineForNormalizedVectors)
            Console.WriteLine("Initialize fit..")
            Dim nEpochs = umap.InitializeFit(data.[Select](Function(entry) entry.Vector).ToArray())
            Console.WriteLine("- Done")
            Console.WriteLine()
            Console.WriteLine("Calculating..")

            For i = 0 To nEpochs - 1
                umap.Step()
                If i Mod 10 = 0 Then Console.WriteLine($"- Completed {i + 1} of {nEpochs}")
            Next

            Console.WriteLine("- Done")
            Dim embeddings = umap.GetEmbedding().[Select](Function(vector) New With {
                .X = vector(0),
                .Y = vector(1)
            }).ToArray()
            timer.Stop()
            Console.WriteLine("Time taken: " & timer.Elapsed.Lanudry)

            ' Fit the vectors to a 0-1 range (this isn't necessary if feeding these values down from a server to a browser to draw with Plotly because ronend because Plotly scales the axes to the data)
            Dim minX = embeddings.Min(Function(vector) vector.X)
            Dim rangeX = embeddings.Max(Function(vector) vector.X) - minX
            Dim minY = embeddings.Min(Function(vector) vector.Y)
            Dim rangeY = embeddings.Max(Function(vector) vector.Y) - minY
            Dim scaledEmbeddings = embeddings.[Select](Function(vector) (X:=(vector.X - minX) / rangeX, Y:=(vector.Y - minY) / rangeY)).ToArray()
            Const width = 1600
            Const height = 1200

            Using bitmap = New Bitmap(width, height)

                Using g = Graphics.FromImage(bitmap)
                    g.FillRectangle(Brushes.DarkBlue, 0, 0, width, height)
                    g.SmoothingMode = SmoothingMode.HighQuality
                    g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic
                    g.PixelOffsetMode = PixelOffsetMode.HighQuality

                    Using font = New Font("Tahoma", 6)

                        For Each vectorUid In scaledEmbeddings.Zip(data, Function(vector, entry) (vector, entry.UID))
                            Dim vector = vectorUid.vector
                            Dim uid = vectorUid.UID

                            g.DrawString(uid, font, Brushes.White, vector.X * width, vector.Y * height)
                        Next
                    End Using
                End Using

                bitmap.Save("Output-Label.png")
            End Using

            Dim colors = "#006400,#00008b,#b03060,#ff4500,#ffd700,#7fff00,#00ffff,#ff00ff,#6495ed,#ffdab9".Split(","c).[Select](Function(c) ColorTranslator.FromHtml(c)).[Select](Function(c) New SolidBrush(c)).ToArray()

            Using bitmap = New Bitmap(width, height)

                Using g = Graphics.FromImage(bitmap)
                    g.FillRectangle(Brushes.White, 0, 0, width, height)
                    g.SmoothingMode = SmoothingMode.HighQuality
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic
                    g.PixelOffsetMode = PixelOffsetMode.HighQuality

                    For Each vectorUid In scaledEmbeddings.Zip(data, Function(vector, entry) (vector, entry.UID))
                        Dim vector = vectorUid.vector
                        Dim uid = vectorUid.UID

                        g.FillEllipse(colors(Integer.Parse(uid)), vector.X * width, vector.Y * height, 5, 5)
                    Next
                End Using

                bitmap.Save("Output-Color.png")
            End Using

            Console.WriteLine("Generated visualisation images")
            Console.WriteLine("Press [Enter] to terminuate..")
            Console.ReadLine()
        End Sub
    End Class

    Public NotInheritable Class LabelledVector

        Public UID As String
        Public Vector As Single()

        Sub New()

        End Sub
    End Class
End Namespace
