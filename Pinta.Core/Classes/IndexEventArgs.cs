/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) Rick Brewster, Tom Jackson, and past contributors.            //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See license-pdn.txt for full licensing and attribution details.             //
/////////////////////////////////////////////////////////////////////////////////

using System;

namespace Pinta.Core
{
	public sealed class IndexEventArgs : EventArgs {
        
		public int Index { get; private set; }
		
        public IndexEventArgs(int i) {
            Index = i;
        }
    }
}

