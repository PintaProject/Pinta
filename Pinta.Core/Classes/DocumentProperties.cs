
using System;

namespace Pinta.Core
{

	public class DocumentProperties
	{
		
		public DocumentProperties (string author, string title, string subject, string keywords, string comments)
		{
			this.Author = author;			
			this.Title = title;
			this.Subject = subject;
			this.Keywords = keywords;
			this.Comments = comments;
		}
				
		public string Author { get; private set; }				
		public string Title { get; private set; }				
		public string Subject { get; private set; }				
		public string Keywords { get; private set; }				
		public string Comments { get; private set; }				

		public void SetProperties (Document document)
		{
			document.Author = Author;
			document.Title = Title;
			document.Subject = Subject;
			document.Keywords = Keywords;
			document.Comments = Comments;
		}
	}
}
