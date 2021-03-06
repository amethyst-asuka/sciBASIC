﻿Imports System.Drawing
Imports System.Drawing.Imaging
Imports System.Runtime.InteropServices
Imports stdNum = System.Math

Namespace Filters

    ''' <summary>
    ''' https://zhuanlan.zhihu.com/p/73363439
    ''' </summary>
    Public Class Matrix

        Friend ReadOnly raw As Bitmap

        Sub New(bitmap As Bitmap)
            raw = bitmap
        End Sub

        Public Function GetSmoothBitmap(method As Matrix2DFilters) As Bitmap
            Dim sMat As Byte(,) = Image_2_Arry2D(raw)

            Select Case method
                Case Matrix2DFilters.Max : sMat = Matrix2DFiltersAlgorithm.sf_maxvalFilter(sMat)
                Case Matrix2DFilters.Mean : sMat = Matrix2DFiltersAlgorithm.sf_meanFilter(sMat)
                Case Matrix2DFilters.Median : sMat = Matrix2DFiltersAlgorithm.sf_medianFilter(sMat)
                Case Matrix2DFilters.Min : sMat = Matrix2DFiltersAlgorithm.sf_minvalFilter(sMat)
                Case Else
                    Throw New InvalidProgramException(method)
            End Select

            Return Matrix.Arry2D_2_Image(sMat)
        End Function

        Public Overrides Function ToString() As String
            Return $"[{raw.Width}, {raw.Height}]"
        End Function

        Public Shared Function Image_2_Arry2D(ByVal srcBmp As Bitmap) As Byte(,)
            Dim rect As New Rectangle(0, 0, srcBmp.Width, srcBmp.Height)
            Dim srcBmpData As BitmapData = srcBmp.LockBits(rect, ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb)
            Dim srcPtr As IntPtr = srcBmpData.Scan0
            Dim Stride As Integer = srcBmpData.Stride
            Dim space As Integer = stdNum.Abs(Stride) - rect.Width * 3
            Dim bytes As Integer = stdNum.Abs(Stride) * rect.Height - space
            Dim srcRGBValues = New Byte(bytes - 1) {}
            Dim mat = New Byte(rect.Height - 1, rect.Width - 1) {}

            Marshal.Copy(srcPtr, srcRGBValues, 0, bytes)
            srcBmp.UnlockBits(srcBmpData)

            Dim ptr = 0
            Dim temp As Double

            For y As Integer = 0 To rect.Height - 1
                ' ptr = y * Stride;
                For x As Integer = 0 To rect.Width - 1
                    temp = srcRGBValues(ptr + 2) * 0.299 + srcRGBValues(ptr + 1) * 0.587 + srcRGBValues(ptr) * 0.114
                    mat(y, x) = CByte(temp)
                    ptr += 3
                Next

                ptr += space
            Next

            Return mat
        End Function

        Public Shared Function Arry2D_2_Image(ByVal mat As Byte(,)) As Bitmap
            Dim height = mat.GetLength(0)
            Dim width = mat.GetLength(1)
            Dim srcBmp As New Bitmap(width, height)
            Dim rect As New Rectangle(0, 0, width, height)
            Dim srcBmpData As BitmapData = srcBmp.LockBits(rect, ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb)
            Dim Stride As Integer = srcBmpData.Stride
            Dim space = stdNum.Abs(Stride) - width * 3
            Dim bytes = stdNum.Abs(Stride) * height - space
            Dim RGBs = New Byte(bytes - 1) {}
            Dim ptr = 0

            For y As Integer = 0 To rect.Height - 1
                ' ptr = y * Stride;
                For x As Integer = 0 To rect.Width - 1
                    RGBs(ptr + 2) = mat(y, x)
                    RGBs(ptr + 1) = RGBs(ptr + 2)
                    RGBs(ptr) = RGBs(ptr + 1)
                    ptr += 3
                Next

                ptr += space
            Next

            Marshal.Copy(RGBs, 0, srcBmpData.Scan0, bytes)
            srcBmp.UnlockBits(srcBmpData)

            Return srcBmp
        End Function
    End Class
End Namespace


