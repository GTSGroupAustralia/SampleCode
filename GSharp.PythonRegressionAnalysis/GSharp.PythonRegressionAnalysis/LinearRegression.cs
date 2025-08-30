using GHost.GSharp.Calculation.AF;
using GHost.GSharp.Core.AF.Data;
using GHost.GSharp.Core.Attributes;
using GHost.GSharp.Core.Enums;
using GHost.GSharp.Core.Interfaces.Model;
using OSIsoft.AF.Asset;
using Python.Runtime;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading;

namespace GSharp.PythonRegressionAnalysis
{
    /// <summary>
    /// A gSharp execution module.
    /// This module will be imported to the server at the time the containing assembly is registered.
    /// </summary>
    public class LinearRegression : CalculationModuleAFBase
    {
        [PointDataDirection(DataDirection.Input)]
        public AFDataPoint ProcessFeedrate { get; set; }

        [PointDataDirection(DataDirection.Output)]
        public AFDataPoint Intercept { get; set; }

        [PointDataDirection(DataDirection.Output)]
        public AFDataPoint Slope { get; set; }


        public override void Initialize()
        {
            IsSequential = true;
            // Set the following to the actual path of the Python interpreter on the local machine
            string pythonDll = @"C:\Program Files\Python\Python312\python312.dll";
            Environment.SetEnvironmentVariable("PYTHONNET_PYDLL", pythonDll);
        }

        public override void Execute(ISchedule schedule, DateTime timestamp, CancellationToken cancelToken)
        {
            try
            {
                /* Depending on requirements, data can be retrieved using one of several options: 
                 * either RecordedValues or SampledValues methods,
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
                string pyDataset = pyDictionary.ToJson();
                string code = ReadPythonScript("LinearRegression.py");

                double[] result = RunPythonCodeAndReturn(code, pyDataset);

                Intercept.Value = result[0];
                Slope.Value = result[1];
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

        private double[] RunPythonCodeAndReturn(string pycode, object dataset)
        {
            try
            {
                PythonEngine.Initialize();

                double[] returnedVariable = new double[2];

                using (Py.GIL())
                {
                    using (var scope = Py.CreateScope())
                    {
                        PyObject pyInputData = dataset.ToPython();
                        scope.Set("data", pyInputData);
                        scope.Exec(pycode);

                        scope.TryGet<object>("slope", out object slope);
                        scope.TryGet<object>("intercept", out object intercept);

                        returnedVariable[0] = Convert.ToDouble(slope);
                        returnedVariable[1] = Convert.ToDouble(intercept);
                    }
                }

                PythonEngine.Shutdown();
                return returnedVariable;
            }
            catch (Exception ex)
            {
                Log.Error(ex);
                Py.GIL().Dispose();
                PythonEngine.Shutdown();
                throw;
            }
        }

        public override void Dispose()
        {
            // Place any class-level disposal code in here.
            // This method is called only once, at the time the context is unscheduled or disabled.
        }
    }

    internal class JsonDataDictionary
    {
        public List<string> Timestamps { get; private set; }

        public List<string> Values { get; private set; }

        public JsonDataDictionary(List<AFValue> data)
        {
            Timestamps = [];
            Values = [];

            foreach (var val in data)
            {
                Timestamps.Add(string.Concat("\"", val.Timestamp.ToString("yyyy-MM-dd"), "\""));
                Values.Add(val.ValueAsDouble().ToString());
            }
        }

        public string ToJson()
        {
            var jsonData = string.Concat(
                $$"""{"Date": [{{string.Join(",", Timestamps)}}],""",
                "\"Value\": ", $$"""[{{string.Join(",", Values)}}]}"""
                );

            return jsonData;
        }
    }
}
