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
#if !FEATURE_XUNIT_1X

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xunit;
using Xunit.Sdk;

namespace Spatial4n.Core
{

    /// <summary>
    /// Repeats a <see cref="TheoryAttribute"/>. Must accept a single <see cref="int"/> parameter named <c>iterationNumber</c>.
    /// This only works on XUnit 2+.
    /// </summary>
    public class RepeatAttribute : DataAttribute
    {
		private readonly int count;

		public RepeatAttribute(int count)
		{
			if (count < 1)
			{
				throw new ArgumentOutOfRangeException(
					paramName: nameof(count),
					message: "Repeat count must be greater than 0."
					);
			}
			this.count = count;
		}

		public override IEnumerable<object[]> GetData(MethodInfo methodUnderTest)
		{
			foreach (var iterationNumber in Enumerable.Range(start: 1, count: this.count))
			{
				yield return new object[] { iterationNumber };
			}
		}
    }
}
#endif