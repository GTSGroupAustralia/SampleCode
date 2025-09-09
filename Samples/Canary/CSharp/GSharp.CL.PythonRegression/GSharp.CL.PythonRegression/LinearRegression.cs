using GHost.Core;
using GHost.Core.CanaryLabs.Enums;
using GHost.Core.CanaryLabs.Model;
using GHost.GSharp.Calculation.CanaryLabs;
using GHost.GSharp.Core.Attributes;
using GHost.GSharp.Core.CanaryLabs.Data;
using GHost.GSharp.Core.Enums;
using GHost.GSharp.Core.Interfaces;
using GHost.GSharp.Core.Interfaces.Model;
using Python.Runtime;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;

namespace GSharp.CL.PythonRegression
{
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

        [PointDataDirection(DataDirection.Input)]
        public CLDoublePoint Well_Level { get; set; }

        [PointDataDirection(DataDirection.Output)]
        public CLDoublePoint Intercept { get; set; }

        [PointDataDirection(DataDirection.Output)]
        public CLDoublePoint Slope { get; set; }


        public override void Initialize()
        {
            IsSequential = true;
            // Set the following to the actual path of the Python interpreter on the local machine
            string pythonDll = @"C:\Program Files\Python\Python312\python312.dll";
            Runtime.PythonDLL = pythonDll;
            Environment.SetEnvironmentVariable("PYTHONNET_PYDLL", pythonDll);
        }

        public override void Execute(ISchedule schedule, DateTime timestamp, CancellationToken cancelToken)
        {
            try
            {
                /* Depending on requirements, data can be retrieved using one of two options: 
                 * either GetTagData (archive data) or GetAggregatedData (summarised data) methods */

                // Uncomment the code below to use the GetTagData method for raw archive data. Change the time range as needed.
                var tsDataset = Well_Level.Data.GetTagData("Now-2d", "Now");

                // Uncomment the code below to use the GetAggregatedData method for evenly spaced interpolated data. Change the time range, AggregateType, and sampling interval as needed.
                //var tsDataset = Well_Level.Data.GetAggregatedData("Now-2d", "Now", AggregateType.Average, "1h");

                // Clean the data before further processing, i.e., remove any bad quality values
                var cleanDataSet = tsDataset.Where(v => v.Q >= 64).ToList();

                var pyDictionary = new JsonDataDictionary(cleanDataSet);
                string code = ReadPythonScript("LinearRegression.py");

                string pyDataset = JsonSerializer.Serialize(pyDictionary);
                var result = RunPythonCodeAndReturn(code, pyDataset);

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
                        var slope = scope.Get<object>("slope");
                        var intercept = scope.Get<object>("intercept");

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
}
