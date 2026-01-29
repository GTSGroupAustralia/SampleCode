using GHost.GSharp.Calculation.AF;
using GHost.GSharp.Core.AF.Data;
using GHost.GSharp.Core.Attributes;
using GHost.GSharp.Core.Enums;
using GHost.GSharp.Core.Interfaces.Model;
using GHost.GSharp.PythonNet.Calculation;
using OSIsoft.AF.Asset;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;

namespace GSharp.PythonRegressionAnalysis
{
    /// <summary>
    /// A gSharp execution module.
    /// This module will be imported to the server at the time the containing assembly is registered.
    /// </summary>
    public class LinearRegression : CalculationModuleAFBase
    {
        private PythonHost _PythonHost;

        [PointDataDirection(DataDirection.Input)]
        public AFDataPoint ProcessFeedrate { get; set; }

        [PointDataDirection(DataDirection.Output)]
        public AFDataPoint Intercept { get; set; }

        [PointDataDirection(DataDirection.Output)]
        public AFDataPoint Slope { get; set; }

        public override void Initialize()
        {
            IsSequential = true;
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
                //AFValues tsDataset = ProcessFeedrate.RecordedValues("*-8h", "*");

                // Uncomment the code below to use the SampledValues method for evenly spaced interpolated data. Change the time range and sampling interval as needed.
                AFValues tsDataset = ProcessFeedrate.SampledValues("*-8h", "*", "5m");

                // Uncomment the code below to use averaged data over the time range. Change the time range and calculation interval as needed.
                //var summariesDataset = ProcessFeedrate.PIPoint.Summaries(new OSIsoft.AF.Time.AFTimeRange("*-8h", "*"),
                //    new OSIsoft.AF.Time.AFTimeSpan(0, 0, 0, 0, 5), // Represents a 5 minute interval
                //    OSIsoft.AF.Data.AFSummaryTypes.Average,
                //    OSIsoft.AF.Data.AFCalculationBasis.TimeWeighted,
                //    OSIsoft.AF.Data.AFTimestampCalculation.Auto);
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

                double.TryParse(pySlope.ToString(), out double slopeValue);
                double.TryParse(pyIntercept.ToString(), out double interceptValue);

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
}
