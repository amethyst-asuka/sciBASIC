﻿Imports System.Runtime.CompilerServices
Imports Microsoft.VisualBasic.MachineLearning.Darwinism.Models

Namespace Darwinism.GAF.ReplacementStrategy

    Public Interface IStrategy(Of Chr As Chromosome(Of Chr))

        Function newPopulation(newPop As Population(Of Chr), GA As GeneticAlgorithm(Of Chr)) As Population(Of Chr)
    End Interface

    Public Enum Strategies
        Naive
        EliteCrossbreed
    End Enum

    <HideModuleName> Public Module Extensions

        <Extension>
        Public Function GetStrategy(Of genome As Chromosome(Of genome))(strategy As Strategies) As IStrategy(Of genome)
            Select Case strategy
                Case Strategies.EliteCrossbreed
                    Return New EliteReplacement(Of genome)
                Case Else
                    Return New SimpleReplacement(Of genome)
            End Select
        End Function
    End Module

    ''' <summary>
    ''' 最简单的种群更替策略
    ''' </summary>
    ''' <typeparam name="Chr"></typeparam>
    Public Structure SimpleReplacement(Of Chr As Chromosome(Of Chr))
        Implements IStrategy(Of Chr)

        ''' <summary>
        ''' 下面的两个步骤是机器学习的关键
        ''' 
        ''' 通过排序,将错误率最小的种群排在前面
        ''' 错误率最大的种群排在后面
        ''' 然后对种群进行裁剪,将错误率比较大的种群删除
        ''' 从而实现了择优进化, 即程序模型对我们的训练数据集产生了学习
        ''' </summary>
        ''' <param name="newPop"></param>
        ''' <param name="GA"></param>
        ''' <returns></returns>
        Public Function newPopulation(newPop As Population(Of Chr), GA As GeneticAlgorithm(Of Chr)) As Population(Of Chr) Implements IStrategy(Of Chr).newPopulation
            Call newPop.SortPopulationByFitness(GA, GA.chromosomesComparator) ' 通过fitness排序来进行择优
            Call newPop.Trim(newPop.initialSize)                              ' 剪裁掉后面的对象，达到淘汰的效果

            Return newPop
        End Function
    End Structure

    ''' <summary>
    ''' 种群的精英杂交更替策略
    ''' </summary>
    ''' <typeparam name="Chr"></typeparam>
    Public Class EliteReplacement(Of Chr As Chromosome(Of Chr))
        Implements IStrategy(Of Chr)

        Dim ranf As Random = Math.seeds

        ''' <summary>
        ''' 只保留10%的个体,然后这些个体杂交补充到种群的大小
        ''' </summary>
        ''' <param name="newPop"></param>
        ''' <param name="GA"></param>
        ''' <returns></returns>
        Public Function newPopulation(newPop As Population(Of Chr), GA As GeneticAlgorithm(Of Chr)) As Population(Of Chr) Implements IStrategy(Of Chr).newPopulation
            Dim x, y As Chr

            ' 通过fitness排序来进行择优
            Call newPop.SortPopulationByFitness(GA, GA.chromosomesComparator)
            Call newPop.Trim(newPop.initialSize * 0.1)

            ' 对剩下的精英个体进行杂交,补充种群的成员
            Do While newPop.Size < newPop.initialSize
                x = newPop.Random(ranf)
                y = newPop.Random(ranf)

                For Each newIndividual As Chr In x.Crossover(y)
                    Call newPop.Add(newIndividual)
                Next
            Loop

            Return newPop
        End Function
    End Class
End Namespace