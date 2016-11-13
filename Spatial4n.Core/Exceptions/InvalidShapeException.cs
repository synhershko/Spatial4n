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

namespace Spatial4n.Core.Exceptions
{
    /// <summary>
    /// A shape was constructed but failed because, based on the given parts, it's invalid. For example
    /// a rectangle's minimum Y was specified as greater than the maximum Y. This class is not used for
    /// parsing exceptions; that's usually <see cref="ParseException"/>.
    /// </summary>
	[Serializable]
	public class InvalidShapeException : ApplicationException
	{
		public InvalidShapeException(string reason) : base(reason)
		{
		}

		public InvalidShapeException(string reason, Exception exception)
			: base(reason, exception)
		{
		}
	}
}
