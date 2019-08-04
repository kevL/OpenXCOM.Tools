using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

using MapView.Forms.MainView;

using XCom;
using XCom.Base;


namespace MapView.Forms.Observers
{
	/// <summary>
	/// The base class for RoutePanel. Generally handles mousey things and
	/// calculating lozenges.
	/// @note This is not a Panel. It is a UserControl inside of a Panel.
	/// </summary>
	internal class RoutePanelParent
		:
			UserControl
	{
		#region Events
		public event EventHandler<RoutePanelEventArgs> RoutePanelMouseDownEvent;
		public event EventHandler<RoutePanelEventArgs> RoutePanelMouseUpEvent;
		#endregion Events


		#region Fields (static)
		internal protected const int OffsetX = 2; // these track the offset between the panel border
		internal protected const int OffsetY = 3; // and the lozenge-tip.
		#endregion Fields (static)


		#region Fields
		internal protected int _overCol = -1; // these track the location of the last mouse-overed tile
		internal protected int _overRow = -1; // NOTE: could be subsumed into 'CursorPosition' except ...
		#endregion Fields


		#region Properties (static)
		/// <summary>
		/// A node that is currently selected. Set its value via RouteView only.
		/// </summary>
		internal protected static RouteNode NodeSelected
		{ get; set; }
		#endregion Properties (static)


		#region Properties
		private Point _pos = new Point(-1,-1);
		/// <summary>
		/// Tracks the position of the mouse cursor. Used to print overinfo,
		/// overlayinfo, and to position the Overlay.
		/// </summary>
		internal Point CursorPosition
		{
			get { return _pos; }
			set { _pos = value; }
		}

		private MapFile _file;
		internal protected MapFile MapFile
		{
			get { return _file; }
			set
			{
				_file = value;
				OnResize(EventArgs.Empty);
			}
		}

		/// <summary>
		/// The top-left point of the panel.
		/// </summary>
		internal protected Point Origin
		{ get; set; }

		private int _halfwidth = 8;
		/// <summary>
		/// Half the horizontal width of a tile-lozenge.
		/// </summary>
		internal protected int HalfWidth
		{
			get { return _halfwidth; }
			set { _halfwidth = value; }
		}
		private int _halfheight = 4;
		/// <summary>
		/// Half the vertical height of a tile-lozenge.
		/// </summary>
		internal protected int HalfHeight
		{
			get { return _halfheight; }
			set { _halfheight = value; }
		}


		private readonly DrawBlobService _blobService = new DrawBlobService();
		internal protected DrawBlobService BlobService
		{
			get { return _blobService; }
		}
		#endregion Properties


		#region cTor
		/// <summary>
		/// cTor. Instantiated only as the parent of RoutePanel.
		/// </summary>
		internal protected RoutePanelParent()
		{
			var t1 = new Timer();	// because the mouse OnLeave event doesn't fire when the mouse
			t1.Interval = 250;		// moves over a different form before actually "leaving" this
			t1.Enabled = true;		// control. btw, this is only to stop the overlay from drawing
			t1.Tick += t1_Tick;		// on both RouteView and TopRouteView(Route) simultaneously.
		}							// so uh yeah it's overkill - Good Lord it works.
		#endregion cTor				// Plus it clears the overed infotext tile-coordinates.


		#region Events
		/// <summary>
		/// A ticker that checks if the mouse has left the building.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void t1_Tick(object sender, EventArgs e)
		{
			if (!Bounds.Contains(PointToClient(Cursor.Position)))
				CursorPosition = new Point(
										_overCol = -1,
										_overRow = -1);
		}
		#endregion Events


		#region Events (override)
		/// <summary>
		/// Ensures that a ticker tick happens pronto.
		/// </summary>
		/// <param name="e"></param>
		protected override void OnLoad(EventArgs e)
		{
			base.OnLoad(e);
			t1_Tick(this, e);
		}

		/// <summary>
		/// Fires from (child)RoutePanel.
		/// </summary>
		/// <param name="e"></param>
		protected override void OnResize(EventArgs e)
		{
			if (MapFile != null)
			{
				int width  = Width  - OffsetX * 2;
				int height = Height - OffsetY * 2;

				if (height > width / 2) // use width
				{
					HalfWidth = width / (MapFile.MapSize.Rows + MapFile.MapSize.Cols);

					if (HalfWidth % 2 != 0)
						--HalfWidth;

					HalfHeight = HalfWidth / 2;
				}
				else // use height
				{
					HalfHeight = height / (MapFile.MapSize.Rows + MapFile.MapSize.Cols);
					HalfWidth  = HalfHeight * 2;
				}

				Origin = new Point( // offset the left and top edges to account for the 3d panel border
								OffsetX + MapFile.MapSize.Rows * HalfWidth,
								OffsetY);

				BlobService.HalfWidth  = HalfWidth;
				BlobService.HalfHeight = HalfHeight;
			}
		}

		/// <summary>
		/// Selects a tile on the mouse-down event.
		/// IMPORTANT: Any changes that are done here regarding node-selection
		/// should be reflected in RouteView.SelectNode() since that is an
		/// alternate way to select a tile/node.
		/// </summary>
		/// <param name="e"></param>
		protected override void OnMouseDown(MouseEventArgs e)
		{
			Select();

			var loc = GetTileLocation(e.X, e.Y);
			if (loc.X != -1)
			{
				MainViewOverlay.that._keyDeltaX =
				MainViewOverlay.that._keyDeltaY = 0;

				MapFile.Location = new MapLocation( // fire SelectLocation
												loc.Y, loc.X,
												MapFile.Level);

				MainViewOverlay.that.ProcessSelection(loc,loc);	// set selected location for other viewers.
																// NOTE: drag-selection is not allowed here.
				var args = new RoutePanelEventArgs(
												e.Button,
												MapFile[loc.Y, loc.X],
												MapFile.Location);
				RoutePanelMouseDownEvent(this, args); // fire RouteView.OnRoutePanelMouseDown()
			}
		}

		/// <summary>
		/// Calls RouteView.OnRoutePanelMouseUp().
		/// </summary>
		/// <param name="e"></param>
		protected override void OnMouseUp(MouseEventArgs e)
		{
			var loc = GetTileLocation(e.X, e.Y);
			if (loc.X != -1)
			{
				MapFile.Location = new MapLocation( // fire SelectLocation
												loc.Y, loc.X,
												MapFile.Level);

				var args = new RoutePanelEventArgs(
												e.Button,
												MapFile[loc.Y, loc.X],
												MapFile.Location);
				RoutePanelMouseUpEvent(this, args); // fire RouteView.OnRoutePanelMouseUp()
			}
		}

		/// <summary>
		/// Tracks x/y location for the mouseover lozenge.
		/// </summary>
		/// <param name="e"></param>
		protected override void OnMouseMove(MouseEventArgs e)
		{
			var loc = GetTileLocation(e.X, e.Y);
			if (loc.X != _overCol || loc.Y != _overRow)
			{
				_overCol = loc.X;
				_overRow = loc.Y;
			}
			base.OnMouseMove(e); // required to fire RouteView.OnRoutePanelMouseMove()
		}
		#endregion Events (override)


		#region Methods
		/// <summary>
		/// Keyboard navigation called by RouteViewForm (form-level) key events
		/// OnKeyDown() and ProcessCmdKey().
		/// </summary>
		/// <param name="keyData"></param>
		internal void Navigate(Keys keyData)
		{
			if (MapFile != null) // safety.
			{
				MainViewOverlay.that._keyDeltaX =
				MainViewOverlay.that._keyDeltaY = 0;

				if (!MainViewOverlay.that.FirstClick)
				{
					MapFile.Location = new MapLocation(0,0, MapFile.Level); // fire SelectLocation event

					var loc = new Point(0,0);
					MainViewOverlay.that.ProcessSelection(loc,loc);

					var args = new RoutePanelEventArgs(
													MouseButtons.Left,
													MapFile[0,0],
													MapFile.Location);
					RoutePanelMouseDownEvent(this, args); // fire RouteView.OnRoutePanelMouseDown()

					ObserverManager.RouteView   .Control     .RoutePanel.Invalidate();
					ObserverManager.TopRouteView.ControlRoute.RoutePanel.Invalidate();
				}
				else if (keyData == Keys.Enter)
				{
					var args = new RoutePanelEventArgs(
													MouseButtons.Right,
													MapFile[MapFile.Location.Row,
															MapFile.Location.Col],
													MapFile.Location);
					RoutePanelMouseDownEvent(this, args); // fire RouteView.OnRoutePanelMouseDown()

					ObserverManager.RouteView   .Control     .RoutePanel.Invalidate();
					ObserverManager.TopRouteView.ControlRoute.RoutePanel.Invalidate();
				}
				else if (!keyData.HasFlag(Keys.Shift))
				{
					var loc = new Point(0,0);
					int dir = MapFileBase.LEVEL_no;

					switch (keyData)
					{
						case Keys.Up:    loc.X = -1; loc.Y = -1; break;
						case Keys.Right: loc.X = +1; loc.Y = -1; break;
						case Keys.Down:  loc.X = +1; loc.Y = +1; break;
						case Keys.Left:  loc.X = -1; loc.Y = +1; break;

						case Keys.PageUp:   loc.Y = -1; break;
						case Keys.PageDown: loc.X = +1; break;
						case Keys.End:      loc.Y = +1; break;
						case Keys.Home:     loc.X = -1; break;

//						case Keys.Delete: // oops Delete is delete tile - try [Shift+Insert]
						case Keys.Add:      dir = MapFileBase.LEVEL_Dn; break;
//						case Keys.Insert:
						case Keys.Subtract: dir = MapFileBase.LEVEL_Up; break;
					}

					if (loc.X != 0 || loc.Y != 0)
					{
						int r = MapFile.Location.Row + loc.Y;
						int c = MapFile.Location.Col + loc.X;
						if (   r > -1 && r < MapFile.MapSize.Rows
							&& c > -1 && c < MapFile.MapSize.Cols)
						{
							MapFile.Location = new MapLocation(r,c, MapFile.Level); // fire SelectLocation event

							loc.X = c; loc.Y = r;
							MainViewOverlay.that.ProcessSelection(loc,loc);

							var args = new RoutePanelEventArgs(
															MouseButtons.Left,
															MapFile[r,c],
															MapFile.Location);
							RoutePanelMouseDownEvent(this, args); // fire RouteView.OnRoutePanelMouseDown()

							ObserverManager.RouteView   .Control     .RoutePanel.Invalidate();
							ObserverManager.TopRouteView.ControlRoute.RoutePanel.Invalidate();
						}
					}
					else if (dir != MapFileBase.LEVEL_no)
					{
						int level = MapFile.Level + dir;
						if (level > -1 && level < MapFile.MapSize.Levs)
						{
							MapFile.ChangeLevel(dir);			// fire SelectLevel event
							MapFile.Location = new MapLocation(	// fire SelectLocation event
															MapFile.Location.Row,
															MapFile.Location.Col,
															level);
						}
					}
				}
				else // [Shift] = drag node ->
				{
					if (NodeSelected != null)
					{
						var loc = new Point(0,0);
						int dir = MapFileBase.LEVEL_no;

						switch (keyData)
						{
							case Keys.Shift | Keys.Up:    loc.X = -1; loc.Y = -1; break;
							case Keys.Shift | Keys.Right: loc.X = +1; loc.Y = -1; break;
							case Keys.Shift | Keys.Down:  loc.X = +1; loc.Y = +1; break;
							case Keys.Shift | Keys.Left:  loc.X = -1; loc.Y = +1; break;

							case Keys.Shift | Keys.PageUp:   loc.Y = -1; break;
							case Keys.Shift | Keys.PageDown: loc.X = +1; break;
							case Keys.Shift | Keys.End:      loc.Y = +1; break;
							case Keys.Shift | Keys.Home:     loc.X = -1; break;

//							case Keys.Shift | Keys.Delete: // oops Delete is delete tile - try [Shift+Insert]
							case Keys.Shift | Keys.Add:      dir = MapFileBase.LEVEL_Dn; break;
//							case Keys.Shift | Keys.Insert:
							case Keys.Shift | Keys.Subtract: dir = MapFileBase.LEVEL_Up; break;
						}

						if (loc.X != 0 || loc.Y != 0)
						{
							int r = MapFile.Location.Row + loc.Y;
							int c = MapFile.Location.Col + loc.X;
							if (   r > -1 && r < MapFile.MapSize.Rows
								&& c > -1 && c < MapFile.MapSize.Cols
								&& MapFile[r,c].Node == null)
							{
								RouteView.Dragnode = NodeSelected;

								MapFile.Location = new MapLocation(r,c, MapFile.Level); // fire SelectLocation event

								var args = new RoutePanelEventArgs(
																MouseButtons.None,
																MapFile[r,c],
																MapFile.Location);
								RoutePanelMouseUpEvent(this, args); // fire RouteView.OnRoutePanelMouseUp()

								ObserverManager.RouteView   .Control     .SetInfotextOver();
								ObserverManager.TopRouteView.ControlRoute.SetInfotextOver();

								ObserverManager.RouteView   .Control     .RoutePanel.Invalidate();
								ObserverManager.TopRouteView.ControlRoute.RoutePanel.Invalidate();
							}
						}
						else if (dir != MapFileBase.LEVEL_no)
						{
							int level = MapFile.Level + dir;
							if (level > -1 && level < MapFile.MapSize.Levs
								&& MapFile[MapFile.Location.Row,
										   MapFile.Location.Col,
										   level].Node == null)
							{
								RouteView.Dragnode = NodeSelected;

								MapFile.ChangeLevel(dir);			// fire SelectLevel event
								MapFile.Location = new MapLocation(	// fire SelectLocation event
																MapFile.Location.Row,
																MapFile.Location.Col,
																level);

								var args = new RoutePanelEventArgs(
																MouseButtons.None,
																MapFile[MapFile.Location.Row,
																		MapFile.Location.Col],
																MapFile.Location);
								RoutePanelMouseUpEvent(this, args); // fire RouteView.OnRoutePanelMouseUp()

								ObserverManager.RouteView   .Control     .SetInfotextOver();
								ObserverManager.TopRouteView.ControlRoute.SetInfotextOver();

								ObserverManager.RouteView   .Control     .PrintSelectedInfo();
								ObserverManager.TopRouteView.ControlRoute.PrintSelectedInfo();

								ObserverManager.RouteView   .Control     .RoutePanel.Invalidate();
								ObserverManager.TopRouteView.ControlRoute.RoutePanel.Invalidate();
							}
						}
					}
				}
			}
		}


		/// <summary>
		/// Gets the tile contained at (x,y) wrt client-area in local screen
		/// coordinates.
		/// </summary>
		/// <param name="x">ref to the x-position of the mouse-cursor wrt
		/// Client-area (refout is the tile-x location)</param>
		/// <param name="y">ref to the y-position of the mouse-cursor wrt
		/// Client-area (refout is the tile-y location)</param>
		/// <returns>the corresponding MapTile or null if (x,y) is an invalid
		/// location for a tile</returns>
		internal protected MapTile GetTile(ref int x, ref int y)
		{
			var loc = GetTileLocation(x,y);
			x = loc.X;
			y = loc.Y;
			return (x != -1) ? MapFile[y,x]
							 : null;
		}

		/// <summary>
		/// Converts a position from client-coordinates to a tile-location.
		/// </summary>
		/// <param name="x">the x-position of the mouse-cursor wrt Client-area</param>
		/// <param name="y">the y-position of the mouse-cursor wrt Client-area</param>
		/// <returns>the corresponding tile-location or (-1,-1) if the location
		/// is invalid</returns>
		internal protected Point GetTileLocation(int x, int y)
		{
			if (MapFile != null)
			{
				x -= Origin.X;
				y -= Origin.Y;

				double xd = (double)x / (HalfWidth  * 2)
						  + (double)y / (HalfHeight * 2);
				double yd = ((double)y * 2 - x) / (HalfWidth * 2);

				var point = new Point(
									(int)Math.Floor(xd),
									(int)Math.Floor(yd));

				if (   point.Y > -1 && point.Y < MapFile.MapSize.Rows
					&& point.X > -1 && point.X < MapFile.MapSize.Cols)
				{
					return point;
				}
			}
			return new Point(-1,-1);
		}
		#endregion Methods
	}
}