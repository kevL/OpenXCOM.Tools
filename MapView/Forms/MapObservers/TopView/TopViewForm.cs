﻿using System;
using System.Windows.Forms;

using MapView.Forms.MainWindow;

using XCom;


namespace MapView.Forms.MapObservers.TopViews
{
	internal sealed partial class TopViewForm
		:
			Form,
			IMapObserverProvider
	{
		#region Properties
		/// <summary>
		/// Gets 'TopViewControl' as a child of 'MapObserverControl'.
		/// </summary>
		internal TopView Control
		{
			get { return TopViewControl; }
		}

		/// <summary>
		/// Satisfies 'IMapObserverProvider'.
		/// </summary>
		public MapObserverControl ObserverControl
		{
			get { return TopViewControl; }
		}
		#endregion Properties


		#region cTor
		internal TopViewForm()
		{
			InitializeComponent();
		}
		#endregion cTor


		#region Events (override)
		/// <summary>
		/// Fires when the form is activated. Maintains the position of this
		/// form in the z-order List and focuses the panel.
		/// </summary>
		/// <param name="e"></param>
		protected override void OnActivated(EventArgs e)
		{
			ShowHideManager._zOrder.Remove(this);
			ShowHideManager._zOrder.Add(this);

			TopViewControl.TopPanel.Focus();

//			base.OnActivated(e);
		}

		/// <summary>
		/// Handles a so-called command-key at the form level. Stops keys that
		/// shall be used for navigating the tiles from doing anything stupid
		/// instead.
		/// - passes the arrow-keys to the TopView control's panel's Navigate()
		///   funct
		/// </summary>
		/// <param name="msg"></param>
		/// <param name="keyData"></param>
		/// <returns></returns>
		protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
		{
			if (Control.TopPanel.Focused)
			{
				switch (keyData)
				{
					case Keys.Left:
					case Keys.Right:
					case Keys.Up:
					case Keys.Down:
					case Keys.Shift | Keys.Left:
					case Keys.Shift | Keys.Right:
					case Keys.Shift | Keys.Up:
					case Keys.Shift | Keys.Down:
						MainViewOverlay.that.Navigate(keyData, true);
						return true;
				}
			}
			return base.ProcessCmdKey(ref msg, keyData);
		}

		/// <summary>
		/// Handles KeyDown events at the form level.
		/// - [Esc] focuses the panel else clears the current selection lozenge
		/// - opens/closes Options on [Ctrl+o] event
		/// - checks for and if so processes a viewer F-key
		/// - passes edit-keys to the TopView control's panel's Navigate()
		///   funct
		/// - selects a quadrant
		/// @note Requires 'KeyPreview' true.
		/// @note See also TileViewForm, RouteViewForm, TopRouteViewForm
		/// @note Edit/Save keys are handled by 'TopPanelParent.OnKeyDown()'.
		/// </summary>
		/// <param name="e"></param>
		protected override void OnKeyDown(KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Escape)
			{
				if (!Control.TopPanel.Focused)
				{
					e.SuppressKeyPress = true;
					Control.TopPanel.Focus();
				}
				else
					MainViewOverlay.that.Edit(e);
			}
			else if (e.KeyCode == Keys.O
				&& (e.Modifiers & Keys.Control) == Keys.Control)
			{
				e.SuppressKeyPress = true;
				Control.OnOptionsClick(Control.GetOptionsButton(), EventArgs.Empty);
			}
			else if (!MainMenusManager.ViewerKeyDown(e)) // NOTE: this can suppress the key
			{
				QuadrantType quadType = QuadrantType.None;
				switch (e.KeyCode)
				{
					case Keys.D1: quadType = QuadrantType.Floor;   break;
					case Keys.D2: quadType = QuadrantType.West;    break;
					case Keys.D3: quadType = QuadrantType.North;   break;
					case Keys.D4: quadType = QuadrantType.Content; break;
				}

				if (quadType != QuadrantType.None)
				{
					e.SuppressKeyPress = true;
					var args = new MouseEventArgs(MouseButtons.Left, 1, 0,0, 0);
					Control.QuadrantPanel.ForceMouseDown(args, quadType);
				}
				else if (Control.TopPanel.Focused)
				{
					switch (e.KeyCode)
					{
						case Keys.Add:
						case Keys.Subtract:
						case Keys.PageDown:
						case Keys.PageUp:
						case Keys.Home:
						case Keys.End:
							e.SuppressKeyPress = true;
							MainViewOverlay.that.Navigate(e.KeyData, true);
							break;
					}
				}
			}
//			base.OnKeyDown(e);
		}
		#endregion Events (override)
	}
}
