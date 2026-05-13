Imports System.IO
Imports System.Reflection
Imports System.Text.Json
Imports System.Threading
Imports GHost.Core.CanaryLabs.Contracts.Enums
Imports GHost.Core.CanaryLabs.Contracts.Model
Imports GHost.GSharp.Calculation.CanaryLabs
Imports GHost.GSharp.Core.Attributes
Imports GHost.GSharp.Core.CanaryLabs.Data
Imports GHost.GSharp.Core.Interfaces.Model
Imports GHost.GSharp.PythonNet.Calculation
Imports Python.Runtime

''' <summary>
''' A gSharp execution module.
''' This module will be imported to the server at the time the containing assembly is registered.
''' </summary>
Public Class LinearRegression
    Inherits CalculationModuleCLBase

    Public _PythonHost As PythonHost

    <PointDataDirectionInput>
    Public Property Well_Level As CLDoublePoint

    <PointDataDirectionOutput>
    Public Property Intercept As CLDoublePoint

    <PointDataDirectionOutput>
    Public Property Slope As CLDoublePoint


    Public Overrides Sub Initialize()
        _PythonHost = InitializePython()
    End Sub

    Public Overrides Sub Execute(schedule As ISchedule, timestamp As Date, cancelToken As CancellationToken)
        Try
            Dim tsDataset = Well_Level.Data.GetAggregatedData("Now-8h", "Now", AggregateType.Average, TimeSpan.FromMinutes(5))

            ' Clean the data before further processing, i.e., remove any bad quality values.
            Dim cleanDataSet = tsDataset.Where(Function(v) v.Q >= 64).ToList()

            Dim pyDictionary = New JsonDataDictionary(cleanDataSet)
            Dim pyDataset = JsonSerializer.Serialize(pyDictionary)
            Dim code As String = ReadPythonScript("LinearRegression.py")

            Dim pySlope As Object = Nothing
            Dim pyIntercept As Object = Nothing

            Using scope As ModuleScope = _PythonHost.CreateScope()
                Dim pyInputData As PyObject = pyDataset.ToPython()
                scope.Set("data", pyInputData)
                scope.Exec(code)
                scope.TryGet("slope", pySlope)
                scope.TryGet("intercept", pyIntercept)
            End Using

            Dim slopeValue As Single = Convert.ToDouble(pySlope)
            Dim interceptValue As Single = Convert.ToDouble(pyIntercept)

            Intercept.Value = interceptValue
            Slope.Value = slopeValue

        Catch ex As Exception

        End Try
    End Sub

    Private Function ReadPythonScript(fileName As String) As String
        Dim assembly As Assembly = assembly.GetExecutingAssembly()
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

    Public Overrides Sub Dispose()
        ' Place any class-level disposal code in here.
        ' This method is called only once, at the time the context is unscheduled Or disabled.
    End Sub
End Class

Friend Class JsonDataDictionary
    Public Property [Date] As List(Of String)
    Public Property Value As List(Of String)

    Public Sub New(data As List(Of TagValue))
        [Date] = New List(Of String)
        Value = New List(Of String)

        For i As Integer = 1 To data.Count
            [Date].Add(data(i).T.ToString("yyyy-MM-ddTHH:mm:ss"))
            Me.Value.Add(data(i).V.ToString())
        Next
    End Sub
End Class