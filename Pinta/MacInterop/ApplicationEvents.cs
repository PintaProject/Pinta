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

namespace Pinta.MacInterop;

public static class ApplicationEvents
{
	static readonly object lock_obj = new ();

	#region Quit

	static EventHandler<ApplicationQuitEventArgs>? quit;
	// Create a delegate instance with static lifetime to avoid the GC destroying it.
	// The delegate can be invoked by native code at any point.
	private static readonly EventDelegate quit_delegate = HandleQuit;
	static IntPtr quit_handler_ref = IntPtr.Zero;

	public static event EventHandler<ApplicationQuitEventArgs> Quit {
		add {
			lock (lock_obj) {
				quit += value;
				if (quit_handler_ref == IntPtr.Zero)
					quit_handler_ref = Carbon.InstallApplicationEventHandler (quit_delegate, CarbonEventApple.QuitApplication);
			}
		}
		remove {
			lock (lock_obj) {
				quit -= value;
				if (quit == null && quit_handler_ref != IntPtr.Zero) {
					Carbon.RemoveEventHandler (quit_handler_ref);
					quit_handler_ref = IntPtr.Zero;
				}
			}
		}
	}

	static CarbonEventHandlerStatus HandleQuit (IntPtr callRef, IntPtr eventRef, IntPtr user_data)
	{
		var args = new ApplicationQuitEventArgs ();
		quit?.Invoke (null, args);
		return args.UserCancelled ? CarbonEventHandlerStatus.UserCancelled : args.HandledStatus;
	}

	#endregion

	#region Reopen

	static EventHandler<ApplicationEventArgs>? reopen;
	private static readonly EventDelegate reopen_delegate = HandleReopen;
	static IntPtr reopen_handler_ref = IntPtr.Zero;

	public static event EventHandler<ApplicationEventArgs> Reopen {
		add {
			lock (lock_obj) {
				reopen += value;
				if (reopen_handler_ref == IntPtr.Zero)
					reopen_handler_ref = Carbon.InstallApplicationEventHandler (reopen_delegate, CarbonEventApple.ReopenApplication);
			}
		}
		remove {
			lock (lock_obj) {
				reopen -= value;
				if (reopen == null && reopen_handler_ref != IntPtr.Zero) {
					Carbon.RemoveEventHandler (reopen_handler_ref);
					reopen_handler_ref = IntPtr.Zero;
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

	static EventHandler<ApplicationDocumentEventArgs>? open_documents;
	private static readonly EventDelegate open_delegate = HandleOpenDocuments;
	static IntPtr open_documents_handler_ref = IntPtr.Zero;

	public static event EventHandler<ApplicationDocumentEventArgs> OpenDocuments {
		add {
			lock (lock_obj) {
				open_documents += value;
				if (open_documents_handler_ref == IntPtr.Zero)
					open_documents_handler_ref = Carbon.InstallApplicationEventHandler (open_delegate, CarbonEventApple.OpenDocuments);
			}
		}
		remove {
			lock (lock_obj) {
				open_documents -= value;
				if (open_documents == null && open_documents_handler_ref != IntPtr.Zero) {
					Carbon.RemoveEventHandler (open_documents_handler_ref);
					open_documents_handler_ref = IntPtr.Zero;
				}
			}
		}
	}

	static CarbonEventHandlerStatus HandleOpenDocuments (IntPtr callRef, IntPtr eventRef, IntPtr user_data)
	{
		try {
			var docs = Carbon.GetFileListFromEventRef (eventRef);
			var args = new ApplicationDocumentEventArgs (docs);
			open_documents?.Invoke (null, args);
			return args.HandledStatus;
		} catch (Exception ex) {
			System.Console.WriteLine (ex);
			return CarbonEventHandlerStatus.NotHandled;
		}
	}

	#endregion

	#region OpenUrls

	static EventHandler<ApplicationUrlEventArgs>? open_urls;
	private static readonly EventDelegate open_urls_delegate = HandleOpenUrls;
	static IntPtr open_urls_handler_ref = IntPtr.Zero;

	public static event EventHandler<ApplicationUrlEventArgs> OpenUrls {
		add {
			lock (lock_obj) {
				open_urls += value;
				if (open_urls_handler_ref == IntPtr.Zero)
					open_urls_handler_ref = Carbon.InstallApplicationEventHandler (open_urls_delegate,
						[
							//For some reason GetUrl doesn't take CarbonEventClass.AppleEvent
							//need to use GURL, GURL
							new CarbonEventTypeSpec (CarbonEventClass.Internet, (int)CarbonEventApple.GetUrl)
						]
					);
			}
		}
		remove {
			lock (lock_obj) {
				open_urls -= value;
				if (open_urls == null && open_urls_handler_ref != IntPtr.Zero) {
					Carbon.RemoveEventHandler (open_urls_handler_ref);
					open_urls_handler_ref = IntPtr.Zero;
				}
			}
		}
	}

	static CarbonEventHandlerStatus HandleOpenUrls (IntPtr callRef, IntPtr eventRef, IntPtr user_data)
	{
		try {
			var urls = Carbon.GetUrlListFromEventRef (eventRef);
			var args = new ApplicationUrlEventArgs (urls);
			open_urls?.Invoke (null, args);
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

	internal CarbonEventHandlerStatus HandledStatus => Handled ? CarbonEventHandlerStatus.Handled : CarbonEventHandlerStatus.NotHandled;
}

public sealed class ApplicationQuitEventArgs : ApplicationEventArgs
{
	public bool UserCancelled { get; set; }
}

public sealed class ApplicationDocumentEventArgs : ApplicationEventArgs
{
	public ApplicationDocumentEventArgs (IReadOnlyDictionary<string, int> documents)
	{
		Documents = documents;
	}

	public IReadOnlyDictionary<string, int> Documents { get; }
}

public sealed class ApplicationUrlEventArgs : ApplicationEventArgs
{
	public ApplicationUrlEventArgs (IList<string> urls)
	{
		Urls = urls;
	}

	public IList<string> Urls { get; }
}

