Imports System.IO
Imports System.Reflection
Imports System.Text.Json
Imports System.Threading
Imports GHost.GSharp.Calculation.AF
Imports GHost.GSharp.Core.AF.Data
Imports GHost.GSharp.Core.Attributes
Imports GHost.GSharp.Core.Enums
Imports GHost.GSharp.Core.Interfaces.Model
Imports OSIsoft.AF.Asset
Imports Python.Runtime

''' <summary>
''' A gSharp execution module.
''' This module will be imported to the server at the time the containing assembly is registered.
''' </summary>
Public Class LinearRegression
    Inherits CalculationModuleAFBase

    <PointDataDirection(DataDirection.Input)>
    Public Property ProcessFeedrate As AFDataPoint

    <PointDataDirection(DataDirection.Output)>
    Public Property Intercept As AFDataPoint

    <PointDataDirection(DataDirection.Output)>
    Public Property Slope As AFDataPoint

    Public Overrides Sub Initialize()
        IsSequential = True
        ' Set the following to the actual path of the Python interpreter on the local machine
        Dim pythonDll As String = "C:\Program Files\Python\Python312\python312.dll"
        Environment.SetEnvironmentVariable("PYTHONNET_PYDLL", pythonDll)
    End Sub

    Public Overrides Sub Execute(schedule As ISchedule, timestamp As Date, cancelToken As CancellationToken)
        Try
            ' Depending on requirements, data can be retrieved using one of several options:
            ' either the built-in RecordedValues or SampledValues methods of AFDataPoint,
            ' or using the underlying AFSDK Summaries method to get averaged data

            ' Uncomment the code below to use the RecordedValues method for raw archive data. Change the time range as needed.
            'Dim tsDataset As AFValues = ProcessFeedrate.RecordedValues("*-8h", "*")

            ' Uncomment the code below to use the SampledValues method for evenly spaced interpolated data. Change the time range and sampling interval as needed.
            Dim tsDataset As AFValues = ProcessFeedrate.SampledValues("*-8h", "*", "5m")

            ' Uncomment the code below to use averaged data over the time range. Change the time range and calculation interval as needed.
            'Dim tsDataset As New AFValues
            'Dim summariesDataset = ProcessFeedrate.PIPoint.Summaries(New OSIsoft.AF.Time.AFTimeRange("*-8h", "*"),
            '                                                  New OSIsoft.AF.Time.AFTimeSpan(0, 0, 0, 0, 5),
            '                                                  OSIsoft.AF.Data.AFSummaryTypes.Average,
            '                                                  OSIsoft.AF.Data.AFCalculationBasis.TimeWeighted,
            '                                                  OSIsoft.AF.Data.AFTimestampCalculation.Auto)

            'summariesDataset.TryGetValue(OSIsoft.AF.Data.AFSummaryTypes.Average, tsDataset)

            Dim cleanDataSet As List(Of AFValue) = tsDataset.Where(Function(v) v.IsGood).ToList()

            Dim pyDictionary = New JsonDataDictionary(cleanDataSet)
            Dim pyDataset = JsonSerializer.Serialize(pyDictionary)
            Dim code As String = ReadPythonScript("LinearRegression.py")

            Dim result(1) As Double
            result = RunPythonCodeAndReturn(code, pyDataset)
            Intercept.Value = result(0)
            Slope.Value = result(1)

        Catch ex As Exception

        End Try
    End Sub

    Private Function ReadPythonScript(fileName As String) As String
        Dim assembly As Assembly = Assembly.GetExecutingAssembly()
        Dim resourcePath As String = fileName

        resourcePath = assembly.GetManifestResourceNames().Single(Function(str) str.EndsWith(fileName))
        Using stream As Stream = assembly.GetManifestResourceStream(resourcePath)
            Using reader As New StreamReader(stream)
                Return reader.ReadToEnd()
            End Using
        End Using
    End Function

    ' An alternative method for reading the Python script. In this case the script
    ' file would not be an embedded resource, but copied to the output directory on build,
    ' and then deployed with the gSharp assembly and it's other dependencies.
    ' An advantage of this approach is that the Python script could be updated as
    ' needed without recompiling the gSharp calculation assembly.

    'Private Function ReadPythonScript(fileName As String) As String
    '    Dim assembly As Assembly = Assembly.GetExecutingAssembly()
    '    Dim filePath As String = Path.Combine(Path.GetDirectoryName(assembly.Location), "PythonScripts", fileName)

    '    Using stream As Stream = File.OpenRead(filePath)
    '        Using reader As New StreamReader(stream)
    '            Return reader.ReadToEnd()
    '        End Using
    '    End Using
    'End Function

    Private Function RunPythonCodeAndReturn(pycode As String, dataset As String) As Double()
        Try
            PythonEngine.Initialize()

            Dim returnedVariable(1) As Double

            ' Get the Python Global Interpreter Lock
            Using (Py.GIL)
                Using scope = Py.CreateScope()
                    ' Convert the JSON dataset to a Python object
                    Dim pyInputData As PyObject = dataset.ToPython()
                    ' Insert the Python object containing the data into the Python scope
                    scope.Set("data", pyInputData)
                    ' Now execute the script
                    scope.Exec(pycode)

                    Dim pySlope As PyObject = Nothing
                    Dim pyIntercept As PyObject = Nothing

                    ' Retrieve the results variables from the Python scope as .NET objects
                    scope.TryGet("slope", pySlope)
                    scope.TryGet("intercept", pyIntercept)
                    returnedVariable(0) = Convert.ToDouble(pySlope)
                    returnedVariable(1) = Convert.ToDouble(pyIntercept)
                End Using
            End Using

            ' Remember to shut down the Python engine when we're done
            PythonEngine.Shutdown()
            Return returnedVariable

        Catch ex As Exception
            Log.Error(ex)

            ' Make sure to dispose of objects and shutdown the Python engine
            Py.GIL().Dispose()
            PythonEngine.Shutdown()
            Throw
        End Try
    End Function

    Public Overrides Sub Dispose()
        ' Place any class-level disposal code in here.
        ' This method is called only once, at the time the context is unscheduled Or disabled.
    End Sub
End Class

Friend Class JsonDataDictionary
    Public Property [Date] As List(Of String)
    Public Property Value As List(Of String)

    Public Sub New(data As List(Of AFValue))
        [Date] = New List(Of String)
        Value = New List(Of String)

        For Each value As AFValue In data
            [Date].Add(value.Timestamp.ToString("yyyy-MM-ddTHH:mm:ss"))
            Me.Value.Add(value.ValueAsDouble().ToString())
        Next
    End Sub
End Class
