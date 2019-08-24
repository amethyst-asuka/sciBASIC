﻿Imports System
Imports System.Collections.Generic
Imports org.nlp.util

Namespace org.nlp.vec


    ''' <summary>
    ''' Created by fangy on 13-12-19.
    ''' Word2Vec 算法实现
    ''' </summary>
    Public Class Word2Vec
        Private logger As Logger = Logger.getLogger("Word2Vec")
        Private windowSize As Integer '文字窗口大小
        Private vectorSize As Integer '词向量的元素个数

        Public Enum Method
            CBow
            Skip_Gram
        End Enum

        Private trainMethod As Method ' 神经网络学习方法
        Private sample As Double
        '    private int negativeSample;
        Private alpha As Double ' 学习率，并行时由线程更新
        Private alphaThresold As Double
        Private initialAlpha As Double ' 初始学习率
        Private freqThresold As Integer = 5
        Private ReadOnly alphaLock As SByte() = New SByte(-1) {} ' alpha同步锁
        Private ReadOnly treeLock As SByte() = New SByte(-1) {} ' alpha同步锁
        Private ReadOnly vecLock As SByte() = New SByte(-1) {} ' alpha同步锁
        Private expTable As Double()
        Private Const EXP_TABLE_SIZE As Integer = 1000
        Private Const MAX_EXP As Integer = 6
        Private neuronMap As IDictionary(Of String, WordNeuron)
        '    private List<Word> words;
        Private totalWordCount As Integer ' 语料中的总词数
        Private currentWordCount As Integer ' 当前已阅的词数，并行时由线程更新
        Private numOfThread As Integer ' 线程个数

        ' 单词或短语计数器
        Private wordCounter As Counter(Of String) = New Counter(Of String)()
        Private tempCorpus As File = Nothing
        Private tempCorpusWriter As StreamWriter

        Public Class Factory

            'JAVA TO C# CONVERTER CRACKED BY X-CRACKER NOTE: Fields cannot have the same name as methods:
            Friend vectorSize_Renamed As Integer = 200
            Friend windowSize As Integer = 5
            'JAVA TO C# CONVERTER CRACKED BY X-CRACKER NOTE: Fields cannot have the same name as methods:
            Friend freqThresold_Renamed As Integer = 5
            Friend trainMethod As Method = Method.Skip_Gram

            'JAVA TO C# CONVERTER CRACKED BY X-CRACKER NOTE: Fields cannot have the same name as methods:
            Friend sample_Renamed As Double = 1e-3
            '        private int negativeSample = 0;

            'JAVA TO C# CONVERTER CRACKED BY X-CRACKER NOTE: Fields cannot have the same name as methods:
            Friend alpha_Renamed As Double = 0.025, alphaThreshold As Double = 0.0001
            'JAVA TO C# CONVERTER CRACKED BY X-CRACKER NOTE: Fields cannot have the same name as methods:
            Friend numOfThread_Renamed As Integer = 1

            Public Overridable Function setVectorSize(ByVal size As Integer) As Factory
                vectorSize_Renamed = size
                Return Me
            End Function

            Public Overridable Function setWindow(ByVal size As Integer) As Factory
                windowSize = size
                Return Me
            End Function

            Public Overridable Function setFreqThresold(ByVal thresold As Integer) As Factory
                freqThresold_Renamed = thresold
                Return Me
            End Function

            Public Overridable Function setMethod(ByVal method As Method) As Factory
                trainMethod = method
                Return Me
            End Function

            Public Overridable Function setSample(ByVal rate As Double) As Factory
                sample_Renamed = rate
                Return Me
            End Function

            '        public Factory setNegativeSample(int sample){
            '            negativeSample = sample;
            '            return this;
            '        }

            Public Overridable Function setAlpha(ByVal alpha As Double) As Factory
                alpha_Renamed = alpha
                Return Me
            End Function

            Public Overridable Function setAlphaThresold(ByVal alpha As Double) As Factory
                alphaThreshold = alpha
                Return Me
            End Function

            Public Overridable Function setNumOfThread(ByVal numOfThread As Integer) As Factory
                numOfThread_Renamed = numOfThread
                Return Me
            End Function

            Public Overridable Function build() As Word2Vec
                Return New Word2Vec(Me)
            End Function
        End Class

        Private Sub New(ByVal factory As Factory)
            vectorSize = factory.vectorSize_Renamed
            windowSize = factory.windowSize
            freqThresold = factory.freqThresold_Renamed
            trainMethod = factory.trainMethod
            sample = factory.sample_Renamed
            '        negativeSample = factory.negativeSample;
            alpha = factory.alpha_Renamed
            initialAlpha = alpha
            alphaThresold = factory.alphaThreshold
            numOfThread = factory.numOfThread_Renamed
            totalWordCount = 0
            expTable = New Double(999) {}
            computeExp()
        End Sub

        ''' <summary>
        ''' 预先计算并保存sigmoid函数结果，加快后续计算速度
        ''' f(x) = x / (x + 1)
        ''' </summary>
        Private Sub computeExp()
            For i = 0 To EXP_TABLE_SIZE - 1
                expTable(i) = Math.Exp((i / EXP_TABLE_SIZE * 2 - 1) * MAX_EXP)
                expTable(i) = expTable(i) / (expTable(i) + 1)
            Next
        End Sub

        ''' <summary>
        ''' 读取一段文本，统计词频和相邻词语出现的频率，
        ''' 文本将输出到一个临时文件中，以方便之后的训练 </summary>
        ''' <paramname="tokenizer"> 标记 </param>
        Public Overridable Sub readTokens(ByVal tokenizer As Tokenizer)
            If tokenizer Is Nothing OrElse tokenizer.size() < 1 Then
                Return
            End If

            currentWordCount += tokenizer.size()
            ' 读取文本中的词，并计数词频
            While tokenizer.hasMoreTokens()
                wordCounter.add(tokenizer.nextToken())
            End While
            ' 将文本输出到临时文件中，供后续训练之用
            Try

                If tempCorpus Is Nothing Then
                    Dim tempDir As File = New File("temp")

                    If Not tempDir.exists() AndAlso Not tempDir.directory Then
                        Dim tempCreated As Boolean = tempDir.mkdir()

                        If Not tempCreated Then
                            logger.severe("unable to create temp file in " & tempDir.absolutePath)
                            '                        System.out.println("临时文件夹创建失败，位于" + tempDir.getAbsolutePath());
                        End If
                    End If

                    tempCorpus = File.createTempFile("tempCorpus", ".txt", tempDir)
                    '                tempCorpus = File.createTempFile("tempCorpus", ".txt");
                    If tempCorpus.exists() Then
                        logger.info("create temp file successfully in" & tempCorpus.absolutePath)
                        '                    System.out.println("临时文件创建成功，位于" + tempCorpus.getAbsolutePath());
                    End If

                    tempCorpusWriter = New StreamWriter(tempCorpus)
                End If

                tempCorpusWriter.Write(tokenizer.ToString(" "))
                tempCorpusWriter.newLine()
            Catch e As IOException
                Console.WriteLine(e.ToString())
                Console.Write(e.StackTrace)

                Try
                    tempCorpusWriter.Close()
                Catch e1 As IOException
                    Console.WriteLine(e1.ToString())
                    Console.Write(e1.StackTrace)
                End Try
            End Try
        End Sub

        Private Sub buildVocabulary()
            neuronMap = New Dictionary(Of String, WordNeuron)()

            For Each wordText As String In wordCounter.Keys
                Dim freq = wordCounter.get(wordText)

                If freq < freqThresold Then
                    Continue For
                End If

                neuronMap(wordText) = New WordNeuron(wordText, wordCounter.get(wordText), vectorSize)
            Next

            logger.info("read " & neuronMap.Count & " word totally.")
            '        System.out.println("共读取了 " + neuronMap.size() + " 个词。");

        End Sub

        Public Overridable Sub training()
            If tempCorpus Is Nothing Then
                Throw New NullReferenceException("训练语料为空，如果之前调用了training()，" & "请调用readLine(String sentence)重新输入语料")
            End If

            buildVocabulary()
            HuffmanTree.make(neuronMap.Values)
            ' 重新遍历语料
            totalWordCount = currentWordCount
            currentWordCount = 0
            ' 处理线程池定义
            Dim threadPool As ExecutorService = Executors.newFixedThreadPool(numOfThread)
            Dim li As LineIterator = Nothing

            Try
                Dim corpusQueue As BlockingQueue(Of LinkedList(Of String)) = New ArrayBlockingQueue(Of LinkedList(Of String))(numOfThread)
                Dim futures As LinkedList(Of Future) = New LinkedList(Of Future)() '每个线程的返回结果，用于等待线程

                For thi = 0 To numOfThread - 1
                    '                threadPool.execute(new Trainer(corpusQueue));
                    futures.AddLast(threadPool.submit(New Trainer(Me, corpusQueue)))
                Next

                tempCorpusWriter.Close()
                li = New LineIterator(New StreamReader(tempCorpus))
                Dim corpus As LinkedList(Of String) = New LinkedList(Of String)() '若干文本组成的语料
                Dim trainBlockSize = 500 '语料中句子个数

                While li.MoveNext()
                    corpus.AddLast(li.nextLine())

                    If corpus.Count = trainBlockSize Then
                        '放进任务队列，供线程处理
                        '                    futures.add(threadPool.submit(new Trainer(corpus)));

                        corpusQueue.put(corpus)
                        '                    System.out.println("put a corpus");

                        corpus = New LinkedList(Of String)()
                    End If
                End While
                '            futures.add(threadPool.submit(new Trainer(corpus)));
                corpusQueue.put(corpus)
                logger.info("the task queue has been allocated completely, " & "please wait the thread pool (" & numOfThread & ") to process...")

                ' 等待线程处理完语料
                For Each future As Future In futures
                    future.[get]()
                Next

                threadPool.shutdown() ' 关闭线程池
            Catch e As IOException
                Console.WriteLine(e.ToString())
                Console.Write(e.StackTrace)
            Catch e As InterruptedException
                Console.WriteLine(e.ToString())
                Console.Write(e.StackTrace)
            Catch e As ExecutionException
                Console.WriteLine(e.ToString())
                Console.Write(e.StackTrace)
            Finally
                LineIterator.closeQuietly(li)

                If Not tempCorpus.delete() Then
                    logger.severe("unable to delete temp file in " & tempCorpus.absolutePath)
                    '                System.err.println("临时文件未被正确删除，位于"+tempCorpus.getAbsolutePath());
                End If

                tempCorpus = Nothing
            End Try
        End Sub

        Private Sub skipGram(ByVal index As Integer, ByVal sentence As IList(Of WordNeuron), ByVal b As Integer, ByVal alpha As Double)
            Dim word = sentence(index)
            Dim a As Integer, c = 0

            For a = b To windowSize * 2 + 1 - b - 1

                If a = windowSize Then
                    Continue For
                End If

                c = index - windowSize + a

                If c < 0 OrElse c >= sentence.Count Then
                    Continue For
                End If

                Dim neu1e = New Double(vectorSize - 1) {} '误差项
                'Hierarchical Softmax
                Dim pathNeurons = word.pathNeurons
                Dim we = sentence(c)

                For neuronIndex = 0 To pathNeurons.Count - 1 - 1
                    Dim out = CType(pathNeurons(neuronIndex), HuffmanNeuron)
                    Dim f As Double = 0
                    ' Propagate hidden -> output
                    For j = 0 To vectorSize - 1
                        f += we.vector(j) * out.vector(j)
                    Next

                    If f <= -MAX_EXP OrElse f >= MAX_EXP Then
                        '                    System.out.println("F: " + f);
                        Continue For
                    Else
                        f = (f + MAX_EXP) * (EXP_TABLE_SIZE / MAX_EXP / 2)
                        f = expTable(f)
                    End If
                    ' 'g' is the gradient multiplied by the learning rate
                    Dim outNext = CType(pathNeurons(neuronIndex + 1), HuffmanNeuron)
                    Dim g = (1 - outNext.code_Renamed - f) * alpha

                    For c = 0 To vectorSize - 1
                        neu1e(c) += g * out.vector(c)
                    Next
                    ' Learn weights hidden -> output
                    For c = 0 To vectorSize - 1
                        out.vector(c) += g * we.vector(c)
                    Next
                Next
                ' Learn weights input -> hidden
                For j = 0 To vectorSize - 1
                    we.vector(j) += neu1e(j)
                Next
            Next

            '        if (word.getName().equals("手")){
            '            for (Double value : word.vector){
            '                System.out.print(value + "\t");
            '            }
            '            System.out.println();
            '        }
        End Sub

        Private Sub cbowGram(ByVal index As Integer, ByVal sentence As IList(Of WordNeuron), ByVal b As Integer, ByVal alpha As Double)
            Dim word = sentence(index)
            Dim a As Integer, c = 0
            Dim neu1e = New Double(vectorSize - 1) {} '误差项
            Dim neu1 = New Double(vectorSize - 1) {} '误差项
            Dim last_word As WordNeuron

            For a = b To windowSize * 2 + 1 - b - 1

                If a <> windowSize Then
                    c = index - windowSize + a

                    If c < 0 Then
                        Continue For
                    End If

                    If c >= sentence.Count Then
                        Continue For
                    End If

                    last_word = sentence(c)

                    If last_word Is Nothing Then
                        Continue For
                    End If

                    For c = 0 To vectorSize - 1
                        neu1(c) += last_word.vector(c)
                    Next
                End If
            Next
            'Hierarchical Softmax
            Dim pathNeurons = word.pathNeurons

            For neuronIndex = 0 To pathNeurons.Count - 1 - 1
                Dim out = CType(pathNeurons(neuronIndex), HuffmanNeuron)
                Dim f As Double = 0
                ' Propagate hidden -> output
                For c = 0 To vectorSize - 1
                    f += neu1(c) * out.vector(c)
                Next

                If f <= -MAX_EXP Then
                    Continue For
                ElseIf f >= MAX_EXP Then
                    Continue For
                Else
                    f = expTable((f + MAX_EXP) * (EXP_TABLE_SIZE / MAX_EXP / 2))
                End If
                ' 'g' is the gradient multiplied by the learning rate
                Dim outNext = CType(pathNeurons(neuronIndex + 1), HuffmanNeuron)
                Dim g = (1 - outNext.code_Renamed - f) * alpha
                '
                For c = 0 To vectorSize - 1
                    neu1e(c) += g * out.vector(c)
                Next
                ' Learn weights hidden -> output
                For c = 0 To vectorSize - 1
                    out.vector(c) += g * neu1(c)
                Next
            Next

            For a = b To windowSize * 2 + 1 - b - 1

                If a <> windowSize Then
                    c = index - windowSize + a

                    If c < 0 Then
                        Continue For
                    End If

                    If c >= sentence.Count Then
                        Continue For
                    End If

                    last_word = sentence(c)

                    If last_word Is Nothing Then
                        Continue For
                    End If

                    For c = 0 To vectorSize - 1
                        last_word.vector(c) += neu1e(c)
                    Next
                End If
            Next
        End Sub

        Private nextRandom As Long = 5

        Public Class Trainer
            Inherits ThreadStart

            Private ReadOnly outerInstance As Word2Vec
            Friend corpusQueue As BlockingQueue(Of LinkedList(Of String))
            Friend corpusToBeTrained As LinkedList(Of String)
            Friend trainingWordCount As Integer
            Friend tempAlpha As Double

            Public Sub New(ByVal outerInstance As Word2Vec, ByVal corpus As LinkedList(Of String))
                Me.outerInstance = outerInstance
                corpusToBeTrained = corpus
                trainingWordCount = 0
            End Sub

            Public Sub New(ByVal outerInstance As Word2Vec, ByVal corpusQueue As BlockingQueue(Of LinkedList(Of String)))
                Me.outerInstance = outerInstance
                Me.corpusQueue = corpusQueue
            End Sub

            Friend Overridable Sub computeAlpha()
                SyncLock outerInstance.alphaLock
                    outerInstance.currentWordCount += trainingWordCount
                    outerInstance.alpha = outerInstance.initialAlpha * (1 - outerInstance.currentWordCount / (outerInstance.totalWordCount + 1))

                    If outerInstance.alpha < outerInstance.initialAlpha * 0.0001 Then
                        outerInstance.alpha = outerInstance.initialAlpha * 0.0001
                    End If
                    '                logger.info("alpha:" + tempAlpha + "\tProgress: "
                    '                        + (int) (currentWordCount / (double) (totalWordCount + 1) * 100) + "%");
                    Console.WriteLine("alpha:" & tempAlpha & vbTab & "Progress: " & outerInstance.currentWordCount / (outerInstance.totalWordCount + 1) * 100 & "%" & vbTab)
                End SyncLock
            End Sub

            Friend Overridable Sub training()
                '            long nextRandom = 5;
                For Each line In corpusToBeTrained
                    Dim sentence As IList(Of WordNeuron) = New List(Of WordNeuron)()
                    Dim tokenizer As Tokenizer = New Tokenizer(line, " ")
                    trainingWordCount += tokenizer.size()

                    While tokenizer.hasMoreTokens()
                        Dim token As String = tokenizer.nextToken()
                        Dim entry = outerInstance.neuronMap.GetValueOrNull(token)

                        If entry Is Nothing Then
                            Continue While
                        End If
                        ' The subsampling randomly discards frequent words while keeping the ranking same
                        If outerInstance.sample > 0 Then
                            Dim ran = (Math.Sqrt(entry.frequency / (outerInstance.sample * outerInstance.totalWordCount)) + 1) * (outerInstance.sample * outerInstance.totalWordCount) / entry.frequency
                            outerInstance.nextRandom = outerInstance.nextRandom * 25214903917L + 11

                            If ran < (outerInstance.nextRandom And &HFFFF) / 65536 Then
                                Continue While
                            End If

                            sentence.Add(entry)
                        End If
                    End While

                    For index = 0 To sentence.Count - 1
                        outerInstance.nextRandom = outerInstance.nextRandom * 25214903917L + 11

                        Select Case outerInstance.trainMethod
                            Case Method.CBow
                                outerInstance.cbowGram(index, sentence, CInt(outerInstance.nextRandom) Mod outerInstance.windowSize, tempAlpha)
                            Case Method.Skip_Gram
                                outerInstance.skipGram(index, sentence, CInt(outerInstance.nextRandom) Mod outerInstance.windowSize, tempAlpha)
                        End Select
                    Next
                Next
            End Sub

            Public Overrides Sub run()
                Dim hasCorpusToBeTrained = True

                Try

                    While hasCorpusToBeTrained
                        '                    System.out.println("get a corpus");
                        corpusToBeTrained = corpusQueue.poll(2, TimeUnit.SECONDS)
                        '                    System.out.println("队列长度:" + corpusQueue.size());
                        If Nothing IsNot corpusToBeTrained Then
                            tempAlpha = outerInstance.alpha
                            trainingWordCount = 0
                            training()
                            computeAlpha() '更新alpha
                        Else
                            ' 超过2s还没获得数据，认为主线程已经停止投放语料，即将停止训练。
                            hasCorpusToBeTrained = False
                        End If
                    End While

                Catch ie As InterruptedException
                    Console.WriteLine(ie.ToString())
                    Console.Write(ie.StackTrace)
                End Try
            End Sub
        End Class

        ''' <summary>
        ''' 保存训练得到的模型 </summary>
        ''' <paramname="file"> 模型存放路径 </param>
        Public Overridable Sub saveModel(ByVal file As File)
            Dim dataOutputStream As DataOutputStream = Nothing

            Try
                dataOutputStream = New DataOutputStream(New BufferedOutputStream(New FileStream(file, FileMode.Create, FileAccess.Write)))
                dataOutputStream.writeInt(neuronMap.Count)
                dataOutputStream.writeInt(vectorSize)

                For Each element In neuronMap.SetOfKeyValuePairs()
                    dataOutputStream.writeUTF(element.Key)

                    For Each d In element.Value.vector
                        dataOutputStream.writeFloat(CType(d, Double?).Value)
                    Next
                Next

                logger.info("saving model successfully in " & file.absolutePath)
            Catch e As IOException
                Console.WriteLine(e.ToString())
                Console.Write(e.StackTrace)
            Finally

                Try

                    If dataOutputStream IsNot Nothing Then
                        dataOutputStream.close()
                    End If

                Catch ioe As IOException
                    Console.WriteLine(ioe.ToString())
                    Console.Write(ioe.StackTrace)
                End Try
            End Try
        End Sub

        Public Overridable Function outputVector() As VectorModel
            Dim wordMapConverted As IDictionary(Of String, Single()) = New Dictionary(Of String, Single())()
            Dim wordKey As String
            Dim vector As Single()
            Dim vectorLength As Double
            Dim vectorNorm As Double()

            For Each element In neuronMap.SetOfKeyValuePairs()
                wordKey = element.Key
                vectorNorm = element.Value.vector
                vector = New Single(vectorSize - 1) {}
                vectorLength = 0

                For vi = 0 To vectorNorm.Length - 1
                    vectorLength += CSng(vectorNorm(vi)) * vectorNorm(vi)
                    vector(vi) = CSng(vectorNorm(vi))
                Next

                vectorLength = Math.Sqrt(vectorLength)

                For vi = 0 To vector.Length - 1
                    vector(vi) /= CSng(vectorLength)
                Next

                wordMapConverted(wordKey) = vector
            Next

            Return New VectorModel(wordMapConverted, vectorSize)
        End Function
    End Class
End Namespace
