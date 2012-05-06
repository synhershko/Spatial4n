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

namespace Spatial4n.Core.Io.Samples
{
	public class SampleData : IComparable<SampleData>
	{
		public String id;
		public String name;
		public String shape;

		public SampleData(String line)
		{
			var vals = line.Split('\t');
			id = vals[0];
			name = vals[1];
			shape = vals[2];
		}

		public int CompareTo(SampleData other)
		{
			if (other == null) return 1;
			return System.String.CompareOrdinal(name, other.name);
		}
	}
}
