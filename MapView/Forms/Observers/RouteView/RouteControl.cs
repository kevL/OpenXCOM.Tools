using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

using MapView.Forms.MainView;

using XCom;


namespace MapView.Forms.Observers
{
	/// <summary>
	/// The derived class <c>RouteControl</c>. Handles drawing/painting the
	/// panel.
	/// </summary>
	internal sealed class RouteControl
		:
			RouteControlParent
	{
		internal void DisposeControl()
		{
			DSShared.Logfile.Log("RouteControl.DisposeControl()");
			_nodeFill   .Dispose();
			_lozSelector.Dispose();
			_lozSelected.Dispose();
			_lozSpotted .Dispose();

			DisposeControlParent();
		}

		internal static void DisposeControlStatics()
		{
			DSShared.Logfile.Log("RouteControl.DisposeControlStatics() static");
			BrushOverlayBlue .Dispose();
			BrushOverlayLight.Dispose();
			FontOverlay      .Dispose();
			FontRose         .Dispose();
		}

		#region Fields (static)
		private const int RoseMarginX = 25;
		private const int RoseMarginY =  5;

		private const int NodeValHeight = 12;

		private const int OverlayColPad = 2;

		private const string Over   = "id"; // these are for the translucent overlay box ->
		private const string Unit   = "unit";
		private const string Rank   = "rank";
		private const string Spawn  = "spawn";
		private const string Patrol = "patrol";
		private const string Attack = "attack";

		private const string TextNorth = "N";
		private const string TextSouth = "S";
		private const string TextEast  = "E";
		private const string TextWest  = "W";

		private static readonly Brush BrushOverlayBlue  = new SolidBrush(Color.FromArgb(205, Color.DarkSlateBlue));
		private static readonly Brush BrushOverlayLight = new SolidBrush(Color.FromArgb( 90, Color.AntiqueWhite));

		private static readonly Font FontOverlay = new Font("Verdana",      7F, FontStyle.Bold);
		private static readonly Font FontRose    = new Font("Courier New", 22F, FontStyle.Bold);

		private static SolidBrush BrushNode;
		private static SolidBrush BrushNodeSpawn;
		private static SolidBrush BrushNodeSelected;

		private static Pen PenLink;
		private static Pen PenLinkSelected;

		internal static BlobColorTool ToolWall;
		internal static BlobColorTool ToolContent;
		#endregion Fields (static)


		#region Fields
		private Graphics _graphics;
		private readonly GraphicsPath _nodeFill    = new GraphicsPath();
		private readonly GraphicsPath _lozSelector = new GraphicsPath(); // mouse-over lozenge
		private readonly GraphicsPath _lozSelected = new GraphicsPath(); // click/drag lozenge
		private readonly GraphicsPath _lozSpotted  = new GraphicsPath(); // go-button lozenge

		/// <summary>
		/// Tracks the position of the mouse-cursor.
		/// </summary>
		/// <remarks>Used to position the InfoOverlay.</remarks>
		private Point _over = new Point(-1,-1);

		/// <summary>
		/// Sets the over position.
		/// </summary>
		/// <param name="over"></param>
		/// <remarks>Used to position the InfoOverlay.</remarks>
		internal void SetOver(Point over)
		{
			_over = over;
		}

		/// <summary>
		/// The location of the tile that is highlighted by a mouseovered Go
		/// button.
		/// </summary>
		private Point _spot = new Point(-1,-1);

		/// <summary>
		/// Sets the spot location.
		/// </summary>
		/// <param name="spot"></param>
		/// <remarks>Used to highlight a destination node.</remarks>
		internal void SetSpot(Point spot)
		{
			_spot = spot;
		}

		private int _widthoverlayleft;
		private int _heightoverlaytext;

		private int _widthNorth;
		private int _widthEast;
		#endregion Fields


		#region Properties (static)
		private static readonly Dictionary<string, Pen> _pens =
							new Dictionary<string, Pen>();
		/// <summary>
		/// Pens for use in <c>RouteControl</c>.
		/// </summary>
		internal static Dictionary<string, Pen> RoutePens
		{
			get { return _pens; }
		}

		private static readonly Dictionary<string, SolidBrush> _brushes =
							new Dictionary<string, SolidBrush>();
		/// <summary>
		/// Brushes for use in <c>RouteControl</c>.
		/// </summary>
		internal static Dictionary<string, SolidBrush> RouteBrushes
		{
			get { return _brushes; }
		}
		#endregion Properties (static)


		#region cTor
		/// <summary>
		/// cTor.
		/// </summary>
		internal RouteControl()
		{
			Dock = DockStyle.Fill;
			MainViewOverlay.that.MouseDrag += PathSelectedLozenge;

			using (Graphics graphics = CreateGraphics())
			{
				int w;
				w = (int)graphics.MeasureString(Over,   FontOverlay).Width;
				if (w > _widthoverlayleft) _widthoverlayleft = w;
	
				w = (int)graphics.MeasureString(Unit,   FontOverlay).Width;
				if (w > _widthoverlayleft) _widthoverlayleft = w;
	
				w = (int)graphics.MeasureString(Rank,   FontOverlay).Width;
				if (w > _widthoverlayleft) _widthoverlayleft = w;
	
				w = (int)graphics.MeasureString(Spawn,  FontOverlay).Width;
				if (w > _widthoverlayleft) _widthoverlayleft = w;
	
				w = (int)graphics.MeasureString(Patrol, FontOverlay).Width;
				if (w > _widthoverlayleft) _widthoverlayleft = w;
	
				w = (int)graphics.MeasureString(Attack, FontOverlay).Width;
				if (w > _widthoverlayleft) _widthoverlayleft = w;
	
				_heightoverlaytext = (int)graphics.MeasureString(TextNorth, FontOverlay).Height + 1;
	
				_widthNorth = (int)graphics.MeasureString(TextNorth, FontRose).Width;
				_widthEast  = (int)graphics.MeasureString(TextEast,  FontRose).Width;
			}
		}
		#endregion cTor


		#region Events (override)
		/// <summary>
		/// Repaths the selected-lozenge on the Resize event.
		/// </summary>
		/// <param name="e"></param>
		protected override void OnResize(EventArgs e)
		{
			if (_file != null)
			{
				base.OnResize(e);
				PathSelectedLozenge();
			}
		}


		/// <summary>
		/// You know the drill ... Paint it, Black
		/// black as night
		/// </summary>
		/// <param name="e"></param>
		/// <remarks>Pens and Brushes need to be refreshed each call to draw
		/// since they can be changed in Options. Or not ....</remarks>
		protected override void OnPaint(PaintEventArgs e)
		{
			_graphics = e.Graphics;
			_graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

			ControlPaint.DrawBorder3D(_graphics, ClientRectangle, Border3DStyle.Etched);

			if (_file != null)
			{
				BlobService.HalfWidth  = HalfWidth;
				BlobService.HalfHeight = HalfHeight;

				PenLink         = RoutePens[RouteViewOptionables.str_LinkColor];
				PenLinkSelected = RoutePens[RouteViewOptionables.str_LinkSelectedColor];

				DrawBlobs();

				DrawLinks();

				if (NodeSelected != null)
					DrawLinkLines(
							Origin.X + (NodeSelected.Col - NodeSelected.Row)     * HalfWidth,
							Origin.Y + (NodeSelected.Col + NodeSelected.Row + 1) * HalfHeight,
							NodeSelected, true);

				DrawNodes();

				DrawGridLines();

				if (Focused && _col != -1) // draw the selector lozenge
				{
					PathSelectorLozenge(
									Origin.X + (_col - _row) * HalfWidth,
									Origin.Y + (_col + _row) * HalfHeight);
					using (var pen = new Pen( // TODO: Make selector-pen a separate Option.
											RouteView.Optionables.GridLineColor,
											RouteView.Optionables.GridLineWidth + 1))
					{
						_graphics.DrawPath(pen, _lozSelector);
					}
				}

				if (MainViewOverlay.that.FirstClick)
				{
					using (var pen = new Pen( // TODO: Make selected-pen a separate Option.
											RouteView.Optionables.NodeSelectedColor,
											RouteView.Optionables.GridLineWidth + 1))
					{
						_graphics.DrawPath(pen, _lozSelected);

						if (_spot.X > -1)
						{
							PathSpottedLozenge(
											Origin.X + (_spot.X - _spot.Y) * HalfWidth,
											Origin.Y + (_spot.X + _spot.Y) * HalfHeight);
							_graphics.DrawPath(pen, _lozSpotted); // TODO: Make spotted-pen a separate Option.
						}
					}
				}

				DrawRose();

				if (RouteView.Optionables.ShowPriorityBars)
					DrawNodeMeters();

				if (RouteView.Optionables.ShowOverlay && _col != -1)
					DrawInfoOverlay();

				if (   ObserverManager.RouteView   .Control     .RouteControl._col == -1
					&& ObserverManager.TopRouteView.ControlRoute.RouteControl._col == -1)
				{
					ObserverManager.RouteView   .Control     .ClearOveredInfo();
					ObserverManager.TopRouteView.ControlRoute.ClearOveredInfo();
				}
			}
		}
		#endregion Events (override)


		#region Methods (draw)
		/// <summary>
		/// Draws any wall and/or content indicators.
		/// </summary>
		private void DrawBlobs()
		{
			MapTile tile;
			for (int
					r = 0,
						startX = Origin.X,
						startY = Origin.Y;
					r != _file.Rows;
					++r,
						startX -= HalfWidth,
						startY += HalfHeight)
			{
				for (int
						c = 0,
							x = startX,
							y = startY;
						c != _file.Cols;
						++c,
							x += HalfWidth,
							y += HalfHeight)
				{
					if (!(tile = _file.GetTile(c,r)).Vacant)
					{
						if (tile.Content != null)
							BlobService.Draw(_graphics, ToolContent, x,y, tile.Content);

						if (tile.West != null)
							BlobService.Draw(_graphics, ToolWall, x,y, tile.West);

						if (tile.North != null)
							BlobService.Draw(_graphics, ToolWall, x,y, tile.North);
					}
				}
			}
		}

		/// <summary>
		/// Draws unselected link-lines.
		/// </summary>
		private void DrawLinks()
		{
			RouteNode node;

			for (int
					rSrc = 0,
						x = Origin.X,
						y = Origin.Y;
					rSrc != _file.Rows;
					++rSrc,
						x -= HalfWidth,
						y += HalfHeight)
			{
				for (int
						cSrc = 0,
							xSrc = x,
							ySrc = y;
						cSrc != _file.Cols;
						++cSrc,
							xSrc += HalfWidth,
							ySrc += HalfHeight)
				{
					if ((node = _file.GetTile(cSrc, rSrc).Node) != null
						&& (NodeSelected == null || node != NodeSelected))
					{
						DrawLinkLines(xSrc, ySrc, node);
					}
				}
			}
		}

		/// <summary>
		/// Draws link-lines for a given <c><see cref="RouteNode"/></c>.
		/// </summary>
		/// <param name="xSrc"></param>
		/// <param name="ySrc"></param>
		/// <param name="node"></param>
		/// <param name="selected"></param>
		private void DrawLinkLines(
				int xSrc,
				int ySrc,
				RouteNode node,
				bool selected = false)
		{
			int xDst, yDst;
			RouteNode dest;
			byte destId;

			for (int slot = 0; slot != RouteNode.LinkSlots; ++slot)
			{
				var link = node[slot] as Link;
				if ((destId = link.Destination) != Link.NotUsed)
				{
					switch (destId)
					{
						case Link.ExitWest:
							if (node.Lev != _file.Level)
								continue;

							xDst = OffsetX + 1;
							yDst = OffsetY + 1;
							dest = null;
							break;

						case Link.ExitNorth:
							if (node.Lev != _file.Level)
								continue;

							xDst = Width - OffsetX * 2;
							yDst =         OffsetY + 1;
							dest = null;
							break;

						case Link.ExitEast:
							if (node.Lev != _file.Level)
								continue;

							xDst = Width  - OffsetX * 2;
							yDst = Height - OffsetY * 2;
							dest = null;
							break;

						case Link.ExitSouth:
							if (node.Lev != _file.Level)
								continue;

							xDst =          OffsetX + 1;
							yDst = Height - OffsetY * 2;
							dest = null;
							break;

						default:
							if ((dest = _file.Routes[destId]) == null
								|| dest.Lev != _file.Level
								|| (NodeSelected != null && dest == NodeSelected)
								|| RouteCheckService.OutsideBounds(dest, _file))
							{
								continue;
							}

							xDst = Origin.X + (dest.Col - dest.Row)     * HalfWidth;
							yDst = Origin.Y + (dest.Col + dest.Row + 1) * HalfHeight;
							break;
					}

					if (selected) // draw link-lines for a selected node ->
					{
						var pen = PenLinkSelected;

						if (_spot.X != -1)
						{
							if (dest != null)
							{
								if (   _spot.X != dest.Col
									|| _spot.Y != dest.Row)
								{
									pen = PenLink;
								}
							}
							else
							{
								switch (destId) // see RouteView.SpotGoDestination() for def'n of the following spot-positions
								{
									case Link.ExitNorth: if (_spot.X != -2) pen = PenLink; break;
									case Link.ExitEast:  if (_spot.X != -3) pen = PenLink; break;
									case Link.ExitSouth: if (_spot.X != -4) pen = PenLink; break;
									case Link.ExitWest:  if (_spot.X != -5) pen = PenLink; break;
								}
							}
						}
						_graphics.DrawLine(
										pen,
										xSrc, ySrc,
										xDst, yDst);
					}
					else // draw link-lines for a non-selected node ->
						_graphics.DrawLine(
										PenLink,
										xSrc, ySrc + HalfHeight, // unselected nodes need an offset
										xDst, yDst);
				}
			}
		}

		/// <summary>
		/// Draws the <c><see cref="RouteNode">RouteNodes</see></c>.
		/// </summary>
		private void DrawNodes()
		{
			BrushNode         = RouteBrushes[RouteViewOptionables.str_NodeColor];
			BrushNodeSpawn    = RouteBrushes[RouteViewOptionables.str_NodeSpawnColor];
			BrushNodeSelected = RouteBrushes[RouteViewOptionables.str_NodeSelectedColor];


			int startX = Origin.X;
			int startY = Origin.Y;

			RouteNode node, dest;
			Link link;

			for (int r = 0; r != _file.Rows; ++r)
			{
				for (int
						c = 0,
							x = startX,
							y = startY;
						c != _file.Cols;
						++c,
							x += HalfWidth,
							y += HalfHeight)
				{
					if ((node = _file.GetTile(c,r).Node) != null)	// NOTE: MapFile has the current level stored and uses
					{												// it to return only tiles on the correct level here.
						_nodeFill.Reset();
						_nodeFill.AddLine(
										x,             y,
										x + HalfWidth, y + HalfHeight);
						_nodeFill.AddLine(
										x + HalfWidth, y + HalfHeight,
										x,             y + HalfHeight * 2);
						_nodeFill.AddLine(
										x,             y + HalfHeight * 2,
										x - HalfWidth, y + HalfHeight);
						_nodeFill.CloseFigure();

						Brush brush;
						if (NodeSelected != null && _file.Level == NodeSelected.Lev
							&& c == NodeSelected.Col
							&& r == NodeSelected.Row)
						{
							brush = BrushNodeSelected;
						}
						else if (node.Spawn != SpawnWeight.None)
						{
							brush = BrushNodeSpawn;
						}
						else
							brush = BrushNode;

						_graphics.FillPath(brush, _nodeFill);


						for (int i = 0; i != RouteNode.LinkSlots; ++i) // check for and if applicable draw the up/down indicators.
						{
							if ((link = node[i]).IsNodelink()
								&& link.Destination < _file.Routes.Nodes.Count
								&& (dest = _file.Routes[link.Destination]) != null)
							{
								if (dest.Lev < _file.Level) // draw arrow up.
								{
									_graphics.DrawLine( // start w/ a vertical line in the tile-lozenge
													PenLink,
													x, y + 1,
													x, y - 1 + HalfHeight * 2);
									_graphics.DrawLine( // then lines on the two top edges of the tile
													PenLink,
													x + 1,             y + 1,
													x + 3 - HalfWidth, y + 0 + HalfHeight);
									_graphics.DrawLine(
													PenLink,
													x - 1,             y + 1,
													x - 3 + HalfWidth, y + 0 + HalfHeight);
								}
								else if (dest.Lev > _file.Level) // draw arrow down.
								{
									_graphics.DrawLine( // start w/ a horizontal line in the tile-lozenge
													PenLink,
													x + 2 - HalfWidth, y + HalfHeight,
													x - 2 + HalfWidth, y + HalfHeight);
									_graphics.DrawLine( // then lines on the two bottom edges of the tile
													PenLink,
													x + 1,             y - 1 + HalfHeight * 2,
													x + 3 - HalfWidth, y - 0 + HalfHeight);
									_graphics.DrawLine(
													PenLink,
													x - 1,             y - 1 + HalfHeight * 2,
													x - 3 + HalfWidth, y - 0 + HalfHeight);
								}
							}
						}
					}
				}
				startX -= HalfWidth;
				startY += HalfHeight;
			}
		}

		/// <summary>
		/// Draws the grid-lines.
		/// </summary>
		private void DrawGridLines()
		{
			Pen pen;
			for (int i = 0; i <= _file.Rows; ++i)
			{
				if (i % 10 != 0) pen = RoutePens[RouteViewOptionables.str_GridLineColor];
				else             pen = RoutePens[RouteViewOptionables.str_GridLine10Color];

				_graphics.DrawLine(
								pen,
								Origin.X - i * HalfWidth,
								Origin.Y + i * HalfHeight,
								Origin.X + (_file.Cols - i) * HalfWidth,
								Origin.Y + (_file.Cols + i) * HalfHeight);
			}

			for (int i = 0; i <= _file.Cols; ++i)
			{
				if (i % 10 != 0) pen = RoutePens[RouteViewOptionables.str_GridLineColor];
				else             pen = RoutePens[RouteViewOptionables.str_GridLine10Color];

				_graphics.DrawLine(
								pen,
								Origin.X + i * HalfWidth,
								Origin.Y + i * HalfHeight,
							   (Origin.X + i * HalfWidth)  - _file.Rows * HalfWidth,
							   (Origin.Y + i * HalfHeight) + _file.Rows * HalfHeight);
			}
		}

		/// <summary>
		/// Draws the node importance bars.
		/// </summary>
		private void DrawNodeMeters()
		{
			int startX = Origin.X;
			int startY = Origin.Y;

			RouteNode node;

			for (int r = 0; r != _file.Rows; ++r)
			{
				for (int
						c = 0,
							x = startX,
							y = startY;
						c != _file.Cols;
						++c,
							x += HalfWidth,
							y += HalfHeight)
				{
					if ((node = _file.GetTile(c,r).Node) != null)
					{
						int infoboxX = x - HalfWidth / 2 - 2;				// -2 to prevent drawing over the link-going-up vertical
						int infoboxY = y + HalfHeight - NodeValHeight / 2;	//    line indicator when panel-size is fairly small.

						DrawNodeMeter(infoboxX,     infoboxY, (int)node.Spawn,  Brushes.LightCoral);
						DrawNodeMeter(infoboxX + 3, infoboxY, (int)node.Patrol, Brushes.DeepSkyBlue);
					}
				}
				startX -= HalfWidth;
				startY += HalfHeight;
			}
		}

		/// <summary>
		/// Helper for <c><see cref="DrawNodeMeters()">DrawNodeMeters()</see></c>.
		/// </summary>
		/// <param name="infoboxX"></param>
		/// <param name="infoboxY"></param>
		/// <param name="value"></param>
		/// <param name="color"></param>
		private void DrawNodeMeter(
				int infoboxX,
				int infoboxY,
				int value,
				Brush color)
		{
			var p0 = new Point(
							infoboxX - 1, // ...
							infoboxY);
			var p1 = new Point(
							infoboxX + 3,
							infoboxY);
			var p2 = new Point(
							infoboxX + 3,
							infoboxY + NodeValHeight - 1);
			var p3 = new Point(
							infoboxX,
							infoboxY + NodeValHeight - 1);
			var p4 = new Point(
							infoboxX,
							infoboxY);

			using (var path = new GraphicsPath())
			{
				path.AddLine(p0, p1);
				path.AddLine(p1, p2);
				path.AddLine(p2, p3);
				path.AddLine(p3, p4);

				_graphics.FillPath(Brushes.WhiteSmoke, path); // fill background.

				if (value > 0)
					_graphics.FillRectangle(
										color,
										infoboxX, infoboxY + NodeValHeight - value - 2,
										2, value);

				_graphics.DrawPath(Pens.Black, path); // draw borders.
			}
		}

		/// <summary>
		/// Draws the compass-rose.
		/// </summary>
		private void DrawRose()
		{
			_graphics.DrawString(
							TextWest,
							FontRose,
							Brushes.Black,
							RoseMarginX,
							RoseMarginY);
			_graphics.DrawString(
							TextNorth,
							FontRose,
							Brushes.Black,
							Width - _widthNorth - RoseMarginX,
							RoseMarginY);
			_graphics.DrawString(
							TextSouth,
							FontRose,
							Brushes.Black,
							RoseMarginX,
							Height - FontRose.Height - RoseMarginY);
			_graphics.DrawString(
							TextEast,
							FontRose,
							Brushes.Black,
							Width - _widthEast - RoseMarginX,
							Height - FontRose.Height - RoseMarginY);
		}

		/// <summary>
		/// Draws tile/node information in the overlay.
		/// </summary>
		private void DrawInfoOverlay()
		{
			MapTile tile = _file.GetTile(_col, _row);
			if (tile != null) // safety.
			{
				string textLoc = Globals.GetLocationString(
														_col,
														_row,
														_file.Level,
														_file.Levs);

				int textWidth = (int)_graphics.MeasureString(textLoc, FontOverlay).Width;

				string
					textOver   = null,
					textUnit   = null,
					textRank   = null,
					textSpawn  = null,
					textPatrol = null,
					textAttack = null;

				RouteNode node = tile.Node;
				if (node != null)
				{
					textOver = node.Id.ToString();
					textUnit = Enum.GetName(typeof(UnitType), node.Unit);

					if (_file.Descriptor.GroupType == GameType.Tftd)
						textRank = RouteNodes.RankTftd[node.Rank].ToString();
					else
						textRank = RouteNodes.RankUfo [node.Rank].ToString();

					textSpawn  = RouteNodes.Spawn [(byte)node.Spawn] .ToString();
					textPatrol = RouteNodes.Patrol[(byte)node.Patrol].ToString();

					int w;

					w = (int)_graphics.MeasureString(textOver,   FontOverlay).Width;
					if (w > textWidth) textWidth = w;

					w = (int)_graphics.MeasureString(textUnit,   FontOverlay).Width;
					if (w > textWidth) textWidth = w;

					w = (int)_graphics.MeasureString(textRank,   FontOverlay).Width;
					if (w > textWidth) textWidth = w;

					w = (int)_graphics.MeasureString(textSpawn,  FontOverlay).Width;
					if (w > textWidth) textWidth = w;

					w = (int)_graphics.MeasureString(textPatrol, FontOverlay).Width;
					if (w > textWidth) textWidth = w;

					if (node.Attack != 0)
					{
						textAttack = RouteNodes.Attack[(byte)node.Attack].ToString();

						w = (int)_graphics.MeasureString(textAttack, FontOverlay).Width;
						if (w > textWidth) textWidth = w;
					}

					// time to move to a higher .NET framework.
				}

				var rect = new Rectangle(
									_over.X + 18, _over.Y,
									OverlayColPad + textWidth + 5, _heightoverlaytext + 7);

				if (node != null)
				{
					rect.Width  += _widthoverlayleft;
					rect.Height += _heightoverlaytext * 5;

					if (node.Attack != 0)
						rect.Height += _heightoverlaytext;
				}

				if (RouteView.Optionables.ReduceDraws)
				{
					rect.X = Origin.X + (_col * HalfWidth)  - (_row * HalfHeight * 2); // heh nailed it.
					rect.Y = Origin.Y + (_row * HalfHeight) + (_col * HalfWidth  / 2);

					rect.X += HalfWidth;
					rect.Y += HalfHeight / 2;

					if (rect.X + rect.Width > ClientRectangle.Width)
						rect.X -= rect.Width + HalfWidth * 2;
	
					if (rect.Y + rect.Height > ClientRectangle.Height)
						rect.Y -= rect.Height;
				}
				else
				{
					if (rect.X + rect.Width > ClientRectangle.Width)
						rect.X = _over.X - rect.Width - 8;
	
					if (rect.Y + rect.Height > ClientRectangle.Height)
						rect.Y = _over.Y - rect.Height;
				}

				_graphics.FillRectangle(BrushOverlayBlue, rect);
				_graphics.FillRectangle(
									BrushOverlayLight,
									rect.X + 2,
									rect.Y + 2,
									rect.Width  - 4,
									rect.Height - 4);

				int textLeft = rect.X + 4;
				int textTop  = rect.Y + 3;

				int colRight = textLeft + OverlayColPad;
				if (node != null) colRight += _widthoverlayleft;

				_graphics.DrawString(textLoc, FontOverlay, Brushes.Yellow, colRight, textTop);

				if (node != null)
				{
					_graphics.DrawString(Over,       FontOverlay, Brushes.Yellow, textLeft, textTop + _heightoverlaytext);
					_graphics.DrawString(textOver,   FontOverlay, Brushes.Yellow, colRight, textTop + _heightoverlaytext);

					_graphics.DrawString(Unit,       FontOverlay, Brushes.Yellow, textLeft, textTop + _heightoverlaytext * 2);
					_graphics.DrawString(textUnit,   FontOverlay, Brushes.Yellow, colRight, textTop + _heightoverlaytext * 2);

					_graphics.DrawString(Rank,       FontOverlay, Brushes.Yellow, textLeft, textTop + _heightoverlaytext * 3);
					_graphics.DrawString(textRank,   FontOverlay, Brushes.Yellow, colRight, textTop + _heightoverlaytext * 3);

					_graphics.DrawString(Spawn,      FontOverlay, Brushes.Yellow, textLeft, textTop + _heightoverlaytext * 4);
					_graphics.DrawString(textSpawn,  FontOverlay, Brushes.Yellow, colRight, textTop + _heightoverlaytext * 4);

					_graphics.DrawString(Patrol,     FontOverlay, Brushes.Yellow, textLeft, textTop + _heightoverlaytext * 5);
					_graphics.DrawString(textPatrol, FontOverlay, Brushes.Yellow, colRight, textTop + _heightoverlaytext * 5);

					if (node.Attack != 0)
					{
						_graphics.DrawString(Attack,     FontOverlay, Brushes.Yellow, textLeft, textTop + _heightoverlaytext * 6);
						_graphics.DrawString(textAttack, FontOverlay, Brushes.Yellow, colRight, textTop + _heightoverlaytext * 6);
					}
				}
			}
		}
		#endregion Methods (draw)


		#region Methods (path)
		/// <summary>
		/// Sets the graphics-path for a lozenge-border around the tile that
		/// is currently mouse-overed.
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <returns></returns>
		private void PathSelectorLozenge(int x, int y)
		{
			int halfWidth  = BlobService.HalfWidth;
			int halfHeight = BlobService.HalfHeight;

			var p0 = new Point(x,             y);
			var p1 = new Point(x + halfWidth, y + halfHeight);
			var p2 = new Point(x,             y + halfHeight * 2);
			var p3 = new Point(x - halfWidth, y + halfHeight);

			_lozSelector.Reset();
			_lozSelector.AddLine(p0, p1);
			_lozSelector.AddLine(p1, p2);
			_lozSelector.AddLine(p2, p3);
			_lozSelector.CloseFigure();
		}

		/// <summary>
		/// Sets the graphics-path for a lozenge-border around all tiles that
		/// are selected or being selected.
		/// </summary>
		private void PathSelectedLozenge()
		{
			Point a = MainViewOverlay.that.GetDragBeg_abs();
			Point b = MainViewOverlay.that.GetDragEnd_abs();

			int halfWidth  = BlobService.HalfWidth;
			int halfHeight = BlobService.HalfHeight;

			var p0 = new Point(
							Origin.X + (a.X - a.Y) * halfWidth,
							Origin.Y + (a.X + a.Y) * halfHeight);
			var p1 = new Point(
							Origin.X + (b.X - a.Y) * halfWidth  + halfWidth,
							Origin.Y + (b.X + a.Y) * halfHeight + halfHeight);
			var p2 = new Point(
							Origin.X + (b.X - b.Y) * halfWidth,
							Origin.Y + (b.X + b.Y) * halfHeight + halfHeight * 2);
			var p3 = new Point(
							Origin.X + (a.X - b.Y) * halfWidth  - halfWidth,
							Origin.Y + (a.X + b.Y) * halfHeight + halfHeight);

			_lozSelected.Reset();
			_lozSelected.AddLine(p0, p1);
			_lozSelected.AddLine(p1, p2);
			_lozSelected.AddLine(p2, p3);
			_lozSelected.CloseFigure();

			Refresh(); // fast update for drag-selection.
		}

		/// <summary>
		/// Sets the graphics-path for a lozenge-border around a "spotted" node
		/// - a node the linkslot to which is currently overed.
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		private void PathSpottedLozenge(int x, int y)
		{
			int halfWidth  = BlobService.HalfWidth;
			int halfHeight = BlobService.HalfHeight;

			var p0 = new Point(x,             y);
			var p1 = new Point(x + halfWidth, y + halfHeight);
			var p2 = new Point(x,             y + halfHeight * 2);
			var p3 = new Point(x - halfWidth, y + halfHeight);

			_lozSpotted.Reset();
			_lozSpotted.AddLine(p0, p1);
			_lozSpotted.AddLine(p1, p2);
			_lozSpotted.AddLine(p2, p3);
			_lozSpotted.CloseFigure();
		}
		#endregion Methods (path)
	}
}
