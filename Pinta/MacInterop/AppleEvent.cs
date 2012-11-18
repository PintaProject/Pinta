// 
// AppleEvent.cs
//  
// Author:
//       Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (c) 2010 Novell, Inc.
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
using System.Runtime.InteropServices;
using System.Collections.Generic;

namespace Pinta.MacInterop
{
	internal static class AppleEvent
	{
		const string AELib = Carbon.CarbonLib;
		
		//FIXME: is "int" correct for size?
		[DllImport (AELib)]
		static extern AEDescStatus AECreateDesc (OSType typeCode, IntPtr dataPtr, int dataSize, out AEDesc desc);
		
		[DllImport (AELib)]
		static extern AEDescStatus AECreateDesc (OSType typeCode, byte[] data, int dataSize, out AEDesc desc);
				
		[DllImport (AELib)]
		static extern AEDescStatus AEGetNthPtr (ref AEDesc descList, int index, OSType desiredType, uint keyword,
		                                        out CarbonEventParameterType actualType, IntPtr buffer, int bufferSize, out int actualSize);
		
		[DllImport (AELib)]
		static extern AEDescStatus AEGetNthPtr (ref AEDesc descList, int index, OSType desiredType, uint keyword,
		                                        uint zero, IntPtr buffer, int bufferSize, int zero2);
		
		[DllImport (AELib)]
		static extern AEDescStatus AECountItems (ref AEDesc descList, out int count); //return an OSErr
		
		[DllImport (AELib)]
		static extern AEDescStatus AEGetNthPtr (ref AEDesc descList, int index, OSType desiredType, uint keyword,
		                                        uint zero, out IntPtr outPtr, int bufferSize, int zero2);
		
		[DllImport (AELib)]
		public static extern AEDescStatus AEDisposeDesc (ref AEDesc desc);
		
		[DllImport (AELib)]
		public static extern AEDescStatus AESizeOfNthItem  (ref AEDesc descList, int index, ref OSType type, out int size);
		
		[DllImport (AELib)]
		static extern AEDescStatus AEGetDescData (ref AEDesc desc, IntPtr ptr, int maximumSize);
		
		[DllImport (AELib)]
		static extern int AEGetDescDataSize (ref AEDesc desc);
		
		[DllImport (AELib)]
		static extern AEDescStatus AECoerceDesc (ref AEDesc theAEDesc, DescType toType, ref AEDesc result);
		
		public static void AECreateDesc (OSType typeCode, byte[] data, out AEDesc result)
		{
			CheckReturn (AECreateDesc (typeCode, data, data.Length, out result));
		}
		
		public static void AECreateDescUtf8 (string value, out AEDesc result)
		{
			var type = (OSType)(int)CarbonEventParameterType.UnicodeText;
			var bytes = System.Text.Encoding.UTF8.GetBytes (value);
			CheckReturn (AECreateDesc (type, bytes, bytes.Length, out result));
		}
		
		public static void AECreateDescAscii (string value, out AEDesc result)
		{
			var type = (OSType)(int)CarbonEventParameterType.Char;
			var bytes = System.Text.Encoding.ASCII.GetBytes (value);
			CheckReturn (AECreateDesc (type, bytes, bytes.Length, out result));
		}
		
		public static void AECreateDescNull (out AEDesc desc)
		{
			CheckReturn (AECreateDesc ((OSType)0, IntPtr.Zero, 0, out desc));
		}
		
		public static int AECountItems (ref AEDesc descList)
		{
			int count;
			CheckReturn (AECountItems (ref descList, out count));
			return count;
		}

		public static T AEGetNthPtr<T> (ref AEDesc descList, int index, OSType desiredType) where T : struct
		{
			int len = Marshal.SizeOf (typeof (T));
			IntPtr bufferPtr = Marshal.AllocHGlobal (len);
			try {
				CheckReturn (AEGetNthPtr (ref descList, index, desiredType, 0, 0, bufferPtr, len, 0));
				T val = (T)Marshal.PtrToStructure (bufferPtr, typeof (T));
				return val;
			} finally{ 
				Marshal.FreeHGlobal (bufferPtr);
			}
		}
		
