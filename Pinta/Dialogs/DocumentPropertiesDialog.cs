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
		private Entry commentsEntry; // TODO: 3 lines instead of single line 
		
        public DocumentPropertiesDialog () : base (Mono.Unix.Catalog.GetString ("Properties"), PintaCore.Chrome.MainWindow, DialogFlags.Modal, Stock.Cancel, ResponseType.Cancel, Stock.Ok, ResponseType.Ok)
        {
            Build ();

            // TODO use gtk stock properties icon
            // this.Icon = PintaCore.Resources.GetIcon ("Menu.Layers.LayerProperties.png");
            
            author = "";
            title = "";
            subject = "";
            keywords = "";
            comments = "";
            
/*          name = PintaCore.Layers.CurrentLayer.Name;
            hidden = PintaCore.Layers.CurrentLayer.Hidden;
            opacity = PintaCore.Layers.CurrentLayer.Opacity;
            blendmode = PintaCore.Layers.CurrentLayer.BlendMode;
*/
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
            commentsEntry.Text = initial_properties.Comments;
            
/*           authorEntry.Changed += OnAuthorChanged;
            titleEntry.Changed += OnTitleChanged;
            subjectEntry.Changed += OnSubjectChanged;
            keywordsEntry.Changed += OnKeywordsChanged;
            commentsEntry.Changed += OnCommentsChanged;
*/
            
            AlternativeButtonOrder = new int[] { (int) Gtk.ResponseType.Ok, (int) Gtk.ResponseType.Cancel };
            DefaultResponse = Gtk.ResponseType.Ok;
        }
        
/*        public bool AreLayerPropertiesUpdated {
            get {
                return initial_properties.Opacity != opacity
                    || initial_properties.Hidden != hidden
                    || initial_properties.Locked != locked
                    || initial_properties.Name != name
                    || initial_properties.BlendMode != blendmode;
            }
        }

        public LayerProperties InitialLayerProperties {
            get {
                return initial_properties;
            }
        }
        
        public LayerProperties UpdatedLayerProperties {
            get {
                return new LayerProperties (name, hidden, locked, opacity, blendmode);
            }
        }
        
        private void OnLayerNameChanged (object sender, EventArgs e)
        {
            name = layerNameEntry.Text;
            PintaCore.Layers.CurrentLayer.Name = name;
        }   
*/
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

            commentsEntry = new Entry ();
            box5.PackStart (commentsEntry);

            VBox.PackStart (box5, false, false, 0);

            // Finish up
            VBox.ShowAll ();

            AlternativeButtonOrder = new int[] { (int)ResponseType.Ok, (int)ResponseType.Cancel };
            DefaultResponse = ResponseType.Ok;
        }
    }
}