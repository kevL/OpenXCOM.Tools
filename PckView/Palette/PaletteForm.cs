using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

using XCom;


namespace PckView
{
	/// <summary>
	/// PaletteForm form.
	/// </summary>
	internal sealed class PaletteForm
		:
			Form
	{
//		internal event PaletteIndexChangedEventHandler PaletteIndexChangedEvent;


		#region Fields
		private PalettePanel _pnlPalette;
		#endregion


		#region Properties
		internal Palette Pal
		{
			set { _pnlPalette.Pal = value; }
		}
		#endregion


		#region cTor
		/// <summary>
		/// cTor.
		/// </summary>
		internal PaletteForm()
		{
			InitializeComponent();

			var size = new Size(
							PalettePanel.Across * 20,
							PalettePanel.Across * 20 + lblStatus.Height);
			ClientSize = size;
//			OnResize(EventArgs.Empty);
		}
		#endregion


		#region Eventcalls
		protected override void OnResize(EventArgs e)
		{
			LogFile.WriteLine("OnResize");
//			base.OnResize(e);

			if (_pnlPalette != null)
			{
				_pnlPalette.Width  = ClientSize.Width;
				_pnlPalette.Height = ClientSize.Height - lblStatus.Height;

				lblStatus.Location = new Point(
											_pnlPalette.Left,
											_pnlPalette.Bottom);

				lblStatus.Width = ClientSize.Width;
			}
		}

		private void OnPaletteIndexChanged(int id)
		{
			string text = String.Format(
									System.Globalization.CultureInfo.CurrentCulture,
									"id:{0} (0x{0:X2})",
									id);

			var color = _pnlPalette.Pal[id];
			text += String.Format(
								System.Globalization.CultureInfo.CurrentCulture,
								" r:{0} g:{1} b:{2} a:{3}",
								color.R,
								color.G,
								color.B,
								color.A);

			lblStatus.Text = text;

//			if (PaletteIndexChangedEvent != null)
//				PaletteIndexChangedEvent(id);
		}
		#endregion


		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose(bool disposing)
		{
			if (disposing && components != null)
				components.Dispose();

			base.Dispose(disposing);
		}

// This will get deleted from InitializeComponent() when any changes are
// made in the designer ... and trying to make it stick with default
// initialization doesn't work either. So copy it back in at the top of
// InitializeComponent() after making changes in the designer.
/*
			this._pnlPalette = new PalettePanel();
*/
// And this will probably get deleted also:
/*
			this._pnlPalette.PaletteIndexChangedEvent += OnPaletteIndexChanged;
*/

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this._pnlPalette = new PalettePanel();
			this.lblStatus = new System.Windows.Forms.Label();
			this.SuspendLayout();
			// 
			// _pnlPalette
			// 
			this._pnlPalette.Dock = System.Windows.Forms.DockStyle.Fill;
			this._pnlPalette.Location = new System.Drawing.Point(0, 0);
			this._pnlPalette.Name = "_pnlPalette";
			this._pnlPalette.Size = new System.Drawing.Size(292, 255);
			this._pnlPalette.TabIndex = 0;
			this._pnlPalette.PaletteIndexChangedEvent += OnPaletteIndexChanged;
			// 
			// lblStatus
			// 
			this.lblStatus.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			this.lblStatus.Dock = System.Windows.Forms.DockStyle.Bottom;
			this.lblStatus.Location = new System.Drawing.Point(0, 255);
			this.lblStatus.Name = "lblStatus";
			this.lblStatus.Padding = new System.Windows.Forms.Padding(3, 0, 0, 0);
			this.lblStatus.Size = new System.Drawing.Size(292, 19);
			this.lblStatus.TabIndex = 1;
			this.lblStatus.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// PaletteForm
			// 
			this.AutoScaleBaseSize = new System.Drawing.Size(5, 12);
			this.ClientSize = new System.Drawing.Size(292, 274);
			this.Controls.Add(this._pnlPalette);
			this.Controls.Add(this.lblStatus);
			this.Font = new System.Drawing.Font("Verdana", 7F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "PaletteForm";
			this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
			this.Text = "Palette";
			this.ResumeLayout(false);

		}
		#endregion

		private Container components = null;

		private Label lblStatus;
	}
}
