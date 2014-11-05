// 
// DocumentPropertiesDialog.cs
//

using System;
using System.Collections.Generic;
using System.Linq;
using Gtk;
using Mono.Unix;
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
		
        public DocumentPropertiesDialog () : base (Catalog.GetString ("Properties"), PintaCore.Chrome.MainWindow, DialogFlags.Modal, Stock.Cancel, ResponseType.Cancel, Stock.Ok, ResponseType.Ok)
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

            VBox nBox = new VBox ();// HACK handwriting GUI code sucks
            nBox.Spacing = 10;
            
            Notebook notebook = new Notebook ();
            notebook.BorderWidth = 6;
            // General Page
//            notebook.AppendPage (new Button ("1"), new Label ("General"));
            // Description Page
            notebook.AppendPage (nBox, new Label (Catalog.GetString ("Description")));
//            notebook.AppendPage (new Button ("3"), new Label ("Custom"));            
                                 
            VBox.Spacing = 10;
			VBox.PackStart (notebook, true, true, 0);

			// TODO: probably want to rewrite this entirely using Gtk Table
            // so that text entries are all the same width
            var box1 = new HBox ();
            
            Label authorLabel = new Label (Catalog.GetString ("_Author:"));
            box1.PackStart (authorLabel, false, false, 0);

            authorEntry = new Entry ();
            authorLabel.MnemonicWidget = authorEntry;
            box1.PackStart (authorEntry);

            nBox.PackStart (box1, false, false, 0);

            var box2 = new HBox ();

            Label titleLabel = new Label (Catalog.GetString ("_Title:"));
            box2.PackStart (titleLabel, false, false, 0);

            titleEntry = new Entry ();
            titleLabel.MnemonicWidget = titleEntry;
            box2.PackStart (titleEntry);

            nBox.PackStart (box2, false, false, 0);

            var box3 = new HBox ();

            Label subjectLabel = new Label (Catalog.GetString ("_Subject:"));
            box3.PackStart (subjectLabel, false, false, 0);

            subjectEntry = new Entry ();
            subjectLabel.MnemonicWidget = subjectEntry;
            box3.PackStart (subjectEntry);

            nBox.PackStart (box3, false, false, 0);

            var box4 = new HBox ();

            Label keywordsLabel = new Label (Catalog.GetString ("_Keywords:"));
            box4.PackStart (keywordsLabel, false, false, 0);

            keywordsEntry = new Entry ();
            keywordsLabel.MnemonicWidget = keywordsEntry;
            box4.PackStart (keywordsEntry);

            nBox.PackStart (box4, false, false, 0);

            var box5 = new HBox ();

            Label commentsLabel = new Label (Catalog.GetString ("Co_mments:")); 
            box5.PackStart (commentsLabel, false, false, 0);

            commentsTextView = new TextView ();
            commentsLabel.MnemonicWidget = commentsTextView;
            box5.PackStart (commentsTextView);

            nBox.PackStart (box5, false, false, 0);
            nBox.ShowAll ();

            // Finish up
            VBox.ShowAll ();

            AlternativeButtonOrder = new int[] { (int)ResponseType.Ok, (int)ResponseType.Cancel };
            DefaultResponse = ResponseType.Ok;
        }
    }
}