﻿Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports System.Runtime.CompilerServices
Imports System.Threading.Tasks
Imports Microsoft.VisualBasic.Language.Python

''' <summary>
''' The progress will be a value from 0 to 1 that indicates approximately how much of the processing has been completed
''' </summary>
Public Delegate Sub ProgressReporter(progress As Single)

Public NotInheritable Class Umap
    Private Const SMOOTH_K_TOLERANCE As Single = 0.00001F
    Private Const MIN_K_DIST_SCALE As Single = 0.001F
    Private ReadOnly _learningRate As Single = 1.0F
    Private ReadOnly _localConnectivity As Single = 1.0F
    Private ReadOnly _minDist As Single = 0.1F
    Private ReadOnly _negativeSampleRate As Integer = 5
    Private ReadOnly _repulsionStrength As Single = 1
    Private ReadOnly _setOpMixRatio As Single = 1
    Private ReadOnly _spread As Single = 1
    Private ReadOnly _distanceFn As DistanceCalculation
    Private ReadOnly _random As IProvideRandomValues
    Private ReadOnly _nNeighbors As Integer
    Private ReadOnly _customNumberOfEpochs As Integer?
    Private ReadOnly _progressReporter As ProgressReporter

    ' KNN state (can be precomputed and supplied via initializeFit)
    Private _knnIndices As Integer()() = Nothing
    Private _knnDistances As Single()() = Nothing

    ' Internal graph connectivity representation
    Private _graph As SparseMatrix = Nothing
    Private _x As Single()() = Nothing
    Private _isInitialized As Boolean = False
    Private _rpForest As Tree.FlatTree() = New Tree.FlatTree(-1) {}

    ' Projected embedding
    Private _embedding As Single()
    Private ReadOnly _optimizationState As Umap.OptimizationState

    Public Sub New(Optional distance As DistanceCalculation = Nothing, Optional random As IProvideRandomValues = Nothing, Optional dimensions As Integer = 2, Optional numberOfNeighbors As Integer = 15, Optional customNumberOfEpochs As Integer? = Nothing, Optional progressReporter As Umap.Umap.ProgressReporter = Nothing)
        If customNumberOfEpochs IsNot Nothing AndAlso customNumberOfEpochs <= 0 Then Throw New ArgumentOutOfRangeException(NameOf(customNumberOfEpochs), "if non-null then must be a positive value")
        _distanceFn = If(distance, AddressOf DistanceFunctions.Cosine)
        _random = If(random, DefaultRandomGenerator.Instance)
        _nNeighbors = numberOfNeighbors
        _optimizationState = New Umap.OptimizationState With {
            .[Dim] = dimensions
        }
        _customNumberOfEpochs = customNumberOfEpochs
        _progressReporter = progressReporter
    End Sub

    ''' <summary>
    ''' Initializes fit by computing KNN and a fuzzy simplicial set, as well as initializing the projected embeddings. Sets the optimization state ahead of optimization steps.
    ''' Returns the number of epochs to be used for the SGD optimization.
    ''' </summary>
    Public Function InitializeFit(x As Single()()) As Integer
        ' We don't need to reinitialize if we've already initialized for this data
        If _x Is x AndAlso _isInitialized Then Return GetNEpochs()

        ' For large quantities of data (which is where the progress estimating is more useful), InitializeFit takes at least 80% of the total time (the calls to Step are
        ' completed much more quickly AND they naturally lend themselves to granular progress updates; one per loop compared to the recommended number of epochs)
        Dim initializeFitProgressReporter As ProgressReporter = If(_progressReporter Is Nothing, Sub(progress)
                                                                                                 End Sub, Umap.ScaleProgressReporter(_progressReporter, 0, 0.8F))
        _x = x

        If _knnIndices Is Nothing AndAlso _knnDistances Is Nothing Then
                ' This part of the process very roughly accounts for 1/3 of the work
                (_knnIndices, _knnDistances) = Me.NearestNeighbors(x, Umap.ScaleProgressReporter(initializeFitProgressReporter, 0, 0.3F))
            End If

        ' This part of the process very roughly accounts for 2/3 of the work (the reamining work is in the Step calls)
        _graph = Me.FuzzySimplicialSet(x, _nNeighbors, _setOpMixRatio, Umap.Umap.ScaleProgressReporter(initializeFitProgressReporter, 0.3F, 1))
        Dim headTailEpochsPerSample = Nothing
        headTailEpochsPerSample = InitializeSimplicialSetEmbedding()

        ' Set the optimization routine state
        _optimizationState.Head = head
        _optimizationState.Tail = tail
        _optimizationState.EpochsPerSample = epochsPerSample

        ' Now, initialize the optimization steps
        InitializeOptimization()
        PrepareForOptimizationLoop()
        _isInitialized = True
        Return GetNEpochs()
    End Function

    Public Function GetEmbedding() As Single()()
        Dim final = New Single(_optimizationState.NVertices - 1)() {}
        Dim span As Single() = _embedding

        For i As Integer = 0 To _optimizationState.NVertices - 1
            ' slice函数需要进行验证
            final(i) = span.slice(CInt(i * _optimizationState.Dim), CInt(_optimizationState.Dim)).ToArray()
        Next

        Return final
    End Function

    ''' <summary>
    ''' Gets the number of epochs for optimizing the projection - NOTE: This heuristic differs from the python version
    ''' </summary>
    Private Function GetNEpochs() As Integer
        If _customNumberOfEpochs IsNot Nothing Then Return _customNumberOfEpochs.Value
        Dim length = _graph.Dims.rows

        If length <= 2500 Then
            Return 500
        ElseIf length <= 5000 Then
            Return 400
        ElseIf length <= 7500 Then
            Return 300
        Else
            Return 200
        End If
    End Function

    ''' <summary>
    ''' Compute the ``nNeighbors`` nearest points for each data point in ``X`` - this may be exact, but more likely is approximated via nearest neighbor descent.
    ''' </summary>
    Friend Function NearestNeighbors(x As Single()(), progressReporter As Umap.ProgressReporter) As (Integer()(), Single()())
        Dim metricNNDescent = NNDescent.MakeNNDescent(_distanceFn, _random)
        progressReporter(0.05F)
        Dim nTrees = 5 + Round(Math.Sqrt(x.Length) / 20)
        Dim nIters = Math.Max(5, CInt(Math.Floor(Math.Round(Math.Log(x.Length, 2)))))
        progressReporter(0.1F)
        Dim leafSize = Math.Max(10, _nNeighbors)
        Dim forestProgressReporter = Umap.ScaleProgressReporter(progressReporter, 0.1F, 0.4F)
        _rpForest = Enumerable.Range(0, nTrees).[Select](Function(i)
                                                             forestProgressReporter(CSng(i) / nTrees)
                                                             Return Tree.FlattenTree(Tree.MakeTree(x, leafSize, i, _random), leafSize)
                                                         End Function).ToArray()
        Dim leafArray = Tree.MakeLeafArray(_rpForest)
        progressReporter(0.45F)
        Dim nnDescendProgressReporter = Umap.ScaleProgressReporter(progressReporter, 0.5F, 1)

        ' Handle python3 rounding down from 0.5 discrpancy
        Return metricNNDescent(x, leafArray, _nNeighbors, nIters, startingIteration:=Sub(i, max) nnDescendProgressReporter(CSng(i) / max))
    End Function

    ''' <summary>
    ''' Handle python3 rounding down from 0.5 discrpancy
    ''' </summary>
    ''' <param name="n"></param>
    ''' <returns></returns>
    Private Shared Function Round(n As Double) As Integer
        If n = 0.5 Then
            Return 0
        Else
            Return Math.Floor(Math.Round(n))
        End If
    End Function

    ''' <summary>
    ''' Given a set of data X, a neighborhood size, and a measure of distance compute the fuzzy simplicial set(here represented as a fuzzy graph in the form of a sparse matrix) associated
    ''' to the data. This is done by locally approximating geodesic distance at each point, creating a fuzzy simplicial set for each such point, and then combining all the local fuzzy
    ''' simplicial sets into a global one via a fuzzy union.
    ''' </summary>
    Private Function FuzzySimplicialSet(x As Single()(), nNeighbors As Integer, setOpMixRatio As Single, progressReporter As Umap.Umap.ProgressReporter) As Umap.SparseMatrix
        Dim knnIndices = If(_knnIndices, New Integer(-1)() {})
        Dim knnDistances = If(_knnDistances, New Single(-1)() {})
        progressReporter(0.1F)
        Dim sigmasRhos = Nothing
        sigmasRhos = Umap.SmoothKNNDistance(knnDistances, nNeighbors, _localConnectivity)
        progressReporter(0.2F)
        Dim rowsColsVals = Nothing
        rowsColsVals = Umap.ComputeMembershipStrengths(knnIndices, knnDistances, sigmas, rhos)
        progressReporter(0.3F)
        Dim sparseMatrix = New SparseMatrix(rows, cols, vals, (x.Length, x.Length))
        Dim transpose = sparseMatrix.Transpose()
        Dim prodMatrix = sparseMatrix.PairwiseMultiply(transpose)
        progressReporter(0.4F)
        Dim a = sparseMatrix.Add(CType(transpose, SparseMatrix)).Subtract(prodMatrix)
        progressReporter(0.5F)
        Dim b = a.MultiplyScalar(setOpMixRatio)
        progressReporter(0.6F)
        Dim c = prodMatrix.MultiplyScalar(1 - setOpMixRatio)
        progressReporter(0.7F)
        Dim result = b.Add(c)
        progressReporter(0.8F)
        Return result
    End Function

    Private Shared Function SmoothKNNDistance(distances As Single()(), k As Integer, Optional localConnectivity As Single = 1, Optional nIter As Integer = 64, Optional bandwidth As Single = 1) As (Single(), Single())
        Dim target = Math.Log(k, 2) * bandwidth ' TODO: Use Math.Log2 (when update framework to a version that supports it) or consider a pre-computed table
        Dim rho = New Single(distances.Length - 1) {}
        Dim result = New Single(distances.Length - 1) {}

        For i = 0 To distances.Length - 1
            Dim lo = 0F
            Dim hi = Single.MaxValue
            Dim mid = 1.0F

            ' TODO[umap-js]: This is very inefficient, but will do for now. FIXME
            Dim ithDistances = distances(i)
            Dim nonZeroDists = ithDistances.Where(Function(d) d > 0).ToArray()

            If nonZeroDists.Length >= localConnectivity Then
                Dim index = CInt(Math.Floor(localConnectivity))
                Dim interpolation = localConnectivity - index

                If index > 0 Then
                    rho(i) = nonZeroDists(index - 1)
                    If interpolation > Umap.SMOOTH_K_TOLERANCE Then rho(i) += interpolation * (nonZeroDists(index) - nonZeroDists(index - 1))
                Else
                    rho(i) = interpolation * nonZeroDists(0)
                End If
            ElseIf nonZeroDists.Length > 0 Then
                rho(i) = Utils.Max(nonZeroDists)
            End If

            For n = 0 To nIter - 1
                Dim psum = 0.0

                For j = 1 To distances(i).Length - 1
                    Dim d = distances(i)(j) - rho(i)

                    If d > 0 Then
                        psum += Math.Exp(-(d / mid))
                    Else
                        psum += 1.0
                    End If
                Next

                If Math.Abs(psum - target) < Umap.SMOOTH_K_TOLERANCE Then Exit For

                If psum > target Then
                    hi = mid
                    mid = (lo + hi) / 2
                Else
                    lo = mid

                    If hi = Single.MaxValue Then
                        mid *= 2
                    Else
                        mid = (lo + hi) / 2
                    End If
                End If
            Next

            result(i) = mid

            ' TODO[umap-js]: This is very inefficient, but will do for now. FIXME
            If rho(i) > 0 Then
                Dim meanIthDistances = Utils.Mean(ithDistances)
                If result(i) < Umap.MIN_K_DIST_SCALE * meanIthDistances Then result(i) = Umap.MIN_K_DIST_SCALE * meanIthDistances
            Else
                Dim meanDistances = Utils.Mean(distances.[Select](New Func(Of Single(), Single)(AddressOf Utils.Mean)).ToArray())
                If result(i) < Umap.MIN_K_DIST_SCALE * meanDistances Then result(i) = Umap.MIN_K_DIST_SCALE * meanDistances
            End If
        Next

        Return (result, rho)
    End Function

    Private Shared Function ComputeMembershipStrengths(knnIndices As Integer()(), knnDistances As Single()(), sigmas As Single(), rhos As Single()) As (Integer(), Integer(), Single())
        Dim nSamples = knnIndices.Length
        Dim nNeighbors = knnIndices(0).Length
        Dim rows = New Integer(nSamples * nNeighbors - 1) {}
        Dim cols = New Integer(nSamples * nNeighbors - 1) {}
        Dim vals = New Single(nSamples * nNeighbors - 1) {}

        For i = 0 To nSamples - 1

            For j = 0 To nNeighbors - 1
                If knnIndices(i)(j) = -1 Then Continue For ' We didn't get the full knn for i
                Dim val As Single

                If knnIndices(i)(j) = i Then
                    val = 0
                ElseIf knnDistances(i)(j) - rhos(i) <= 0.0 Then
                    val = 1
                Else
                    val = CSng(Math.Exp(-((knnDistances(i)(j) - rhos(i)) / sigmas(i))))
                End If

                rows(i * nNeighbors + j) = i
                cols(i * nNeighbors + j) = knnIndices(i)(j)
                vals(i * nNeighbors + j) = val
            Next
        Next

        Return (rows, cols, vals)
    End Function

    ''' <summary>
    ''' Initialize a fuzzy simplicial set embedding, using a specified initialisation method and then minimizing the fuzzy set cross entropy between the 1-skeletons of the high and low
    ''' dimensional fuzzy simplicial sets.
    ''' </summary>
    Private Function InitializeSimplicialSetEmbedding() As (Integer(), Integer(), Single())
        Dim nEpochs = GetNEpochs()
        Dim graphMax = 0F

        For Each value In _graph.GetValues()
            If graphMax < value Then graphMax = value
        Next

        Dim graph = _graph.Map(Function(value) If(value < graphMax / nEpochs, 0, value))

        ' We're not computing the spectral initialization in this implementation until we determine a better eigenvalue/eigenvector computation approach

        _embedding = New Single(graph.Dims.rows * _optimizationState.Dim - 1) {}
        SIMDint.Uniform(_embedding, 10, _random)

        ' Get graph data in ordered way...
        Dim weights = New List(Of Single)()
        Dim head = New List(Of Integer)()
        Dim tail = New List(Of Integer)()

        For Each rowColValue In graph.GetAll()
            Dim row = rowColValue.Item1
            Dim col = rowColValue.Item2
            Dim value = rowColValue.Item3

            If value <> 0 Then
                weights.Add(value)
                tail.Add(row)
                head.Add(col)
            End If
        Next

        ShuffleTogether(head, tail, weights)
        Return (head.ToArray(), tail.ToArray(), Umap.MakeEpochsPerSample(weights.ToArray(), nEpochs))
    End Function

    Private Sub ShuffleTogether(Of T, T2, T3)(list As List(Of T), other As List(Of T2), weights As List(Of T3))
        Dim n = list.Count

        If other.Count <> n Then
            Throw New Exception()
        End If

        While n > 1
            n -= 1
            Dim k As Integer = _random.Next(0, n + 1)
            Dim value = list(k)
            list(k) = list(n)
            list(n) = value
            Dim otherValue = other(k)
            other(k) = other(n)
            other(n) = otherValue
            Dim weightsValue = weights(k)
            weights(k) = weights(n)
            weights(n) = weightsValue
        End While
    End Sub

    Private Shared Function MakeEpochsPerSample(weights As Single(), nEpochs As Integer) As Single()
        Dim result = Utils.Filled(weights.Length, -1)
        Dim max = Utils.Max(weights)

        For Each nI In weights.Select(Function(w, i) (w / max * nEpochs, i))
            Dim n = nI.Item1
            Dim i = nI.Item2
            If n > 0 Then result(i) = nEpochs / n
        Next

        Return result
    End Function

    Private Sub InitializeOptimization()
        ' Initialized in initializeSimplicialSetEmbedding()
        Dim head = _optimizationState.Head
        Dim tail = _optimizationState.Tail
        Dim epochsPerSample = _optimizationState.EpochsPerSample
        Dim nEpochs = GetNEpochs()
        Dim nVertices = _graph.Dims.cols
        Dim aB As (a!, b!) = Umap.FindABParams(_spread, _minDist)
        _optimizationState.Head = head
        _optimizationState.Tail = tail
        _optimizationState.EpochsPerSample = epochsPerSample
        _optimizationState.A = aB.a
        _optimizationState.B = aB.b
        _optimizationState.NEpochs = nEpochs
        _optimizationState.NVertices = nVertices
    End Sub

    Friend Shared Function FindABParams(spread As Single, minDist As Single) As (Single, Single)
        ' 2019-06-21 DWR: If we need to support other spread, minDist values then we might be able to use the LM implementation in Accord.NET but I'll hard code values that relate to the default configuration for now
        If spread <> 1 OrElse minDist <> 0.1F Then Throw New ArgumentException($"Currently, the {NameOf(FindABParams)} method only supports spread, minDist values of 1, 0.1 (the Levenberg-Marquardt algorithm is required to process other values")
        Return (1.56947052F, 0.8941996F)
    End Function

    Private Sub PrepareForOptimizationLoop()
        ' Hyperparameters
        Dim repulsionStrength = _repulsionStrength
        Dim learningRate = _learningRate
        Dim negativeSampleRate = _negativeSampleRate
        Dim epochsPerSample = _optimizationState.EpochsPerSample
        Dim [dim] = _optimizationState.Dim
        Dim epochsPerNegativeSample = epochsPerSample.[Select](Function(e) e / negativeSampleRate).ToArray()
        Dim epochOfNextNegativeSample = epochsPerNegativeSample.ToArray()
        Dim epochOfNextSample = epochsPerSample.ToArray()
        _optimizationState.EpochOfNextSample = epochOfNextSample
        _optimizationState.EpochOfNextNegativeSample = epochOfNextNegativeSample
        _optimizationState.EpochsPerNegativeSample = epochsPerNegativeSample
        _optimizationState.MoveOther = True
        _optimizationState.InitialAlpha = learningRate
        _optimizationState.Alpha = learningRate
        _optimizationState.Gamma = repulsionStrength
        _optimizationState.Dim = [dim]
    End Sub

    ''' <summary>
    ''' Manually step through the optimization process one epoch at a time
    ''' </summary>
    Public Function [Step]() As Integer
        Dim currentEpoch = _optimizationState.CurrentEpoch
        Dim numberOfEpochsToComplete = GetNEpochs()

        If currentEpoch < numberOfEpochsToComplete Then
            Me.OptimizeLayoutStep(currentEpoch)

            If _progressReporter IsNot Nothing Then
                ' InitializeFit roughly approximately takes 80% of the processing time for large quantities of data, leaving 20% for the Step iterations - the progress reporter
                ' calls made here are based on the assumption that Step will be called the recommended number of times (the number-of-epochs value returned from InitializeFit)
                Umap.ScaleProgressReporter(_progressReporter, 0.8F, 1)(CSng(currentEpoch) / numberOfEpochsToComplete)
            End If
        End If

        Return _optimizationState.CurrentEpoch
    End Function

    ''' <summary>
    ''' Improve an embedding using stochastic gradient descent to minimize the fuzzy set cross entropy between the 1-skeletons of the high dimensional and low dimensional fuzzy simplicial sets.
    ''' In practice this is done by sampling edges based on their membership strength(with the (1-p) terms coming from negative sampling similar to word2vec).
    ''' </summary>
    Private Sub OptimizeLayoutStep(n As Integer)
        If _random.IsThreadSafe Then
            Parallel.For(0, _optimizationState.EpochsPerSample.Length, Sub(i) Call Iterate(i, n))
        Else

            For i = 0 To _optimizationState.EpochsPerSample.Length - 1
                Iterate(i, n)
            Next
        End If

        _optimizationState.Alpha = _optimizationState.InitialAlpha * (1.0F - n / _optimizationState.NEpochs)
        _optimizationState.CurrentEpoch += 1 'Preparation for future work for interpolating the table before optimizing
    End Sub

    Private Sub Iterate(i As Integer, n As Integer)

        If (_optimizationState.EpochOfNextSample(i) >= n) Then Return

        Dim embeddingSpan = _embedding.ToArray()

        Dim j As Integer = _optimizationState.Head(i)
        Dim k As Integer = _optimizationState.Tail(i)

        Dim current = embeddingSpan.slice(j * _optimizationState.Dim, _optimizationState.Dim).ToArray
        Dim other = embeddingSpan.slice(k * _optimizationState.Dim, _optimizationState.Dim).ToArray

        Dim distSquared = Umap.RDist(current, other)
        Dim gradCoeff = 0F

        If (distSquared > 0) Then

            gradCoeff = -2 * _optimizationState.A * _optimizationState.B * Math.Pow(distSquared, _optimizationState.B - 1)
            gradCoeff /= _optimizationState.A * Math.Pow(distSquared, _optimizationState.B) + 1
        End If

        Const clipValue = 4.0F
        For d = 0 To _optimizationState.Dim - 1

            Dim gradD = Umap.Clip(gradCoeff * (current(d) - other(d)), clipValue)
            current(d) += gradD * _optimizationState.Alpha
            If (_optimizationState.MoveOther) Then
                other(d) += -gradD * _optimizationState.Alpha
            End If
        Next

        _optimizationState.EpochOfNextSample(i) += _optimizationState.EpochsPerSample(i)

        Dim nNegSamples As Integer = Math.Floor((n - _optimizationState.EpochOfNextNegativeSample(i)) / _optimizationState.EpochsPerNegativeSample(i))

        For p = 0 To nNegSamples - 1

            k = _random.Next(0, _optimizationState.NVertices)
            other = embeddingSpan.slice(k * _optimizationState.Dim, _optimizationState.Dim)
            distSquared = Umap.RDist(current, other)
            gradCoeff = 0F
            If (distSquared > 0) Then
                gradCoeff = 2 * _optimizationState.Gamma * _optimizationState.B
                gradCoeff *= _optimizationState.GetDistanceFactor(distSquared) ' Preparation For future work For interpolating the table before optimizing

            ElseIf (j = k) Then
                Continue For
            End If

            For d = 0 To _optimizationState.Dim - 1

                Dim gradD = 4.0F
                If (gradCoeff > 0) Then gradD = Umap.Clip(gradCoeff * (current(d) - other(d)), clipValue)
                current(d) += gradD * _optimizationState.Alpha
            Next
        Next

        _optimizationState.EpochOfNextNegativeSample(i) += nNegSamples * _optimizationState.EpochsPerNegativeSample(i)
    End Sub

    ''' <summary>
    ''' Reduced Euclidean distance
    ''' </summary>
    Private Shared Function RDist(x As Single(), y As Single()) As Single
        'return Mosaik.Core.SIMD.Euclidean(ref x, ref y);
        Dim distSquared = 0F

        For i = 0 To x.Length - 1
            Dim d = x(i) - y(i)
            distSquared += d * d
        Next

        Return distSquared
    End Function

    ''' <summary>
    ''' Standard clamping of a value into a fixed range
    ''' </summary>
    Private Shared Function Clip(x As Single, clipValue As Single) As Single
        If x > clipValue Then
            Return clipValue
        ElseIf x < -clipValue Then
            Return -clipValue
        Else
            Return x
        End If
    End Function

    Private Shared Function ScaleProgressReporter(progressReporter As ProgressReporter, start As Single, [end] As Single) As ProgressReporter
        Dim range = [end] - start
        Return Sub(progress) progressReporter(range * progress + start)
    End Function

    Public NotInheritable Class DistanceFunctions
        Public Shared Function Cosine(lhs As Single(), rhs As Single()) As Single
            Return 1 - SIMD.DotProduct(lhs, rhs) / (SIMD.Magnitude(lhs) * SIMD.Magnitude(rhs))
        End Function

        Public Shared Function CosineForNormalizedVectors(lhs As Single(), rhs As Single()) As Single
            Return 1 - SIMD.DotProduct(lhs, rhs)
        End Function

        Public Shared Function Euclidean(lhs As Single(), rhs As Single()) As Single
            Return Math.Sqrt(SIMD.Euclidean(lhs, rhs)) ' TODO: Replace with netcore3 MathF class when the framework is available
        End Function
    End Class

    Private NotInheritable Class OptimizationState
        Public CurrentEpoch As Integer = 0
        Public Head As Integer() = New Integer(-1) {}
        Public Tail As Integer() = New Integer(-1) {}
        Public EpochsPerSample As Single() = New Single(-1) {}
        Public EpochOfNextSample As Single() = New Single(-1) {}
        Public EpochOfNextNegativeSample As Single() = New Single(-1) {}
        Public EpochsPerNegativeSample As Single() = New Single(-1) {}
        Public MoveOther As Boolean = True
        Public InitialAlpha As Single = 1
        Public Alpha As Single = 1
        Public Gamma As Single = 1
        Public A As Single = 1.57694352F
        Public B As Single = 0.8950609F
        Public [Dim] As Integer = 2
        Public NEpochs As Integer = 500
        Public NVertices As Integer = 0

        <MethodImpl(MethodImplOptions.AggressiveInlining)>
        Public Function GetDistanceFactor(distSquared As Single) As Single
            Return 1.0F / ((0.001F + distSquared) * CSng(A * Math.Pow(distSquared, B) + 1))
        End Function
    End Class
End Class
