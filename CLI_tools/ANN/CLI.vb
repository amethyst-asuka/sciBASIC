﻿Imports Microsoft.VisualBasic.CommandLine
Imports Microsoft.VisualBasic.CommandLine.Reflection
Imports Microsoft.VisualBasic.MachineLearning.NeuralNetwork
Imports Microsoft.VisualBasic.MachineLearning.NeuralNetwork.StoreProcedure

Module CLI

    <ExportAPI("/config.template")>
    <Usage("/config.template [/save <default=./config.ini>]")>
    Public Function ConfigTemplate(args As CommandLine) As Integer

    End Function

    ''' <summary>
    ''' 这个函数会输出训练好的模型, 训练集测试结果, 错误率变化曲线图, 训练日志
    ''' </summary>
    ''' <param name="args"></param>
    ''' <returns></returns>
    <ExportAPI("/training")>
    <Usage("/training /samples <sample_matrix.Xml> [/config <config.ini> /parallel /out <ANN.Xml>]")>
    Public Function Train(args As CommandLine) As Integer
        Dim in$ = args <= "/samples"
        Dim parallel As Boolean = args("/parallel")
        Dim out$ = args("/out") Or $"{[in].TrimSuffix}_ANN_trained.Xml"
        Dim samples As DataSet = [in].LoadXml(Of DataSet)
        Dim config As Config
        Dim trainingHelper As New TrainingUtils(
            samples.Size.Width, {},
            samples.OutputSize,
            config.learnRate,
            config.momentum
        )

        For Each sample In samples.DataSamples
            Call trainingHelper.Add(sample.status, sample.target)
        Next

        Call trainingHelper.Train()

        Return trainingHelper _
            .TakeSnapshot _
            .GetXml _
            .SaveTo(out) _
            .CLICode
    End Function

    ''' <summary>
    ''' 使用测试训练数据集继续训练人工神经网络模型
    ''' </summary>
    ''' <param name="args"></param>
    ''' <returns></returns>
    ''' 
    <ExportAPI("/encourage")>
    <Usage("/encourage /model <ANN.xml> /samples <samples.Xml> [/parallel /out <out.Xml>]")>
    Public Function Encourage(args As CommandLine) As Integer
        Dim in$ = args <= "/model"
        Dim samples$ = args <= "/samples"
        Dim out$ = args("/out") Or $"{[in].TrimSuffix}.encouraged.Xml"
        Dim parallel As Boolean = args("/parallel")
        Dim network As Network = [in].LoadXml(Of NeuralNetwork).LoadModel
        Dim training As New TrainingUtils(network)

        For Each sample As Sample In samples.LoadXml(Of DataSet).DataSamples
            Call training.Add(sample.status, sample.target)
        Next

        Call Console.WriteLine(network.ToString)
        Call training.Train(parallel, normalize:=True)

        Return training.TakeSnapshot _
            .GetXml _
            .SaveTo(out) _
            .CLICode
    End Function
End Module