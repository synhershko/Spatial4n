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

using Xunit;

namespace Spatial4n.Core
{
	public static class CustomAssert
	{
		/// <summary>
		/// This is purely here so that we have an easy was to make Xunit compatible with some of the JUnit tests methods
		/// </summary>
		/// <param name="expected"></param>
		/// <param name="actual"></param>
		/// <param name="delta"></param>
		public static void EqualWithDelta(double expected, double actual, double delta)
		{
			Assert.InRange(actual, expected - delta, expected + delta);
		}

        public static void EqualWithDelta(string msg, double expected, double actual, double delta)
        {
            Assert.InRange(actual, expected - delta, expected + delta);
        }
	}
}