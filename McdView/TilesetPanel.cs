﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Windows.Forms;

using XCom;
using XCom.Interfaces;


namespace McdView
{
	/// <summary>
	/// The panel that displays the entire MCD recordset with each record's
	/// Sprite1 sprite.
	/// </summary>
	internal sealed class TilesetPanel
		:
			Panel
	{
		#region Fields (static)
		private readonly static Brush BrushHilightsub = new SolidBrush(Color.FromArgb(33, SystemColors.MenuHighlight));
		#endregion Fields (static)


		#region Fields
		private readonly McdviewF _f;
		private readonly HScrollBar Scroller = new HScrollBar();

		private int TableWidth;
		private const int _largeChange = XCImage.SpriteWidth32 + 1;

		private readonly Pen   _penControl   = new Pen(SystemColors.Control, 1);
		private readonly Brush _brushControl = new SolidBrush(SystemColors.Control);

		private bool _bypassScrollZero;

		private readonly List<int>      SubSelIds  = new List<int>();
		private readonly List<Tilepart> _copyparts = new List<Tilepart>();
		#endregion Fields


		#region Properties
		private Tilepart[] _parts;
		internal Tilepart[] Parts
		{
			private get { return _parts; }
			set // IMPORTANT: Set 'Parts' via McdviewF only.
			{
				_parts = value;

				TableWidth = _parts.Length * (XCImage.SpriteWidth32 + 1) - 1;

				OnResize(null);

				if (!_bypassScrollZero)
					Scroller.Value = 0;
				else
					_bypassScrollZero = false;

				Invalidate();
			}
		}

		private ContextMenu Context
		{ get; set; }
		#endregion Properties


		#region cTor
		internal TilesetPanel(McdviewF f)
		{
			_f = f;

			SetStyle(ControlStyles.OptimizedDoubleBuffer
				   | ControlStyles.AllPaintingInWmPaint
				   | ControlStyles.UserPaint
				   | ControlStyles.ResizeRedraw
				   | ControlStyles.Selectable, true);

			Anchor = (AnchorStyles)(AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right);
			BackColor = SystemColors.Desktop;
			Margin = new Padding(0);
			Name = "pnl_Collection";
			TabIndex = 0;
			TabStop = true;

			Location = new Point(5, 15);

			Scroller.Dock = DockStyle.Bottom;
			Scroller.LargeChange = _largeChange;
			Scroller.ValueChanged += OnValueChanged_Scroll;
			Controls.Add(Scroller);

			Height = y3_sprite + XCImage.SpriteHeight40 + Scroller.Height;

			CreateContext();
		}

		/// <summary>
		/// Builds and assigns an RMB context-menu.
		/// </summary>
		private void CreateContext()
		{
			var itAdd         = new MenuItem("add",           OnAddClick);
			var itAddRange    = new MenuItem("add range ...", OnAddRangeClick);

			var itSep0        = new MenuItem("-");

			var itCut         = new MenuItem("cut",           OnCutClick);
			var itCopy        = new MenuItem("copy",          OnCopyClick);
			var itPaste       = new MenuItem("paste",         OnPasteClick);
			var itDelete      = new MenuItem("delete",        OnDeleteClick);

			var itSep1        = new MenuItem("-");

			var itFile        = new MenuItem("file ...",      OnFileClick);

			var itSep2        = new MenuItem("-");

			var itLeft        = new MenuItem("left",          OnLeftClick);
			var itRight       = new MenuItem("right",         OnRightClick);

			var itSep3        = new MenuItem("-");

			var itDeselect    = new MenuItem("deselect",      OnDeselectClick);

			Context = new ContextMenu();
			Context.MenuItems.AddRange(new []
										{
											itAdd,			//  0
											itAddRange,		//  1
											itSep0,			//  2
											itCut,			//  3
											itCopy,			//  4
											itPaste,		//  5
											itDelete,		//  6
											itSep1,			//  7
											itFile,			//  8
											itSep2,			//  9
											itLeft,			// 10
											itRight,		// 11
											itSep3,			// 12
											itDeselect		// 13
										});
			ContextMenu = Context;

			Context.Popup += OnPopup_Context;
		}
		#endregion cTor


		#region Events (context)
		/// <summary>
		/// Determines which contextmenu commands are enabled when the menu
		/// is opened.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void OnPopup_Context(object sender, EventArgs e)
		{
			bool selid = (_f.SelId != -1);

			Context.MenuItems[0].Enabled = Parts != null;							// add
			Context.MenuItems[1].Enabled = Parts != null;							// add range

			Context.MenuItems[3].Enabled = selid;									// cut
			Context.MenuItems[4].Enabled = selid;									// copy
			Context.MenuItems[5].Enabled = selid && _copyparts.Count != 0;			// paste
			Context.MenuItems[6].Enabled = selid;									// delete

			Context.MenuItems[8].Enabled = (Parts != null && false);				// file

			Context.MenuItems[10].Enabled = _f.SelId > 0;							// left
			Context.MenuItems[11].Enabled = selid && _f.SelId != Parts.Length - 1;	// right

			Context.MenuItems[13].Enabled = selid;									// deselect
		}

		/// <summary>
		/// Closes the contextmenu.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void OnIdClick(object sender, EventArgs e)
		{
			Context.Dispose();
		}

		/// <summary>
		/// Adds a blank part to the parts-array.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void OnAddClick(object sender, EventArgs e)
		{
			_f.Changed = true;

			var array = new Tilepart[Parts.Length + 1];

			int id = _f.SelId + 1;
			for (int i = 0; i != id; ++i)
				array[i] = Parts[i];

			McdRecord record = McdRecordFactory.CreateRecord();

			array[id] = new Tilepart(
									id,
									_f.Spriteset,
									record);
			array[id].Dead      =
			array[id].Alternate = null;

			for (int i = id + 1; i != array.Length; ++i)
			{
				array[i] = Parts[i - 1];
				array[i].TerId = i; // not used in McdView but keep things consistent ....
			}

			_bypassScrollZero = true;
			_f.Parts = array; // assign back to 'Parts' via McdviewF

			ShiftRefs(id, 1);

			SubSelIds.Clear();
			_f.SelId = id;
		}

		internal static int _add;
		private void OnAddRangeClick(object sender, EventArgs e)
		{
			using (var ari = new AddRangeInput())
			{
				if (ari.ShowDialog() == DialogResult.OK)
				{
					if (_add != 0) // input allows 0 but not neg
					{
						_f.Changed = true;

						int length = Parts.Length + _add;
						var array = new Tilepart[length];

						int id = _f.SelId + 1;
						int i;
						for (i = 0; i != id; ++i)
							array[i] = Parts[i];

						McdRecord record;
						int stop = i + _add;
						for (; i != stop; ++i)
						{
							record = McdRecordFactory.CreateRecord();

							array[i] = new Tilepart(
												i,
												_f.Spriteset,
												record);
							array[i].Dead =
							array[i].Alternate = null;
						}

						for (; i != length; ++i)
						{
							array[i] = Parts[i - _add];
							array[i].TerId = i;
						}

						_bypassScrollZero = true;
						_f.Parts = array;

						ShiftRefs(id, _add);

						SubSelIds.Clear();
						_f.SelId = id;
					}
				}
			}
		}

		/// <summary>
		/// Cuts a currently selected part from the parts-array.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void OnCutClick(object sender, EventArgs e)
		{
			OnCopyClick(  null, EventArgs.Empty);
			OnDeleteClick(null, EventArgs.Empty);
		}

		/// <summary>
		/// Copies a currently selected part along with any sub-selected parts
		/// to the copy-array.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void OnCopyClick(object sender, EventArgs e)
		{
			_copyparts.Clear();

			_copyparts.Add(Parts[_f.SelId].Clone());

			foreach (int i in SubSelIds)
				_copyparts.Add(Parts[i].Clone());
		}

		/// <summary>
		/// Pastes the copy-array into the parts-array, overwriting the
		/// currently selected part. Ergo if user wants to insert the copyparts
		/// (without overwriting any part) a blank-part shall be added and
		/// selected to be overwritten before pasting.
		/// NOTE: If a pasted part's refs are less than the insertion point then
		/// the refs are okay; if the refs are equal to or greater than the
		/// insertion point then the refs need to be advanced. This assumes that
		/// the pasted parts are from the same recordset; if not then the refs
		/// ought be deleted.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void OnPasteClick(object sender, EventArgs e)
		{
			_f.Changed = true;

			Parts[_f.SelId] = _copyparts[0].Clone();
			Parts[_f.SelId].TerId = _f.SelId;

			Parts[_f.SelId].Record.DieTile = (byte)0;
			Parts[_f.SelId].Dead = null;

			Parts[_f.SelId].Record.Alt_MCD = (byte)0;
			Parts[_f.SelId].Alternate = null;

			if (_copyparts.Count > 1)
			{
				var array = new Tilepart[Parts.Length + (_copyparts.Count - 1)];

				for (int i = 0, j = 0; i != array.Length; ++i, ++j)
				{
					if (i == _f.SelId + 1)
					{
						for (int id = 1; id != _copyparts.Count; ++id, ++i)
						{
							array[i] = _copyparts[id].Clone();
							array[i].TerId = i;

							array[i].Record.DieTile = (byte)0;
							array[i].Dead = null;

							array[i].Record.Alt_MCD = (byte)0;
							array[i].Alternate = null;
						}
					}

					if (i == array.Length)
						break;

					array[i] = Parts[j];
					array[i].TerId = i;
				}

				_f.Parts = array;
			}

			ShiftRefs(_f.SelId, _copyparts.Count - 1); // is overwriting the first ID, then inserting the rest.

			_f.InvalidatePanels();
			_f.PopulateTextFields();
		}

		/// <summary>
		/// Updates refs when parts are added.
		/// Shifts references to death- and alternate-parts by a given amount
		/// starting at references at or greater than a given ID.
		/// </summary>
		/// <param name="start"></param>
		/// <param name="shift"></param>
		private void ShiftRefs(int start, int shift)
		{
			if (start + shift != Parts.Length)
			{
				Tilepart part;
				McdRecord record;

				int id;

				for (int i = 0; i != Parts.Length; ++i)
				{
					part   = Parts[i];
					record = part.Record;

					if ((id = record.DieTile) != 0 && id >= start)
					{
						if ((id = record.DieTile + shift) < Parts.Length)
						{
							record.DieTile = (byte)id;
							part.Dead = Parts[id];
						}
						else
						{
							record.DieTile = (byte)0;
							part.Dead = null;
						}
					}

					if ((id = record.Alt_MCD) != 0 && id >= start)
					{
						if ((id = record.Alt_MCD + shift) < Parts.Length)
						{
							record.Alt_MCD = (byte)id;
							part.Alternate = Parts[id];
						}
						else
						{
							record.Alt_MCD = (byte)0;
							part.Alternate = null;
						}
					}
				}
			}
		}

		/// <summary>
		/// Deletes a currently selected part 'SelId' and any sub-selected parts
		/// 'SubSelIds'.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void OnDeleteClick(object sender, EventArgs e)
		{
			_f.Changed = true;

			var array = new Tilepart[Parts.Length - (1 + SubSelIds.Count)];

			for (int i = 0, j = 0; i != Parts.Length; ++i)
			{
				if (i == _f.SelId || SubSelIds.Contains(i))
				{
					++j;
				}
				else
				{
					array[i - j] = Parts[i];
					array[i - j].TerId = i - j;
				}
			}

			var sels = new List<int>();
			sels.Add(_f.SelId);
			foreach (int sel in SubSelIds)
				sels.Add(sel);

			SubSelIds.Clear();
			_f.SelId = -1;

			_bypassScrollZero = true;
			_f.Parts = array;

			for (int id = 0; id != sels.Count; ++id)
				ClearRefs(sels[id]);

			UpdateRefs(sels);
//			Invalidate(); // not needed apparently.
		}

		/// <summary>
		/// Nulls all death- and alternate-references to a given part.
		/// </summary>
		/// <param name="id"></param>
		private void ClearRefs(int id)
		{
			if (id != 0) // ie. DeathId or AlternateId is not already null-part.
			{
				Tilepart part;
				McdRecord record;

				for (int i = 0; i != Parts.Length; ++i)
				{
					part   = Parts[i];
					record = part.Record;

					if (record.DieTile == id)
					{
						record.DieTile = (byte)0;
						part.Dead = null;
					}

					if (record.Alt_MCD == id)
					{
						record.Alt_MCD = (byte)0;
						part.Alternate = null;
					}
				}
			}
		}

		/// <summary>
		/// Updates refs when a part or parts get deleted.
		/// </summary>
		/// <param name ="sels">a list of IDs that got deleted</param>
		private void UpdateRefs(List<int> sels)
		{
			sels.Sort();

			Tilepart part;
			McdRecord record;

			int pos, id;

			for (int i = 0; i != Parts.Length; ++i)
			{
				part   = Parts[i];
				record = part.Record;

				pos = sels.Count - 1; // start with the last entry in 'sels'

				if ((id = record.DieTile) != 0)// && (pos = sels.FindIndex(val => val == @ref)) != -1)
				{
					while (pos != -1 && id > sels[pos--])
						--id;

					if (id != record.DieTile)
					{
						record.DieTile = (byte)id;
						part.Dead = Parts[id];
					}
				}

				pos = sels.Count - 1;

				if ((id = record.Alt_MCD) != 0)// && (pos = sels.FindIndex(val => val == @ref)) != -1)
				{
					while (pos != -1 && id > sels[pos--])
						--id;

					if (id != record.Alt_MCD)
					{
						record.Alt_MCD = (byte)id;
						part.Alternate = Parts[id];
					}
				}
			}
		}

		private void OnFileClick(object sender, EventArgs e)
		{
		}

		/// <summary>
		/// Swaps a part with the part to its left.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void OnLeftClick(object sender, EventArgs e)
		{
			_f.Changed = true;

			var array = new Tilepart[Parts.Length];

			int id = _f.SelId;
			for (int i = 0; i != id - 1; ++i)
				array[i] = Parts[i];

			array[id - 1] = Parts[id];
			array[id - 1].TerId = id - 1;

			array[id] = Parts[id - 1];
			array[id].TerId = id;

			for (int i = id + 1; i != Parts.Length; ++i)
				array[i] = Parts[i];

			_bypassScrollZero = true;
			_f.Parts = array;

			SwapRefs(id, id - 1);

			_f.SelId = id - 1; // does refresh.
		}

		/// <summary>
		/// Swaps a part with the part to its right.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void OnRightClick(object sender, EventArgs e)
		{
			_f.Changed = true;

			var array = new Tilepart[Parts.Length];

			int id = _f.SelId;
			for (int i = 0; i != id; ++i)
				array[i] = Parts[i];

			array[id] = Parts[id + 1];
			array[id].TerId = id;

			array[id + 1] = Parts[id];
			array[id + 1].TerId = id + 1;

			for (int i = id + 2; i != Parts.Length; ++i)
				array[i] = Parts[i];

			_bypassScrollZero = true;
			_f.Parts = array;

			SwapRefs(id, id + 1);

			_f.SelId = id + 1; // does refresh.
		}

		/// <summary>
		/// Swaps two death- and alternate-references. Any references that point
		/// to 'a' point to 'b' and vice versa.
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		private void SwapRefs(int a, int b)
		{
			Tilepart part;
			McdRecord record;

			int id;

			for (int i = 0; i != Parts.Length; ++i)
			{
				part   = Parts[i];
				record = part.Record;

				if ((id = record.DieTile) != 0)
				{
					if (id == a)
					{
						record.DieTile = (byte)b;
						part.Dead = Parts[b];
					}
					else if (id == b)
					{
						record.DieTile = (byte)a;
						part.Dead = Parts[a];
					}
				}

				if ((id = record.Alt_MCD) != 0)
				{
					if (id == a)
					{
						record.Alt_MCD = (byte)b;
						part.Alternate = Parts[b];
					}
					else if (id == b)
					{
						record.Alt_MCD = (byte)a;
						part.Alternate = Parts[a];
					}
				}
			}
		}

		/// <summary>
		/// Deselects a currently selected part as well as any sub-selected
		/// parts.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void OnDeselectClick(object sender, EventArgs e)
		{
			_f.SelId = -1;
			SubSelIds.Clear();
		}
		#endregion Events (context)


		#region Events
		/// <summary>
		/// Refreshes the PartsPanel when the scrollbar's value changes.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void OnValueChanged_Scroll(object sender, EventArgs e)
		{
			Invalidate();
		}
		#endregion Events


		#region Events (override)
		Graphics _graphics;
		ImageAttributes _attri;

		// constants for vertical align ->
		const int y1_sprite = 0;
		const int y1_fill   = XCImage.SpriteHeight40;
		const int y1_fill_h = 18;
		const int y2_sprite = y1_fill + y1_fill_h;
		const int y2_line   = y2_sprite + XCImage.SpriteHeight40 + 1;
		const int y3_sprite = y2_line;

		/// <summary>
		/// Paints this TilesetPanel.
		/// </summary>
		/// <param name="e"></param>
		protected override void OnPaint(PaintEventArgs e)
		{
			if (Parts != null && Parts.Length != 0)
			{
				_graphics = e.Graphics;
				_graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

				_attri = new ImageAttributes();
				if (_f._spriteShadeEnabled)
					_attri.SetGamma(_f.SpriteShadeFloat, ColorAdjustType.Bitmap);

				Bitmap sprite;

				Rectangle rect;

				int offset = -Scroller.Value;

				int i;
				for (i = 0; i != Parts.Length; ++i)
				{
					if (i != 0)
						_graphics.DrawLine( // draw vertical line before each sprite except the first sprite
										_penControl,
										i * XCImage.SpriteWidth32 + i + offset, 0,
										i * XCImage.SpriteWidth32 + i + offset, Height);

					if (_f.Spriteset != null
						&& Parts[i] != null // not sure why Tilepart entries go null but aren't null but they do
						&& Parts[i].Sprites != null
						&& Parts[i][0] != null
						&& (sprite = Parts[i][0].Sprite) != null)
					{
						DrawSprite(
								sprite,
								i * XCImage.SpriteWidth32 + i + offset,
								y1_sprite - Parts[i].Record.TileOffset);
					}
				}

				_graphics.FillRectangle(
									_brushControl,
									0,     y1_fill,
									Width, y1_fill_h);

				if (_f.SelId != -1)
				{
					_graphics.FillRectangle(
										McdviewF.BrushHilight,
										_f.SelId * (XCImage.SpriteWidth32 + 1) + offset,
										y1_fill,
										XCImage.SpriteWidth32,
										y1_fill_h);

					foreach (int id in SubSelIds)
						_graphics.FillRectangle(
											BrushHilightsub,
											id * (XCImage.SpriteWidth32 + 1) + offset,
											y1_fill,
											XCImage.SpriteWidth32,
											y1_fill_h);
				}

				for (i = 0; i != Parts.Length; ++i)
				{
					rect = new Rectangle(
									i * XCImage.SpriteWidth32 + i + offset,
									y1_fill,
									XCImage.SpriteWidth32,
									y1_fill_h);

					TextRenderer.DrawText(
										_graphics,
										i.ToString(),
										Font,
										rect,
										SystemColors.ControlText,
										McdviewF.FLAGS);
				}

				if (_f.Spriteset != null)
				{
					for (i = 0; i != Parts.Length; ++i) // dead part ->
					{
						if (Parts[i] != null
							&& Parts[i].Dead != null
							&& Parts[i].Dead.Sprites != null
							&& Parts[i].Dead[0] != null
							&& (sprite = Parts[i].Dead[0].Sprite) != null)
						{
							DrawSprite(
									sprite,
									i * XCImage.SpriteWidth32 + i + offset,
									y2_sprite - Parts[i].Dead.Record.TileOffset);
						}
					}
				}

				_graphics.DrawLine(
								_penControl,
								0,     y2_line,
								Width, y2_line);

				if (_f.Spriteset != null)
				{
					for (i = 0; i != Parts.Length; ++i) // alternate part ->
					{
						if (Parts[i] != null
							&& Parts[i].Alternate != null
							&& Parts[i].Alternate.Sprites != null
							&& Parts[i].Alternate[0] != null
							&& (sprite = Parts[i].Alternate[0].Sprite) != null)
						{
							DrawSprite(
									sprite,
									i * XCImage.SpriteWidth32 + i + offset,
									y3_sprite - Parts[i].Alternate.Record.TileOffset);
						}
					}
				}
			}
		}

		/// <summary>
		/// Helper for OnPaint().
		/// </summary>
		/// <param name="sprite"></param>
		/// <param name="x"></param>
		/// <param name="y"></param>
		private void DrawSprite(
				Image sprite,
				int x,
				int y)
		{
			_graphics.DrawImage(
							sprite,
							new Rectangle(
										x, y,
										XCImage.SpriteWidth32,
										XCImage.SpriteHeight40),
							0, 0, XCImage.SpriteWidth32, XCImage.SpriteHeight40,
							GraphicsUnit.Pixel,
							_attri);
		}


		/// <summary>
		/// Handles client resizing. Sets the scrollbar's Enabled and Maximum
		/// values.
		/// @note Holy f*ck I hate .NET scrollbars.
		/// </summary>
		/// <param name="eventargs"></param>
		protected override void OnResize(EventArgs eventargs)
		{
			if (eventargs != null) // ie. is *not* Parts load
				base.OnResize(eventargs);

			int range = 0;
			if (Parts != null && Parts.Length != 0)
			{
				range = TableWidth + (_largeChange - 1) - Width;
				if (range < _largeChange)
					range = 0;
			}

			Scroller.Maximum =  range;
			Scroller.Enabled = (range != 0);

			if (Scroller.Enabled
				&& TableWidth < Width + Scroller.Value)
			{
				Scroller.Value = TableWidth - Width;
			}
			ScrollTile();
		}


		/// <summary>
		/// @note If user has the openfile dialog open and double-clicks to open
		/// a file that happens to be over the panel a mouse-up event fires. So
		/// use MouseDown here.
		/// </summary>
		/// <param name="e"></param>
		protected override void OnMouseDown(MouseEventArgs e)
		{
			Select();

			if (e.Button == MouseButtons.Left
				&& Parts != null && Parts.Length != 0
				&& e.Y < Height - Scroller.Height)
			{
				int id = (e.X + Scroller.Value) / (XCImage.SpriteWidth32 + 1);
				if (id >= Parts.Length)
				{
					SubSelIds.Clear();
					_f.SelId = -1;
				}
				else if (id != _f.SelId)
				{
					if (ModifierKeys == Keys.Control)
					{
						if (!SubSelIds.Contains(id))
						{
							SubSelIds.Add(id);
							SubSelIds.Sort();
						}
						else
							SubSelIds.Remove(id);

						Invalidate();
					}
					else if (ModifierKeys == Keys.Shift)
					{
						if (id < _f.SelId)
						{
							for (int i = id; i != _f.SelId; ++i)
							{
								if (!SubSelIds.Contains(i))
								{
									SubSelIds.Add(i);
									SubSelIds.Sort();
									Invalidate();
								}
							}
						}
						else
						{
							for (int i = id; i != _f.SelId; --i)
							{
								if (!SubSelIds.Contains(i))
								{
									SubSelIds.Add(i);
									SubSelIds.Sort();
									Invalidate();
								}
							}
						}
					}
					else
					{
						SubSelIds.Clear();
						_f.SelId = id;
					}
				}
				else
				{
					SubSelIds.Clear();
					Invalidate();
				}
			}
		}

		/// <summary>
		/// Scrolls the table by the mousewheel.
		/// </summary>
		/// <param name="e"></param>
		protected override void OnMouseWheel(MouseEventArgs e)
		{
			if (Scroller.Enabled)
			{
				if (e.Delta > 0)
				{
					if (Scroller.Value - Scroller.LargeChange < 0)
						Scroller.Value = 0;
					else
						Scroller.Value -= Scroller.LargeChange;
				}
				else if (e.Delta < 0)
				{
					if (Scroller.Value + Scroller.LargeChange + (Scroller.LargeChange - 1) > Scroller.Maximum)
						Scroller.Value = Scroller.Maximum - (Scroller.LargeChange - 1);
					else
						Scroller.Value += Scroller.LargeChange;
				}
			}
		}

		/// <summary>
		/// This is required in order to accept arrow-keyboard-input via
		/// McdviewF.OnKeyDown().
		/// </summary>
		/// <param name="keyData"></param>
		/// <returns></returns>
		protected override bool IsInputKey(Keys keyData)
		{
			switch (keyData)
			{
				case Keys.Left:
				case Keys.Up:
				case Keys.Right:
				case Keys.Down:
					return true;
			}
			return base.IsInputKey(keyData);
		}
		#endregion Events (override)


		#region Methods
		/// <summary>
		/// Scrolls the panel to ensure that the currently selected tile is
		/// fully displayed.
		/// </summary>
		internal void ScrollTile()
		{
			if (Scroller.Enabled && _f.SelId != -1)
			{
				int x = _f.SelId * (XCImage.SpriteWidth32 + 1);
				if (x < Scroller.Value)
				{
					Scroller.Value = x;
				}
				else if ((x += XCImage.SpriteWidth32) > Width + Scroller.Value)
				{
					Scroller.Value = x - Width;
				}
			}
		}

		/// <summary>
		/// Takes keyboard-input from the Form's KeyDown event to scroll this
		/// panel.
		/// </summary>
		/// <param name="e"></param>
		internal void KeyTile(KeyEventArgs e)
		{
			// TODO: Ctrl and Shift to select SubSelIds

			switch (e.KeyCode)
			{
				case Keys.Left:
				case Keys.Up:
				case Keys.Back:
					SubSelIds.Clear();
					if (_f.SelId != 0)
						_f.SelId -= 1;
					else
						Invalidate();
					break;

				case Keys.Right:
				case Keys.Down:
				case Keys.Space:
					SubSelIds.Clear();
					if (_f.SelId != Parts.Length - 1)
						_f.SelId += 1;
					else
						Invalidate();
					break;

				case Keys.PageUp:
				{
					SubSelIds.Clear();
					int d = Width / (XCImage.SpriteWidth32 + 1);
					if (_f.SelId - d < 0)
						_f.SelId = 0;
					else
						_f.SelId -= d;
					Invalidate();
					break;
				}

				case Keys.PageDown:
				{
					SubSelIds.Clear();
					int d = Width / (XCImage.SpriteWidth32 + 1);
					if (_f.SelId + d > Parts.Length - 1)
						_f.SelId = Parts.Length - 1;
					else
						_f.SelId += d;
					Invalidate();
					break;
				}

				case Keys.Home:
					SubSelIds.Clear();
					_f.SelId = 0;
					Invalidate();
					break;

				case Keys.End:
					SubSelIds.Clear();
					_f.SelId = Parts.Length - 1;
					Invalidate();
					break;
			}
		}


/*		/// <summary>
		/// Gets the loc of the currently selected tile relative to the table.
		/// </summary>
		/// <returns></returns>
		private int GetTileLeft()
		{
			return _f.SelId * (XCImage.SpriteWidth32 + 1);
		}
		/// <summary>
		/// Gets the loc+width of the currently selected tile relative to the table.
		/// </summary>
		/// <returns></returns>
		private int GetTileRight()
		{
			return _f.SelId * (XCImage.SpriteWidth32 + 1) + XCImage.SpriteWidth32;
		} */
		#endregion Methods
	}
}
