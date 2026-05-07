using GHost.Core;
using GHost.Core.Logging.Context;
using GHost.Core.Time;
using GHost.GSharp.Calculation.AF;
using GHost.GSharp.Core.AF.Data;
using GHost.GSharp.Core.AF.Time;
using GHost.GSharp.Core.Attributes;
using GHost.GSharp.Core.Enums;
using GHost.GSharp.Core.Interfaces.Model;
using GHost.GSharp.Core.PI.Enums;
using GHost.GSharp.PythonNet.Calculation;
using OSIsoft.AF.Asset;
using OSIsoft.AF.Data;
using OSIsoft.AF.Time;
using OSIsoft.AF.UnitsOfMeasure;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;

namespace GSharp.PythonRegressionAnalysis;

/// <summary>
/// A gSharp execution module.
/// This module will be imported to the server at the time the containing assembly is registered.
/// </summary>
public class LinearRegression : CalculationModuleAFBase
{
    private PythonHost _PythonHost;

    [PointDataDirection(DataDirection.Input)]
    public AFAttrFloatPoint ProcessFeedrate { get; set; }

    [PointDataDirection(DataDirection.Output)]
    public AFAttrFloatPoint Intercept { get; set; }

    [PointDataDirection(DataDirection.Output)]
    public AFAttrFloatPoint Slope { get; set; }


    public override void Initialize()
    {
        _PythonHost = this.InitializePython();
    }

    public override void Execute(ISchedule schedule, DateTime timestamp, CancellationToken cancelToken)
    {
        try
        {
            /* Depending on requirements, data can be retrieved using one of several options: 
             * either the built-in RecordedValues or SampledValues methods of AFDataPoint,
             * or using the underlying AFSDK Summaries method to get averaged data */

            // Uncomment the code below to use the RecordedValues method for raw archive data. Change the time range as needed.
            //AFValues tsDataset = ProcessFeedrate.Data.RecordedValues("*-8h", "*", AFBoundaryType.Inside, null);

            // Uncomment the code below to use the SampledValues method for evenly spaced interpolated data. Change the time range and sampling interval as needed.
            AFValues tsDataset = ProcessFeedrate.Data.InterpolatedValues("*-8h", "*", new AFTimeSpan(0, 0, 0, 0, 5), null, null, true);

            // Uncomment the code below to use averaged data over the time range. Change the time range and calculation interval as needed.
            //var summariesDataset = ProcessFeedrate.PIPoint.Summaries(new AFTimeRange("*-8h", "*"),
            //    new AFTimeSpan(0, 0, 0, 0, 5), // Represents a 5 minute interval
            //    AFSummaryTypes.Average,
            //    AFCalculationBasis.TimeWeighted,
            //    AFTimestampCalculation.Auto);
            //summariesDataset.TryGetValue(OSIsoft.AF.Data.AFSummaryTypes.Average, out AFValues tsDataset);

            // Clean the data before further processing, i.e., remove any Bad values
            var cleanDataSet = tsDataset.Where(v => v.IsGood).ToList();

            var pyDictionary = new JsonDataDictionary(cleanDataSet);
            string pyDataset = JsonSerializer.Serialize(pyDictionary);
            string code = ReadPythonScript("LinearRegression.py");

            object pySlope;
            object pyIntercept;

            using (ModuleScope scope = _PythonHost.CreateScope())
            {
                scope.Set("data", pyDataset);
                scope.Exec(code);
                scope.TryGet<object>("slope", out pySlope);
                scope.TryGet<object>("intercept", out pyIntercept);
            }

            float.TryParse(pySlope.ToString(), out float slopeValue);
            float.TryParse(pyIntercept.ToString(), out float interceptValue);

            Slope.Value = slopeValue;
            Intercept.Value = interceptValue;
        }
        catch (Exception ex)
        {
            Log.Error($"An error occurred during Linear Regression calculation: {ex.Message}");
        }
    }

    private string ReadPythonScript(string fileName)
    {
        var assembly = System.Reflection.Assembly.GetExecutingAssembly();
        string resourcePath = fileName;
        resourcePath = assembly.GetManifestResourceNames()
            .Single(str => str.EndsWith(fileName));

        using Stream stream = assembly.GetManifestResourceStream(resourcePath);
        using StreamReader reader = new(stream);
        return reader.ReadToEnd();
    }

    //private string ReadPythonScript(string fileName)
    //{
    //    var assembly = System.Reflection.Assembly.GetExecutingAssembly();
    //    string filePath = Path.Combine(Path.GetDirectoryName(assembly.Location), "PythonScripts", fileName);

    //    using Stream stream = File.OpenRead(filePath);
    //    using StreamReader reader = new(stream);
    //    return reader.ReadToEnd();
    //}

    public override void Dispose()
    {
        // Place any class-level disposal code in here.
        // This method is called only once, at the time the context is unscheduled or disabled.
    }
}

internal class JsonDataDictionary
{
    public List<string> Date { get; private set; }

    public List<string> Value { get; private set; }

    public JsonDataDictionary(List<AFValue> data)
    {
        Date = [];
        Value = [];

        foreach (var val in data)
        {
            Date.Add(val.Timestamp.ToString("yyyy-MM-ddTHH:mm:ss"));
            Value.Add(val.ValueAsDouble().ToString());
        }
    }
}
