using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows.Forms;

using DSShared;
using DSShared.Controls;

using XCom;


namespace PckView
{
	public sealed partial class PckViewF
		:
			Form
	{
		public enum Type : byte
		{
			non,	// default

			Pck,	// a terrain or unit PCK+TAB set is currently loaded.
					// These are 32x40 w/ 2-byte Tabword (terrain or ufo-unit) or 4-byte Tabword (tftd-unit)
			Bigobs,	// a Bigobs PCK+TAB set is currently loaded.
					// Bigobs are 32x48 w/ 2-byte Tabword.
			ScanG,	// a ScanG iconset is currently loaded.
					// ScanGs are 4x4 w/ 0-byte Tabword.
			LoFT	// a LoFT iconset is currently loaded.
					// LoFTs are 16x16 w/ 0-byte Tabword.
		}
		public Type SetType;


		#region Delegates
		internal delegate void PaletteChangedEvent();
		#endregion Delegates


		#region Events (static)
		internal static event PaletteChangedEvent PaletteChanged;
		#endregion Events (static)


		#region Fields (static)
		private const string TITLE    = "PckView";

		private const string Total    = "Total ";
		private const string Selected = "Selected ";
		private const string Over     = "Over ";
		private const string None     = "n/a";

		internal static bool Quit;

		internal static float SpriteShadeFloat;

		internal const int SPRITESHADE_ON       =  0;
		internal const int SPRITESHADE_DISABLED = -1;
		internal const int SPRITESHADE_OFF      = -2;

		internal static bool BypassActivatedEvent;
		#endregion Fields (static)


		#region Fields
		internal static string[] _args;

		/// <summary>
		/// True if PckView has been invoked via TileView.
		/// </summary>
		private bool IsInvoked;

//		private TabControl _tcTabs; // for OnCompareClick()

		private ToolStripMenuItem _miEdit;
		private ToolStripMenuItem _miAdd;
		private ToolStripMenuItem _miInsertBefor;
		private ToolStripMenuItem _miInsertAfter;
		private ToolStripMenuItem _miReplace;
		private ToolStripMenuItem _miMoveL;
		private ToolStripMenuItem _miMoveR;
		private ToolStripMenuItem _miDelete;
		private ToolStripMenuItem _miExport;

		private readonly Dictionary<Palette, MenuItem> _itPalettes =
					 new Dictionary<Palette, MenuItem>();

		internal int SpriteShade = SPRITESHADE_DISABLED;
		internal readonly ImageAttributes Ia = new ImageAttributes();


		private string _lastCreateDirectory;
		private string _lastBrowserDirectory;
		private string _lastSpriteDirectory;

		private bool _minimized;

		/// <summary>
		/// A placeholder sprite to draw instead of totally transparent sprite.
		/// </summary>
		internal Bitmap BlankSprite;

		/// <summary>
		/// A placeholder icon to draw instead of totally transparent icon.
		/// </summary>
		internal Bitmap BlankIcon;
		#endregion Fields


		#region Properties
		/// <summary>
		/// The current palette per the Palette menu.
		/// </summary>
		/// <remarks>Use <see cref="GetCurrentPalette"/> as appropriate since
		/// LoFTsets don't have a standard palette.</remarks>
		internal Palette Pal
		{ get; private set; }

		/// <summary>
		/// The sprite-editor form.
		/// </summary>
		internal SpriteEditorF SpriteEditor
		{ get; private set; }

		/// <summary>
		/// The panel in which all sprites of a currently loaded spriteset are
		/// displayed.
		/// </summary>
		internal PckViewPanel TilePanel
		{ get; private set; }


		/// <summary>
		/// For reloading the Map when PckView is invoked via TileView.
		/// </summary>
		/// <remarks>Reload MapView's Map even if the PCK+TAB is saved as a
		/// different file; any modified terrain (etc) could be in the Map's
		/// terrainset or other resources.</remarks>
		public bool RequestReload
		{ get; private set; }


		/// <summary>
		/// The fullpath of the spriteset. Shall not contain the file-extension
		/// for terrain/unit/bigobs files (since it's easier to add .PCK and.TAB
		/// strings later) but ScanG and LoFT files retain their .DAT extension.
		/// </summary>
		private string _path
		{ get; set; }

		private bool _changed;
		/// <summary>
		/// Sets the titlebar-text when a spriteset loads or gets changed.
		/// </summary>
		internal bool Changed
		{
			private get { return _changed; }
			set
			{
				if (TilePanel.Spriteset == null)
				{
					Text = TITLE;
					_changed = value;
				}
				else if (!value || _changed != value)
				{
					string text;
					switch (SetType)
					{
						case Type.Pck:
						case Type.Bigobs:
							text = GlobalsXC.PckExt_lc;
							break;

						default:
							text = String.Empty;
							break;
					}
					text = TITLE + GlobalsXC.PADDED_SEPARATOR + _path + text;

					if (value)
						text += GlobalsXC.PADDED_ASTERISK;

					Text = text;
					_changed = value;
				}
			}
		}
		#endregion Properties


		#region cTor
		/// <summary>
		/// cTor. Creates the PckView window.
		/// </summary>
		/// <param name="isInvoked">true if invoked via TileView</param>
		/// <param name="spriteshade">if 'isInvoked' is true you can pass in a
		/// SpriteShade value from MapView</param>
		public PckViewF(bool isInvoked = false, int spriteshade = -1)
		{
			IsInvoked = isInvoked;

			string dirAppL = Path.GetDirectoryName(Application.ExecutablePath);
#if DEBUG
			LogFile.SetLogFilePath(dirAppL, IsInvoked);
#endif

			InitializeComponent();

			// WORKAROUND: See note in MainViewF cTor.
			MaximumSize = new Size(0,0); // fu.net

			if (!IsInvoked)
				RegistryInfo.InitializeRegistry(dirAppL);

			RegistryInfo.RegisterProperties(this);
//			regInfo.AddProperty("SelectedPalette"); // + Transparency On/Off

			TilePanel = new PckViewPanel(this);
			TilePanel.Click       += OnPanelClick;
			TilePanel.DoubleClick += OnSpriteEditorClick;
			Controls.Add(TilePanel);

			SpriteEditor = new SpriteEditorF(this);
			SpriteEditor.FormClosing += OnSpriteEditorClosing;

			TilePanel.ContextMenuStrip = CreateContext();

			PrintSelected();
			PrintOver();

			PopulatePaletteMenu(); // WARNING: Palettes created here <-

			BlankSprite = Properties.Resources.blanksprite;
			BlankIcon   = Properties.Resources.blankicon;


			miCreate.MenuItems.Add(miCreateTerrain);	// NOTE: These items are added to the Filemenu first
			miCreate.MenuItems.Add(miCreateBigobs);		// and get transfered to the Create submenu here.
			miCreate.MenuItems.Add(miCreateUnitUfo);
			miCreate.MenuItems.Add(miCreateUnitTftd);

			tssl_SpritesetLabel.Text = None;
			tssl_TilesTotal    .Text = Total + None;
			tssl_OffsetLast    .Text =
			tssl_OffsetAftr    .Text = String.Empty;

			ss_Status.Renderer = new CustomToolStripRenderer();


			bool @set = false;
			if (IsInvoked)
			{
				@set = (spriteshade > 0);
			}
			else
			{
				string shade = PathInfo.GetSpriteShade(dirAppL); // get shade from MapView's options
				if (shade != null)
				{
					@set = Int32.TryParse(shade, out spriteshade)
						&& spriteshade > 0;
				}
			}

			if (@set)
			{
				miSpriteShade.Checked = true;

				SpriteShade = Math.Min(spriteshade, 100);
				SpriteShadeFloat = (float)SpriteShade * 0.03F;

				Ia.SetGamma(SpriteShadeFloat, ColorAdjustType.Bitmap);
			}


			if (_args != null && _args.Length != 0)
			{
				string file = Path.GetFileNameWithoutExtension(_args[0]).ToLower();
				switch (Path.GetExtension(_args[0]).ToLower())
				{
					case ".pck":
						// NOTE: LoadSpriteset() will check for a TAB file and
						// issue an error if not found.

						LoadSpriteset(_args[0], file.Contains("bigobs"));
						break;

					case ".dat":
						if (file.Contains("scang"))
						{
							LoadScanG(_args[0]);
						}
						else if (file.Contains("loftemps"))
						{
							LoadLoFT(_args[0]);
						}
						break;
				}
			}
		}


		// PckView shortcut table:
		// miCreateTerrain		CtrlR
		// miCreateBigobs		CtrlB
		// miCreateUnitUfo		CtrlU
		// miCreateUnitTftd		CtrlT
		// miOpen				CtrlO
		// miOpenBigobs			CtrlG
		// miOpenScanG			CtrlD
		// miOpenLoFT			CtrlM
		// miSave				CtrlS
		// miSaveAs				CtrlE
		// miExportSprites		CtrlP
		// miExportSpritesheet	F5
		// miImportSpritesheet	F6
		// miQuit				CtrlQ
		// miCompare
		// miTransparent		F7
		// miSpriteShade		F8
		// palette items		Ctrl1..Ctrl8
		// miBytes				F9
		// miHelp				F1
		//
		// CONTEXT:
		// Edit					Enter
		// Add ...				d
		// InsertBefore ...		b
		// InsertAfter ...		a
		// Replace ...			r
		// MoveLeft				-
		// MoveRight			+
		// Delete				Delete
		// ExportSprite ...		p

		/// <summary>
		/// Builds the RMB contextmenu.
		/// </summary>
		/// <returns></returns>
		private ContextMenuStrip CreateContext()
		{
			_miEdit        = new ToolStripMenuItem("Edit",              null, OnSpriteEditorClick);			// OnKeyDown Enter
			_miAdd         = new ToolStripMenuItem("Add ...",           null, OnAddSpritesClick);			// OnKeyDown d
			_miInsertBefor = new ToolStripMenuItem("Insert before ...", null, OnInsertSpritesBeforeClick);	// OnKeyDown b
			_miInsertAfter = new ToolStripMenuItem("Insert after ...",  null, OnInsertSpritesAfterClick);	// OnKeyDown a
			_miReplace     = new ToolStripMenuItem("Replace ...",       null, OnReplaceSpriteClick);		// OnKeyDown r
			_miMoveL       = new ToolStripMenuItem("Move left",         null, OnMoveLeftSpriteClick);		// OnKeyDown -
			_miMoveR       = new ToolStripMenuItem("Move right",        null, OnMoveRightSpriteClick);		// OnKeyDown +
			_miDelete      = new ToolStripMenuItem("Delete",            null, OnDeleteSpriteClick);			// OnKeyDown Delete
			_miExport      = new ToolStripMenuItem("Export sprite ...", null, OnExportSpriteClick);			// OnKeyDown p

			_miEdit       .ShortcutKeyDisplayString = "Enter";
			_miAdd        .ShortcutKeyDisplayString = "d";
			_miInsertBefor.ShortcutKeyDisplayString = "b";
			_miInsertAfter.ShortcutKeyDisplayString = "a";
			_miReplace    .ShortcutKeyDisplayString = "r";
			_miMoveL      .ShortcutKeyDisplayString = "-";
			_miMoveR      .ShortcutKeyDisplayString = "+";
			_miDelete     .ShortcutKeyDisplayString = "Del";
			_miExport     .ShortcutKeyDisplayString = "p";


			var context = new ContextMenuStrip();

			context.Items.Add(_miEdit);
			context.Items.Add(new ToolStripSeparator());
			context.Items.Add(_miAdd);
			context.Items.Add(_miInsertBefor);
			context.Items.Add(_miInsertAfter);
			context.Items.Add(new ToolStripSeparator());
			context.Items.Add(_miReplace);
			context.Items.Add(_miMoveL);
			context.Items.Add(_miMoveR);
			context.Items.Add(new ToolStripSeparator());
			context.Items.Add(_miDelete);
			context.Items.Add(new ToolStripSeparator());
			context.Items.Add(_miExport);

			_miAdd        .Enabled =
			_miInsertBefor.Enabled =
			_miInsertAfter.Enabled =
			_miReplace    .Enabled =
			_miMoveL      .Enabled =
			_miMoveR      .Enabled =
			_miDelete     .Enabled =
			_miExport     .Enabled = false;

			return context;
		}

		/// <summary>
		/// Adds the palettes as menuitems to the palettes menu on the main
		/// menubar.
		/// </summary>
		private void PopulatePaletteMenu()
		{
			// instantiate the palettes
			// iff not invoked by MapView - else the palettes have already been
			// instantiated and these are just pointers in which case
			// 'BypassTonescales' is irrelevant

			Palette.BypassTonescales = true;

			var pals = new List<Palette>();
			pals.Add(Palette.UfoBattle);
			pals.Add(Palette.UfoGeo);
			pals.Add(Palette.UfoGraph);
			pals.Add(Palette.UfoResearch);
			pals.Add(Palette.TftdBattle);
			pals.Add(Palette.TftdGeo);
			pals.Add(Palette.TftdGraph);
			pals.Add(Palette.TftdResearch);

			Palette.BypassTonescales = false;


			MenuItem it;
			Palette pal;

			for (int i = 0; i != pals.Count; ++i)
			{
				pal = pals[i];
				it = new MenuItem(pal.Label, OnPaletteClick);	// I believe these will be disposed
				it.Tag = pal;									// when the Form gets closed since
				miPaletteMenu.MenuItems.Add(it);				// they are owned by 'miPaletteMenu'
				_itPalettes[pal] = it;							// which is owned/disposed by the Form.

				switch (i)
				{
					case 0: it.Shortcut = Shortcut.Ctrl1; break;
					case 1: it.Shortcut = Shortcut.Ctrl2; break;
					case 2: it.Shortcut = Shortcut.Ctrl3; break;
					case 3: it.Shortcut = Shortcut.Ctrl4; break;
					case 4: it.Shortcut = Shortcut.Ctrl5; break;
					case 5: it.Shortcut = Shortcut.Ctrl6; break;
					case 6: it.Shortcut = Shortcut.Ctrl7; break;
					case 7: it.Shortcut = Shortcut.Ctrl8; break;
				}
			}

			OnPaletteClick(_itPalettes[Palette.UfoBattle], EventArgs.Empty);
		}
		#endregion cTor


		#region Events (override)
		/// <summary>
		/// Brings all forms to top when this is activated.
		/// </summary>
		/// <param name="e"></param>
		protected override void OnActivated(EventArgs e)
		{
			if (!BypassActivatedEvent)
			{
				BypassActivatedEvent = true;

				if (SpriteEditor._fpalette.Visible)
				{
					SpriteEditor._fpalette.TopMost = true;
					SpriteEditor._fpalette.TopMost = false;
				}

				if (SpriteEditor.Visible)
				{
					SpriteEditor.TopMost = true;
					SpriteEditor.TopMost = false;
				}

				TopMost = true;		// req'd else this form won't activate at all
				TopMost = false;	// unless user closes the other forms

				BypassActivatedEvent = false;
			}
			base.OnActivated(e);
		}

		/// <summary>
		/// Minimizes and restores this along with the SpriteEditor and
		/// PaletteViewer synchronistically.
		/// </summary>
		/// <param name="e"></param>
		protected override void OnResize(EventArgs e)
		{
			base.OnResize(e);

			if (WindowState == FormWindowState.Minimized)
			{
				_minimized = true;

				if (SpriteEditor.Visible)
					SpriteEditor.WindowState = FormWindowState.Minimized;

				if (SpriteEditor._fpalette.Visible)
					SpriteEditor._fpalette.WindowState = FormWindowState.Minimized;
			}
			else if (_minimized)
			{
				_minimized = false;

				if (SpriteEditor.Visible)
					SpriteEditor.WindowState = FormWindowState.Normal;

				if (SpriteEditor._fpalette.Visible)
					SpriteEditor._fpalette.WindowState = FormWindowState.Normal;
			}
		}

		/// <summary>
		/// Focuses the viewer-panel after the app loads.
		/// </summary>
		/// <param name="e"></param>
		protected override void OnShown(EventArgs e)
		{
			TilePanel.Select();
			base.OnShown(e);
		}

		/// <summary>
		/// Closes the app after a .NET call to close (roughly).
		/// </summary>
		/// <param name="e"></param>
		protected override void OnFormClosing(FormClosingEventArgs e)
		{
			if (!RegistryInfo.FastClose(e.CloseReason))
			{
				if (RequestSpritesetClose())
				{
					RegistryInfo.UpdateRegistry(this);

					Quit = true;

					SpriteEditor.ClosePalette();	// these are needed when PckView is invoked via TileView
					SpriteEditor.Close();			// it's also just good procedure

					Ia.Dispose();
					TilePanel.Destroy();

					BlankSprite.Dispose();
					BlankIcon  .Dispose();

					ByteTableManager.HideTable();

					if (!IsInvoked)
						RegistryInfo.WriteRegistry();
				}
				else
					e.Cancel = true;
			}
			base.OnFormClosing(e);
		}

		/// <summary>
		/// Handles keydown events at the form level - context and navigation
		/// shortcuts.
		/// </summary>
		/// <param name="e"></param>
		protected override void OnKeyDown(KeyEventArgs e)
		{
			//LogFile.WriteLine("PckViewF.OnKeyDown() " + e.KeyData);

			switch (e.KeyData)
			{
				// Context shortcuts ->

				case Keys.Enter:											// edit
					e.Handled = e.SuppressKeyPress = true;
					OnSpriteEditorClick(null, EventArgs.Empty);
					break;

				case Keys.D:												// add
					if (_miAdd.Enabled)
					{
						e.Handled = e.SuppressKeyPress = true;
						OnAddSpritesClick(null, EventArgs.Empty);
					}
					break;

				case Keys.B:												// insert before
					if (_miInsertBefor.Enabled)
					{
						e.Handled = e.SuppressKeyPress = true;
						OnInsertSpritesBeforeClick(null, EventArgs.Empty);
					}
					break;

				case Keys.A:												// insert after
					if (_miInsertAfter.Enabled)
					{
						e.Handled = e.SuppressKeyPress = true;
						OnInsertSpritesAfterClick(null, EventArgs.Empty);
					}
					break;

				case Keys.R:												// replace
					if (_miReplace.Enabled)
					{
						e.Handled = e.SuppressKeyPress = true;
						OnReplaceSpriteClick(null, EventArgs.Empty);
					}
					break;

				case Keys.OemMinus: // drugs ...
				case Keys.Subtract:											// move left
					if (_miMoveL.Enabled)
					{
						e.Handled = e.SuppressKeyPress = true;
						OnMoveLeftSpriteClick(null, EventArgs.Empty);
					}
					break;

				case Keys.Oemplus: // drugs ...
				case Keys.Add:												// move right
					if (_miMoveR.Enabled)
					{
						e.Handled = e.SuppressKeyPress = true;
						OnMoveRightSpriteClick(null, EventArgs.Empty);
					}
					break;

				case Keys.Delete:											// delete
					if (_miDelete.Enabled)
					{
						e.Handled = e.SuppressKeyPress = true;
						OnDeleteSpriteClick(null, EventArgs.Empty);
					}
					break;

				case Keys.P:												// export
					if (_miExport.Enabled)
					{
						e.Handled = e.SuppressKeyPress = true;
						OnExportSpriteClick(null, EventArgs.Empty);
					}
					break;


				// Navigation shortcuts ->

				// TODO: [Home] [End] [Ctrl+Home] [Ctrl+End] [PgUp] [PgDn]

				case Keys.Left:
					if (TilePanel.Spriteset != null)
					{
						e.Handled = e.SuppressKeyPress = true;
						TilePanel.SelectAdjacentHori(-1);
					}
					break;

				case Keys.Right:
					if (TilePanel.Spriteset != null)
					{
						e.Handled = e.SuppressKeyPress = true;
						TilePanel.SelectAdjacentHori(+1);
					}
					break;

				case Keys.Up:
					if (TilePanel.Spriteset != null)
					{
						e.Handled = e.SuppressKeyPress = true;
						TilePanel.SelectAdjacentVert(-1);
					}
					break;

				case Keys.Down:
					if (TilePanel.Spriteset != null)
					{
						e.Handled = e.SuppressKeyPress = true;
						TilePanel.SelectAdjacentVert(+1);
					}
					break;

				case Keys.Escape:
					if (TilePanel.Spriteset != null)
					{
						e.Handled = e.SuppressKeyPress = true;
						if (SetSelected(-1)) TilePanel.Invalidate();
					}
					break;
			}

			base.OnKeyDown(e);
		}
		#endregion Events (override)


		#region Events
		/// <summary>
		/// Bring back the dinosaurs. Called when the tile-panel's click-event
		/// is raised.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// <remarks>This fires after PckViewPanel.OnMouseDown() - thought you'd
		/// like to know.</remarks>
		private void OnPanelClick(object sender, EventArgs e)
		{
			EnableContext();
		}

		/// <summary>
		/// Opens the currently selected sprite in the sprite-editor. Called
		/// when the Context menu's click-event or the viewer-panel's
		/// DoubleClick event is raised or [Enter] is pressed.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void OnSpriteEditorClick(object sender, EventArgs e)
		{
			if (!_miEdit.Checked)
			{
				_miEdit.Checked = true;

				SpriteEditor.Show();

				if (SetType != Type.LoFT)
					SpriteEditor._fpalette.Show();
			}
		}

		/// <summary>
		/// Dechecks the context's Edit it.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// <remarks>This fires after the editor's FormClosing event.</remarks>
		private void OnSpriteEditorClosing(object sender, CancelEventArgs e)
		{
			_miEdit.Checked = false;
		}


		/// <summary>
		/// Displays an errorbox to the user about incorrect Bitmap dimensions
		/// and/or pixel-format.
		/// </summary>
		/// <param name="hint">true to suggest proper dimensions/format</param>
		private void ShowBitmapError(bool hint = true)
		{
			string copyable;
			if (hint)
				copyable = FileDialogStrings.GetError(SetType);
			else
				copyable = null;

			using (var f = new Infobox(
									"Image error",
									"Detected incorrect Dimensions and/or PixelFormat.",
									copyable,
									Infobox.BoxType.Error))
			{
				f.ShowDialog(this);
			}
		}

		/// <summary>
		/// Disposes temporary bitmaps.
		/// </summary>
		/// <param name="bs"></param>
		private void DisposeBitmaps(IList<Bitmap> bs)
		{
			for (int i = bs.Count - 1; i != -1; --i) // not sure if Dispose() needs to be done in reverse order
			if (bs[i] != null)
				bs[i].Dispose();
		}

		/// <summary>
		/// Adds a sprite or sprites to the collection. Called when the Context
		/// menu's click-event is raised.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void OnAddSpritesClick(object sender, EventArgs e)
		{
			using (var ofd = new OpenFileDialog())
			{
				ofd.Title  = FileDialogStrings.GetTitle(SetType, true);
				ofd.Filter = FileDialogStrings.GetFilter();

				if (Directory.Exists(_lastSpriteDirectory))
					ofd.InitialDirectory = _lastSpriteDirectory;
				else
				{
					string dir = Path.GetDirectoryName(_path);
					if (Directory.Exists(dir))
						ofd.InitialDirectory = dir;
				}

				ofd.Multiselect =
				ofd.RestoreDirectory = true;


				if (ofd.ShowDialog(this) == DialogResult.OK)
				{
					_lastSpriteDirectory = Path.GetDirectoryName(ofd.FileName);

					bool valid = true;

					var bs = new Bitmap[ofd.FileNames.Length]; // first run a check against all sprites and if any are borked set error.
					for (int i = 0; valid && i != ofd.FileNames.Length; ++i)
					{
//						var b = new Bitmap(ofd.FileNames[i]);	// <- .net.bork. Creates a 32-bpp Argb image if source is
																// 8-bpp PNG w/transparency; GIF,BMP however retains 8-bpp format.

						byte[] bindata = FileService.ReadFile(ofd.FileNames[i]);
						if (bindata != null)
						{
							Bitmap b = BitmapLoader.LoadBitmap(bindata);

							if (b == null) // error was shown by BitmapLoader.
							{
								valid = false;
							}
							else if (b.Width       != XCImage.SpriteWidth
								||   b.Height      != XCImage.SpriteHeight
								||   b.PixelFormat != PixelFormat.Format8bppIndexed)
							{
								ShowBitmapError();
								valid = false;
							}
							else
								bs[i] = b;
						}
						else valid = false; // error was shown by FileService.
					}

					if (valid)
					{
						int id = (TilePanel.Spriteset.Count - 1);
						foreach (var b in bs)
						{
							XCImage sprite = BitmapService.CreateSprite(
																	b,
																	++id,
																	GetCurrentPalette(),
																	XCImage.SpriteWidth,
																	XCImage.SpriteHeight,
																	SetType == Type.ScanG || SetType == Type.LoFT);
							TilePanel.Spriteset.Sprites.Add(sprite);
						}

						SpritesetCountChanged(TilePanel.Selid);
					}

					DisposeBitmaps(bs);
				}
			}
		}

		/// <summary>
		/// Inserts sprites into the currently loaded spriteset before the
		/// currently selected sprite. Called when the Context menu's click-
		/// event is raised.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void OnInsertSpritesBeforeClick(object sender, EventArgs e)
		{
			using (var ofd = new OpenFileDialog())
			{
				ofd.Title  = FileDialogStrings.GetTitle(SetType, true);
				ofd.Filter = FileDialogStrings.GetFilter();

				if (Directory.Exists(_lastSpriteDirectory))
					ofd.InitialDirectory = _lastSpriteDirectory;
				else
				{
					string dir = Path.GetDirectoryName(_path);
					if (Directory.Exists(dir))
						ofd.InitialDirectory = dir;
				}

				ofd.Multiselect =
				ofd.RestoreDirectory = true;


				if (ofd.ShowDialog(this) == DialogResult.OK)
				{
					_lastSpriteDirectory = Path.GetDirectoryName(ofd.FileName);

					if (InsertSprites(TilePanel.Selid, ofd.FileNames))
					{
						SpritesetCountChanged(TilePanel.Selid + ofd.FileNames.Length);
					}
				}
			}
		}

		/// <summary>
		/// Inserts sprites into the currently loaded spriteset after the
		/// currently selected sprite. Called when the Context menu's click-
		/// event is raised.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void OnInsertSpritesAfterClick(object sender, EventArgs e)
		{
			using (var ofd = new OpenFileDialog())
			{
				ofd.Title  = FileDialogStrings.GetTitle(SetType, true);
				ofd.Filter = FileDialogStrings.GetFilter();

				if (Directory.Exists(_lastSpriteDirectory))
					ofd.InitialDirectory = _lastSpriteDirectory;
				else
				{
					string dir = Path.GetDirectoryName(_path);
					if (Directory.Exists(dir))
						ofd.InitialDirectory = dir;
				}

				ofd.Multiselect =
				ofd.RestoreDirectory = true;


				if (ofd.ShowDialog(this) == DialogResult.OK)
				{
					_lastSpriteDirectory = Path.GetDirectoryName(ofd.FileName);

					if (InsertSprites(TilePanel.Selid + 1, ofd.FileNames))
					{
						SpritesetCountChanged(TilePanel.Selid);
					}
				}
			}
		}

		/// <summary>
		/// Inserts sprites into the currently loaded spriteset starting at a
		/// given Id.
		/// </summary>
		/// <param name="id">the terrain-id to start inserting at</param>
		/// <param name="files">an array of filenames</param>
		/// <returns>true if all sprites are inserted successfully</returns>
		/// <remarks>Helper for <see cref="OnInsertSpritesBeforeClick"/> and
		/// <see cref="OnInsertSpritesAfterClick"/></remarks>
		private bool InsertSprites(int id, string[] files)
		{
			bool valid = true;

			var bs = new Bitmap[files.Length]; // first run a check against all sprites and if any are borked exit w/ false.
			for (int i = 0; valid && i != files.Length; ++i)
			{
				byte[] bindata = FileService.ReadFile(files[i]);
				if (bindata != null)
				{
					Bitmap b = BitmapLoader.LoadBitmap(bindata);

					if (b == null) // error was shown by BitmapLoader.
					{
						valid = false;
					}
					else if (b.Width       != XCImage.SpriteWidth
						||   b.Height      != XCImage.SpriteHeight
						||   b.PixelFormat != PixelFormat.Format8bppIndexed)
					{
						ShowBitmapError();
						valid = false;
					}
					else
						bs[i] = b;
				}
				else valid = false; // error was shown by FileService.
			}


			if (valid)
			{
				int length = files.Length;
				for (int i = id; i != TilePanel.Spriteset.Count; ++i)
					TilePanel.Spriteset[i].Id = i + length;

				foreach (var b in bs)
				{
					XCImage sprite = BitmapService.CreateSprite(
															b,
															id,
															GetCurrentPalette(),
															XCImage.SpriteWidth,
															XCImage.SpriteHeight,
															SetType == Type.ScanG || SetType == Type.LoFT);
					TilePanel.Spriteset.Sprites.Insert(id++, sprite);
				}
			}

			DisposeBitmaps(bs);

			return valid;
		}

		/// <summary>
		/// Finishes an operation that changed the spriteset-count.
		/// </summary>
		/// <param name="id">sprite-id to select</param>
		private void SpritesetCountChanged(int id)
		{
			SetSelected(id, true);

			PrintTotal();

			TilePanel.ForceResize();
			TilePanel.Invalidate();

			Changed = true;
		}

		/// <summary>
		/// Replaces the selected sprite in the collection with a different
		/// sprite. Called when the Context menu's click-event is raised.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void OnReplaceSpriteClick(object sender, EventArgs e)
		{
			using (var ofd = new OpenFileDialog())
			{
				ofd.Title  = FileDialogStrings.GetTitle(SetType, false);
				ofd.Filter = FileDialogStrings.GetFilter();

				if (Directory.Exists(_lastSpriteDirectory))
					ofd.InitialDirectory = _lastSpriteDirectory;
				else
				{
					string dir = Path.GetDirectoryName(_path);
					if (Directory.Exists(dir))
						ofd.InitialDirectory = dir;
				}

				ofd.RestoreDirectory = true;


				if (ofd.ShowDialog(this) == DialogResult.OK)
				{
					_lastSpriteDirectory = Path.GetDirectoryName(ofd.FileName);

					byte[] bindata = FileService.ReadFile(ofd.FileName);
					if (bindata != null) // else error was shown by FileService.
					{
						using (Bitmap b = BitmapLoader.LoadBitmap(bindata))
						{
							if (b != null) // else error was shown by BitmapLoader.
							{
								if (   b.Width       != XCImage.SpriteWidth
									|| b.Height      != XCImage.SpriteHeight
									|| b.PixelFormat != PixelFormat.Format8bppIndexed)
								{
									ShowBitmapError();
								}
								else
								{
									XCImage sprite = BitmapService.CreateSprite(
																			b,
																			TilePanel.Selid,
																			GetCurrentPalette(),
																			XCImage.SpriteWidth,
																			XCImage.SpriteHeight,
																			SetType == Type.ScanG || SetType == Type.LoFT);

									TilePanel.Spriteset[TilePanel.Selid].Dispose();
									TilePanel.Spriteset[TilePanel.Selid] = sprite;

									SetSelected(TilePanel.Selid, true);

									TilePanel.Refresh();
									Changed = true;
								}
							}
						}
					}
				}
			}
		}

		/// <summary>
		/// Moves a sprite one slot to the left.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void OnMoveLeftSpriteClick(object sender, EventArgs e)
		{
			MoveSprite(-1);
		}

		/// <summary>
		/// Moves a sprite one slot to the right.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void OnMoveRightSpriteClick(object sender, EventArgs e)
		{
			MoveSprite(+1);
		}

		/// <summary>
		/// Moves a sprite to the left or right by one slot.
		/// </summary>
		/// <param name="dir">-1 to move left, +1 to move right</param>
		private void MoveSprite(int dir)
		{
			int id = TilePanel.Selid;

			var sprite = TilePanel.Spriteset[id];

			TilePanel.Spriteset[id] = TilePanel.Spriteset[id + dir];
			TilePanel.Spriteset[id + dir] = sprite;

			TilePanel.Spriteset[id].Id = id;
			TilePanel.Spriteset[id + dir].Id = id + dir;

			SetSelected(id + dir);

			TilePanel.Refresh();
			Changed = true;
		}

		/// <summary>
		/// Deletes the selected sprite from the collection. Called when the
		/// Context menu's click-event is raised.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void OnDeleteSpriteClick(object sender, EventArgs e)
		{
			int id = TilePanel.Selid;

			TilePanel.Spriteset.Sprites[id].Dispose();
			TilePanel.Spriteset.Sprites.RemoveAt(id);

			for (int i = id; i != TilePanel.Spriteset.Count; ++i)
				TilePanel.Spriteset[i].Id = i;

			if (id == TilePanel.Spriteset.Count)
				id = -1;

			SpritesetCountChanged(id);
		}

		/// <summary>
		/// Exports the selected sprite in the collection to a PNG file. Called
		/// when the Context menu's click-event is raised.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void OnExportSpriteClick(object sender, EventArgs e)
		{
			int count = TilePanel.Spriteset.Count;
			string digits = String.Empty;
			do
			{ digits += "0"; }
			while ((count /= 10) != 0);

			string suffix = String.Format(
										"_{0:" + digits + "}",
										TilePanel.Selid);

			using (var sfd = new SaveFileDialog())
			{
				sfd.Title      = "Export sprite to 8-bpp PNG file";
				sfd.Filter     = FileDialogStrings.GetFilterPng();
				sfd.DefaultExt = GlobalsXC.PngExt;
				sfd.FileName   = TilePanel.Spriteset.Label.ToUpperInvariant() + suffix;

				if (!Directory.Exists(_lastSpriteDirectory))
				{
					string dir = Path.GetDirectoryName(_path);
					if (Directory.Exists(dir))
						sfd.InitialDirectory = dir;
				}
				else
					sfd.InitialDirectory = _lastSpriteDirectory;

				sfd.RestoreDirectory = true;


				if (sfd.ShowDialog(this) == DialogResult.OK)
				{
					_lastSpriteDirectory = Path.GetDirectoryName(sfd.FileName);

					// TODO: Ask to overwrite an existing file.
					BitmapService.ExportSprite(
											sfd.FileName,
											TilePanel.Spriteset[TilePanel.Selid].Sprite);
//											SetType == Type.LoFT
				}
			}
		}


		/// <summary>
		/// Creates a brand sparkling new (blank) sprite-collection. Called when
		/// the File menu's click-event is raised.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// <remarks>ScanG.dat and LoFTemps.dat cannot be created.</remarks>
		private void OnCreateClick(object sender, EventArgs e)
		{
			if (RequestSpritesetClose())
			{
				using (var sfd = new SaveFileDialog())
				{
					sfd.Filter     = FileDialogStrings.GetFilterPck();
					sfd.DefaultExt = GlobalsXC.PckExt;

					string text;
					if (sender == miCreateBigobs)			// Bigobs support for XCImage/PckSprite
						text = "Bigobs";
					else if (sender == miCreateUnitTftd)	// Tftd Unit support for XCImage/PckSprite
						text = "Tftd Unit";
					else if (sender == miCreateUnitUfo)		// Ufo Unit support for XCImage/PckSprite
						text = "Ufo Unit";
					else //if (sender == miCreateTerrain)	// Terrain support for XCImage/PckSprite
						text = "Terrain";

					sfd.Title = "Create " + text + " pck+tab files";

					if (Directory.Exists(_lastCreateDirectory))
						sfd.InitialDirectory = _lastCreateDirectory;
					else if (_path != null)
					{
						string dir = Path.GetDirectoryName(_path);
						if (Directory.Exists(dir))
							sfd.InitialDirectory = dir;
					}


					if (sfd.ShowDialog(this) == DialogResult.OK)
					{
						string pfe = sfd.FileName;
						_lastCreateDirectory = Path.GetDirectoryName(pfe);

						string label = Path.GetFileNameWithoutExtension(pfe);
						string pf    = Path.Combine(Path.GetDirectoryName(pfe), label);

						string pfePck = pf + GlobalsXC.PckExt;
						string pfeTab = pf + GlobalsXC.TabExt;

						string pfePckT = pfePck;
						string pfeTabT = pfeTab;
						if (File.Exists(pfePck)) pfePckT += GlobalsXC.TEMPExt;
						if (File.Exists(pfeTab)) pfeTabT += GlobalsXC.TEMPExt;

						// NOTE: Use 'fail' to allow the files to unlock - for
						// ReplaceFile() if necessary - after they get created.
						bool fail = true;

						using (var fsPck = FileService.CreateFile(pfePckT))
						if (fsPck != null)
						using (var fsTab = FileService.CreateFile(pfeTabT))
						if (fsTab != null)
							fail = false;

						if (!fail
							&& (pfePckT == pfePck || FileService.ReplaceFile(pfePck))
							&& (pfeTabT == pfeTab || FileService.ReplaceFile(pfeTab)))
						{
							XCImage.SpriteWidth = XCImage.SpriteWidth32;

							int tabwordLength = SpritesetsManager.TAB_WORD_LENGTH_2;
//							Palette pal = Palette.UfoBattle;

							if (sender == miCreateBigobs)
							{
								SetType = Type.Bigobs;
								XCImage.SpriteHeight = XCImage.SpriteHeight48;
							}
							else
							{
								SetType = Type.Pck;
								XCImage.SpriteHeight = XCImage.SpriteHeight40;

								if (sender == miCreateUnitTftd)
								{
									tabwordLength = SpritesetsManager.TAB_WORD_LENGTH_4;
//									pal = Palette.TftdBattle;
								}
							}

//							if (!_itPalettes[pal].Checked)
//							{
//								miTransparent.Checked = true;
//								OnPaletteClick(_itPalettes[pal], EventArgs.Empty);
//							}
//							else if (!miTransparent.Checked)
//							{
//								OnTransparencyClick(null, EventArgs.Empty);
//							}

							if (TilePanel.Spriteset != null)
								TilePanel.Spriteset.Dispose();

							TilePanel.Spriteset = new Spriteset(
															label,
															Pal, //pal
															tabwordLength);

							_path = pf;
							Changed = false;
						}
					}
				}
			}
		}

		/// <summary>
		/// Opens a sprite-collection of a terrain or a unit. Called when the
		/// File menu's click-event is raised.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void OnOpenPckClick(object sender, EventArgs e)
		{
			if (RequestSpritesetClose())
			{
				using (var ofd = new OpenFileDialog())
				{
					ofd.Title  = "Select a PCK (terrain/unit) file";
					ofd.Filter = FileDialogStrings.GetFilterPck();

					if (_path != null)
					{
						string dir = Path.GetDirectoryName(_path);
						if (Directory.Exists(dir))
							ofd.InitialDirectory = dir;
					}


					if (ofd.ShowDialog(this) == DialogResult.OK)
						LoadSpriteset(ofd.FileName);
				}
			}
		}

		/// <summary>
		/// Opens a sprite-collection of bigobs. Called when the File menu's
		/// click-event is raised.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void OnOpenBigobsClick(object sender, EventArgs e)
		{
			if (RequestSpritesetClose())
			{
				using (var ofd = new OpenFileDialog())
				{
					ofd.Title    = "Select a PCK (bigobs) file";
					ofd.Filter   = FileDialogStrings.GetFilterPck();
					ofd.FileName = "BIGOBS.PCK";

					if (_path != null)
					{
						string dir = Path.GetDirectoryName(_path);
						if (Directory.Exists(dir))
							ofd.InitialDirectory = dir;
					}


					if (ofd.ShowDialog(this) == DialogResult.OK)
						LoadSpriteset(ofd.FileName, true);
				}
			}
		}

		/// <summary>
		/// Opens a sprite-collection of ScanG icons. Called when the File
		/// menu's click-event is raised.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void OnOpenScanGClick(object sender, EventArgs e)
		{
			if (RequestSpritesetClose())
			{
				using (var ofd = new OpenFileDialog())
				{
					ofd.Title    = "Select a ScanG file";
					ofd.Filter   = FileDialogStrings.GetFilterDat();
					ofd.FileName = "SCANG.DAT";


					if (ofd.ShowDialog(this) == DialogResult.OK)
						LoadScanG(ofd.FileName);
				}
			}
		}

		/// <summary>
		/// Opens a sprite-collection of LoFT icons. Called when the File menu's
		/// click-event is raised.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void OnOpenLoFTClick(object sender, EventArgs e)
		{
			if (RequestSpritesetClose())
			{
				using (var ofd = new OpenFileDialog())
				{
					ofd.Title    = "Select a LoFTemps file";
					ofd.Filter   = FileDialogStrings.GetFilterDat();
					ofd.FileName = "LOFTEMPS.DAT";


					if (ofd.ShowDialog(this) == DialogResult.OK)
						LoadLoFT(ofd.FileName);
				}
			}
		}

		/// <summary>
		/// Saves all the sprites to the currently loaded PCK+TAB files if
		/// terrain/unit/bigobs or to the currently loaded DAT file if ScanG or
		/// LoFT. Called when the File menu's click-event is raised.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void OnSaveClick(object sender, EventArgs e)
		{
			if (TilePanel.Spriteset != null)
			{
				switch (SetType)
				{
					case Type.Pck: // save Pck+Tab terrain/unit/bigobs ->
					case Type.Bigobs:
						if (Spriteset.WriteSpriteset(_path, TilePanel.Spriteset))
						{
							Changed = false;
							RequestReload = true;
						}
						break;

					case Type.ScanG:
						if (Spriteset.WriteScanG(_path, TilePanel.Spriteset))
						{
							Changed = false;
							// TODO: FireMvReloadScanG file
						}
						break;

					case Type.LoFT:
						if (Spriteset.WriteLoFT(_path, TilePanel.Spriteset))
						{
							Changed = false;
						}
						break;
				}
			}
		}

		/// <summary>
		/// Saves all the sprites to potentially different PCK+TAB files if
		/// terrain/unit/bigobs or to a potentially different DAT file if ScanG
		/// or LoFT. Called when the File menu's click-event is raised.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void OnSaveAsClick(object sender, EventArgs e)
		{
			if (TilePanel.Spriteset != null)
			{
				using (var sfd = new SaveFileDialog())
				{
					switch (SetType)
					{
						case Type.Pck:
						case Type.Bigobs:
							sfd.Title = "Save Pck+Tab as ...";

							sfd.Filter     = FileDialogStrings.GetFilterPck();
							sfd.DefaultExt = GlobalsXC.PckExt;
							sfd.FileName   = Path.GetFileName(_path) + GlobalsXC.PckExt;
							break;

						case Type.ScanG:
							sfd.Title = "Save ScanG as ...";
							goto case Type.non;

						case Type.LoFT:
							sfd.Title = "Save LoFTemps as ...";
							goto case Type.non;

						case Type.non: // not Type.non - is only a label
							sfd.Filter     = FileDialogStrings.GetFilterDat();
							sfd.DefaultExt = GlobalsXC.DatExt;
							sfd.FileName   = Path.GetFileName(_path);
							break;
					}

					if (!Directory.Exists(_lastBrowserDirectory))
					{
						string dir = Path.GetDirectoryName(_path);
						if (Directory.Exists(dir))
							sfd.InitialDirectory = dir;
					}
					else
						sfd.InitialDirectory = _lastBrowserDirectory;


					if (sfd.ShowDialog(this) == DialogResult.OK)
					{
						string pfe = sfd.FileName;
						string dir = Path.GetDirectoryName(pfe);
						_lastBrowserDirectory = dir;

						switch (SetType)
						{
							case Type.Pck:
							case Type.Bigobs:
							{
								string label = Path.GetFileNameWithoutExtension(pfe);
								string pf    = Path.Combine(dir, label);

								if (Spriteset.WriteSpriteset(pf, TilePanel.Spriteset))
								{
									_path = pf;
									Changed = false;
									RequestReload = true;
								}
								break;
							}

							case Type.ScanG:
								if (Spriteset.WriteScanG(pfe, TilePanel.Spriteset))
								{
									_path = pfe;
									Changed = false;
									// TODO: FireMvReloadScanG file
								}
								break;

							case Type.LoFT:
								if (Spriteset.WriteLoFT(pfe, TilePanel.Spriteset))
								{
									_path = pfe;
									Changed = false;
								}
								break;
						}
					}
				}
			}
		}


		/// <summary>
		/// Exports all sprites in the currently loaded spriteset to PNG files.
		/// Called when the File menu's click-event is raised.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void OnExportSpritesClick(object sender, EventArgs e)
		{
			if (TilePanel.Spriteset != null)
			{
				int count = TilePanel.Spriteset.Count;
				if (count != 0)
				{
					using (var fbd = new FolderBrowserDialog())
					{
						string label = TilePanel.Spriteset.Label.ToUpperInvariant();

						fbd.Description = "Export spriteset to 8-bpp PNG files"
										+ Environment.NewLine + Environment.NewLine
										+ "\t" + label;

						if (!Directory.Exists(_lastSpriteDirectory))
						{
							string dir = Path.GetDirectoryName(_path);
							if (Directory.Exists(dir))
								fbd.SelectedPath = dir;
						}
						else
							fbd.SelectedPath = _lastSpriteDirectory;


						if (fbd.ShowDialog(this) == DialogResult.OK)
						{
							_lastSpriteDirectory = fbd.SelectedPath;

							string digits = String.Empty;
							int digittest = count;
							do
							{ digits += "0"; }
							while ((digittest /= 10) != 0);

							XCImage sprite;
							for (int id = 0; id != count; ++id)
							{
								sprite = TilePanel.Spriteset[id];
								string suffix = String.Format(
															"_{0:" + digits + "}",
															sprite.Id);
								string pfe = Path.Combine(_lastSpriteDirectory, label + suffix + GlobalsXC.PngExt);
								// TODO: Ask to overwrite an existing file.
								BitmapService.ExportSprite(pfe, sprite.Sprite); // SetType == Type.LoFT
							}
						}
					}
				}
			}
		}

		/// <summary>
		/// Exports all sprites in the currently loaded spriteset to a PNG
		/// spritesheet file.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// <remarks>Called when the File menu's click-event is raised.</remarks>
		private void OnExportSpritesheetClick(object sender, EventArgs e)
		{
			if (TilePanel.Spriteset != null && TilePanel.Spriteset.Count != 0)
			{
				using (var fbd = new FolderBrowserDialog()) // TODO: That should be a SaveFileDialog.
				{
					string label = TilePanel.Spriteset.Label.ToUpperInvariant();

					fbd.Description = "Export spriteset to an 8-bpp PNG spritesheet file"
									+ Environment.NewLine + Environment.NewLine
									+ "\t" + label;

					if (!Directory.Exists(_lastSpriteDirectory))
					{
						string dir = Path.GetDirectoryName(_path);
						if (Directory.Exists(dir))
							fbd.SelectedPath = dir;
					}
					else
						fbd.SelectedPath = _lastSpriteDirectory;


					if (fbd.ShowDialog(this) == DialogResult.OK)
					{
						_lastSpriteDirectory = fbd.SelectedPath;

						string pfe = Path.Combine(_lastSpriteDirectory, label + GlobalsXC.PngExt);
						// TODO: Ask to overwrite an existing file.
						BitmapService.ExportSpritesheet(
													pfe,
													TilePanel.Spriteset,
													GetCurrentPalette());
//													8, SetType == Type.LoFT
					}
				}
			}
		}

		/// <summary>
		/// Imports (and replaces) the current spriteset from an external
		/// spritesheet.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// <remarks>Called when the File menu's click-event is raised.</remarks>
		private void OnImportSpritesheetClick(object sender, EventArgs e)
		{
			if (TilePanel.Spriteset != null)
			{
				using (var ofd = new OpenFileDialog())
				{
					ofd.Title = "Import an 8-bpp spritesheet file";
					ofd.Filter = FileDialogStrings.GetFilter();

					if (!Directory.Exists(_lastSpriteDirectory))
					{
						string dir = Path.GetDirectoryName(_path);
						if (Directory.Exists(dir))
							ofd.InitialDirectory = dir;
					}
					else
						ofd.InitialDirectory = _lastSpriteDirectory;


					if (ofd.ShowDialog(this) == DialogResult.OK)
					{
						byte[] bindata = FileService.ReadFile(ofd.FileName);
						if (bindata != null) // else error was shown by FileService.
						{
							using (Bitmap b = BitmapLoader.LoadBitmap(bindata))
							{
								if (b != null) // else error was shown by BitmapLoader.
								{
									if (   b.Width  % XCImage.SpriteWidth  != 0
										|| b.Height % XCImage.SpriteHeight != 0
										|| b.PixelFormat != PixelFormat.Format8bppIndexed)
									{
										ShowBitmapError(false);
									}
									else
									{
										TilePanel.Spriteset.Dispose();
										BitmapService.CreateSprites(
																TilePanel.Spriteset.Sprites,
																b,
																GetCurrentPalette(),
																XCImage.SpriteWidth,
																XCImage.SpriteHeight,
																SetType == Type.ScanG || SetType == Type.LoFT);
										SpritesetCountChanged(-1);
									}
								}
							}
						}
					}
				}
			}
		}

		/// <summary>
		/// Closes the app. Called when the File menu's click-event is raised.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void OnQuitClick(object sender, EventArgs e)
		{
			Close();
		}

		/// <summary>
		/// Changes the current palette. Called when the Palette menu's click-
		/// event is raised whether by mouse or keyboard.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// <remarks>LoFTsets don't need their palette set; their palette is set
		/// on creation and don't change.</remarks>
		private void OnPaletteClick(object sender, EventArgs e)
		{
			var it = sender as MenuItem;
			if (!it.Checked)
			{
				if (Pal != null)
					_itPalettes[Pal].Checked = false;

				it.Checked = true;

				Pal = it.Tag as Palette;
				Pal.SetTransparent(miTransparent.Checked);

				if (TilePanel.Spriteset != null && SetType != Type.LoFT)
					TilePanel.Spriteset.Pal = Pal;

				PaletteChanged(); // TODO: That probably doesn't need to fire if a LoFTset is loaded.

				SpriteEditor._fpalette.Text = "Palette - " + Pal.Label;
			}
		}

		/// <summary>
		/// Toggles transparency of the currently loaded palette. Called when
		/// the Palette menu's click-event is raised whether by mouse or
		/// keyboard.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// <remarks>LoFTsets don't need their palette set; their palette is set
		/// on creation and don't change.</remarks>
		private void OnTransparencyClick(object sender, EventArgs e)
		{
			Pal.SetTransparent(miTransparent.Checked = !miTransparent.Checked);

			if (TilePanel.Spriteset != null && SetType != Type.LoFT)
				TilePanel.Spriteset.Pal = Pal;

			PaletteChanged(); // TODO: That probably doesn't need to fire if a LoFTset is loaded.
		}

		/// <summary>
		/// Toggles usage of the sprite-shade value of MapView's options. Called
		/// when the Palette menu's click-event is raised.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// <remarks>'SpriteShade' is no longer the sprite-shade value.
		/// 'SpriteShade' was converted to 'SpriteShadeFloat' in the cTor, hence
		/// it can and does take a new definition here:
		/// -2 user toggled sprite-shade off;
		/// -1 sprite-shade was not found by the cTor thus it cannot be enabled;
		///  0 draw sprites/swatches w/ the 'SpriteShadeFloat' val.</remarks>
		private void OnSpriteshadeClick(object sender, EventArgs e)
		{
			if (SpriteShade != SPRITESHADE_DISABLED)
			{
				if (miSpriteShade.Checked = !miSpriteShade.Checked)
				{
					SpriteShade = SPRITESHADE_ON;
				}
				else
					SpriteShade = SPRITESHADE_OFF;

				TilePanel                      .Invalidate();
				SpriteEditor.SpritePanel       .Invalidate();
				SpriteEditor._fpalette.PalPanel.Invalidate();
			}
		}


		/// <summary>
		/// Shows a richtextbox with all the bytes of the currently selected
		/// sprite laid out in a fairly readable fashion. Called when the Bytes
		/// menu's click-event is raised.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void OnBytesClick(object sender, EventArgs e)
		{
			if (miBytes.Checked = !miBytes.Checked)
			{
				XCImage sprite;
				if (TilePanel.Spriteset != null && TilePanel.Selid != -1)
					sprite = TilePanel.Spriteset[TilePanel.Selid];
				else
					sprite = null;

				ByteTableManager.LoadTable(
										sprite,
										SetType,
										BytesClosingCallback);
			}
			else
				ByteTableManager.HideTable();
		}

		/// <summary>
		/// Callback for LoadBytesTable().
		/// </summary>
		private void BytesClosingCallback()
		{
			miBytes.Checked = false;
		}

		/// <summary>
		/// Shows the CHM helpfile. Called when the Help menu's click-event is
		/// raised.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void OnHelpClick(object sender, EventArgs e)
		{
			string help = Path.GetDirectoryName(Application.ExecutablePath);
				   help = Path.Combine(help, "MapView.chm");
			Help.ShowHelp(this, "file://" + help, HelpNavigator.Topic, "html/pckview.htm");
		}

		/// <summary>
		/// Shows the about-box. Called when the Help menu's click-event is
		/// raised.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void OnAboutClick(object sender, EventArgs e)
		{
			new About().ShowDialog(this);
		}

		/// <summary>
		/// is disabled.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void OnCompareClick(object sender, EventArgs e) // disabled in designer w/ Visible=FALSE
		{
/*			var original = TileTable.Spriteset; // store original spriteset

			OnOpenClick(null, EventArgs.Empty); // load a second spriteset
			var spriteset = TileTable.Spriteset;

			TileTable.Spriteset = original; // revert to original spriteset

			if (Controls.Contains(TileTable))
			{
				Controls.Remove(TileTable); // ...

				_tcTabs = new TabControl(); // create tabs
				_tcTabs.Dock = DockStyle.Fill;
				pnlView.Controls.Add(_tcTabs); // add the tabs to the stock panel

				var tabpage = new TabPage(); // create a page
				tabpage.Controls.Add(TileTable); // add the viewer to the page
				tabpage.Text = "Original";
				_tcTabs.TabPages.Add(tabpage); // add the page to the tab-control

				var viewpanel = new PckViewPanel(); // create a second viewer
				viewpanel.ContextMenu = ViewerContextMenu(); // ...
				viewpanel.Dock = DockStyle.Fill;
				viewpanel.Spriteset = spriteset; // assign the second spriteset to the second viewer

				tabpage = new TabPage(); // create a second page
				tabpage.Controls.Add(viewpanel); // add the second viewer to the second page
				tabpage.Text = "Other";
				_tcTabs.TabPages.Add(tabpage); // add the second page to the tab-control.

				// that sounds like a bad idea. Sounds like plenty of stuff
				// would have to be tested and tracked, or disabled to ensure
				// that things still work correctly when some monkey goes, "Oh
				// cool... watch this!" **sproing***
			} */
		}
		#endregion Events


		#region Methods (load)
		/// <summary>
		/// Loads PCK+TAB spriteset files.
		/// @note Pck files require their corresponding Tab file. That is, the
		/// load-routine does not handle Pck files that do not use a Tab file -
		/// eg. single-image Bigobs in the UFOGRAPH directory.
		/// @note May be called from MapView.Forms.Observers.TileView.OnPckEditorClick()
		/// </summary>
		/// <param name="pfePck">path-file-extension of a PCK file</param>
		/// <param name="isBigobs">true if Bigobs, false if terrain or unit Pck</param>
		public void LoadSpriteset(string pfePck, bool isBigobs = false)
		{
			byte[] bytesPck = FileService.ReadFile(pfePck);
			if (bytesPck != null)
			{
				string label = Path.GetFileNameWithoutExtension(pfePck);
				string pf    = Path.Combine(Path.GetDirectoryName(pfePck), label);

				byte[] bytesTab = FileService.ReadFile(pf + GlobalsXC.TabExt);
				if (bytesTab != null)
				{
					int pre_width  = XCImage.SpriteWidth;
					int pre_height = XCImage.SpriteHeight;

					XCImage.SpriteWidth = XCImage.SpriteWidth32;

					int tabwordLength = SpritesetsManager.TAB_WORD_LENGTH_2;
//					Palette pal = Palette.UfoBattle; // User can change this but for now I need a palette ...

					if (isBigobs)
					{
						XCImage.SpriteHeight = XCImage.SpriteHeight48;
					}
					else
					{
						XCImage.SpriteHeight = XCImage.SpriteHeight40;

						if (bytesTab.Length >= SpritesetsManager.TAB_WORD_LENGTH_4
							&& bytesTab[2] == 0
							&& bytesTab[3] == 0) // if both the 3rd or 4th bytes are zero ... it's a TFTD set.
						{
							tabwordLength = SpritesetsManager.TAB_WORD_LENGTH_4;
//							pal = Palette.TftdBattle;
						}
					}

					var spriteset = new Spriteset(
												label,
												Pal, //pal
												tabwordLength,
												bytesPck,
												bytesTab,
												true);

					if ((spriteset.Fail & Spriteset.FAIL_COUNT_MISMATCH) != Spriteset.FAIL_non) // pck vs tab mismatch counts
					{
						spriteset.Dispose();

						XCImage.SpriteWidth  = pre_width;
						XCImage.SpriteHeight = pre_height;

						using (var f = new Infobox(
												"Load error",
												Infobox.SplitString("The count of sprites in the PCK file ["
														+ spriteset.CountSprites + "] does not match"
														+ " the count of sprites expected by the TAB file ["
														+ spriteset.CountOffsets + "]."),
												null,
												Infobox.BoxType.Error))
						{
							f.ShowDialog(this);
						}
					}
					else if ((spriteset.Fail & Spriteset.FAIL_OF_SPRITE) != Spriteset.FAIL_non) // too many bytes for a sprite
					{
						spriteset.Dispose();

						XCImage.SpriteWidth  = pre_width;
						XCImage.SpriteHeight = pre_height;

						string head;
						if (isBigobs)
							head = "Bigobs : "; // won't happen unless a file is corrupt.
						else
							head = String.Empty; // possibly trying to load a Bigobs to 32x40

						head += "File data overflowed the sprite's count of pixels.";

						using (var f = new Infobox(
												"Load error",
												head,
												null,
												Infobox.BoxType.Error))
						{
							f.ShowDialog(this);
						}
					}
					else
					{
						if (isBigobs) SetType = Type.Bigobs;
						else          SetType = Type.Pck;

						if (TilePanel.Spriteset != null)
							TilePanel.Spriteset.Dispose();

						TilePanel.Spriteset = spriteset;

//						if (!_itPalettes[pal].Checked)
//						{
//							miTransparent.Checked = true;
//							OnPaletteClick(_itPalettes[pal], EventArgs.Empty);
//						}
//						else if (!miTransparent.Checked)
//						{
//							OnTransparencyClick(null, EventArgs.Empty);
//						}

						_path = pf;
						Changed = false;
					}
				}
			}
		}

		/// <summary>
		/// Loads a ScanG iconset.
		/// </summary>
		/// <param name="pfeScanG">path-file-extension of SCANG.DAT</param>
		private void LoadScanG(string pfeScanG)
		{
			using (var fs = FileService.OpenFile(pfeScanG))
			if (fs != null)
			{
				if (((int)fs.Length % ScanGicon.Length_ScanG) != 0)
				{
					using (var f = new Infobox(
											"Load error",
											Infobox.SplitString("The file appears to be corrupted. The length of the"
													+ " file is not exactly divisible by the length of an icon."),
											pfeScanG,
											Infobox.BoxType.Error))
					{
						f.ShowDialog(this);
					}
				}
				else
				{
					SetType = Type.ScanG;

					XCImage.SpriteWidth  =
					XCImage.SpriteHeight = XCImage.ScanGside;

					if (TilePanel.Spriteset != null)
						TilePanel.Spriteset.Dispose();

					TilePanel.Spriteset = new Spriteset(Path.GetFileNameWithoutExtension(pfeScanG), fs, false);

//					if (!_itPalettes[Palette.UfoBattle].Checked)
//					{
//						miTransparent.Checked = true;
//						OnPaletteClick(
//									_itPalettes[Palette.UfoBattle],
//									EventArgs.Empty);
//					}
//					else if (!miTransparent.Checked)
//					{
//						OnTransparencyClick(null, EventArgs.Empty);
//					}

					_path = pfeScanG;
					Changed = false;
				}
			}
		}

		/// <summary>
		/// Loads a LoFT iconset.
		/// </summary>
		/// <param name="pfeLoFT">path-file-extension of LOFTEMPS.DAT</param>
		private void LoadLoFT(string pfeLoFT)
		{
			using (var fs = FileService.OpenFile(pfeLoFT))
			if (fs != null)
			{
				if (((int)fs.Length % LoFTicon.Length_LoFT) != 0)
				{
					using (var f = new Infobox(
											"Load error",
											Infobox.SplitString("The file appears to be corrupted. The length of the"
													+ " file is not exactly divisible by the length of an icon."),
											pfeLoFT,
											Infobox.BoxType.Error))
					{
						f.ShowDialog(this);
					}
				}
				else
				{
					SetType = Type.LoFT;

					XCImage.SpriteWidth  =
					XCImage.SpriteHeight = XCImage.LoFTside;

					if (TilePanel.Spriteset != null)
						TilePanel.Spriteset.Dispose();

					TilePanel.Spriteset = new Spriteset(Path.GetFileNameWithoutExtension(pfeLoFT), fs, true);

//					if (!_itPalettes[Palette.TftdGeo].Checked) // 'Palette.TftdGeo' has white palid #1 (255,255,255)
//					{
//						miTransparent.Checked = false;
//						OnPaletteClick(
//									_itPalettes[Palette.TftdGeo],
//									EventArgs.Empty);
//					}
//					else if (miTransparent.Checked)
//					{
//						OnTransparencyClick(null, EventArgs.Empty);
//					}

					if (SpriteEditor._fpalette.Visible)
						SpriteEditor._fpalette.Close(); // actually Hide() + uncheck the SpriteEditor's it

					_path = pfeLoFT;
					Changed = false;
				}
			}
		}
		#endregion Methods (load)


		#region Methods
		/// <summary>
		/// Sets the current palette. Called only from TileView to set the
		/// palette externally.
		/// </summary>
		/// <param name="pal"></param>
		public void SetPalette(Palette pal)
		{
			OnPaletteClick(_itPalettes[pal], EventArgs.Empty);
		}

		/// <summary>
		/// Gets the currently selected palette unless a LoFTset is loaded, in
		/// which case return 'Palette.Binary'.
		/// </summary>
		/// <returns>the current palette or the binary palette if a LoFTset is
		/// loaded</returns>
		internal Palette GetCurrentPalette()
		{
			if (SetType == Type.LoFT)
				return Palette.Binary;

			return Pal;
		}

		/// <summary>
		/// Enables or disables various menus and initializes the statusbar.
		/// </summary>
		/// <remarks>Called only when the spriteset changes in
		/// <see cref="PckViewPanel.Spriteset"/></remarks>
		internal void EnableInterface()
		{
			SpriteEditor.SpritePanel.Sprite = null;

			miSave             .Enabled =									// File ->
			miSaveAs           .Enabled =
			miExportSprites    .Enabled =
			miExportSpritesheet.Enabled =
			miImportSpritesheet.Enabled =
			miPaletteMenu      .Enabled =									// Main
			_miAdd             .Enabled = (TilePanel.Spriteset != null);	// context

			EnableContext();

			SpriteEditor.OnLoad(null, EventArgs.Empty); // resize the Editor to the sprite-size

			PrintTotal();
			PrintSelected();
			PrintOver();

			PrintSpritesetLabel();

			// NOTE: Although the palette 'Pal' does not change here the
			// palette-viewer might need to change its statusbar description if
			// either palid #254 or #255 is currently selected.
			PaletteChanged(); // TODO: That probably doesn't need to fire if a LoFTset is loaded.
		}

		/// <summary>
		/// Enables or disables several context its.
		/// </summary>
		private void EnableContext()
		{
			bool enabled = (TilePanel.Selid != -1);

			_miInsertBefor.Enabled = // Context ->
			_miInsertAfter.Enabled =
			_miReplace    .Enabled =
			_miDelete     .Enabled =
			_miExport     .Enabled = enabled;

			_miMoveL.Enabled = enabled && (TilePanel.Selid != 0);
			_miMoveR.Enabled = enabled && (TilePanel.Selid != TilePanel.Spriteset.Count - 1);
		}

		/// <summary>
		/// Sets the currently selected sprite-id.
		/// </summary>
		/// <param name="id">the sprite-id to select</param>
		/// <param name="force">true to force init even if <see cref="PckViewPanel.Selid"/>
		/// doesn't change</param>
		/// <returns>true if currently selected sprite-id changed or is forced</returns>
		/// <remarks>Can be called by TileView to set <see cref="PckViewPanel.Selid"/>
		/// externally.</remarks>
		public bool SetSelected(int id, bool force = false)
		{
			if (id != TilePanel.Selid || force)
			{
				TilePanel.Selid = id;

				if (id != -1 && id < TilePanel.Spriteset.Count)
				{
					SpriteEditor.SpritePanel.Sprite = TilePanel.Spriteset[id];
				}
				else
					SpriteEditor.SpritePanel.Sprite = null;

				TilePanel.ScrollToTile();

				EnableContext();

				PrintSelected();

				return true;
			}
			return false;
		}

		/// <summary>
		/// Updates the status-information for the sprite that is currently
		/// selected.
		/// </summary>
		internal void PrintSelected()
		{
			string selected;

			int id = TilePanel.Selid;
			if (id != -1)
			{
				selected = id.ToString();
				if (SetType == Type.ScanG)
				{
					if (id > 34)
						selected += " [" + (id - 35) + "]";
					else
						selected += " [0]";
				}
			}
			else
				selected = None;

			tssl_TileSelected.Text = Selected + selected;

			PrintOffsets();
		}

		/// <summary>
		/// Prints last and after offsets to the statubar.
		/// </summary>
		/// <remarks>Helper for <see cref="PrintSelected"/></remarks>
		private void PrintOffsets()
		{
			if (   TilePanel.Spriteset != null
				&& TilePanel.Spriteset.TabwordLength == SpritesetsManager.TAB_WORD_LENGTH_2)
			{
				int id;
				if (TilePanel.Selid != -1) id = TilePanel.Selid;
				else                       id = TilePanel.Spriteset.Count - 1;

				uint last, aftr;
				Spriteset.TestTabOffsets(TilePanel.Spriteset, out last, out aftr, id);

				tssl_OffsetLast.ForeColor = (last > UInt16.MaxValue) ? Color.Crimson : SystemColors.ControlText;
				tssl_OffsetAftr.ForeColor = (aftr > UInt16.MaxValue) ? Color.Crimson : SystemColors.ControlText;

				tssl_OffsetLast.Text = last.ToString();
				tssl_OffsetAftr.Text = aftr.ToString();

				tssl_Offset    .Visible =
				tssl_OffsetLast.Visible =
				tssl_OffsetAftr.Visible = true;

				tssl_SpritesetLabel.BorderSides = ToolStripStatusLabelBorderSides.Right;
			}
			else
			{
				tssl_OffsetLast.Text =
				tssl_OffsetAftr.Text = String.Empty;

				tssl_Offset    .Visible =
				tssl_OffsetLast.Visible =
				tssl_OffsetAftr.Visible = false;

				tssl_SpritesetLabel.BorderSides = ToolStripStatusLabelBorderSides.None;
			}
		}

		/// <summary>
		/// Updates the status-information for the sprite that the cursor is
		/// currently over.
		/// </summary>
		internal void PrintOver()
		{
			string text;
			if (TilePanel.Ovid != -1)
				text = TilePanel.Ovid.ToString();
			else
				text = None;

			tssl_TileOver.Text = Over + text;
		}

		/// <summary>
		/// Prints the quantity of sprites in the currently loaded spriteset to
		/// the statusbar.
		/// </summary>
		private void PrintTotal()
		{
			if (TilePanel.Spriteset != null)
				tssl_TilesTotal.Text = Total + TilePanel.Spriteset.Count;
			else
				tssl_TilesTotal.Text = String.Empty;
		}

		/// <summary>
		/// Prints the label of the currently loaded spriteset to the statubar.
		/// </summary>
		/// <remarks>Helper for <see cref="EnableInterface"/></remarks>
		private void PrintSpritesetLabel()
		{
			string text;
			if (TilePanel.Spriteset != null)
			{
				text = TilePanel.Spriteset.Label;

				switch (SetType)
				{
					case Type.Pck:    text += " (32x40)"; break;
					case Type.Bigobs: text += " (32x48)"; break;
					case Type.ScanG:  text += " (4x4)";   break;
					case Type.LoFT:   text += " (16x16)"; break;
				}
			}
			else
				text = String.Empty;

			tssl_SpritesetLabel.Text = text;
		}


		/// <summary>
		/// Checks state of the 'Changed' flag and/or asks user if the spriteset
		/// ought be closed anyway.
		/// </summary>
		/// <returns>true if state is NOT changed or 'DialogResult.OK'</returns>
		private bool RequestSpritesetClose()
		{
			if (Changed)
			{
				using (var f = new Infobox(
										"Spriteset changed",
										"The spriteset has changed. Do you really want to close it?",
										null,
										Infobox.BoxType.Warn,
										Infobox.Buttons.CancelOkay))
				{
					return (f.ShowDialog(this) == DialogResult.OK);
				}
			}
			return true;
		}
		#endregion Methods
	}
}
