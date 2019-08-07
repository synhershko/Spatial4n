/*
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Globalization;
using System.Threading;

namespace Spatial4n.Core
{
    public sealed class TemporaryCulture : IDisposable
    {
        private readonly CultureInfo oldCurrentCulture;
        private readonly CultureInfo oldCurrentUiCulture;

        public TemporaryCulture(CultureInfo cultureInfo)
        {
#if NETCOREAPP1_0
            oldCurrentCulture = CultureInfo.CurrentCulture;
            oldCurrentUiCulture = CultureInfo.CurrentUICulture;

            CultureInfo.CurrentCulture = cultureInfo;
            CultureInfo.CurrentUICulture = cultureInfo;
#else
            oldCurrentCulture = Thread.CurrentThread.CurrentCulture;
            oldCurrentUiCulture = Thread.CurrentThread.CurrentUICulture;

            Thread.CurrentThread.CurrentCulture = cultureInfo;
            Thread.CurrentThread.CurrentUICulture = cultureInfo;
#endif
        }

        public TemporaryCulture(string cultureName)
            : this(new CultureInfo(cultureName))
        {
        }

        public void Dispose()
        {
#if NETCOREAPP1_0
            CultureInfo.CurrentCulture = oldCurrentCulture;
            CultureInfo.CurrentUICulture = oldCurrentUiCulture;
#else
            Thread.CurrentThread.CurrentCulture = oldCurrentCulture;
            Thread.CurrentThread.CurrentUICulture = oldCurrentUiCulture;
#endif
        }
    }
}