		public static IntPtr AEGetNthPtr (ref AEDesc descList, int index, OSType desiredType)
		{
			IntPtr ret;
			CheckReturn (AEGetNthPtr (ref descList, index, desiredType, 0, 0, out ret, 4, 0));
			return ret;
		}
		
		//FIXME: this might not work in some encodings. need to test more.
		static string GetUtf8StringFromAEPtr (ref AEDesc descList, int index)
		{
			int size;
			var type = (OSType)(int)CarbonEventParameterType.UnicodeText;
			if (AESizeOfNthItem (ref descList, index, ref type, out size) == AEDescStatus.Ok) {
				IntPtr buffer = Marshal.AllocHGlobal (size);
				try {
					if (AEGetNthPtr (ref descList, index, type, 0, 0, buffer, size, 0) == AEDescStatus.Ok)
						return Marshal.PtrToStringAuto (buffer, size);
				} finally {
					Marshal.FreeHGlobal (buffer);
				}
			}
			return null;
		}
		
		public static string GetStringFromAEDesc (ref AEDesc desc)
		{
			int size = AEGetDescDataSize (ref desc);
			if (size > 0) {
				IntPtr buffer = Marshal.AllocHGlobal (size);
				try {
					if (AEGetDescData (ref desc, buffer, size) == AEDescStatus.Ok)
						return Marshal.PtrToStringAuto (buffer, size);
				} finally {
					Marshal.FreeHGlobal (buffer);
				}
			}
			return null;
		}
		
		public static IList<string> GetUtf8StringListFromAEDesc (ref AEDesc list, bool skipEmpty)
		{
			long count = AppleEvent.AECountItems (ref list);
			var items = new List<string> ();
			for (int i = 1; i <= count; i++) {
				string str = AppleEvent.GetUtf8StringFromAEPtr (ref list, i);
				if (!string.IsNullOrEmpty (str))
					items.Add (str);
			}
			return items;
		}
		
		public static T[] GetListFromAEDesc<T,TRef> (ref AEDesc list, AEDescValueSelector<TRef,T> sel, OSType type)
			where TRef : struct
		{
			long count = AppleEvent.AECountItems (ref list);
			T[] arr = new T[count];
			for (int i = 1; i <= count; i++) {
				TRef r = AppleEvent.AEGetNthPtr<TRef> (ref list, i, type);
				arr [i - 1] = sel (ref r);
			}
			return arr;
		}
		
		static void CheckReturn (AEDescStatus status)
		{
			if (status != AEDescStatus.Ok)
			throw new Exception ("Failed with code " + status.ToString ());
		}
	}
	
	public delegate T AEDescValueSelector<TRef,T> (ref TRef desc);
	
	[StructLayout(LayoutKind.Sequential, Pack = 2)]
	public struct AEDesc
	{
		public uint descriptorType;
		public IntPtr dataHandle;
	}
	
	public enum AEDescStatus
	{
		Ok = 0,
		MemoryFull = -108,
		CoercionFail = -1700,
		DescRecordNotFound = -1701,
		WrongDataType = -1703,
		NotAEDesc = -1704,
		ReplyNotArrived = -1718,
	}
	
	public enum AESendMode {
		NoReply = 0x00000001,
		QueueReply = 0x00000002,
		WaitReply = 0x00000003,
		DontReconnect = 0x00000080,
		WantReceipt = 0x00000200,
		NeverInteract = 0x00000010,
		CanInteract = 0x00000020,
		AlwaysInteract = 0x00000030,
		CanSwitchLayer = 0x00000040,
		DontRecord = 0x00001000,
		DontExecute = 0x00002000,
		ProcessNonReplyEvents = 0x00008000,
	}

	struct DescType
	{
		public OSType Value;
	}
}