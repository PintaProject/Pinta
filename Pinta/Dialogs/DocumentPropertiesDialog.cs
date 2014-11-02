// 
// DocumentPropertiesDialog.cs
//

using System;
using System.Collections.Generic;
using System.Linq;
using Gtk;
using Pinta.Core;

namespace Pinta
{
    public class DocumentPropertiesDialog : Dialog
    {
        private DocumentProperties initial_properties;

        private string author;
        private string title;
        private string subject;
        private string keywords;
        private string comments;

		private Entry authorEntry;
		private Entry titleEntry;
		private Entry subjectEntry;
		private Entry keywordsEntry;
		private TextView commentsTextView; // TODO: 3 lines instead of single line 
		// private TextBuffer buffer;
		
        public DocumentPropertiesDialog () : base (Mono.Unix.Catalog.GetString ("Properties"), PintaCore.Chrome.MainWindow, DialogFlags.Modal, Stock.Cancel, ResponseType.Cancel, Stock.Ok, ResponseType.Ok)
        {
            Build ();

            this.Icon = PintaCore.Resources.GetIcon (Stock.Properties, 16);
            
            // TODO suggest helpful default text but don't force it 
            // suggest current user as author name
////            author = Environment.UserName;
            // suggest filename as document title
////			title = PintaCore.Workspace.ActiveDocument.Filename;
            author = PintaCore.Workspace.ActiveDocument.Author; 
            title = PintaCore.Workspace.ActiveDocument.Title;
            subject = PintaCore.Workspace.ActiveDocument.Subject;
            keywords = PintaCore.Workspace.ActiveDocument.Keywords;
            comments = PintaCore.Workspace.ActiveDocument.Comments;
            
            initial_properties = new DocumentProperties(
                author,
                title,
                subject,
                keywords,
                comments);
            
            authorEntry.Text = initial_properties.Author;
            titleEntry.Text = initial_properties.Title;
            subjectEntry.Text = initial_properties.Subject;
            keywordsEntry.Text = initial_properties.Keywords;
            commentsTextView.Buffer.Text = initial_properties.Comments;
                        
            authorEntry.Changed += OnAuthorChanged;
            titleEntry.Changed += OnTitleChanged;
            subjectEntry.Changed += OnSubjectChanged;
            keywordsEntry.Changed += OnKeywordsChanged;
            commentsTextView.Buffer.Changed += OnCommentsChanged; 
            
            AlternativeButtonOrder = new int[] { (int) Gtk.ResponseType.Ok, (int) Gtk.ResponseType.Cancel };
            DefaultResponse = Gtk.ResponseType.Ok;
        }
        
        public bool AreDocumentPropertiesUpdated {
            get {
                return initial_properties.Author != author
                    || initial_properties.Title != title
                    || initial_properties.Subject != subject
                    || initial_properties.Keywords != keywords
                    || initial_properties.Comments != comments;
            }
        }

        public DocumentProperties InitialDocumentProperties {
            get {
                return initial_properties;
            }
        }
        
        public DocumentProperties UpdatedDocumentProperties {
            get {
                return new DocumentProperties (author, title, subject, keywords, comments);
            }
        }
        
        private void OnAuthorChanged (object sender, EventArgs e)
        {
            author = authorEntry.Text;
            PintaCore.Workspace.ActiveDocument.Author = author; 
        }

        private void OnTitleChanged (object sender, EventArgs e)
        {
            title = titleEntry.Text;
            PintaCore.Workspace.ActiveDocument.Title = title; 
        }

        private void OnSubjectChanged (object sender, EventArgs e)
        {
            subject = subjectEntry.Text;
            PintaCore.Workspace.ActiveDocument.Subject = subject; 
        }

        private void OnKeywordsChanged (object sender, EventArgs e)
        {
            keywords = keywordsEntry.Text;
            PintaCore.Workspace.ActiveDocument.Keywords = keywords; 
        }

        private void OnCommentsChanged (object sender, EventArgs e)
        {
            comments = commentsTextView.Buffer.Text;
            PintaCore.Workspace.ActiveDocument.Comments = comments; 
        }
        
        private void Build ()
        {
            DefaultWidth = 400;
            DefaultHeight = 300;
            BorderWidth = 6;
            VBox.Spacing = 10;
            
            var box1 = new HBox ();

            box1.PackStart (new Label (Mono.Unix.Catalog.GetString ("Author:")), false, false, 0);

            authorEntry = new Entry ();
            box1.PackStart (authorEntry);

            VBox.PackStart (box1, false, false, 0);

            var box2 = new HBox ();

            box2.PackStart (new Label (Mono.Unix.Catalog.GetString ("Title:")), false, false, 0);

            titleEntry = new Entry ();
            box2.PackStart (titleEntry);

            VBox.PackStart (box2, false, false, 0);

            var box3 = new HBox ();

            box3.PackStart (new Label (Mono.Unix.Catalog.GetString ("Subject:")), false, false, 0);

            subjectEntry = new Entry ();
            box3.PackStart (subjectEntry);

            VBox.PackStart (box3, false, false, 0);

            var box4 = new HBox ();

            box4.PackStart (new Label (Mono.Unix.Catalog.GetString ("Keywords:")), false, false, 0);

            keywordsEntry = new Entry ();
            box4.PackStart (keywordsEntry);

            VBox.PackStart (box4, false, false, 0);

            var box5 = new HBox ();

            box5.PackStart (new Label (Mono.Unix.Catalog.GetString ("Comments:")), false, false, 0);

            commentsTextView = new TextView ();
            box5.PackStart (commentsTextView);

            VBox.PackStart (box5, false, false, 0);

            // Finish up
            VBox.ShowAll ();

            AlternativeButtonOrder = new int[] { (int)ResponseType.Ok, (int)ResponseType.Cancel };
            DefaultResponse = ResponseType.Ok;
        }
    }
}