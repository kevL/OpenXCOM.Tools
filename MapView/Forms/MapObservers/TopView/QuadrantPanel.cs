using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

using MapView.Forms.MainWindow;

using XCom;
using XCom.Interfaces.Base;


namespace MapView.Forms.MapObservers.TopViews
{
	/// <summary>
	/// These are not actually "quadrants"; they are tile-part types. But that's
	/// the way this trolls.
	/// @note This is not a Panel. It is a Control.
	/// </summary>
	internal sealed class QuadrantPanel
		:
			MapObserverControl1
	{
		#region Fields & Properties
		private readonly QuadrantPanelDrawService _drawService =
					 new QuadrantPanelDrawService();

		private XCMapTile _tile;
		private MapLocation _location;

		private QuadrantType _quadrant;
		internal QuadrantType SelectedQuadrant
		{
			get { return _quadrant; }
			set
			{
				_quadrant = value;
				Refresh();
			}
		}

		[Browsable(false)]
		internal Dictionary<string, SolidBrush> Brushes
		{
			set { _drawService.Brushes = value; }
		}

		[Browsable(false)]
		internal Dictionary<string, Pen> Pens
		{
			set { _drawService.Pens = value; }
		}

		[Browsable(false)]
		internal SolidBrush SelectColor
		{
			get { return _drawService.Brush; }
			set
			{
				_drawService.Brush = value;
				Refresh();
			}
		}
		#endregion Fields & Properties


		#region cTor
		/// <summary>
		/// cTor. There are 2 quadpanels: one in TopView and another in
		/// TopRouteView(Top).
		/// </summary>
		internal QuadrantPanel()
		{
			SetStyle(ControlStyles.OptimizedDoubleBuffer
				   | ControlStyles.AllPaintingInWmPaint
				   | ControlStyles.UserPaint
				   | ControlStyles.ResizeRedraw, true);
		}
		#endregion cTor


		#region Events (override)
		/// <summary>
		/// Inherited from IMapObserver through MapObserverControl0.
		/// </summary>
		/// <param name="args"></param>
		public override void OnSelectLocationObserver(SelectLocationEventArgs args)
		{
			//LogFile.WriteLine("");
			//LogFile.WriteLine("QuadrantPanel.OnSelectLocationObserver");

			_tile     = args.Tile as XCMapTile;
			_location = args.Location;
			Refresh();
		}

		/// <summary>
		/// Inherited from IMapObserver through MapObserverControl0.
		/// </summary>
		/// <param name="args"></param>
		public override void OnSelectLevelObserver(SelectLevelEventArgs args)
		{
			if (_location != null)
			{
				_tile = MapBase[_location.Row, _location.Col] as XCMapTile;
				_location.Lev = args.Level;
			}
			Refresh();
		}


		private QuadrantType _keyQuadtype = QuadrantType.None;
		internal void ForceMouseDown(MouseEventArgs e, QuadrantType quadType)
		{
			_keyQuadtype = quadType;
			OnMouseDown(e);
		}

		protected override void OnMouseDown(MouseEventArgs e)
		{
			ViewerFormsManager.TopView     .Control   .TopPanel.Select();
			ViewerFormsManager.TopRouteView.ControlTop.TopPanel.Select();

			QuadrantType quadType;
			if (_keyQuadtype == QuadrantType.None) // ie. is *not* forced by keyboard-input
			{
				quadType = (QuadrantType)((e.X - QuadrantPanelDrawService.StartX)
											   / QuadrantPanelDrawService.QuadWidthTotal);
			}
			else
			{
				quadType = _keyQuadtype;
				_keyQuadtype = QuadrantType.None;
			}

			PartType partType = PartType.All;
			switch (quadType)
			{
				case QuadrantType.Floor:   partType = PartType.Floor;   break;
				case QuadrantType.West:    partType = PartType.West;    break;
				case QuadrantType.North:   partType = PartType.North;   break;
				case QuadrantType.Content: partType = PartType.Content; break;
			}

			if (partType != PartType.All)
			{
				ViewerFormsManager.TopView     .Control   .SelectQuadrant(partType);
				ViewerFormsManager.TopRouteView.ControlTop.SelectQuadrant(partType);

				SetSelected(e.Button, e.Clicks);
				if (e.Button == MouseButtons.Right) // see SetSelected()
				{
					MainViewUnderlay.that.MainViewOverlay.Refresh();

					ViewerFormsManager.TopView     .Refresh();
					ViewerFormsManager.RouteView   .Refresh();
					ViewerFormsManager.TopRouteView.Refresh();

					if (XCMainWindow.ScanG != null)
						XCMainWindow.ScanG.InvalidatePanel();
				}
			}
		}

		/// <summary>
		/// Overrides DoubleBufferedControl.RenderGraphics() - ie, OnPaint().
		/// Passes the draw-function on to QuadrantPanelDrawService.
		/// </summary>
		/// <param name="graphics"></param>
		protected override void RenderGraphics(Graphics graphics)
		{
			_drawService.Draw(graphics, _tile, SelectedQuadrant);
		}
		#endregion Events (override)


		#region Methods
		/// <summary>
		/// Handles the details of LMB and RMB wrt the QuadrantPanels.
		/// </summary>
		/// <param name="btn"></param>
		/// <param name="clicks"></param>
		internal void SetSelected(MouseButtons btn, int clicks)
		{
			if (_tile != null)
			{
				switch (btn)
				{
					case MouseButtons.Left:
						switch (clicks)
						{
							case 1:
								break;

							case 2:
								var tileView = ViewerFormsManager.TileView.Control;
								tileView.SelectedTilepart = _tile[SelectedQuadrant];
								break;
						}
						break;

					case MouseButtons.Right:
					{
						if (MainViewUnderlay.that.MainViewOverlay.FirstClick) // do not set a part in a quad unless a tile is selected.
						{
							switch (clicks)
							{
								case 1:
									var tileView = ViewerFormsManager.TileView.Control;
									_tile[SelectedQuadrant] = tileView.SelectedTilepart;

									MainViewUnderlay.that.MainViewOverlay.Refresh();

									ViewerFormsManager.RouteView   .Control     .Refresh();
									ViewerFormsManager.TopRouteView.ControlRoute.Refresh();
									break;

								case 2:
									_tile[SelectedQuadrant] = null;
									break;
							}

							XCMainWindow.that.MapChanged = true;

							Refresh();

							ViewerFormsManager.TopView     .Control   .TopPanel.Refresh();
							ViewerFormsManager.TopRouteView.ControlTop.TopPanel.Refresh();
						}
						break;
					}
				}
			}
		}
		#endregion Methods
	}
}
