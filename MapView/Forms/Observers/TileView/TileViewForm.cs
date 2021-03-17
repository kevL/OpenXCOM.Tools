﻿using System;
using System.Windows.Forms;

using MapView.Forms.MainView;


namespace MapView.Forms.Observers
{
	internal sealed partial class TileViewForm
		:
			Form,
			IObserverProvider
	{
		#region Fields
		private TileView _tile;
		#endregion Fields


		#region Properties
		/// <summary>
		/// Gets '_tile' as a child of <see cref="ObserverControl"/>.
		/// </summary>
		internal TileView Control
		{
			get { return _tile; }
		}

		/// <summary>
		/// Satisfies <see cref="IObserverProvider"/>.
		/// </summary>
		public ObserverControl Observer
		{
			get { return _tile; }
		}
		#endregion Properties


		#region cTor
		/// <summary>
		/// cTor.
		/// </summary>
		internal TileViewForm()
		{
			InitializeComponent();

			_tile = new TileView();

			_tile.Name     = "TileViewControl";
			_tile.Dock     = DockStyle.Fill;
			_tile.TabIndex = 0;

			Controls.Add(_tile);
		}
		#endregion cTor


		#region Events (override)
		/// <summary>
		/// Fires when the form is activated. Maintains the position of this
		/// form in the z-order List and focuses the selected panel.
		/// </summary>
		/// <param name="e"></param>
		protected override void OnActivated(EventArgs e)
		{
			ShowHideManager._zOrder.Remove(this);
			ShowHideManager._zOrder.Add(this);

			_tile.GetVisiblePanel().Focus();

//			base.OnActivated(e);
		}
		/// <summary>
		/// Handles a so-called command-key at the form level. Stops keys that
		/// shall be used for navigating the tiles from doing anything stupid
		/// instead.
		/// - passes the arrow-keys to the TileView control's current panel's
		///   Navigate() funct
		/// </summary>
		/// <param name="msg"></param>
		/// <param name="keyData"></param>
		/// <returns></returns>
		protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
		{
			TilePanel panel = _tile.GetVisiblePanel();
			if (panel.Focused)
			{
				switch (keyData)
				{
					case Keys.Left:
					case Keys.Right:
					case Keys.Up:
					case Keys.Down:
						panel.Navigate(keyData);
						return true;
				}
			}
			return base.ProcessCmdKey(ref msg, keyData);
		}

		/// <summary>
		/// Handles KeyDown events at the form level.
		/// - [Esc] focuses the current panel
		/// - opens/closes Options on [Ctrl+o] event
		/// - checks for and if so processes a viewer F-key
		/// - passes edit-keys to the TileView control's current panel's
		///   Navigate() funct
		/// @note Requires 'KeyPreview' true.
		/// @note See also TopViewForm, RouteViewForm, TopRouteViewForm
		/// </summary>
		/// <param name="e"></param>
		protected override void OnKeyDown(KeyEventArgs e)
		{
			switch (e.KeyData)
			{
				case Keys.Escape:
					e.SuppressKeyPress = true;
					_tile.GetVisiblePanel().Focus();
					break;

				case Keys.Control | Keys.O:
					e.SuppressKeyPress = true;
					_tile.OnOptionsClick(_tile.GetOptionsButton(), EventArgs.Empty);
					break;

				case Keys.Control | Keys.Q:
					e.SuppressKeyPress = true;
					MainViewF.that.OnQuitClick(null, EventArgs.Empty);
					break;

				case Keys.PageUp:
				case Keys.PageDown:
				case Keys.Home:
				case Keys.End:
				case Keys.Control | Keys.Home:
				case Keys.Control | Keys.End:
				{
					TilePanel panel = _tile.GetVisiblePanel();
					if (panel.Focused)
					{
						e.SuppressKeyPress = true;
						panel.Navigate(e.KeyData);
					}
					break;
				}

				default:
					MenuManager.ViewerKeyDown(e); // NOTE: this can suppress the key
					break;
			}
			base.OnKeyDown(e);
		}
		#endregion Events (override)
	}
}
