﻿using System;
using System.Windows.Forms;

using DSShared.Windows;

using MapView.Forms.MainWindow;
using MapView.Forms.MapObservers.RouteViews;
using MapView.Forms.MapObservers.TopViews;

using XCom;


namespace MapView.Forms.MapObservers.TileViews // y, "TileView" thanks for knifing the concept of namespaces in the butt.
{
	internal sealed partial class TopRouteViewForm
		:
			Form
	{
		internal TopRouteViewForm()
		{
			InitializeComponent();

			var regInfo = new RegistryInfo(RegistryInfo.TopRouteView, this); // subscribe to Load and Closing events.
			regInfo.RegisterProperties();
		}


		internal TopView ControlTop
		{
			get { return TopViewControl; }
		}

		internal RouteView ControlRoute
		{
			get { return RouteViewControl; }
		}


		#region Events (override)
		/// <summary>
		/// Handles KeyDown events at the form level.
		/// - [Esc] focuses the appropriate panel
		/// - opens/closes Options on [Ctrl+o] event
		/// - checks for and if so processes a viewer F-key
		/// - passes edit-keys to the appropriate viewer's control's panel's
		///   Navigate() funct
		/// - selects a quadrant if TopView is the current tabpage
		/// @note Requires 'KeyPreview' true.
		/// @note See also TileViewForm, TopViewForm, RouteViewForm
		/// </summary>
		/// <param name="e"></param>
		protected override void OnKeyDown(KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Escape)
			{
				e.SuppressKeyPress = true;
				switch (tabControl.SelectedIndex)
				{
					case 0: ControlTop  .TopPanel  .Focus(); break;
					case 1: ControlRoute.RoutePanel.Focus(); break;
				}
			}
			else if (e.KeyCode == Keys.O
				&& (e.Modifiers & Keys.Control) == Keys.Control)
			{
				e.SuppressKeyPress = true;
				switch (tabControl.SelectedIndex)
				{
					case 0: ControlTop  .OnOptionsClick(ControlTop  .GetOptionsButton(), EventArgs.Empty); break;
					case 1: ControlRoute.OnOptionsClick(ControlRoute.GetOptionsButton(), EventArgs.Empty); break;
				}
			}
			else if (!MainMenusManager.ViewerKeyDown(e)) // NOTE: this can suppress the key
			{
				if (tabControl.SelectedIndex == 0) // Top
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
						ControlTop.QuadrantsPanel.ForceMouseDown(args, quadType);
					}
					else if (ControlTop.TopPanel.Focused)
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
								MainViewUnderlay.Instance.MainViewOverlay.Navigate(e.KeyData);
								break;
						}
					}
				}
				else if (ControlRoute.RoutePanel.Focused) // Route
				{
					switch (e.KeyCode)
					{
						case Keys.Add:
						case Keys.Subtract:
						case Keys.PageDown:
						case Keys.PageUp:
						case Keys.Home:
						case Keys.End:
						case Keys.Enter:
							e.SuppressKeyPress = true;
							ControlRoute.RoutePanel.Navigate(e.KeyData);
							break;
					}
				}
			}

			if (tabControl.SelectedIndex == 1) // Route
				base.OnKeyDown(e);
		}

		/// <summary>
		/// Handles a so-called command-key at the form level. Stops keys that
		/// shall be used for navigating the tiles from doing anything stupid
		/// instead.
		/// - passes the arrow-keys to the appropriate control's panel's
		///   Navigate() funct
		/// </summary>
		/// <param name="msg"></param>
		/// <param name="keyData"></param>
		/// <returns></returns>
		protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
		{
			switch (tabControl.SelectedIndex)
			{
				case 0: // Top
				if (ControlTop.TopPanel.Focused)
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
								MainViewUnderlay.Instance.MainViewOverlay.Navigate(keyData);
								return true;
						}
					}
					break;

				case 1: // Route
					if (ControlRoute.RoutePanel.Focused)
					{
						switch (keyData)
						{
							case Keys.Left:
							case Keys.Right:
							case Keys.Up:
							case Keys.Down:
								ControlRoute.RoutePanel.Navigate(keyData);
								return true;
						}
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
	}
}
