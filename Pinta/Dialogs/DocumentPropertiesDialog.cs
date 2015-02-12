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

        public string author;
        public string title;
        public string subject;
        public string keywords;
        public string comments;

        private Entry authorEntry;
        private Entry titleEntry;
        private Entry subjectEntry;
        private Entry keywordsEntry;
        private TextView commentsTextView; 
        
        public DocumentPropertiesDialog () : base (Catalog.GetString ("Properties"), PintaCore.Chrome.MainWindow, DialogFlags.Modal, Stock.Cancel, ResponseType.Cancel, Stock.Ok, ResponseType.Ok)
        {
            Build ();

            this.Icon = PintaCore.Resources.GetIcon (Stock.Properties, 16);
            
            // suggest current user as author name
////            author = Environment.UserName;
            // suggest filename as document title
////            title = PintaCore.Workspace.ActiveDocument.Filename;
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
            set {
                authorEntry.Text = author;
                titleEntry.Text = title;
                subjectEntry.Text = subject;
                keywordsEntry.Text = keywords;
                commentsTextView.Buffer.Text = comments;
            }
        }
        
        private void OnAuthorChanged (object sender, EventArgs e)
        {
            author = authorEntry.Text;
        }

        private void OnTitleChanged (object sender, EventArgs e)
        {
            title = titleEntry.Text;
        }

        private void OnSubjectChanged (object sender, EventArgs e)
        {
            subject = subjectEntry.Text;
        }

        private void OnKeywordsChanged (object sender, EventArgs e)
        {
            keywords = keywordsEntry.Text;
        }

        private void OnCommentsChanged (object sender, EventArgs e)
        {
            comments = commentsTextView.Buffer.Text;
        }
        
        private void Build ()
        {
            DefaultWidth = 400;
            DefaultHeight = 300;
            BorderWidth = 6;

            VBox tBox = new VBox ();// HACK handwriting GUI code sucks
            tBox.Spacing = 10;

            Notebook notebook = new Notebook ();
            notebook.BorderWidth = 6;
            // General Page
////            notebook.AppendPage (new Button ("1"), new Label ("General"));
            // Description Page
            notebook.AppendPage (tBox, new Label (Catalog.GetString ("Description")));
////            notebook.AppendPage (new Button ("3"), new Label ("Custom"));            
////            notebook.AppendPage (new Button ("4"), new Label ("Advanced"));

            VBox.Spacing = 10;
            VBox.PackStart (notebook, true, true, 0);

            // handwriting any GUI code is ugly, GTK especially so
            Table table = new Table (5, 2, false);
            tBox.Add (table);
            uint xPad = 6;
            uint yPad = 6;
            
////    table1.Attach (Widget, leftAttach, rightAttach, topAttach, bottomAttach,
////    xOptions, yOptions, xPadding, yPadding );
            Label authorLabel = new Label (Mono.Unix.Catalog.GetString ("_Author:"));
            table.Attach(authorLabel, 0, 1, 0, 1, AttachOptions.Fill, 0, xPad, yPad);
            
            authorEntry = new Entry ();
            authorLabel.MnemonicWidget = authorEntry;
            table.Attach(authorEntry, 1, 2, 0, 1, AttachOptions.Fill, 0, xPad, yPad);

            Label titleLabel = new Label (Mono.Unix.Catalog.GetString ("_Title:"));
            table.Attach(titleLabel, 0, 1, 1, 2, AttachOptions.Fill, 0, xPad, yPad);

            titleEntry = new Entry ();
            titleLabel.MnemonicWidget = titleEntry;
            table.Attach(titleEntry, 1, 2, 1, 2, AttachOptions.Fill, 0, xPad, yPad);

            var box3 = new HBox ();

            Label subjectLabel = new Label (Mono.Unix.Catalog.GetString ("_Subject:"));
            table.Attach(subjectLabel, 0, 1, 2, 3, AttachOptions.Fill, 0, xPad, yPad);
            
            subjectEntry = new Entry ();
            subjectLabel.MnemonicWidget = subjectEntry;
            table.Attach(subjectEntry, 1, 2, 2, 3, AttachOptions.Fill, 0, xPad, yPad);

            Label keywordsLabel = new Label (Mono.Unix.Catalog.GetString ("_Keywords:"));
            table.Attach(keywordsLabel, 0, 1, 3, 4, AttachOptions.Fill, 0, xPad, yPad);
            
            keywordsEntry = new Entry ();
            keywordsLabel.MnemonicWidget = keywordsEntry;
            table.Attach(keywordsEntry, 1, 2, 3, 4, AttachOptions.Fill, 0, xPad, yPad);

            Label commentsLabel = new Label (Mono.Unix.Catalog.GetString ("Co_mments:"));
            table.Attach(commentsLabel, 0, 1, 4, 5, AttachOptions.Fill, 0, xPad, yPad);
            
            // TODO ideally this would be a scrolled box
            commentsTextView = new TextView ();
            commentsLabel.MnemonicWidget = commentsTextView;
            table.Attach(commentsTextView, 1, 2, 4, 5);

            table.Show ();
            // table.ShowAll ();
            tBox.ShowAll ();

            // Finish up
            VBox.ShowAll ();

            AlternativeButtonOrder = new int[] { (int)ResponseType.Ok, (int)ResponseType.Cancel };
            DefaultResponse = ResponseType.Ok;
        }
        
    }
}