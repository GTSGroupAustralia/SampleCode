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

        /// <summary>
        /// Executes the Python script with the data provided
        /// </summary>
        /// <param name="pycode">The Python script code to be executed</param>
        /// <param name="dataset">The serialised JSON dataset containing the data retrieved from PI</param>
        /// <returns>Double array containing the results from the Python script</returns>
        private double[] RunPythonCodeAndReturn(string pycode, object dataset)
        {
            try
            {
                PythonEngine.Initialize();

                double[] returnedVariable = new double[2];

                // Get the Python Global Interpreter Lock
                using (Py.GIL())
                {
                    using (var scope = Py.CreateScope())
                    {
                        // Convert the JSON dataset to a Python object
                        PyObject pyInputData = dataset.ToPython();

                        // Insert the Python object containing the data into the Python scope
                        scope.Set("data", pyInputData);

                        // Now execute the script
                        scope.Exec(pycode);

                        // Retrieve the results variables from the Python scope as .NET objects
                        scope.TryGet<object>("slope", out object slope);
                        scope.TryGet<object>("intercept", out object intercept);

                        returnedVariable[0] = Convert.ToDouble(slope);
                        returnedVariable[1] = Convert.ToDouble(intercept);
                    }
                }

                // Remember to shut down the Python engine when we're done
                PythonEngine.Shutdown();
                return returnedVariable;
            }
            catch (Exception ex)
            {
                Log.Error(ex);

                // Make sure to dispose of objects and shutdown the Python engine
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
