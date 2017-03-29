using System;
using System.Windows.Forms;


namespace DSShared.Windows
{
	/// <summary>
	/// A generic form providing a textbox for user-input plus Ok and Cancel
	/// buttons.
	/// </summary>
	public sealed partial class InputBox
		:
			Form
	{
		/// <summary>
		/// Main constructor.
		/// </summary>
		/// <param name="notice">text that will be shown above the textbox</param>
		/// <param name="caption">caption for the titlebar</param>
		/// <param name="input">initial value of the textbox</param>
		public InputBox(string notice, string caption, string input)
		{
			InitializeComponent();
			
			Text = caption;
			lblNotice.Text = notice;

			tbInput.Text = input;
			tbInput.Select();
		}
		/// <summary>
		/// Auxiliary constructor.
		/// </summary>
		/// <param name="notice"></param>
		public InputBox(string notice)
			:
				this(notice, "Input", String.Empty)
		{}


		/// <summary>
		/// Gets the text in the textbox.
		/// </summary>
		public string InputString
		{
			get { return tbInput.Text; }
		}
	}
}
