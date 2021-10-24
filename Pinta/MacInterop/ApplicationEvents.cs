// 
// ApplicationEvents.cs
//  
// Author:
//       Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (c) 2010 Novell, Inc. (http://www.novell.com)
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

namespace Pinta.MacInterop
{
	public static class ApplicationEvents
	{
		static object lockObj = new object ();
		
		#region Quit
		
		static EventHandler<ApplicationQuitEventArgs>? quit;
		// Create a delegate instance with static lifetime to avoid the GC destroying it.
		// The delegate can be invoked by native code at any point.
		private static EventDelegate quit_delegate = HandleQuit;
		static IntPtr quitHandlerRef = IntPtr.Zero;
		
		public static event EventHandler<ApplicationQuitEventArgs> Quit {
			add {
				lock (lockObj) {
					quit += value;
					if (quitHandlerRef == IntPtr.Zero)
						quitHandlerRef = Carbon.InstallApplicationEventHandler (quit_delegate, CarbonEventApple.QuitApplication);
				}
			}
			remove {
				lock (lockObj) {
					quit -= value;
					if (quit == null && quitHandlerRef != IntPtr.Zero) {
						Carbon.RemoveEventHandler (quitHandlerRef);
						quitHandlerRef = IntPtr.Zero;
					}
				}
			}
		}
		
		static CarbonEventHandlerStatus HandleQuit (IntPtr callRef, IntPtr eventRef, IntPtr user_data)
		{
			var args = new ApplicationQuitEventArgs ();
			quit?.Invoke (null, args);
			return args.UserCancelled? CarbonEventHandlerStatus.UserCancelled : args.HandledStatus;
		}
		
		#endregion
		
		#region Reopen
		
		static EventHandler<ApplicationEventArgs>? reopen;
		private static EventDelegate reopen_delegate = HandleReopen;
		static IntPtr reopenHandlerRef = IntPtr.Zero;
		
		public static event EventHandler<ApplicationEventArgs> Reopen {
			add {
				lock (lockObj) {
					reopen += value;
					if (reopenHandlerRef == IntPtr.Zero)
						reopenHandlerRef = Carbon.InstallApplicationEventHandler (reopen_delegate, CarbonEventApple.ReopenApplication);
				}
			}
			remove {
				lock (lockObj) {
					reopen -= value;
					if (reopen == null && reopenHandlerRef != IntPtr.Zero) {
						Carbon.RemoveEventHandler (reopenHandlerRef);
						reopenHandlerRef = IntPtr.Zero;
					}
				}
			}
		}
		
		static CarbonEventHandlerStatus HandleReopen (IntPtr callRef, IntPtr eventRef, IntPtr user_data)
		{
			var args = new ApplicationEventArgs ();
			reopen?.Invoke (null, args);
			return args.HandledStatus;
		}
		
		#endregion
		
		#region OpenDocuments
		
		static EventHandler<ApplicationDocumentEventArgs>? openDocuments;
		private static EventDelegate open_delegate = HandleOpenDocuments;
		static IntPtr openDocumentsHandlerRef = IntPtr.Zero;
		
		public static event EventHandler<ApplicationDocumentEventArgs> OpenDocuments {
			add {
				lock (lockObj) {
					openDocuments += value;
					if (openDocumentsHandlerRef == IntPtr.Zero)
						openDocumentsHandlerRef = Carbon.InstallApplicationEventHandler (open_delegate, CarbonEventApple.OpenDocuments);
				}
			}
			remove {
				lock (lockObj) {
					openDocuments -= value;
					if (openDocuments == null && openDocumentsHandlerRef != IntPtr.Zero) {
						Carbon.RemoveEventHandler (openDocumentsHandlerRef);
						openDocumentsHandlerRef = IntPtr.Zero;
					}
				}
			}
		}
		
		static CarbonEventHandlerStatus HandleOpenDocuments (IntPtr callRef, IntPtr eventRef, IntPtr user_data)
		{
			try {
				var docs = Carbon.GetFileListFromEventRef (eventRef);
				var args = new ApplicationDocumentEventArgs (docs);
				openDocuments?.Invoke (null, args);
				return args.HandledStatus;
			} catch (Exception ex) {
				System.Console.WriteLine (ex);
				return CarbonEventHandlerStatus.NotHandled;
			}
		}
		
		#endregion
		
		#region OpenUrls
		
		static EventHandler<ApplicationUrlEventArgs>? openUrls;
		private static EventDelegate open_urls_delegate = HandleOpenUrls;
		static IntPtr openUrlsHandlerRef = IntPtr.Zero;
		
		public static event EventHandler<ApplicationUrlEventArgs> OpenUrls {
			add {
				lock (lockObj) {
					openUrls += value;
					if (openUrlsHandlerRef == IntPtr.Zero)
						openUrlsHandlerRef = Carbon.InstallApplicationEventHandler (open_urls_delegate,
							new CarbonEventTypeSpec[] {
								//For some reason GetUrl doesn't take CarbonEventClass.AppleEvent
								//need to use GURL, GURL
								new CarbonEventTypeSpec (CarbonEventClass.Internet, (int)CarbonEventApple.GetUrl)
							}
						);
				}
			}
			remove {
				lock (lockObj) {
					openUrls -= value;
					if (openUrls == null && openUrlsHandlerRef != IntPtr.Zero) {
						Carbon.RemoveEventHandler (openUrlsHandlerRef);
						openUrlsHandlerRef = IntPtr.Zero;
					}
				}
			}
		}
		
		static CarbonEventHandlerStatus HandleOpenUrls (IntPtr callRef, IntPtr eventRef, IntPtr user_data)
		{
			try {
				var urls = Carbon.GetUrlListFromEventRef (eventRef);
				var args = new ApplicationUrlEventArgs (urls);
				openUrls?.Invoke (null, args);
				return args.HandledStatus;
			} catch (Exception ex) {
				System.Console.WriteLine (ex);
				return CarbonEventHandlerStatus.NotHandled;
			}
		}
		
		#endregion
	}
	
	public class ApplicationEventArgs : EventArgs
	{
		public bool Handled { get; set; }
		
		internal CarbonEventHandlerStatus HandledStatus {
			get {
				return Handled? CarbonEventHandlerStatus.Handled : CarbonEventHandlerStatus.NotHandled;
			}
		}
	}
	
	public class ApplicationQuitEventArgs : ApplicationEventArgs
	{
		public bool UserCancelled { get; set; }
	}
	
	public class ApplicationDocumentEventArgs : ApplicationEventArgs
	{
		public ApplicationDocumentEventArgs (IDictionary<string,int> documents)
		{
			this.Documents = documents;
		}		
		
		public IDictionary<string,int> Documents { get; private set; }
	}
	
	public class ApplicationUrlEventArgs : ApplicationEventArgs
	{
		public ApplicationUrlEventArgs (IList<string> urls)
		{
			this.Urls = urls;
		}		
		
		public IList<string> Urls { get; private set; }
	}
}

