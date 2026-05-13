using GHost.Core;
using GHost.Core.CanaryLabs.Contracts.Enums;
using GHost.Core.CanaryLabs.Contracts.Model;
using GHost.Core.Logging.Context;
using GHost.Core.Time;
using GHost.GSharp.Calculation.CanaryLabs;
using GHost.GSharp.Core.Attributes;
using GHost.GSharp.Core.CanaryLabs.Data;
using GHost.GSharp.Core.Enums;
using GHost.GSharp.Core.Interfaces.Model;
using GHost.GSharp.PythonNet.Calculation;
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
public class LinearRegression : CalculationModuleCLBase
{
    private PythonHost _PythonHost;

    [PointDataDirectionInput]
    public CLDoublePoint Well_Level { get; set; }

    [PointDataDirectionOutput]
    public CLDoublePoint Intercept { get; set; }

    [PointDataDirectionOutput]
    public CLDoublePoint Slope { get; set; }


    public override void Initialize()
    {
        _PythonHost = this.InitializePython();
    }

    public override void Execute(ISchedule schedule, DateTime timestamp, CancellationToken cancelToken)
    {
        try
        {
            var tsDataset = Well_Level.Data.GetAggregatedData("Now-8h", "Now", AggregateType.Average, TimeSpan.FromMinutes(5));

            // Clean the data before further processing, i.e., remove any bad quality values.
            var cleanDataSet = tsDataset.Where(v => v.Q >= 64).ToList();

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

            double.TryParse(pySlope.ToString(), out double slopeValue);
            double.TryParse(pyIntercept.ToString(), out double interceptValue);

            Slope.Value = slopeValue;
            Intercept.Value = interceptValue;
        }
        catch (Exception ex)
        {
            Log.Error($"An error occurred during execution: {ex}");
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

    public JsonDataDictionary(List<TagValue> data)
    {
        Date = [];
        Value = [];

        for (int i = 0; i < data.Count -1; i++)
        {
            Date.Add(data[i].T.ToString("yyyy-MM-ddTHH:mm:ss"));
            Value.Add(data[i].V.ToString());
        }
    }
}