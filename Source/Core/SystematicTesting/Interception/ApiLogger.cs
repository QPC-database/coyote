﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Serialization;

namespace Microsoft.Coyote.SystematicTesting.Interception
{
    /// <summary>
    /// Logs invocation of APIs during testing.
    /// </summary>
    /// <remarks>This type is intended for compiler use rather than use directly in code.</remarks>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public static class ApiLogger
    {
        /// <summary>
        /// API invocation information about the latest test executing.
        /// </summary>
        private static ApiInvocationInfo LatestTestInfo;

        /// <summary>
        /// Logs that the specified test started executing.
        /// </summary>
        public static void LogTestStarted(string name)
        {
            var info = new ApiInvocationInfo(name);
            info.Save();
            LatestTestInfo = info;
        }

        /// <summary>
        /// Logs that the specified API was invoked.
        /// </summary>
        public static void LogInvocation(string name)
        {
            // TODO: log the test constructor.
            if (LatestTestInfo != null)
            {
                LatestTestInfo.LogInvocation(name);
            }
        }

        /// <summary>
        /// Information about an API.
        /// </summary>
        public class ApiInvocation
        {
            /// <summary>
            /// The name of the API.
            /// </summary>
            public string Name { get; set; }

            /// <summary>
            /// The frequency of the API invocation.
            /// </summary>
            public int Frequency { get; set; }
        }

        /// <summary>
        /// Information about API invocations that can be serialized to an XML file.
        /// </summary>
        public class ApiInvocationInfo
        {
            private static readonly object SyncObject = new object();

            /// <summary>
            /// The name of the test.
            /// </summary>
            public string Name { get; set; }

            /// <summary>
            /// The location of the test.
            /// </summary>
            public string Location { get; set; }

            /// <summary>
            /// List of invoked APIs.
            /// </summary>
            public List<ApiInvocation> APIs
            {
                get
                {
                    var list = new List<ApiInvocation>();
                    foreach (var kvp in this.ApiFrequencies)
                    {
                        list.Add(new ApiInvocation()
                        {
                            Name = kvp.Key,
                            Frequency = kvp.Value
                        });
                    }

                    return list;
                }

                set
                {
                    foreach (var api in value)
                    {
                        this.ApiFrequencies[api.Name] = api.Frequency;
                    }
                }
            }

            /// <summary>
            /// Map from APIs to their invocation frequency.
            /// </summary>
            private readonly IDictionary<string, int> ApiFrequencies;

            /// <summary>
            /// Path to the serialized file.
            /// </summary>
            private readonly string FilePath;

            /// <summary>
            /// Initializes a new instance of the <see cref="ApiInvocationInfo"/> class.
            /// </summary>
            public ApiInvocationInfo()
            {
                this.ApiFrequencies = new SortedDictionary<string, int>();
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="ApiInvocationInfo"/> class.
            /// </summary>
            internal ApiInvocationInfo(string name)
            {
                this.Name = name;
                this.Location = this.GetLocation();
                this.FilePath = this.GetFilePath(this.Location, Guid.NewGuid().ToString());
                this.ApiFrequencies = new SortedDictionary<string, int>();
            }

            internal void LogInvocation(string name)
            {
                if (!this.ApiFrequencies.ContainsKey(name))
                {
                    this.ApiFrequencies.Add(name, 0);
                }

                this.ApiFrequencies[name]++;
                this.Save();
            }

            /// <summary>
            /// Serializes to an XML file.
            /// </summary>
            internal void Save()
            {
                lock (SyncObject)
                {
                    using FileStream fs = new FileStream(this.FilePath, FileMode.OpenOrCreate, FileAccess.Write);
                    var serializer = new XmlSerializer(typeof(ApiInvocationInfo));
                    serializer.Serialize(fs, this);
                }
            }

            private string GetLocation() => Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            private string GetFilePath(string location, string name) => Path.Combine(location, $"test.{name}.api.xml");
        }
    }
}