using GHost.Core.CanaryLabs.Contracts.Model;
using GHost.Core.CanaryLabs.Contracts.Enums;
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

namespace PythonAnalyses;

/// <summary>
/// A gSharp execution module.
/// This module will be imported to the server at the time the containing assembly is registered.
/// </summary>
public class LinearRegression : CalculationModuleCLBase
{
    // Update/change the variables below to use your own input and output tags

    /* Canary tag data types include:
     * CLDoublePoint
     * CLFloatPoint
     * CLIntPoint
     * CLLongPoint
     * CLStringPoint
    */
    private PythonHost _PythonHost;

    [PointDataDirection(DataDirection.Input)]
    public CLDoublePoint Well_Level { get; set; }

    [PointDataDirection(DataDirection.Output)]
    public CLDoublePoint Intercept { get; set; }

    [PointDataDirection(DataDirection.Output)]
    public CLDoublePoint Slope { get; set; }


    public override void Initialize()
    {
        _PythonHost = this.InitializePython();
    }

    public override void Execute(ISchedule schedule, DateTime timestamp, CancellationToken cancelToken)
    {
        try
        {
            /* Depending on requirements, data can be retrieved using one of two options: 
             * either GetTagData (archive data) or GetAggregatedData (summarised data) methods */

            // Uncomment the code below to use the GetTagData method for raw archive data. Change the time range as needed.
            //var tsDataset = Well_Level.Data.GetTagData("Now-2d", "Now");

            // Uncomment the code below to use the GetAggregatedData method for evenly spaced interpolated data. Change the time range, AggregateType, and sampling interval as needed.
            var tsDataset = Well_Level.Data.GetAggregatedData("Now-8h", "Now", AggregateType.Average, TimeSpan.FromMinutes(5));

            // Clean the data before further processing, i.e., remove any bad quality values
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

            float.TryParse(pySlope.ToString(), out float slopeValue);
            float.TryParse(pyIntercept.ToString(), out float interceptValue);

            Slope.Value = slopeValue;
            Intercept.Value = interceptValue;
        }
        catch (Exception ex)
        {
            Log.Error(ex);
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

    /* An alternative method for reading the Python script. In this case the script
    * file would not be an embedded resource, but copied to the output directory on build,
    * and then deployed with the gSharp assembly and it's other dependencies.
    * An advantage of this approach is that the Python script could be updated as
    * needed without recompiling the gSharp calculation assembly.
    */
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

        for (int i = 0; i < data.Count - 1; i++)
        {
            if (data[i].V != null)
            {
                Date.Add(data[i].T.ToString("yyyy-MM-ddTHH:mm:ss"));
                Value.Add(data[i].V.ToString());
            }
        }
    }
}