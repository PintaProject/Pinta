using System;
using System.Collections.Generic;
using System.Linq;
using Gtk;

namespace Pinta.Core
{
	public static class DialogExtensions
	{
		public static ResponseType ShowErrorDialog (string markup)
			=> ShowMessageDialog (markup, MessageType.Error, ButtonsType.Ok);

		public static ResponseType ShowMessageDialog (string markup)
			=> ShowMessageDialog (markup, MessageType.Info, ButtonsType.Ok);

		public static ResponseType ShowQuestionDialog (string markup, params DialogButton[] buttons)
		{
			using (var md = CreateMessageDialog (markup, MessageType.Question, ButtonsType.None)) {
				foreach (var button in SortButtonsForOS (buttons)) {
					md.AddButton (button.Text, button.Response);

					if (button.IsDefault)
						md.DefaultResponse = button.Response;
				}

				return (ResponseType) md.Run ();
			}
		}

		private static ResponseType ShowMessageDialog (string markup, MessageType type, ButtonsType buttons, ResponseType? defaultResponse = null)
		{
			using (var md = CreateMessageDialog (markup, type, buttons, defaultResponse))
				return (ResponseType) md.Run ();
		}

		private static MessageDialog CreateMessageDialog (string markup, MessageType type, ButtonsType buttons, ResponseType? defaultResponse = null)
		{
			var md = new MessageDialog (PintaCore.Chrome.MainWindow, DialogFlags.Modal, type, buttons, true, markup);

			if (defaultResponse.HasValue)
				md.DefaultResponse = defaultResponse.Value;

			return md;
		}

		private static IEnumerable<DialogButton> SortButtonsForOS (DialogButton[] buttons)
		{
			var affirmative = new[] { ResponseType.Accept, ResponseType.Apply, ResponseType.Ok, ResponseType.Yes };
			var negative = new[] { ResponseType.No, ResponseType.Reject };
			var cancel = new[] { ResponseType.Cancel };

			if (PintaCore.System.OperatingSystem == OS.Windows) {
				// The button order on Windows is: Affirmative, Negative, Cancel
				return buttons.Where (b => b.Response.In (affirmative))
					      .Concat (buttons.Where (b => b.Response.In (negative))
					      .Concat (buttons.Where (b => b.Response.In (cancel))));
			}

			// The button order on other OS's is: Negative, Cancel, Affirmative
			return buttons.Where (b => b.Response.In (negative))
				      .Concat (buttons.Where (b => b.Response.In (cancel))
				      .Concat (buttons.Where (b => b.Response.In (affirmative))));
		}
	}

	public class DialogButton
	{
		public string Text { get; }
		public ResponseType Response { get; }
		public bool IsDefault { get; }

		public DialogButton (string text, ResponseType response, bool isDefault = false)
		{
			Text = text;
			Response = response;
			IsDefault = isDefault;
		}
	}
}
