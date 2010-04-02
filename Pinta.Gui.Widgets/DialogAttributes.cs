// 
// DialogAttributes.cs
//  
// Author:
//       Jonathan Pobst <monkey@jpobst.com>
// 
// Copyright (c) 2010 Jonathan Pobst
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Collections.Generic;

namespace Pinta.Gui.Widgets
{
	[AttributeUsage (AttributeTargets.Field | AttributeTargets.Property, Inherited = false)]
	public class SkipAttribute : Attribute
	{
	}

	[AttributeUsage (AttributeTargets.Field | AttributeTargets.Property, Inherited = false)]
	public class CaptionAttribute : Attribute
	{
		public CaptionAttribute (string caption)
		{
			Caption = caption;
		}

		public string Caption;
	}

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = false)]
    public class DigitsValueAttribute : Attribute
    {
        public DigitsValueAttribute(int value)
        {
            Value = value;
        }

        public int Value;
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = false)]
    public class IncrementValueAttribute : Attribute
    {
        public IncrementValueAttribute(double value)
        {
            Value = value;
        }

        public double Value;
    }

	[AttributeUsage (AttributeTargets.Field | AttributeTargets.Property, Inherited = false)]
	public class MinimumValueAttribute : Attribute
	{
		public MinimumValueAttribute (int value)
		{
			Value = value;
		}

		public int Value;
	}

	[AttributeUsage (AttributeTargets.Field | AttributeTargets.Property, Inherited = false)]
	public class MaximumValueAttribute : Attribute
	{
		public MaximumValueAttribute (int value)
		{
			Value = value;
		}

		public int Value;
	}
	
	[AttributeUsage (AttributeTargets.Field | AttributeTargets.Property, Inherited = false)]
	public class HintAttribute : Attribute
	{
		public HintAttribute (string caption)
		{
			Hint = caption;
		}

		public string Hint;
	}

	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = false)]
	public class StaticListAttribute : Attribute
	{
		public StaticListAttribute (string dict)
		{
			this.dictionaryName = dict;
		}

		public string dictionaryName;
	}
	
}
