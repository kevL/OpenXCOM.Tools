using System;
using System.Drawing;
using System.Windows.Forms;

using MapView.Forms.MainView;

using XCom;


namespace MapView.Forms.Observers
{
	/// <summary>
	/// The base class for
	/// <c><see cref="RouteControl"/></c>. Generally handles mousey things and
	/// keyboard navigation.
	/// </summary>
	/// <remarks><c><see cref="RouteView"/></c> also handles mouse events.</remarks>
	internal class RouteControlParent
		:
			UserControl
	{
		protected void DisposeControlParent()
		{
			DSShared.Logfile.Log("RouteControlParent.DisposeControlParent()");
			_t1.Dispose();
		}


		#region Events
		public event EventHandler<RouteControlEventArgs> RouteControlMouseDownEvent;
		public event EventHandler<RouteControlEventArgs> RouteControlMouseUpEvent;
		#endregion Events


		#region Fields (static)
		protected const int OffsetX = 2; // these track the offset between the panel border
		protected const int OffsetY = 3; // and the lozenge-tip.
		#endregion Fields (static)


		#region Fields
		protected MapFile _file;

		/// <summary>
		/// <c>_col</c> and <c>_row</c> track the location of the last
		/// mouse-overed tile; <c>_col</c> needs to be set to <c>-1</c> when the
		/// mouse is not over a tile. Their values need to be updated only when
		/// the mouseovered tile-location changes via
		/// <c><see cref="OnMouseMove()">OnMouseMove()</see></c>.
		/// </summary>
		internal int _col = -1;
		/// <summary>
		/// <c>_col</c> and <c>_row</c> track the location of the last
		/// mouse-overed tile; <c>_col</c> needs to be set to <c>-1</c> when the
		/// mouse is not over a tile. Their values need to be updated only when
		/// the mouseovered tile-location changes via
		/// <c><see cref="OnMouseMove()">OnMouseMove()</see></c>.
		/// </summary>
		internal int _row = -1;

		/// <summary>
		/// Tracks tile-location for move/up/down mouse events:
		/// <c><see cref="_col"/></c> and <c><see cref="_row"/></c> in a
		/// convenient <c>Point</c> object.
		/// </summary>
		private Point _loc;

		/// <summary>
		/// A timer whose tick event determines if the cursor has left this
		/// control and if so clears the overed lozenge.
		/// </summary>
		private Timer _t1 = new Timer();
		#endregion Fields


		#region Properties (static)
		/// <summary>
		/// A <c><see cref="XCom.RouteNode"/></c> that is currently selected.
		/// </summary>
		/// <remarks>Set its value via
		/// <c><see cref="RouteView.NodeSelected">RouteView.NodeSelected</see></c>
		/// only.</remarks>
		protected static RouteNode NodeSelected
		{ get; private set; }

		/// <summary>
		/// Sets <c><see cref="NodeSelected"/></c>.
		/// </summary>
		/// <param name="node">a <c><see cref="XCom.RouteNode"/></c></param>
		internal static void SetNodeSelected(RouteNode node)
		{
			NodeSelected = node;
		}
		#endregion Properties (static)


		#region Properties
		/// <summary>
		/// The top-left point of the panel.
		/// </summary>
		protected Point Origin
		{ get; set; }

		private int _halfwidth = 8;
		/// <summary>
		/// Half the horizontal width of a tile-lozenge.
		/// </summary>
		protected int HalfWidth
		{
			get { return _halfwidth; }
			set { _halfwidth = value; }
		}
		private int _halfheight = 4;
		/// <summary>
		/// Half the vertical height of a tile-lozenge.
		/// </summary>
		protected int HalfHeight
		{
			get { return _halfheight; }
			set { _halfheight = value; }
		}


		private readonly BlobDrawService _blobService = new BlobDrawService();
		protected BlobDrawService BlobService
		{
			get { return _blobService; }
		}
		#endregion Properties


		#region cTor
		/// <summary>
		/// cTor. Instantiated only as the parent of
		/// <c><see cref="RouteControl"/></c>.
		/// </summary>
		protected RouteControlParent()
		{
			// Because the mouse OnLeave event doesn't fire when the mouse
			// moves over a different form before actually "leaving" this
			// control. btw, this is only to stop the overlay from drawing
			// on both RouteView and TopRouteView(Route) simultaneously.
			// so uh yeah it's overkill - Good Lord it works.
			// Plus it clears the overed infotext tile-coordinates.

			_t1.Interval = Globals.PERIOD;
			_t1.Enabled = true;
			_t1.Tick += t1_Tick;
		}
		#endregion cTor


		#region Events
		/// <summary>
		/// A ticker that checks if the cursor has left the building. See also
		/// <c><see cref="RouteView"/>.OnRouteControlMouseLeave()</c>.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void t1_Tick(object sender, EventArgs e)
		{
			if (!Bounds.Contains(PointToClient(Control.MousePosition)))
			{
				_col = -1;
				// TODO: perhaps fire OnMouseMove()
			}
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
		/// Fires from <c><see cref="RouteControl"/></c>.
		/// </summary>
		/// <param name="e"></param>
		protected override void OnResize(EventArgs e)
		{
			if (_file != null) // safety.
			{
				int width  = Width  - OffsetX * 2;
				int height = Height - OffsetY * 2;

				if (height > width / 2) // use width
				{
					HalfWidth = width / (_file.Rows + _file.Cols);

					if (HalfWidth % 2 != 0)
						--HalfWidth;

					HalfHeight = HalfWidth / 2;
				}
				else // use height
				{
					HalfHeight = height / (_file.Rows + _file.Cols);
					HalfWidth  = HalfHeight * 2;
				}

				Origin = new Point( // offset the left and top edges to account for the 3d panel border
								OffsetX + _file.Rows * HalfWidth,
								OffsetY);

				BlobService.HalfWidth  = HalfWidth;
				BlobService.HalfHeight = HalfHeight;
			}
		}

		/// <summary>
		/// Selects a tile on the mouse-down event.
		/// 
		/// 
		/// Fires
		/// <c><see cref="RouteControl.RouteControlMouseDownEvent">RouteControl.RouteControlMouseDownEvent</see></c>
		/// which is handled by
		/// <c><see cref="RouteView"></see>.OnRouteControlMouseDown()</c> for
		/// <c><see cref="RouteNode"/></c> operations.
		/// </summary>
		/// <param name="e"></param>
		/// <remarks>Any changes that are done here regarding node-selection
		/// should be reflected in <c><see cref="RouteView"/>.SelectNode()</c>
		/// since that is an alternate way to select a tile/node.</remarks>
		protected override void OnMouseDown(MouseEventArgs e)
		{
			Select();

			if (_col != -1)
			{
				MainViewOverlay.that._keyDeltaX =
				MainViewOverlay.that._keyDeltaY = 0;

				_file.Location = new MapLocation( // fire LocationSelected
											_col, _row,
											_file.Level);

				MainViewOverlay.that.ProcessSelection(_loc, _loc);	// set selected location for other viewers.
																	// NOTE: drag-selection is not allowed here.
				var args = new RouteControlEventArgs(
												e.Button,
												_file.GetTile(_col, _row),
												_file.Location);
				RouteControlMouseDownEvent(this, args); // fire RouteView.OnRouteControlMouseDown()
			}
		}

		/// <summary>
		/// Fires
		/// <c><see cref="RouteControl.RouteControlMouseUpEvent">RouteControl.RouteControlMouseUpEvent</see></c>
		/// which is handled by
		/// <c><see cref="RouteView"></see>.OnRouteControlMouseUp()</c> to
		/// complete a dragnode operation.
		/// </summary>
		/// <param name="e"></param>
		protected override void OnMouseUp(MouseEventArgs e)
		{
			if (_col != -1)
			{
				_file.Location = new MapLocation( // fire LocationSelected
											_col, _row,
											_file.Level);

				var args = new RouteControlEventArgs(
												e.Button,
												_file.GetTile(_col, _row),
												_file.Location);
				RouteControlMouseUpEvent(this, args); // fire RouteView.OnRouteControlMouseUp()
			}
		}

		/// <summary>
		/// Tracks col/row location for the mouseover lozenge and mouseover info.
		/// </summary>
		/// <param name="e"></param>
		protected override void OnMouseMove(MouseEventArgs e)
		{
			_loc = GetTileLocation(e.X, e.Y);
			if (_loc.X != _col || _loc.Y != _row)
			{
				_col = _loc.X;
				_row = _loc.Y;

				// this fires panel refreshes only when the cursor moves to another tile
				// The InfoOverlay goes sticky but the panel feels tighter.
				base.OnMouseMove(e); // fire RouteView.OnRouteControlMouseMove()
				return;
			}

			if (RouteView.Optionables.ReduceDraws || !RouteView.Optionables.ShowOverlay)
				return;

			// this fires panel refreshes whenever the mouse moves a single pixel
			// The InfoOverlay moves freely.
			base.OnMouseMove(e); // fire RouteView.OnRouteControlMouseMove()
		}
		#endregion Events (override)


		#region Methods
		/// <summary>
		/// Sets <c><see cref="_file"/></c>.
		/// </summary>
		/// <param name="file">a <c><see cref="MapFile"/></c></param>
		internal void SetMapFile(MapFile file)
		{
			_file = file;
			OnResize(EventArgs.Empty);
		}

		/// <summary>
		/// Keyboard navigation called by <c><see cref="RouteViewForm"/></c> key
		/// events <c>OnKeyDown()</c> and <c>ProcessCmdKey()</c>.
		/// </summary>
		/// <param name="keyData"></param>
		internal void Navigate(Keys keyData)
		{
			if (_file != null && (keyData & (Keys.Control | Keys.Alt)) == Keys.None) // safety.
			{
				bool invalidate = false;

				MainViewOverlay.that._keyDeltaX =
				MainViewOverlay.that._keyDeltaY = 0;

				if (!MainViewOverlay.that.FirstClick) // allow Shift
				{
					_file.Location = new MapLocation(0,0, _file.Level); // fire LocationSelected event

					var loc = new Point(0,0);
					MainViewOverlay.that.ProcessSelection(loc,loc);

					var args = new RouteControlEventArgs(
													MouseButtons.Left,
													_file.GetTile(0,0),
													_file.Location);
					RouteControlMouseDownEvent(this, args); // fire RouteView.OnRouteControlMouseDown()
					invalidate = true;
				}
				else if (keyData == Keys.Enter)
				{
					var args = new RouteControlEventArgs(
													MouseButtons.Right,
													_file.GetTile(_file.Location.Col,
																  _file.Location.Row),
													_file.Location);
					RouteControlMouseDownEvent(this, args); // fire RouteView.OnRouteControlMouseDown()
					invalidate = true;
				}
				else if ((keyData & Keys.Shift) == Keys.None)
				{
					var loc = new Point(0,0);
					int vert = MapFile.LEVEL_no;

					switch (keyData)
					{
						case Keys.Up:       loc.X = -1; loc.Y = -1; break;
						case Keys.Right:    loc.X = +1; loc.Y = -1; break;
						case Keys.Down:     loc.X = +1; loc.Y = +1; break;
						case Keys.Left:     loc.X = -1; loc.Y = +1; break;

						case Keys.PageUp:   loc.Y = -1; break;
						case Keys.PageDown: loc.X = +1; break;
						case Keys.End:      loc.Y = +1; break;
						case Keys.Home:     loc.X = -1; break;

						case Keys.Add:      vert = MapFile.LEVEL_Dn; break;
						case Keys.Subtract: vert = MapFile.LEVEL_Up; break;
					}

					if (loc.X != 0 || loc.Y != 0)
					{
						int c = _file.Location.Col + loc.X;
						int r = _file.Location.Row + loc.Y;
						if (   c > -1 && c < _file.Cols
							&& r > -1 && r < _file.Rows)
						{
							_file.Location = new MapLocation(c,r, _file.Level); // fire LocationSelected event

							loc.X = c; loc.Y = r;
							MainViewOverlay.that.ProcessSelection(loc,loc);

							var args = new RouteControlEventArgs(
															MouseButtons.Left,
															_file.GetTile(c,r),
															_file.Location);
							RouteControlMouseDownEvent(this, args); // fire RouteView.OnRouteControlMouseDown()
							invalidate = true;
						}
					}
					else if (vert != MapFile.LEVEL_no)
					{
						int level = _file.Level + vert;
						if (level > -1 && level < _file.Levs)
						{
							_file.ChangeLevel(vert);			// fire LevelSelected event
							_file.Location = new MapLocation(	// fire LocationSelected event
														_file.Location.Col,
														_file.Location.Row,
														level);
						}
					}
				}
				else if (NodeSelected != null) // Shift = drag node ->
				{
					var loc = new Point(0,0);
					int vert = MapFile.LEVEL_no;

					switch (keyData)
					{
						case Keys.Shift | Keys.Up:       loc.X = -1; loc.Y = -1; break;
						case Keys.Shift | Keys.Right:    loc.X = +1; loc.Y = -1; break;
						case Keys.Shift | Keys.Down:     loc.X = +1; loc.Y = +1; break;
						case Keys.Shift | Keys.Left:     loc.X = -1; loc.Y = +1; break;

						case Keys.Shift | Keys.PageUp:   loc.Y = -1; break;
						case Keys.Shift | Keys.PageDown: loc.X = +1; break;
						case Keys.Shift | Keys.End:      loc.Y = +1; break;
						case Keys.Shift | Keys.Home:     loc.X = -1; break;

						case Keys.Shift | Keys.Add:      vert = MapFile.LEVEL_Dn; break;
						case Keys.Shift | Keys.Subtract: vert = MapFile.LEVEL_Up; break;
					}

					if (loc.X != 0 || loc.Y != 0)
					{
						int c = _file.Location.Col + loc.X;
						int r = _file.Location.Row + loc.Y;
						if (   c > -1 && c < _file.Cols
							&& r > -1 && r < _file.Rows
							&& _file.GetTile(c,r).Node == null)
						{
							RouteView.Dragnode = NodeSelected;

							_file.Location = new MapLocation(c,r, _file.Level); // fire LocationSelected event

							var args = new RouteControlEventArgs(
															MouseButtons.None,
															_file.GetTile(c,r),
															_file.Location);
							RouteControlMouseUpEvent(this, args); // fire RouteView.OnRouteControlMouseUp()
							invalidate = true;

							ObserverManager.RouteView.Control.SetInfoOverText(); // update both viewers.
						}
					}
					else if (vert != MapFile.LEVEL_no)
					{
						int level = _file.Level + vert;
						if (level > -1 && level < _file.Levs
							&& _file.GetTile(_file.Location.Col,
											 _file.Location.Row,
											 level).Node == null)
						{
							RouteView.Dragnode = NodeSelected;

							_file.ChangeLevel(vert);			// fire LevelSelected event
							_file.Location = new MapLocation(	// fire LocationSelected event
														_file.Location.Col,
														_file.Location.Row,
														level);

							var args = new RouteControlEventArgs(
															MouseButtons.None,
															_file.GetTile(_file.Location.Col,
																		  _file.Location.Row),
															_file.Location);
							RouteControlMouseUpEvent(this, args); // fire RouteView.OnRouteControlMouseUp()
							invalidate = true;

							ObserverManager.RouteView.Control.SetInfoOverText(); // update both viewers.

							ObserverManager.RouteView   .Control     .PrintSelectedInfo();
							ObserverManager.TopRouteView.ControlRoute.PrintSelectedInfo();
						}
					}
				}

				if (invalidate)
				{
					ObserverManager.RouteView   .Control     .RouteControl.Invalidate();
					ObserverManager.TopRouteView.ControlRoute.RouteControl.Invalidate();
				}
			}
		}


		/// <summary>
		/// Converts a position from client-coordinates to a tile-location.
		/// </summary>
		/// <param name="x">the x-position of the mouse-cursor wrt Client-area</param>
		/// <param name="y">the y-position of the mouse-cursor wrt Client-area</param>
		/// <returns>the corresponding tile-location or (-1,-1) if the location
		/// is invalid</returns>
		private Point GetTileLocation(int x, int y)
		{
			if (_file != null) // safety.
			{
				x -= Origin.X;
				y -= Origin.Y;

				double xd = (double)x / (HalfWidth  * 2)
						  + (double)y / (HalfHeight * 2);
				double yd = ((double)y * 2 - x) / (HalfWidth * 2);

				var loc = new Point(
								(int)Math.Floor(xd),
								(int)Math.Floor(yd));

				if (   loc.X > -1 && loc.X < _file.Cols
					&& loc.Y > -1 && loc.Y < _file.Rows)
				{
					return loc;
				}
			}
			return new Point(-1,-1);
		}
		#endregion Methods
	}
}
