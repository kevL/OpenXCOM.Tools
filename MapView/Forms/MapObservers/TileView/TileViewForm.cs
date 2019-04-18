﻿using System;
using System.Windows.Forms;

using MapView.Forms.MainWindow;


namespace MapView.Forms.MapObservers.TileViews
{
	internal sealed class TileViewForm
		:
			Form,
			IMapObserverProvider
	{
		#region Properties
		/// <summary>
		/// Gets TileView as a child of MapObserverControl0.
		/// </summary>
		internal TileView Control
		{
			get { return TileViewControl; }
		}

		/// <summary>
		/// Satisfies IMapObserverProvider.
		/// </summary>
		public MapObserverControl0 ObserverControl0
		{
			get { return TileViewControl; }
		}
		#endregion


		#region cTor
		internal TileViewForm()
		{
			InitializeComponent();

			Activated += OnActivated;
		}
		#endregion


		#region Eventcalls
		/// <summary>
		/// Fires when the form is activated.
		/// </summary>
		private void OnActivated(object sender, EventArgs e)
		{
			TileViewControl.GetSelectedPanel().Focus();
		}
		#endregion


		#region Events (override)
		/// <summary>
		/// Handles KeyDown events at the form level.
		/// - opens/closes Options on [Ctrl+o] event.
		/// @note Requires 'KeyPreview' true.
		/// @note See also TopViewForm, RouteViewForm, TopRouteViewForm
		/// </summary>
		/// <param name="e"></param>
		protected override void OnKeyDown(KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Escape)
			{
				Control.GetSelectedPanel().Focus();
				e.SuppressKeyPress = true;
			}
			else if (e.KeyCode == Keys.O
				&& (e.Modifiers & Keys.Control) == Keys.Control)
			{
				Control.OnOptionsClick(Control.GetOptionsButton(), EventArgs.Empty);
				e.SuppressKeyPress = true;
			}
			else if (!MainMenusManager.ViewerKeyDown(e))
			{
				switch (e.KeyCode)
				{
					case  Keys.Home:
					case  Keys.End:
					case  Keys.PageUp:
					case  Keys.PageDown:
						e.SuppressKeyPress = true;
						Control.GetSelectedPanel().Navigate(e.KeyData);
						break;
				}
			}

			base.OnKeyDown(e);
		}

		/// <summary>
		/// Cycles through controls when the tab-key is pressed.
		/// </summary>
		/// <param name="msg"></param>
		/// <param name="keyData"></param>
		/// <returns></returns>
		protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
		{
			switch (keyData)
			{
				case Keys.Tab:
					if (Control.GetToolStrip().Focused)
					{
						Control.GetTabControl().Focus();
					}
					else if (Control.GetTabControl().Focused)
					{
						Control.GetSelectedPanel().Focus();
					}
					else
						Control.GetToolStrip().Focus();

					return true;

				case Keys.Left:
				case Keys.Right:
				case Keys.Up:
				case Keys.Down:
					if (Control.GetSelectedPanel().Focused)
					{
						Control.GetSelectedPanel().Navigate(keyData);
						return true;
					}
					break;
			}
			return base.ProcessCmdKey(ref msg, keyData);
		}


		/// <summary>
		/// Handles form closing event.
		/// </summary>
		/// <param name="e"></param>
		protected override void OnFormClosing(FormClosingEventArgs e)
		{
			WindowState = FormWindowState.Normal; // else causes probls when opening a viewer that was closed while maximized.
			base.OnFormClosing(e);
		}
		#endregion Events (override)


		/// <summary>
		/// Cleans up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && components != null)
				components.Dispose();

			base.Dispose(disposing);
		}

		/* The #develop designer is going to delete this:

			TileViewControl = new MapView.Forms.MapObservers.TileViews.TileView();

		- so copy it back into InitializeComponent() */

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			TileViewControl = new MapView.Forms.MapObservers.TileViews.TileView();
			this.SuspendLayout();
			// 
			// TileViewControl
			// 
			this.TileViewControl.Dock = System.Windows.Forms.DockStyle.Fill;
			this.TileViewControl.Font = new System.Drawing.Font("Verdana", 7F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.TileViewControl.Location = new System.Drawing.Point(0, 0);
			this.TileViewControl.Name = "TileViewControl";
			this.TileViewControl.Size = new System.Drawing.Size(632, 454);
			this.TileViewControl.TabIndex = 0;
			// 
			// TileViewForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(632, 454);
			this.Controls.Add(this.TileViewControl);
			this.Font = new System.Drawing.Font("Verdana", 7F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.KeyPreview = true;
			this.Name = "TileViewForm";
			this.ShowInTaskbar = false;
			this.Text = "TileView";
			this.ResumeLayout(false);

		}
		#endregion

		private System.ComponentModel.IContainer components = null;

		private TileView TileViewControl;
	}
}
