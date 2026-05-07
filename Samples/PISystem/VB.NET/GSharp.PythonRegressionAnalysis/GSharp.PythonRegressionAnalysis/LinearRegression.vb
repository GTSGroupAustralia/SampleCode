Imports System.IO
Imports System.Reflection
Imports System.Text.Json
Imports System.Threading
Imports GHost.GSharp.Calculation.AF
Imports GHost.GSharp.Core.AF.Data
Imports GHost.GSharp.Core.Attributes
Imports GHost.GSharp.Core.Enums
Imports GHost.GSharp.Core.Interfaces.Model
Imports GHost.GSharp.PythonNet.Calculation
Imports OSIsoft.AF.Asset
Imports OSIsoft.AF.Time
Imports Python.Runtime

''' <summary>
''' A gSharp execution module.
''' This module will be imported to the server at the time the containing assembly is registered.
''' </summary>
Public Class LinearRegression
    Inherits CalculationModuleAFBase

    Private _PythonHost As PythonHost

    <PointDataDirection(DataDirection.Input)>
    Public Property ProcessFeedrate As AFAttrFloatPoint

    <PointDataDirection(DataDirection.Output)>
    Public Property Intercept As AFAttrFloatPoint

    <PointDataDirection(DataDirection.Output)>
    Public Property Slope As AFAttrFloatPoint


    Public Overrides Sub Initialize()
        _PythonHost = InitializePython()
    End Sub

    Public Overrides Sub Execute(schedule As ISchedule, timestamp As Date, cancelToken As CancellationToken)
        Try
            ' Depending on requirements, data can be retrieved using one of several options:
            ' either the built-in RecordedValues or InterpolatedValues methods of AFAttrxxxPoint.Data,
            ' or using the underlying AFSDK Summaries method to get averaged data

            ' Uncomment the code below to use the RecordedValues method for raw archive data. Change the time range as needed.
            'Dim tsDataset As AFValues = ProcessFeedrate.Data.RecordedValues("*-8h", "*", AFBoundaryType.Inside, Nothing)

            ' Uncomment the code below to use the InterpolatedValues method for evenly spaced interpolated data. Change the time range and sampling interval as needed.
            Dim tsDataset As AFValues = ProcessFeedrate.Data.InterpolatedValues("*-8h", "*", New AFTimeSpan(0, 0, 0, 0, 5), Nothing, Nothing, True)

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

            Dim pySlope As Object = Nothing
            Dim pyIntercept As Object = Nothing

            Using scope As ModuleScope = _PythonHost.CreateScope()
                Dim pyInputData As PyObject = pyDataset.ToPython()
                scope.Set("data", pyInputData)
                scope.Exec(code)
                scope.TryGet("slope", pySlope)
                scope.TryGet("intercept", pyIntercept)
            End Using

            Dim slopeValue As Single = Convert.ToSingle(pySlope)
            Dim interceptValue As Single = Convert.ToSingle(pyIntercept)

            Intercept.Value = interceptValue
            Slope.Value = slopeValue

        Catch ex As Exception
            Log.Error("An error occurred during Linear Regression calculation: ", ex.Message)
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

    Public Sub New(data As List(Of AFValue))
        [Date] = New List(Of String)
        Value = New List(Of String)

        For Each value As AFValue In data
            [Date].Add(value.Timestamp.ToString("yyyy-MM-ddTHH:mm:ss"))
            Me.Value.Add(value.ValueAsDouble().ToString())
        Next
    End Sub
End Class
