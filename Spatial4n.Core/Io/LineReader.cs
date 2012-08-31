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
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Spatial4n.Core.Io
{
	public abstract class LineReader<T> : IEnumerator<T>
	{
		private int count = 0;
		private int lineNumber = 0;
		private readonly StreamReader reader;
		private String nextLine;
		private T current;

		public abstract T ParseLine(String line);

		protected void ReadComment(String line)
		{

		}

		protected LineReader(Stream @in)
		{
			reader = new StreamReader(@in, Encoding.UTF8);
			MoveNext();
		}

		protected LineReader(StreamReader r)
		{
			reader = r;
			MoveNext();
		}

		public void Dispose()
		{
			reader.Dispose();
		}

		public bool MoveNext()
		{
			current = default(T);

#if NET35
			if (string.IsNullOrEmpty((nextLine ?? "").Trim()) && reader.EndOfStream)
#else
			if (string.IsNullOrWhiteSpace(nextLine) && reader.EndOfStream)
#endif
			{
				nextLine = null;
				return false;
			}

			if (nextLine != null)
			{
				current = ParseLine(nextLine);
				count++;
				nextLine = null;
			}

			try
			{
				while (!reader.EndOfStream)
				{
					nextLine = reader.ReadLine();
					lineNumber++;
					if (nextLine == null)
					{
						Debug.Assert(reader.EndOfStream);
						break;
					}
					else if (nextLine.StartsWith("#"))
					{
						ReadComment(nextLine);
					}
					else
					{
						nextLine = nextLine.Trim();
						if (nextLine.Length > 0)
						{
							break;
						}
					}
				}
			}
			catch (IOException ioe)
			{
				throw new Exception("IOException thrown while reading/closing reader", ioe);
			}

			return true;
		}

		public void Reset()
		{
			throw new System.NotImplementedException();
		}

		public T Current
		{
			get { return current; }
		}

		object IEnumerator.Current
		{
			get { return Current; }
		}

		public int GetLineNumber()
		{
			return lineNumber;
		}

		public int GetCount()
		{
			return count;
		}
	}
}
