// 
// DocumentPropertiesAction.cs
//

using System;
using Gtk;
using Mono.Unix;
using Pinta.Core;

namespace Pinta.Actions
{
    class PropertiesAction : IActionHandler
    {
        #region IActionHandler Members
        public void Initialize ()
        {
            PintaCore.Actions.File.Properties.Activated += Activated;
        }

        public void Uninitialize ()
        {
            PintaCore.Actions.File.Properties.Activated -= Activated;
        }
        #endregion

        private void Activated (object sender, EventArgs e)
        {
            var dialog = new DocumentPropertiesDialog ();

            int response = dialog.Run ();

            if (response == (int)Gtk.ResponseType.Ok
                && dialog.AreDocumentPropertiesUpdated) {

            	PintaCore.Workspace.ActiveDocument.Author = dialog.author;
            	PintaCore.Workspace.ActiveDocument.Title = dialog.title;
            	PintaCore.Workspace.ActiveDocument.Subject = dialog.subject;
                PintaCore.Workspace.ActiveDocument.Keywords = dialog.keywords;
                PintaCore.Workspace.ActiveDocument.Comments = dialog.comments; 
                
                var historyMessage = GetDocumentPropertyUpdateMessage (
                    dialog.InitialDocumentProperties,
                    dialog.UpdatedDocumentProperties
                );

                var historyItem = new SimpleHistoryItem (
                    Stock.Properties,
                    historyMessage
                );
                
                PintaCore.Workspace.ActiveWorkspace.History.PushNewItem (historyItem);

                PintaCore.Workspace.ActiveWorkspace.Invalidate ();

            } else {
				// Cancel was pressed, reset dialog to initial properties
                var initial = dialog.InitialDocumentProperties;
                dialog.UpdatedDocumentProperties = initial;          
            }

            dialog.Destroy ();
        }

        private string GetDocumentPropertyUpdateMessage (DocumentProperties initial, DocumentProperties updated)
        {

            string ret = null;
            int count = 0;

            if (updated.Author != initial.Author) {
                ret = Catalog.GetString ("Document Author");
                count++;
            }

            if (updated.Title != initial.Title) {
                ret = Catalog.GetString ("Document Title");
                count++;
            }

            if (updated.Subject != initial.Subject) {
                ret = Catalog.GetString ("Document Subject");
                count++;
            }

            if (updated.Keywords != initial.Keywords) {
                ret = Catalog.GetString ("Document Keywords");
                count++;
            }

            if (updated.Comments != initial.Comments) {
                ret = Catalog.GetString ("Document Comments");
                count++;
            }

            if (ret == null || count > 1)
                ret = Catalog.GetString ("Document Properties");

            return ret;
        }
    }
}