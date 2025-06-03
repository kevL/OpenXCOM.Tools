using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Reflection;
#if !__MonoCS__
using System.Runtime.InteropServices;
#endif
using System.Windows.Forms;

using DSShared;
using DSShared.Controls;

using MapView.Forms.MainView;
using MapView.Forms.Observers;

using XCom;

using YamlDotNet.RepresentationModel; // read values (deserialization)


namespace MapView
{
	/// <summary>
	/// Instantiates a MainView screen as the basis for all user-interaction.
	/// </summary>
	internal sealed partial class MainViewF
		:
			Form
#if !__MonoCS__
			, IMessageFilter
#endif
	{
#if !__MonoCS__
		#region P/Invoke declarations
		[DllImport("user32.dll")]
		static extern IntPtr WindowFromPoint(Point pt);

		[DllImport("user32.dll")]
		static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wp, IntPtr lp);
		#endregion P/Invoke declarations

		#region IMessageFilter
		/// <summary>
		/// Sends mousewheel messages to the control that the mouse-cursor is
		/// hovering over.
		/// </summary>
		/// <param name="m">the message</param>
		/// <returns>true if a mousewheel message was handled successfully</returns>
		/// <remarks>https://stackoverflow.com/questions/4769854/windows-forms-capturing-mousewheel#4769961</remarks>
		public bool PreFilterMessage(ref Message m)
		{
			if (m.Msg == 0x20a)
			{
				// WM_MOUSEWHEEL - find the control at screen position m.LParam
				var pos = new Point(m.LParam.ToInt32());

				IntPtr hWnd = WindowFromPoint(pos);
				if (hWnd != IntPtr.Zero && hWnd != m.HWnd && Control.FromHandle(hWnd) != null)
				{
					SendMessage(hWnd, m.Msg, m.WParam, m.LParam);
					return true;
				}
			}
			return false;
		}
		#endregion IMessageFilter
#endif

		#region Delegates
		/// <summary>
		/// Good fuckin Lord I just wrote a "DontBeep" delegate.
		/// </summary>
		internal delegate void DontBeepEventHandler();
		#endregion Delegates


		#region Events
		internal event DontBeepEventHandler DontBeepEvent;
		#endregion Events


		#region Fields (static)
		private const string TITLE = "Map Editor ||";

		private const int TREELEVEL_GROUP    = 0;
		private const int TREELEVEL_CATEGORY = 1;
		private const int TREELEVEL_TILESET  = 2;

		/// <summary>
		/// Stops <c><see cref="MainViewOverlay"/>.OnPaint()</c> when quitting
		/// per <c><see cref="SafeQuit()">SafeQuit()</see></c> or if reloading a
		/// Mapfile per
		/// <c><see cref="OnReloadDescriptor()">OnReloadDescriptor()</see></c>.
		/// </summary>
		/// <remarks>If the currently loaded tileset has crippled tileparts and
		/// MapView is closing .net will try to paint <c>MainViewOverlay</c> one
		/// last time. But the crippled tileparts' sprites have just been
		/// disposed and nulled ...
		/// <br/><br/>
		/// Or when reloading a tileset that has crippled tileparts the
		/// warning dialog appears before the sprites for the regular tileparts
		/// are valid ... and MainView attempts to paint <c>MainViewOverlay</c>
		/// as the dialog is displayed.</remarks>
		internal static bool Dontdrawyougits;


		internal static readonly Pen        TreenodeLine_def    = new Pen(       MainViewOptionables.def_TreeBackcolor);
		internal static readonly SolidBrush TreenodeFill_def    = new SolidBrush(MainViewOptionables.def_TreeBackcolor);

		internal static readonly Pen        TreenodeLine_sel    = new Pen(       MainViewOptionables.def_TreenodeSelectedBordercolor);

		internal static readonly SolidBrush TreenodeFill_selfoc = new SolidBrush(MainViewOptionables.def_TreenodeSelectedBackcolor_foc);
		internal static readonly SolidBrush TreenodeFill_selunf = new SolidBrush(MainViewOptionables.def_TreenodeSelectedBackcolor_unf);

		internal static readonly SolidBrush TreenodeFill_serfoc = new SolidBrush(MainViewOptionables.def_TreenodeSearchedBackcolor_foc);
		internal static readonly SolidBrush TreenodeFill_serunf = new SolidBrush(MainViewOptionables.def_TreenodeSearchedBackcolor_unf);
		#endregion Fields (static)


		#region Fields
		internal CompositedTreeView MapTree;

		internal readonly Options Options = new Options();

		internal ColorHelp     _fcolors;
		private  About         _fabout;
		private  MapInfoDialog _finfo;


		private MainViewUnderlay _underlay;
		private MainViewOverlay  _overlay;
		#endregion Fields


		#region Properties (static)
		internal static MainViewF that
		{ get; private set; }

		/// <summary>
		/// A class-object that holds MainView's optionable Properties.
		/// @note C# doesn't allow inheritance of multiple class-objects, which
		/// would have been a way to separate the optionable properties from all
		/// the other properties that are not optionable; they need to be
		/// separate or else all Properties would show up in the Options form's
		/// PropertyGrid. An alternative would have been to give all those other
		/// properties the Browsable(false) attribute but I didn't want to
		/// clutter up the code and also because the Browsable(false) attribute
		/// is used to hide Properties from the designer also - but whether or
		/// not they are accessible in the designer is an entirely different
		/// consideration than whether or not they are Optionable Properties. So
		/// I created an independent class just to hold and handle MainView's
		/// Optionable Properties ... and wired it up. It's a tedious shitfest
		/// but better than the arcane MapViewI system or screwing around with
		/// equally arcane TypeDescriptors. Both of which had been implemented
		/// but then rejected.
		/// </summary>
		internal static MainViewOptionables Optionables
		{ get; private set; }

		/// <summary>
		/// Allows MainView and all its subsidiary forms to close, otherwise
		/// usually only hide. This boolean gets flagged true only by MainView's
		/// FormClosing event-handler. But it shall be checked by the
		/// FormClosing event-handlers of any subsidiary form that should only
		/// hide unless MainView itself is closing and therefore flagged to
		/// Quit. If the flag is still false, 'e.Cancel' any subsidiary form
		/// from closing in its FormClosing event-handler.
		/// </summary>
		internal static bool Quit
		{ get; private set; }

		internal static ScanGViewer ScanG
		{ get; set; }

		internal static Spriteset MonotoneSprites
		{ get; private set; }
		#endregion Properties (static)


		#region Properties
		private MapFile _file;
		/// <summary>
		/// Gets/Sets the <c><see cref="XCom.MapFile"/></c> for this
		/// <c>MainViewF</c> and passes the pointer along to
		/// <c><see cref="MainViewUnderlay"/>._file</c> and
		/// <c><see cref="MainViewOverlay"/>._file</c>.
		/// </summary>
		internal MapFile MapFile
		{
			get { return _file; }
			private set
			{
				_file = value;

				Dontdrawyougits = true;
				_underlay.SetMapFile(_file);
				_overlay .SetMapFile(_file);
				Dontdrawyougits = false;
			}
		}


		private bool _treeChanged;
		/// <summary>
		/// Gets/Sets the <c>MaptreeChanged</c> flag.
		/// </summary>
		internal bool MaptreeChanged
		{
			get { return _treeChanged; }
			set
			{
				if (_treeChanged = value)
				{
					if (!Text.EndsWith(GlobalsXC.PADDED_ASTERISK, StringComparison.Ordinal))
						Text += GlobalsXC.PADDED_ASTERISK;
				}
				else if (Text.EndsWith(GlobalsXC.PADDED_ASTERISK, StringComparison.Ordinal))
					Text = Text.Substring(0, Text.Length - GlobalsXC.PADDED_ASTERISK.Length);
			}
		}

		/// <summary>
		/// Sets the <c>MapChanged</c> flag.
		/// </summary>
		/// <remarks>This is only an intermediary that adds an asterisk to the
		/// file-label in MainView's statusbar; the real flag is stored in
		/// <c><see cref="MapFile">MapFile</see>.MapChanged</c>.
		/// reasons.</remarks>
		internal bool MapChanged
		{
			set
			{
				string text = tsslMapLabel.Text;
				if (MapFile.MapChanged = value) // shuffle the value down to MapFile.MapChanged ...
				{
					if (!text.EndsWith(GlobalsXC.PADDED_ASTERISK, StringComparison.Ordinal))
						text += GlobalsXC.PADDED_ASTERISK;
				}
				else if (text.EndsWith(GlobalsXC.PADDED_ASTERISK, StringComparison.Ordinal))
					text = text.Substring(0, text.Length - GlobalsXC.PADDED_ASTERISK.Length);

				tsslMapLabel.Text = text;
			}
		}

		/// <summary>
		/// The currently searched and found and highlighted treenode on the
		/// <c><see cref="MapTree"/></c>.
		/// </summary>
		private TreeNode Searched
		{ get; set; }
		#endregion Properties


		#region cTor
		/// <summary>
		/// This is where the user-app end of things *really* starts.
		/// </summary>
		internal MainViewF()
		{
			Cursor.Current = Cursors.AppStarting;


			string dirAppL = Path.GetDirectoryName(Application.ExecutablePath);
			string dirSetT = Path.Combine(dirAppL, PathInfo.DIR_Settings);
#if DEBUG
			Logfile.SetPath(dirAppL); // creates a logfile/wipes the old one.
#endif

			//Logfile.Log("Instantiating MAIN MapView window ...");

			// TODO: Either move all this SharedSpace stuff to DSShared so it
			// can be implemented/instantiated for Mcd/PckView also, or better
			// get rid of it (or at least de-spaghettify it as much as possible).

			SharedSpace.SetShare(SharedSpace.ApplicationDirectory, dirAppL);
			SharedSpace.SetShare(SharedSpace.SettingsDirectory,    dirSetT);

			//Logfile.Log("App paths cached.");


			// TODO: The .NET framework already has a class for "PathInfo":
			// https://docs.microsoft.com/en-us/dotnet/api/system.io.fileinfo?view=netframework-4.8
			// ie. FileInfo.

			var piOptions   = new PathInfo(dirSetT, PathInfo.CFG_Options);		// define a PathInfo for 'MapOptions.cfg'
			var piResources = new PathInfo(dirSetT, PathInfo.YML_Resources);	// define a PathInfo for 'MapResources.yml'
			var piTilesets  = new PathInfo(dirSetT, PathInfo.YML_Tilesets);		// define a PathInfo for 'MapTilesets.yml'
			var piViewers   = new PathInfo(dirSetT, PathInfo.YML_Viewers);		// define a PathInfo for 'MapViewers.yml'

			SharedSpace.SetShare(SharedSpace.MapOptionsFile,   piOptions);		// set share for 'MapOptions.cfg'
			SharedSpace.SetShare(SharedSpace.MapResourcesFile, piResources);	// set share for 'MapResources.yml'
			SharedSpace.SetShare(SharedSpace.MapTilesetsFile,  piTilesets);		// set share for 'MapTilesets.yml'
			SharedSpace.SetShare(SharedSpace.MapViewersFile,   piViewers);		// set share for 'MapViewers.yml'

			//Logfile.Log("PathInfo cached.");


			// Check if MapTilesets.yml and MapResources.yml exist yet, show the
			// Configuration window if not.
			// NOTE: MapResources.yml and MapTilesets.yml are created by ConfigurationForm.
			if (!piResources.FileExists() || !piTilesets.FileExists())
			{
				//Logfile.Log("Resources or Tilesets file does not exist: run configurator.");

				using (var f = new ConfigurationForm())
					f.ShowDialog(this);
			}
//			else Logfile.Log("Resources and Tilesets files exist.");


			// Exit app if either MapResources.yml or MapTilesets.yml doesn't exist
			if (!piResources.FileExists() || !piTilesets.FileExists()) // safety. The Configurator shall demand that both these files get created.
			{
				//Logfile.Log("Resources or Tilesets file does not exist: quit MapView.");
				using (var dialog = new Infobox(
											"Error",
											"Cannot find configuration files. The application will exit.",
											null,
											InfoboxType.Error))
				{
					dialog.ShowDialog(this);
				}
				Process.GetCurrentProcess().Kill();
			}


			// Check if settings/MapViewers.yml exists yet, if not create it
			if (!piViewers.FileExists())
			{
				CopyViewersFile(piViewers.Fullpath);
//				if (CopyViewersFile(piViewers.Fullpath))
//					Logfile.Log("Viewers file created.");
//				else
//					Logfile.Log("Viewers file could not be created.");
			}
//			else Logfile.Log("Viewers file exists.");



			InitializeComponent(); // ffs. This fires OnActivated but the Optionables aren't ready yet.
			//Logfile.Log("MainView initialized.");


			var splitter = new CollapsibleSplitter(); // NOTE: This needs to be weird ->
			Controls.Add(splitter);

			MapTree = new CompositedTreeView();

			MapTree.DrawNode       += OnMaptreeDrawNode;
			MapTree.GotFocus       += OnMaptreeFocusChanged;
			MapTree.LostFocus      += OnMaptreeFocusChanged;
			MapTree.MouseDown      += OnMaptreeMouseDown;
			MapTree.BeforeSelect   += OnMaptreeBeforeSelect;
			MapTree.AfterSelect    += OnMaptreeAfterSelect;
			MapTree.NodeMouseClick += OnMaptreeNodeMouseClick;
//			MapTree.NodeMouseClick += (sender, args) => MapTree.SelectedNode = args.Node;

			Controls.Add(MapTree);
			splitter.SetControl(MapTree);

			MapTree.TreeViewNodeSorter = StringComparer.Ordinal;


			// WORKAROUND: The size of the form in the designer keeps increasing
			// (for whatever reason) based on the
			// 'SystemInformation.CaptionButtonSize.Height' value (the titlebar
			// height - note that 'SystemInformation.CaptionHeight' seems to be
			// 1 pixel larger, which (for whatever reason) is not the
			// 'SystemInformation.BorderSize.Height'). To prevent all that, in
			// the designer cap the form's Size by setting its MaximumSize to
			// the Size - but now run this code that sets the MaximumSize to
			// "0,0" (unlimited, as wanted). And, as a safety, do the same thing
			// with MinimumSize ... if desired.
			//
			// - observed & tested in SharpDevelop 5.1
			//
			// NOTE: This code (the constructor of a Form) shouldn't run when
			// opening the designer; it appears to run only when actually
			// running the application:
			// NOTE: Mono users have to restart the app for the window to be
			// resizable.

			MaximumSize =
			MinimumSize = new Size(0,0); // fu.net


			QuadrantDrawService.SetQuadrantPaths();
			//Logfile.Log("Quadrant panel graphics paths cached.");


			that = this;

			_underlay = new MainViewUnderlay();
			_overlay  = MainViewOverlay.that;
			//Logfile.Log("MainView panels instantiated.");

			RegistryInfo.InitializeRegistry(dirAppL);
			//Logfile.Log("Registry initialized.");
			RegistryInfo.RegisterProperties(this);
			//Logfile.Log("MainView registered.");

			Optionables = new MainViewOptionables();
			//Logfile.Log("MainView optionables initialized.");


			Palette.UfoBattle.SetTransparent(true); // WARNING: ufo/tftd Palettes created here ->
			//Logfile.Log("ufo-battle Palette instantiated.");
			Palette.TftdBattle.SetTransparent(true);
			//Logfile.Log("tftd-battle Palette instantiated.");
			//Logfile.Log("Palette transparencies set.");

			Palette.CreateUfoBrushes();  // for Mono draw
			Palette.CreateTftdBrushes(); // for Mono draw


			OptionsManager.SetOptionsSection(RegistryInfo.MainView, Options);

			Optionables.LoadDefaults(Options);					// TODO: check if this should go after the managers load
			//Logfile.Log("MainView Default Options loaded.");	// since managers might be re-instantiating needlessly
																// when OnOptionsClick() runs ....

			Palette.UfoBattle.CreateTonescaledPalettes(Optionables.SelectedTonerBrightness);
			//Logfile.Log("ufo-battle Tonescaled Palettes instantiated.");
			Palette.TftdBattle.CreateTonescaledPalettes(Optionables.SelectedTonerBrightness);
			//Logfile.Log("tftd-battle Tonescaled Palettes instantiated.");


			MonotoneSprites = EmbeddedService.CreateMonotoneSpriteset("Monotone");	// sprites for TileView's eraser and QuadrantControl's blank quads.
																					// NOTE: transparency of the 'UfoBattle' palette must be set first.
			//Logfile.Log("Monotone sprites loaded.");


			ObserverManager.CreateObservers(); // adds each subsidiary viewer's options and Options-type etc.
			//Logfile.Log("ObserverManager initialized.");

			ObserverManager.TileView.Control.ReloadDescriptor += OnReloadDescriptor;


			tscPanel.ContentPanel.Controls.Add(_underlay);

			tsTools.SuspendLayout();
			ObserverManager.ToolFactory.AddSearchTools(tsTools);
			ObserverManager.ToolFactory.AddScalerTools(tsTools);
			ObserverManager.ToolFactory.AddEditorTools(tsTools);
			ObserverManager.ToolFactory.AddOptionsTool(tsTools);
			tsTools.ResumeLayout();
			//Logfile.Log("MainView toolstrip created.");

			ViewersMenuManager.Initialize(menuViewers);
			//Logfile.Log("Viewers menu created.");


			PathInfo piScanGufo  = null;
			PathInfo piScanGtftd = null;

			// Read MapResources.yml to get the resources dir (for both UFO and TFTD).
			// NOTE: MapResources.yml is created by ConfigurationForm
			using (var fs = FileService.OpenFile(piResources.Fullpath))
			if (fs != null)
			using (var sr = new StreamReader(fs))
			{
				var str = new YamlStream();
				str.Load(sr);

				string key, val;

				var nodeRoot = str.Documents[0].RootNode as YamlMappingNode;
				foreach (var node in nodeRoot.Children)
				{
					switch (node.Key.ToString())
					{
						// NOTE: These do not check if Directory.Exists().

						case "ufo":
							key = SharedSpace.ResourceDirectoryUfo;
							val = node.Value.ToString();
							if (val == PathInfo.NotConfigured)
								val = null;
							else
								piScanGufo = new PathInfo(Path.Combine(val, PathInfo.ScanGfile));

							SharedSpace.SetShare(key, val);
							break;

						case "tftd":
							key = SharedSpace.ResourceDirectoryTftd;
							val = node.Value.ToString();
							if (val == PathInfo.NotConfigured)
								val = null;
							else
								piScanGtftd = new PathInfo(Path.Combine(val, PathInfo.ScanGfile));

							SharedSpace.SetShare(key, val);
							break;
					}
				}
			}


			// Setup an XCOM cursor-sprite.
			// NOTE: This is the only stock XCOM resource that is required for
			// MapView to start. See ConfigurationForm ...
			string dir;

			if (!String.IsNullOrEmpty(dir = SharedSpace.GetShareString(SharedSpace.ResourceDirectoryUfo))
				&& Directory.Exists(Path.Combine(dir, GlobalsXC.UfographDir)))
			{
				SpritesetManager.SetCursorType(SpritesetManager.CURSOR_UFO); // for spriteset Label
				CuboidSprite.Ufoset = SpritesetManager.CreateSpriteset(
																	PathInfo.CursorFile,
																	dir,
																	Palette.UfoBattle);
				SpritesetManager.SetCursorType(SpritesetManager.CURSOR_non);

				if (CuboidSprite.Ufoset != null)
				{
					if (CuboidSprite.Ufoset.Failreason != SpritesetFail.non)
					{
						CuboidSprite.Ufoset = null;
						//Logfile.Log("UFO Cursor failed to load.");
					}
//					else Logfile.Log("UFO Cursor loaded.");
				}
			}
//			else
//				Logfile.Log("UFO Cursor directory not found.");

			if (!String.IsNullOrEmpty(dir = SharedSpace.GetShareString(SharedSpace.ResourceDirectoryTftd))
				&& Directory.Exists(Path.Combine(dir, GlobalsXC.UfographDir)))
			{
				SpritesetManager.SetCursorType(SpritesetManager.CURSOR_TFTD); // for spriteset Label
				CuboidSprite.Tftdset = SpritesetManager.CreateSpriteset(
																	PathInfo.CursorFile,
																	dir,
																	Palette.TftdBattle);
				SpritesetManager.SetCursorType(SpritesetManager.CURSOR_non);

				if (CuboidSprite.Tftdset != null)
				{
					if (CuboidSprite.Tftdset.Failreason != SpritesetFail.non)
					{
						CuboidSprite.Tftdset = null;
						//Logfile.Log("TFTD Cursor failed to load.");
					}
//					else Logfile.Log("TFTD Cursor loaded.");
				}
			}
//			else Logfile.Log("TFTD Cursor directory not found.");


			// NOTE: ScanG's are conditional loads iff File exists.
			if (piScanGufo != null && piScanGufo.FileExists())
				SpritesetManager.LoadScanGufo(SharedSpace.GetShareString(SharedSpace.ResourceDirectoryUfo));
//			if (piScanGufo != null && piScanGufo.FileExists()
//				&& SpritesetManager.LoadScanGufo(SharedSpace.GetShareString(SharedSpace.ResourceDirectoryUfo)))
//			{
//				Logfile.Log("ScanG UFO loaded.");
//			}
//			else Logfile.Log("ScanG UFO not found.");

			if (piScanGtftd != null && piScanGtftd.FileExists())
				SpritesetManager.LoadScanGtftd(SharedSpace.GetShareString(SharedSpace.ResourceDirectoryTftd));
//			if (piScanGtftd != null && piScanGtftd.FileExists()
//				&& SpritesetManager.LoadScanGtftd(SharedSpace.GetShareString(SharedSpace.ResourceDirectoryTftd)))
//			{
//				Logfile.Log("ScanG TFTD loaded.");
//			}
//			else Logfile.Log("ScanG TFTD not found.");


			TileGroupManager.LoadTileGroups(piTilesets.Fullpath); // load resources from YAML.
			//Logfile.Log("Tilesets loaded/Descriptors created.");


			if (piOptions.FileExists()) // NOTE: load user-options before ViewersMenuManager.StartSecondStageBoosters() in LoadSelectedDescriptor()
			{
				OptionsManager.LoadUserOptions(piOptions.Fullpath);
//				if (OptionsManager.LoadUserOptions(piOptions.Fullpath))
//				{
//					Logfile.Log("User options loaded.");
//				}
//				else Logfile.Log("User options could not be opened.");
			}
//			else Logfile.Log("User options NOT loaded - no options file to load.");


			if (CuboidSprite.Cursorset == null && !CuboidSprite.SetCursor()) // exit app if a cuboid-targeter is not instantiated
			{
				//Logfile.Log("Targeter not instantiated: quit MapView.");

				string copyable = Path.Combine("[basepath]", PathInfo.CursorFile);
					   copyable = copyable + GlobalsXC.PckExt + Environment.NewLine
								+ copyable + GlobalsXC.TabExt;

				using (var dialog = new Infobox(
											"Error",
											"Cannot find CURSOR spriteset. The application will exit.",
											copyable,
											InfoboxType.Error))
				{
					dialog.ShowDialog(MainViewF.that);
				}
				Process.GetCurrentProcess().Kill();
			}


			CreateTree();
			//Logfile.Log("Maptree instantiated.");

			splitter.SetClickableRectangle();
			ShiftSplitter();


			DontBeepEvent += DontBeep;

			ssMain.Renderer = new CustomToolStripRenderer();

#if !__MonoCS__
			// WindowFromPoint() is windows specific. If that ever works with
			// Mono remove this check. Better: Check if function exists or
			// replace function.
			if (Type.GetType("Mono.Runtime") == null)
			{
				Application.AddMessageFilter(this);
			}
			// else // "Mousewheel (partly) not handled. Reason: Mono runtime detected."
#endif
			Cursor.Current = Cursors.Default;
			//Logfile.Log("About to show MainView ..." + Environment.NewLine);
		}
		#endregion cTor


		#region Methods (static)
		/// <summary>
		/// Transposes all the default viewer positions and sizes from the
		/// embedded MapViewers manifest to a file: "settings/MapViewers.yml".
		/// </summary>
		/// <param name="fullpath"></param>
		/// <returns><c>true</c> on success</returns>
		private static bool CopyViewersFile(string fullpath)
		{
			using (var fs = FileService.CreateFile(fullpath))
			if (fs != null)
			using (var sw = new StreamWriter(fs))
			using (var sr = new StreamReader(Assembly.GetExecutingAssembly()
													 .GetManifestResourceStream(PathInfo.MAN_Viewers)))
			{
				string line;
				while ((line = sr.ReadLine()) != null)
					sw.WriteLine(line);

				return true;
			}
			return false;
		}
		#endregion Methods (static)


		#region Create tree
		/// <summary>
		/// Creates the <c><see cref="MapTree"/></c> on the left side of
		/// MainView.
		/// </summary>
		private void CreateTree()
		{
			//Logfile.Log("MainViewF.CreateTree()");

			MapTree.BeginUpdate();
			MapTree.Nodes.Clear();

			TileGroup tileGroup;
			SortableTreeNode nodeGroup;
			Dictionary<string, Descriptor> category;
			SortableTreeNode nodeCategory;
			SortableTreeNode nodeTileset;

			Dictionary<string, TileGroup> groups = TileGroupManager.TileGroups;
			//Logfile.Log(". groups= " + groups);

			foreach (string keyGroup in groups.Keys)
			{
				//Logfile.Log(". . keyGroup= " + keyGroup);

				tileGroup = groups[keyGroup];

				nodeGroup = new SortableTreeNode(tileGroup.Label);
//				nodeGroup.Tag = tileGroup;								// <- Tag not used.
				MapTree.Nodes.Add(nodeGroup);

				foreach (string keyCategory in tileGroup.Categories.Keys)
				{
					//Logfile.Log(". . . keyCategory= " + keyCategory);

					nodeCategory = new SortableTreeNode(keyCategory);
					category = tileGroup.Categories[keyCategory];
//					nodeCategory.Tag = category;						// <- Tag not used.
					nodeGroup.Nodes.Add(nodeCategory);

					foreach (string keyTileset in category.Keys)
					{
						//Logfile.Log(". . . . keyTileset= " + keyTileset);

						nodeTileset = new SortableTreeNode(keyTileset);
						nodeTileset.Tag = category[keyTileset];			// <- Tag is Descriptor
						nodeCategory.Nodes.Add(nodeTileset);
					}
				}
			}
			MapTree.EndUpdate();
		}

		/// <summary>
		/// A <c>TreeNode</c> that can be sorted.
		/// </summary>
		private sealed class SortableTreeNode
			:
				TreeNode,
				IComparable
		{
			public SortableTreeNode(string text)
				:
					base(text)
			{}

			/// <summary>
			/// Required by <c>IComparable</c>.
			/// </summary>
			/// <param name="other"></param>
			/// <returns></returns>
			public int CompareTo(object other)
			{
				return String.Compare(
									Text,
									(other as SortableTreeNode).Text,
									StringComparison.Ordinal);
			}
		}

		/// <summary>
		/// Shifts the splitter between the <c><see cref="MapTree"/></c> and the
		/// panel to ensure that the longest tree-node's Text gets fully
		/// displayed.
		/// </summary>
		private void ShiftSplitter()
		{
			int width = MapTree.Width, widthTest;

			foreach (TreeNode node0 in MapTree.Nodes)
			{
				widthTest = TextRenderer.MeasureText(node0.Text, MapTree.Font).Width + 30;
				if (widthTest > width)
					width = widthTest;

				foreach (TreeNode node1 in node0.Nodes)
				{
					widthTest = TextRenderer.MeasureText(node1.Text, MapTree.Font).Width + 60;
					if (widthTest > width)
						width = widthTest;

					foreach (TreeNode node2 in node1.Nodes)
					{
						widthTest = TextRenderer.MeasureText(node2.Text, MapTree.Font).Width + 90;
						if (widthTest > width)
							width = widthTest;
					}
				}
			}
			MapTree.Width = width;
		}
		#endregion Create tree


		#region Options
/*		/// <summary>
		/// Loads user-settings into MainView's Options screen.
		/// </summary>
		private void LoadDefaultOptions()
		{
			// kL_note: This is for retrieving MainView's location and size from
			// the Windows Registry:
//			using (var keySoftware = Registry.CurrentUser.CreateSubKey(DSShared.Windows.RegistryInfo.SoftwareRegistry))
//			using (var keyMapView  = keySoftware.CreateSubKey(DSShared.Windows.RegistryInfo.MapViewRegistry))
//			using (var keyMainView = keyMapView.CreateSubKey("MainView"))
//			{
//				Left   = (int)keyMainView.GetValue("Left",   Left);
//				Top    = (int)keyMainView.GetValue("Top",    Top);
//				Width  = (int)keyMainView.GetValue("Width",  Width);
//				Height = (int)keyMainView.GetValue("Height", Height);
//				keyMainView.Close();
//				keyMapView.Close();
//				keySoftware.Close();
//			}
		} */


		internal static OptionsF _foptions; // is static for no special reason

		/// <summary>
		/// Handles a click on the Options button to show or hide an
		/// <c><see cref="OptionsF"/></c>. Instantiates an <c>OptionsF</c> if
		/// one doesn't exist for this viewer. Also subscribes to a
		/// <c>FormClosing</c> handler that will hide the <c>Form</c> unless
		/// MapView is quitting.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		internal void OnOptionsClick(object sender, EventArgs e)
		{
			if (miOptions.Checked && _foptions.WindowState == FormWindowState.Minimized)
			{
				_foptions.WindowState = FormWindowState.Normal;
			}
			else if (miOptions.Checked = !miOptions.Checked)
			{
				ObserverManager.ToolFactory.GetOptionsButton().Checked = true;

				if (_foptions == null)
				{
					_foptions = new OptionsF(
										Optionables,
										Options,
										OptionableType.MainView);
					_foptions.Text = "MainView Options";

//					if (Optionables.OptionsOnTop)
//						MainViewF._foptions.Owner = this;

					OptionsManager.Options.Add(_foptions);

					_foptions.FormClosing += (sender1, e1) =>
					{
						if (!MainViewF.Quit)
						{
							ObserverManager.ToolFactory.GetOptionsButton().Checked =
							miOptions.Checked = false;

							e1.Cancel = true;
							_foptions.Hide();
						}
						else
							RegistryInfo.UpdateRegistry(_foptions);
					};
				}

				_foptions.Show();

				if (_foptions.WindowState == FormWindowState.Minimized)
					_foptions.WindowState  = FormWindowState.Normal;
			}
			else
				_foptions.Close();
		}
		#endregion Options


		#region Events (override)
		/// <summary>
		/// Handles CL-args after Configurator restart - selects a treenode in
		/// the <c><see cref="MapTree"/></c>.
		/// </summary>
		/// <param name="e"></param>
		protected override void OnShown(EventArgs e)
		{
			base.OnShown(e);

			switch (Program.Args.Length)
			{
				case 0:
					if (MapTree.SelectedNode != null)
						MapTree.SelectedNode.Collapse(false);
					break;

				case 1:
					SelectGroupNode(Program.Args[TREELEVEL_GROUP]);
					break;

				case 2:
					SelectCategoryNode(
									Program.Args[TREELEVEL_GROUP],
									Program.Args[TREELEVEL_CATEGORY]);
					break;

				case 3:
					SelectTilesetNode(
									Program.Args[TREELEVEL_GROUP],
									Program.Args[TREELEVEL_CATEGORY],
									Program.Args[TREELEVEL_TILESET]);
					break;
			}

			TopMost = true;		// NOTE: MapView could be hidden behind other
			TopMost = false;	//       open windows after a forced reload.
		}


		private static bool FirstActivated; // on 1st Activated keep the tree focused, on 2+ focus the panel
		internal static bool BypassActivatedEvent;

		/// <summary>
		/// Overrides the <c>Activated</c> event. Brings any other open viewers
		/// to the top of the desktop, along with this. And focuses the panel.
		/// </summary>
		/// <param name="e"></param>
		/// <remarks>Trying to bring this form to the top after the other forms
		/// apparently fails in Windows 10 - which makes it impossible for
		/// MainView to gain focus when clicked (if there are other viewers
		/// open). Hence
		/// <c><see cref="MainViewOptionables.BringAllToFront()">MainViewOptionables.BringAllToFront()</see></c>
		/// is <c>false</c> by default.</remarks>
		protected override void OnActivated(EventArgs e)
		{
			ShowHideManager._zOrder.Remove(this);
			ShowHideManager._zOrder.Add(this);

			if (!BypassActivatedEvent && Optionables != null && Optionables.BringAllToFront) // don't let 'TopMost_set' (etc) fire the OnActivated event.
			{
				BypassActivatedEvent = true;	// don't let the loop over the viewers re-trigger this activated event.
												// NOTE: 'TopMost_set' won't, but other calls like BringToFront() or Select() can/will.

				IList<Form> zOrder = ShowHideManager.getZorderList();
				foreach (var f in zOrder)
				{
					f.TopMost = true;
					f.TopMost = false;
				}

				BypassActivatedEvent = false;
			}

			if (FirstActivated)
			{
				if (MapFile != null) _overlay.Select();
				else                  MapTree.Select();
			}
			else
				FirstActivated = true;
		}

		/// <summary>
		/// Overrides the <c>Deactivate</c> event. Allows the targeter to go
		/// away.
		/// </summary>
		/// <param name="e"></param>
		protected override void OnDeactivate(EventArgs e)
		{
			_overlay.ReleaseTargeter();
			Invalidate();
		}


		/// <summary>
		/// This has nothing to do with the Registry anymore, but it saves
		/// MainView's Options as well as its screen-location and -size to YAML
		/// when the app closes.
		/// </summary>
		/// <param name="e"></param>
		protected override void OnFormClosing(FormClosingEventArgs e)
		{
			if (!RegistryInfo.FastClose(e.CloseReason))
			{
				Quit = true;

				if (SaveAlertMap() == DialogResult.Cancel) // NOTE: do not short-circuit these ->
					Quit = false;

				if (SaveAlertRoutes() == DialogResult.Cancel)
					Quit = false;

				if (SaveAlertMaptree() == DialogResult.Cancel)
					Quit = false;

				if (Quit)
				{
					SafeQuit();

					// kL_note: This is for storing MainView's location and size in
					// the Windows Registry:
//					if (PathsEditor.SaveRegistry)
//					{
//						using (var keySoftware = Registry.CurrentUser.CreateSubKey(DSShared.Windows.RegistryInfo.SoftwareRegistry))
//						using (var keyMapView = keySoftware.CreateSubKey(DSShared.Windows.RegistryInfo.MapViewRegistry))
//						using (var keyMainView = keyMapView.CreateSubKey("MainView"))
//						{
//							keyMainView.SetValue("Left",   Left);
//							keyMainView.SetValue("Top",    Top);
//							keyMainView.SetValue("Width",  Width);
//							keyMainView.SetValue("Height", Height - SystemInformation.CaptionButtonSize.Height); ps. not
//							keyMainView.Close();
//							keyMapView.Close();
//							keySoftware.Close();
//						}
//					}
				}
				else
					e.Cancel = true;
			}
			base.OnFormClosing(e);
		}

		/// <summary>
		/// Saves out Options and Registry as well as closes any open viewers
		/// and miscellany.
		/// </summary>
		private void SafeQuit()
		{
			//Logfile.Log("MainViewF.SafeQuit() EXIT MapView ->");

			Dontdrawyougits = true;

			RegistryInfo.UpdateRegistry(this); // store MainView's current location and size

			OptionsManager.SaveOptions();	// save MapOptionsFile // TODO: do SaveOptions() every time an Options form closes.
			OptionsManager.CloseOptions();	// close any open Options windows

			if ( ScanG   != null)  ScanG  .Close(); // close ScanG
			if (_fcolors != null) _fcolors.Close(); // close ColorsHelp
			if (_fabout  != null) _fabout .Close(); // close About
			if (_finfo   != null) _finfo  .Close(); // close MapInfo and its Detail dialog

			// NOTE: TopView's PartslotTest dialog is closed when TopView closes.
			// TODO: McdRecordsExceeded dialog is pseudo-static ... close it (if it was instantiated).

			if (RouteView.SpawnInfo != null)
				RouteView.SpawnInfo.Close();	// close RouteView's SpawnInfo dialog

			ObserverManager.CloseObservers();	// close secondary viewers (TileView, TopView, RouteView, TopRouteView)


			MonotoneSprites.Dispose();
			CuboidSprite.DisposeCursorset();

			Tilepart.DisposeCrippledSprites();	// NOTE: .net will try to draw the MainView panel again but
												// if the tileset has crippled sprites it throws.

			ObserverManager.ToolFactory.DisposeMainviewTools(); // might not be req'd

			_overlay        .DisposeOverlay();
			MainViewUnderlay.DisposeUnderlay();

			_overlay .Dispose();
			_underlay.Dispose();


			// static
			TreenodeLine_def   .Dispose();
			TreenodeFill_def   .Dispose();
			TreenodeLine_sel   .Dispose();
			TreenodeFill_selfoc.Dispose();
			TreenodeFill_selunf.Dispose();
			TreenodeFill_serfoc.Dispose();
			TreenodeFill_serunf.Dispose();

			Palette             .DisposeMonoBrushes();

			QuadrantControl     .DisposeControl();
			QuadrantDrawService .DisposeService();

			RouteControl        .DisposeControlStatics();

			TileViewOptionables .DisposeOptionables();
			TopViewOptionables  .DisposeOptionables();
			RouteViewOptionables.DisposeOptionables();

			Optionables         .DisposeOptionables();

			SpritesetManager    .Dispose();

			Globals.Ia          .Dispose();


			RegistryInfo.WriteRegistry(); // write all registered windows' locations and sizes to file
		}


		/// <summary>
		/// Stops keys that shall be used for navigating the tiles from doing
		/// anything stupid. Does other stupid stuff instead.
		/// </summary>
		/// <param name="msg"></param>
		/// <param name="keyData"></param>
		/// <returns></returns>
		protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
		{
			//Logfile.Log("MainViewF.ProcessCmdKey() " + keyData + " _loadready= " + _loadready);

			bool invalidate = false;
			bool focusearch = false;

			switch (keyData)
			{
				case Keys.Tab:
					invalidate = true;
					focusearch = _overlay.Focused;
					break;

				case Keys.Shift | Keys.Tab:
					invalidate = true;
					focusearch = MapTree.Focused;
					break;

				case Keys.Shift | Keys.F3:
					invalidate = true;
					focusearch = true;
					break;
			}

			if (invalidate)
			{
				_overlay.Invalidate();

				if (focusearch)
				{
					ObserverManager.ToolFactory.FocusSearch();
					return true;
				}
			}


			if (!_overlay.Focused)
			{
				switch (keyData)
				{
					case Keys.F3:		// panel must *not* have focus (F3 also toggles doors)
						Search(ObserverManager.ToolFactory.GetSearchText());
						return true;

					case Keys.Escape:	// panel must *not* have focus (Escape also cancels multi-tile selection)
						_overlay.Select();
						_overlay.Invalidate();
						return true;
				}
			}
			else if (MapFile != null)
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
						_overlay.Navigate(keyData);
						return true;
				}
			}
			return base.ProcessCmdKey(ref msg, keyData);
		}

		/// <summary>
		/// Overrides the <c>KeyDown</c> handler.
		/// </summary>
		/// <param name="e"></param>
		/// <remarks>Requires <c>KeyPreview</c>.
		/// <br/><br/>
		/// This differs from the Viewers-menu item-click/key in that
		/// <c>[Ctrl]</c> is used to focus a subsidiary viewer instead of doing
		/// show/hide. See also <c>OnKeyDown()</c> in
		/// <c><see cref="TileViewForm"/></c>, <c><see cref="TopViewForm"/></c>,
		/// <c><see cref="RouteViewForm"/></c>, and
		/// <c><see cref="TopRouteViewForm"/></c> ->
		/// <c><see cref="ViewersMenuManager.ViewerKeyDown()">ViewersMenuManager.ViewerKeyDown()</see></c>.
		/// <br/><br/>
		/// Edit/Save keys are handled by
		/// <c><see cref="MainViewOverlay">MainViewOverlay</see>.OnKeyDown()</c>.</remarks>
		/// <seealso cref="DontBeep()"><c>DontBeep()</c></seealso>
		protected override void OnKeyDown(KeyEventArgs e)
		{
			//Logfile.Log("MainViewF.OnKeyDown() " + e.KeyData);

			_loadready = false;

			string key = null; object val = null;
			ToolStripMenuItem it = null;
			int id = ViewersMenuManager.MI_non;

			switch (e.KeyData)
			{
				case Keys.Space:				// open Context
					if (MapTree.Focused && MapTree.SelectedNode != null)
					{
						e.SuppressKeyPress = true;

						if (MapTree.SelectedNode.Level != TREELEVEL_TILESET
							|| MapTree.SelectedNode == _loaded)
						{
							_dontbeeptype = DontBeepType.OpenContext;
							BeginInvoke(DontBeepEvent);
						}
					}
					break;

				case Keys.Enter:				// load Descriptor (do NOT reload)
					if (MapTree.Focused && MapTree.SelectedNode != null)
					{
						e.SuppressKeyPress = true;

						if (MapTree.SelectedNode.Level == TREELEVEL_TILESET)
						{
							if (   MapFile == null
								|| MapFile.Descriptor != MapTree.SelectedNode.Tag as Descriptor)
							{
								_dontbeeptype = DontBeepType.LoadDescriptor;
								BeginInvoke(DontBeepEvent);
							}
						}
						else if (!MapTree.SelectedNode.IsExpanded)
						{
							MapTree.SelectedNode.Expand();
						}
						else
							MapTree.SelectedNode.Collapse();
					}
					break;

				case Keys.Shift | Keys.Enter:	// open MapBrowserDialog
					if (MapTree.Focused && MapTree.SelectedNode != null)
					{
						e.SuppressKeyPress = true;

						if (MapTree.SelectedNode.Level == TREELEVEL_TILESET
							&& !(MapTree.SelectedNode.Tag as Descriptor).FileValid)
						{
							_dontbeeptype = DontBeepType.MapBrowserDialog;
							BeginInvoke(DontBeepEvent);
						}
					}
					break;

				case Keys.F9:					// cycle LayerSelectionBorder
					key = MainViewOptionables.str_LayerSelectionBorder;
					val = (Optionables.LayerSelectionBorder + 1) % 3;
					break;

				case Keys.Shift | Keys.F9:		// toggle OneTileDraw
					key = MainViewOptionables.str_OneTileDraw;
					val = !Optionables.OneTileDraw;
					break;

				case Keys.F10:					// cycle tiletoner option forward
					key = MainViewOptionables.str_SelectedTileToner;
					val = Optionables.GetNextTileToner(+1);
					break;

				case Keys.Shift | Keys.F10:		// cycle tiletoner option reverse
					key = MainViewOptionables.str_SelectedTileToner;
					val = Optionables.GetNextTileToner(-1);
					break;

				case Keys.F2:
					key = MainViewOptionables.str_AnimateSprites;
					val = !Optionables.AnimateSprites;
					break;

				case Keys.F3:
					key = MainViewOptionables.str_OpenDoors;
					val = !Optionables.OpenDoors;
					break;

				case Keys.F4:
					key = MainViewOptionables.str_GridVisible;
					val = !Optionables.GridVisible;
					break;

				// toggle TopView tilepart visibilities ->
				case Keys.Control | Keys.F1:
					it = ObserverManager.TopView.Control.it_Floor;
					break;

				case Keys.Control | Keys.F2:
					it = ObserverManager.TopView.Control.it_West;
					break;

				case Keys.Control | Keys.F3:
					it = ObserverManager.TopView.Control.it_North;
					break;

				case Keys.Control | Keys.F4:
					it = ObserverManager.TopView.Control.it_Content;
					break;

				case Keys.Control | Keys.F9:
					it = ObserverManager.TopView.Control.it_Enable;
					break;

				// focus viewer (show/hide shortcuts are handled by menuitems directly) ->
				case Keys.Control | Keys.F5: id = ViewersMenuManager.MI_TILE;     break;
				case Keys.Control | Keys.F6: id = ViewersMenuManager.MI_TOP;      break;
				case Keys.Control | Keys.F7: id = ViewersMenuManager.MI_ROUTE;    break;
				case Keys.Control | Keys.F8: id = ViewersMenuManager.MI_TOPROUTE; break;

				case Keys.Add:
				case Keys.Subtract:
				case Keys.Home:
				case Keys.End:
				case Keys.PageUp:
				case Keys.PageDown:
				case Keys.Shift | Keys.Home:
				case Keys.Shift | Keys.End:
				case Keys.Shift | Keys.PageUp:
				case Keys.Shift | Keys.PageDown:
					if (_overlay.Focused)
					{
						e.SuppressKeyPress = true;
						_overlay.Navigate(e.KeyData);
					}
					break;
			}

			if (key != null)
			{
				e.SuppressKeyPress = true;
				Options[key].SetValue(key,val);

				if (_foptions != null && _foptions.Visible)
					_foptions.propertyGrid.Refresh();
			}
			else if (menuViewers.Enabled)
			{
				if (it != null)
				{
					e.SuppressKeyPress = true;
					if (it != ObserverManager.TopView.Control.it_Enable)
					{
						ObserverManager.TopView.Control.OnDisableClick(it, EventArgs.Empty);
					}
					else if (it.Enabled)
					{
						ObserverManager.TopView.Control.OnEnableAllClick(it, EventArgs.Empty);
					}
				}
				else if (id != ViewersMenuManager.MI_non)
				{
					e.SuppressKeyPress = true;
					ViewersMenuManager.OnMenuItemClick(menuViewers.MenuItems[id], EventArgs.Empty);
				}
			}

			base.OnKeyDown(e);
		}
		#endregion Events (override)


		#region Events
		/// <summary>
		/// Draws treenodes on the <c><see cref="MapTree"/></c>.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void OnMaptreeDrawNode(object sender, DrawTreeNodeEventArgs e)
		{
			TreeNode node = e.Node;
			if (node != null)
			{
				Graphics graphics = e.Graphics;

				Brush colorfill; Pen colorline; Color colortext;

				if (node == Searched)
				{
					colorline = TreenodeLine_sel;

					if (MapTree.Focused) colorfill = TreenodeFill_serfoc;
					else                 colorfill = TreenodeFill_serunf;
				}
				else if ((e.State & TreeNodeStates.Focused) != 0) // WARNING: May require 'HideSelection' false.
				{
					colorline = TreenodeLine_sel;

					if (node.Level != TREELEVEL_TILESET || node == _loaded)
						colorfill = TreenodeFill_selfoc;
					else
						colorfill = TreenodeFill_def;
				}
				else if ((e.State & TreeNodeStates.Selected) != 0) // WARNING: Requires 'HideSelection' false.
				{
					colorline = TreenodeLine_sel;

					if (node.Level != TREELEVEL_TILESET || node == _loaded)
						colorfill = TreenodeFill_selunf;
					else
						colorfill = TreenodeFill_def;
				}
				else if (node == _loaded)
				{
					colorline = TreenodeLine_def;

					if (MapTree.Focused) colorfill = TreenodeFill_selfoc;
					else                 colorfill = TreenodeFill_selunf;
				}
				else
				{
					colorline = TreenodeLine_def;
					colorfill = TreenodeFill_def;
				}

				if (node.Level != TREELEVEL_TILESET || (node.Tag as Descriptor).FileValid)
					colortext = Optionables.TreeForecolor;
				else
					colortext = Optionables.TreeForecolorInvalidFile;


				Rectangle rect = e.Bounds;
				rect.Width += 30;
				graphics.FillRectangle(TreenodeFill_def, rect);	// draw over TreeView's glitchy default node-selection rect.
				rect.Width -= 30;

				int w = TextRenderer.MeasureText(node.Text, node.TreeView.Font).Width;
				while (w / 70 != 0) { ++rect.Width; w -= 70; }

				rect.Width  += 3;								// widen the custom node-selection rect a bit.
				rect.Height -= 1;								// keep border inside bot-bound
				graphics.FillRectangle(colorfill, rect);
				graphics.DrawRectangle(colorline, rect);

				rect = e.Bounds;

				rect.X += 2;									// re-align text due to .net glitch.
				TextRenderer.DrawText(
									graphics,
									node.Text,
									node.TreeView.Font,
									rect,
									colortext);
			}
		}


		/// <summary>
		/// Handles a save-all click.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void OnSaveAllClick(object sender, EventArgs e)
		{
			if (MapFile != null)
			{
				if (MapFile.SaveMap())
				{
					MapChanged = false;

					if (MapFile.ForceReload)
					{
						MapFile.ForceReload = false;
						LoadSelectedDescriptor(false, true);
					}
				}

				if (MapFile.SaveRoutes())
					RouteView.RoutesChangedCoordinator = false;
			}
			MaptreeChanged = !TileGroupManager.WriteTileGroups();
		}

		/// <summary>
		/// Handles a save-Mapfile click.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		internal void OnSaveMapClick(object sender, EventArgs e)
		{
			if (MapFile != null && MapFile.SaveMap())
			{
				MapChanged = false;

				if (MapFile.ForceReload)
				{
					MapFile.ForceReload = false;
					LoadSelectedDescriptor(false, true);
				}
			}
		}

		/// <summary>
		/// Handles a save-Routesfile click.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		internal void OnSaveRoutesClick(object sender, EventArgs e)
		{
			if (MapFile != null && MapFile.SaveRoutes())
			{
				RouteView.RoutesChangedCoordinator = false;
			}
		}


		private string _lastExportDirectory;

		/// <summary>
		/// Handles an export-MapRoutes click.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void OnExportMapRoutesClick(object sender, EventArgs e)
		{
			if (MapFile != null && MapFile.Descriptor != null)
			{
				using (var sfd = new SaveFileDialog())
				{
					sfd.Title      = "Export Map (and Routes) ...";
					sfd.Filter     = "Map files (*.MAP)|*.MAP|All files (*.*)|*.*";
					sfd.DefaultExt = GlobalsXC.MapExt;
					sfd.FileName   = MapFile.Descriptor.Label;

					if (!Directory.Exists(_lastExportDirectory))
					{
						string path = Path.Combine(MapFile.Descriptor.Basepath, GlobalsXC.MapsDir);
						if (Directory.Exists(path))
							sfd.InitialDirectory = path;
					}
					else
						sfd.InitialDirectory = _lastExportDirectory;


					if (sfd.ShowDialog() == DialogResult.OK)
					{
						string pfe = sfd.FileName;
						_lastExportDirectory = Path.GetDirectoryName(pfe);

						// NOTE: GetDirectoryName() will return a string ending with a
						// path-separator if it's the root dir, and without one if it's
						// not. But Path.Combine() doesn't figure out the difference.
						// woohoo ...

						if (_lastExportDirectory.EndsWith(GlobalsXC.MapsDir, StringComparison.OrdinalIgnoreCase))
						{
							string dir       = _lastExportDirectory.Substring(0, _lastExportDirectory.Length - GlobalsXC.MapsDir.Length - 1);
							string dirMaps   = Path.Combine(dir, GlobalsXC.MapsDir);
							string dirRoutes = Path.Combine(dir, GlobalsXC.RoutesDir);
							string label     = Path.GetFileNameWithoutExtension(pfe);

							MapFile.ExportMap(   Path.Combine(dirMaps,   label));
							MapFile.ExportRoutes(Path.Combine(dirRoutes, label));
						}
						else
						{
							using (var f = new Infobox(
													"Error",
													Infobox.SplitString("Maps must be saved to a directory MAPS."
															+ " Routes can then be saved to its sibling directory ROUTES."),
													null,
													InfoboxType.Error))
							{
								f.ShowDialog(this);
							}
						}
					}
				}
			}
		}

		/// <summary>
		/// Saves the <c><see cref="MapTree"/></c> to "settings/MapTilesets.yml".
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void OnSaveMaptreeClick(object sender, EventArgs e)
		{
			MaptreeChanged = !TileGroupManager.WriteTileGroups();
		}

		/// <summary>
		/// Reloads the current tileset.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void OnReloadClick(object sender, EventArgs e)
		{
			OnReloadDescriptor();
		}

		/// <summary>
		/// Reloads the Map/Routes/Terrains when a save is done in PckView or
		/// McdView via <c><see cref="TileView"/>.OnPckViewClick()</c>
		/// or <c><see cref="TileView"/>.OnMcdViewClick()</c>.
		/// <br/><br/>
		/// TODO: Neither event really needs to reload the Map/Routes (in fact
		/// it would be better if it didn't so that the SaveAlerts could be
		/// bypassed) - so this function ought be reworked to reload only the
		/// Terrains (MCDs/PCKs/TABs). But that's a headache and a half ...
		/// <br/><br/>
		/// TODO: Actually there should be a separate ReloadTerrains() funct.
		/// </summary>
		/// <remarks>Is double-purposed to reload the Map/Routes/Terrains when
		/// user chooses to reload the current Map et al. on the File menu.</remarks>
		private void OnReloadDescriptor()
		{
			bool cancel  = (SaveAlertMap()    == DialogResult.Cancel);
				 cancel |= (SaveAlertRoutes() == DialogResult.Cancel); // NOTE: that bitwise had better execute ....

			if (!cancel)
			{
				LoadSelectedDescriptor();
				_overlay.Invalidate();
			}
		}


		/// <summary>
		/// Closes the current tileset.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void OnCloseClick(object sender, EventArgs e)
		{
			bool cancel = false;

			if (sender != null) // bypass SaveAlert iff group/category/tileset is deleted from the Maptree
			{
				cancel  = (SaveAlertMap()    == DialogResult.Cancel);
				cancel |= (SaveAlertRoutes() == DialogResult.Cancel); // NOTE: that bitwise had better execute ....
			}


			if (!cancel)
			{
				EnableMenus(false);
				EnableObservers(false);
				ViewersMenuManager.EnableScanG(false);

				if (ScanG != null)
					ScanG.Close();

				TopView.ClosePartslotTestDialog();

//				McdInfoF fMcdInfo = ObserverManager.TileView.Control.McdInfo; // update MCD Info if open ->
//				if (fMcdInfo != null) fMcdInfo.UpdateData();

				if (RouteView.SpawnInfo != null)
					RouteView.SpawnInfo.Close();

				if (_finfo != null)
				{
					_finfo.Close();
					DecheckMapInfo();
				}

				RouteView.DeselectNodeStatic(true);

				ObserverManager.AssignMapfile(MapFile = null); // clear all observers' Mapfile vars and '_overlay.FirstClick'

				ObserverManager.ToolFactory.EnableLevelers(0,1);
				ObserverManager.ToolFactory.EnableEditors(false);
				ObserverManager.ToolFactory.EnablePasters(false);
				ObserverManager.ToolFactory.EnableAutoscale(false);
				ObserverManager.ToolFactory.EnableScalein(false);
				ObserverManager.ToolFactory.EnableScaleout(false);

				tsslMapLabel     .Text =
				tsslDimensions   .Text =
				tsslPosition     .Text =
				tsslScale        .Text =
				tsslSelectionSize.Text = String.Empty;

				ObserverManager.TileView.Control.SelectedTilepart = null;
				ObserverManager.TileView.Control.SelectTilepart(null);

				ResetQuadrantPanel();

				RouteView.ClearSelectedInfo();

				_loaded = null;
				MapTree.Invalidate();
				MapTree.Select();
			}
			else
				_overlay.Select();
		}


		private string _lastScreenshotDirectory;

		/// <summary>
		/// Handles a screenshot click.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void OnScreenshotClick(object sender, EventArgs e)
		{
			if (MapFile != null)
			{
				using (var sfd = new SaveFileDialog())
				{
					if (Optionables.Png_notGif)
					{
						sfd.Title      = "Save PNG Screenshot";
						sfd.Filter     = "PNG files|*.PNG|All files (*.*)|*.*";
						sfd.DefaultExt = GlobalsXC.PngExt;
					}
					else
					{
						sfd.Title      = "Save GIF Screenshot";
						sfd.Filter     = "GIF files|*.GIF|All files (*.*)|*.*";
						sfd.DefaultExt = GlobalsXC.GifExt;
					}

					string digits = String.Empty;
					int levs = MapFile.Levs;
					do
					{ digits += "0"; }
					while ((levs /= 10) != 0);

					string suffix = String.Format(
												"_L{0:" + digits + "}",
												MapFile.Levs - MapFile.Level);
					sfd.FileName = MapFile.Descriptor.Label + suffix;

					if (!Directory.Exists(_lastScreenshotDirectory))
					{
						string path = Path.Combine(MapFile.Descriptor.Basepath, GlobalsXC.MapsDir);
						if (Directory.Exists(path))
							sfd.InitialDirectory = path;
					}
					else
						sfd.InitialDirectory = _lastScreenshotDirectory;

					sfd.RestoreDirectory = true;


					if (sfd.ShowDialog(this) == DialogResult.OK)
					{
						_lastScreenshotDirectory = Path.GetDirectoryName(sfd.FileName);
						Screenshot(sfd.FileName);
					}
				}
			}
		}

		/// <summary>
		/// Takes a screenshot of the currently loaded Map.
		/// </summary>
		/// <param name="fullpath"></param>
		public void Screenshot(string fullpath)
		{
			const int ConstHalfWidth  = 16;
			const int ConstHalfHeight =  8;
			const int Layers          = 24;


			int level = MapFile.Level;

			int width = MapFile.Rows + MapFile.Cols;
			using (var b = SpriteService.CreateTransparent(
														width * ConstHalfWidth,
														width * ConstHalfHeight + (MapFile.Levs - level) * Layers,
														MapFile.Descriptor.Pal.Table))
			{
				var start = new Point(
									(MapFile.Rows - 1) * ConstHalfWidth,
								   -(level * Layers));

				MapTileArray tiles = MapFile.Tiles;
				if (tiles != null)
				{
					Tilepart part;
					MapTile tile;

					for (int l = MapFile.Levs - 1; l >= level; --l)
					{
						for (int
								r = 0,
									startX = start.X,
									startY = start.Y + l * Layers;
								r != MapFile.Rows;
								++r,
									startX -= ConstHalfWidth,
									startY += ConstHalfHeight)
						{
							for (int
									c = 0,
										x = startX,
										y = startY;
									c != MapFile.Cols;
									++c,
										x += ConstHalfWidth,
										y += ConstHalfHeight)
							{
								if (!(tile = tiles.GetTile(c,r,l)).Vacant)
								for (int i = 0; i != MapTile.QUADS; ++i)
								if ((part = tile[(PartType)i]) != null)
								{
									SpriteService.BlitSprite(
															part[0].Sprite,
															b,
															x,
															y - part.Record.SpriteOffset);
								}
							}
						}
					}
				}


				Bitmap b2; // workaround the inability to re-assign a using-variable inside a using-statement ->
				if (Optionables.CropBackground)
				{
					b2 = SpriteService.CropTransparentEdges(b);
				}
				else
					b2 = b;

				using (b2)
				{
					ColorPalette pal = b2.Palette;
					pal.Entries[Palette.Tid] = Optionables.BackgroundColor;
					b2.Palette = pal;

					ImageFormat format;
					if (Optionables.Png_notGif) format = ImageFormat.Png;
					else                        format = ImageFormat.Gif;

					b2.Save(fullpath, format);
				}
			}
		}


		/// <summary>
		/// Closes Everything.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		internal void OnQuitClick(object sender, EventArgs e)
		{
			Close();
		}


		/// <summary>
		/// Opens a dialog for user to resize the current
		/// <c><see cref="MapFile"/></c>.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void OnMapResizeClick(object sender, EventArgs e)
		{
			if (MapFile != null)
			{
				using (var f = new MapResizeInputBox(MapFile))
				{
					if (f.ShowDialog(this) == DialogResult.OK)
					{
						TopView.ClosePartslotTestDialog();

						RouteCheckService.SetBase1( // send the base1-count options to 'XCom' ->
												MainViewF.Optionables.Base1_xy,
												MainViewF.Optionables.Base1_z);

						int changes = MapFile.MapResize(
													f.Cols,
													f.Rows,
													f.Levs,
													f.zType);

						if ((changes & MapFile.MAPRESIZERESULT_CHANGEDMAP) == MapFile.MAPRESIZERESULT_CHANGEDMAP)
						{
							if (!MapFile.MapChanged)
								MapChanged = true;
						}

						if ((changes & MapFile.MAPRESIZERESULT_CHANGEDROUTES) == MapFile.MAPRESIZERESULT_CHANGEDROUTES)
						{
							if (!MapFile.RoutesChanged)
								RouteView.RoutesChangedCoordinator = true;
						}

						if ((changes & MapFile.MAPRESIZERESULT_DELETEROUTENODES) == MapFile.MAPRESIZERESULT_DELETEROUTENODES)
						{
							if (!MapFile.RoutesChanged)
								RouteView.RoutesChangedCoordinator = true;

							foreach (RouteNode node in RouteCheckService.Invalids)
							{
								if (RouteView.SpawnInfo != null)
									RouteView.SpawnInfo.DeleteNode(node);

								MapFile.Routes.DeleteNode(node);
							}
						}

						_underlay.ForceResize();

						_overlay.FirstClick = false;

						RouteView.ClearSelectedInfo();

						ObserverManager.ToolFactory.EnableLevelers(MapFile.Level, MapFile.Levs);

						tsslDimensions   .Text = MapFile.SizeString;
						tsslPosition     .Text =
						tsslSelectionSize.Text = String.Empty;

						ObserverManager.AssignMapfile(MapFile); // TODO: That is overkill ... the pointer to the current MapFile does not change.

						TopControl.ClearSelectorLozengeStatic();

						if (ScanG != null) // update ScanG viewer if open
							ScanG.LoadMapfile(MapFile);

						ResetQuadrantPanel();
					}
				}
			}
		}

		/// <summary>
		/// Opens a dialog that allows user to replace a tilepart throughout the
		/// Map with another tilepart.
		/// </summary>
		/// <param name="sender"><c><see cref="miTilepartSubstitution"/></c></param>
		/// <param name="e"></param>
		private void OnTilepartSubstitutionClick(object sender, EventArgs e)
		{
			using (var f = new TilepartSubstitution(MapFile))
			{
				if (f.ShowDialog() == DialogResult.OK)
				{
					int src0, src1, dst, shift;

					src0 = TilepartSubstitution.src0;

					if (TilepartSubstitution.src1 == Int32.MaxValue)
					{
						src1 = TilepartSubstitution.src0;
					}
					else
						src1 = TilepartSubstitution.src1;

					switch (TilepartSubstitution.rb_selected)
					{
						default: // ie. TilepartSubstitution.RadioSelected.Clear
							dst   =
							shift = Int32.MaxValue;
							break;

						case TilepartSubstitutionType.Desti:
							dst   = TilepartSubstitution.dst;
							shift = Int32.MaxValue;
							break;

						case TilepartSubstitutionType.Shift:
							dst   = Int32.MaxValue;
							shift = TilepartSubstitution.shift;
							break;
					}

					_overlay.SubstituteTileparts(src0, src1, dst, shift);
				}
			}
		}

		/// <summary>
		/// Opens a dialog that allows user to switch around allocated terrains.
		/// TODO: CHM-helpfile doc
		/// </summary>
		/// <param name="sender"><c><see cref="miTerrainSwap"/></c></param>
		/// <param name="e"></param>
		private void OnTerrainSwapClick(object sender, EventArgs e)
		{
			if (MaptreeChanged || MapFile.MapChanged)
			{
				const string head = "Terrain swapping changes the tileset's data and"
								  + " its Maptree data - they need to be kept synchronized."
								  + " The Maptree and Map must both be in a saved"
								  + " state before a Terrain Swap is allowed.";

				using (var f = new Infobox("save state", Infobox.SplitString(head, 80)))
					f.ShowDialog(this);
			}
			else
			{
				using (var f = new TerrainSwapDialog(MapFile))
				{
					if (f.ShowDialog() == DialogResult.OK)
					{
						// NOTE: If user uses the TerrainSwapDialog twice and orders
						// the terrains back to their original state the MaptreeChanged
						// flag does not get removed.

						MaptreeChanged = !TileGroupManager.WriteTileGroups();
						MapChanged     = !MapFile.SaveMap();

						// NOTE: There ought be no need to reload the Map.
						// Except that if another TerrainSwap is performed; the terrainset's setids are whack.
						// so force reload (keep Routes) ->
						LoadSelectedDescriptor(false, true);
					}
				}
			}
		}

		/// <summary>
		/// Clears all
		/// <c><see cref="Descriptor"></see>.BypassRecordsExceeded</c>
		/// flags in the <c><see cref="MapTree"/></c>.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void OnClearRecordsExceededClick(object sender, EventArgs e)
		{
			int count = 0;

			foreach (var @group in TileGroupManager.TileGroups)
			foreach (var category in @group.Value.Categories)
			foreach (var descriptor in category.Value.Values)
			if (descriptor.BypassRecordsExceeded)
			{
				descriptor.BypassRecordsExceeded = false;
				++count;
			}

			string info;
			if (count != 0)
			{
				if (!MainViewF.that.MaptreeChanged)
					 MainViewF.that.MaptreeChanged = true;

				if (count == 1) info = count + " flag cleared.";
				else            info = count + " flags cleared.";
			}
			else
				info = "There were no flags set.";

			using (var dialog = new Infobox(
										"flags cleared",
										info))
			{
				dialog.ShowDialog(this);
			}
		}


		/// <summary>
		/// Gets a string of changed objects.
		/// <list type="bullet">
		/// <item><c><see cref="MapFile"/>.MapChanged</c></item>
		/// <item><c><see cref="MapFile"/>.RoutesChanged</c></item>
		/// <item><c><see cref="MaptreeChanged"/></c></item>
		/// </list>
		/// </summary>
		/// <returns></returns>
		private string GetChangedInfo()
		{
			string info = String.Empty;

			if (MapFile != null)
			{
				if (MapFile.MapChanged)
					info = "Map";

				if (MapFile.RoutesChanged)
				{
					if (info.Length != 0) info += " and ";
					info += "Routes";
				}
			}

			if (MaptreeChanged)
			{
				if (info.Length != 0) info += " and ";
				info += "Maptree";
			}

			return info;
		}

		/// <summary>
		/// Checks for changes before calling
		/// <c><see cref="Configurator()">Configurator()</see></c>.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void OnConfiguratorClick(object sender, EventArgs e)
		{
			string info = GetChangedInfo();

			if (info.Length != 0)
			{
				string head = Infobox.SplitString("Accepting the Configuration Editor can"
							+ " restart MapView. The current " + info + " should be saved or"
							+ " else any changes will be lost. How do you wish to proceed?", 80);

				string copyable = "retry  - save changes and open the Configurator"        + Environment.NewLine
								+ "ok     - risk losing changes and open the Configurator" + Environment.NewLine
								+ "cancel - return to state";

				using (var f = new Infobox(
										"Changes detected",
										head,
										copyable,
										InfoboxType.Warn,
										InfoboxButton.CancelOkayRetry))
				{
					switch (f.ShowDialog(this))
					{
						default: // DialogResult.Cancel
							return;

						case DialogResult.Retry:
							if (MapFile != null)
							{
								if (MapFile.MapChanged && MapFile.SaveMap())
								{
									MapChanged = false;

									if (MapFile.ForceReload)	// NOTE: Forcing reload is probably not necessary here
									{							// because the current Map is *probably* going to change. I think ...
										MapFile.ForceReload = false;
										LoadSelectedDescriptor(false, true);
									}
								}

								if (MapFile.RoutesChanged && MapFile.SaveRoutes())
								{
									RouteView.RoutesChangedCoordinator = false;
								}
							}

							if (MaptreeChanged)
							{
//								MaptreeChanged = !TileGroupInfo.WriteTileGroups(); // <- that could cause endless recursion.
								// TODO: if(TileGroupManager.WriteTileGroups()) MaptreeChanged=false;
								TileGroupManager.WriteTileGroups();
								MaptreeChanged = false;
							}
							break;

						case DialogResult.OK:
							// The process will be killed or Canceled so don't bother to change these ->
//							MapChanged =
//							ObserverManager.RouteView   .Control     .RoutesChanged =
//							ObserverManager.TopRouteView.ControlRoute.RoutesChanged =
//							MaptreeChanged = false;
							break;
					}
				}
			}

			Configurator();
		}

		/// <summary>
		/// Opens the <c><see cref="ConfigurationForm">Configurator</see></c>
		/// dialog then does a restart if user clicks Accept.
		/// </summary>
		private void Configurator()
		{
			using (var f = new ConfigurationForm(true))
			{
				if (f.ShowDialog(this) == DialogResult.OK)
				{
					SafeQuit();

					string args = String.Empty;

					TreeNode node0 = MapTree.SelectedNode;
					if (node0 != null)
					{
						TreeNode node1 = node0.Parent;
						if (node1 != null)
						{
							TreeNode node2 = node1.Parent;
							if (node2 != null)
							{
								args = node2.Text + " " + node1.Text + " " + node0.Text;
							}
							else
								args = node1.Text + " " + node0.Text;
						}
						else
							args = node0.Text;
					}

					Process.Start(new ProcessStartInfo(Application.ExecutablePath, args));
					Process.GetCurrentProcess().Kill();
				}
			}
		}


		/// <summary>
		/// Selects a selected-tile toner color. It does this by changing the
		/// option and firing
		/// <c><see cref="MainViewOptionables"/>.OnOptionChanged()</c> which
		/// calls <c><see cref="SelectToner()">SelectToner()</see></c> which
		/// sets an alternate set of sprites with the toner-palette and also
		/// checks the it in MainView's Toner menu. so bite
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void OnTonerClick(object sender, EventArgs e)
		{
			var it = sender as MenuItem;
			if (!it.Checked)
			{
				object val;
				if      (it == miNone)  val = MainViewOptionables.TONER_NONE;
				else if (it == miGray)  val = MainViewOptionables.TONER_GRAY;
				else if (it == miRed)   val = MainViewOptionables.TONER_RED;
				else if (it == miGreen) val = MainViewOptionables.TONER_GREEN;
				else                    val = MainViewOptionables.TONER_BLUE; // it == miBlue

				const string key = MainViewOptionables.str_SelectedTileToner;
				Options[key].SetValue(key,val);

				if (_foptions != null && _foptions.Visible)
					_foptions.propertyGrid.Refresh();
			}
		}


		/// <summary>
		/// Opens the CHM helpfile.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void OnHelpClick(object sender, EventArgs e)
		{
			string help = Path.GetDirectoryName(Application.ExecutablePath);
				   help = Path.Combine(help, "MapView.chm");
			Help.ShowHelp(MainViewF.that, "file://" + help);
		}


		/// <summary>
		/// Opens the <c><see cref="ColorHelp"/></c> dialog.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// <remarks>This handler is not a toggle. The dialog will be focused if
		/// already open.</remarks>
		internal void OnColorsClick(object sender, EventArgs e)
		{
			if (!miColors.Checked)
			{
				miColors.Checked = true;
				ObserverManager.TileView.Control.CheckColorhelp(true);

				_fcolors = new ColorHelp();
			}
			else
				_fcolors.BringToFront();
		}

		/// <summary>
		/// Dechecks the Colors it when the <c><see cref="ColorHelp"/></c>
		/// dialog closes.
		/// </summary>
		internal void DecheckColors()
		{
			miColors.Checked = false;
			ObserverManager.TileView.Control.CheckColorhelp(false);

			_fcolors = null;
		}


		/// <summary>
		/// Opens the <c><see cref="About"/></c> dialog.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// <remarks>This handler is not a toggle. The dialog will be focused if
		/// already open.</remarks>
		private void OnAboutClick(object sender, EventArgs e)
		{
			if (!miAbout.Checked)
			{
				miAbout.Checked = true;

				_fabout = new About();
				_fabout.Show(); // no owner.
			}
			else
				_fabout.BringToFront();
		}

		/// <summary>
		/// Dechecks the About it when the <c><see cref="About"/></c> dialog
		/// closes.
		/// </summary>
		internal void DecheckAbout()
		{
			miAbout.Checked = false;
			_fabout = null;
		}


		/// <summary>
		/// Opens the <c><see cref="MapInfoDialog"/></c>.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// <remarks>This handler is a toggle. The dialog will be closed if it's
		/// open.</remarks>
		private void OnMapInfoClick(object sender, EventArgs e)
		{
			if (!miMapInfo.Checked)
			{
				if (MapFile != null) // safety.
				{
					miMapInfo.Checked = true;
					_finfo = new MapInfoDialog(this);
				}
			}
			else
				_finfo.Close();
		}

		/// <summary>
		/// Dechecks the MapInfo it when the <c><see cref="MapInfoDialog"/></c>
		/// closes.
		/// </summary>
		internal void DecheckMapInfo()
		{
			miMapInfo.Checked = false;
			_finfo = null;
		}


		/// <summary>
		/// Zooms in.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		internal void OnScaleInClick(object sender, EventArgs e)
		{
			if (Globals.Scale < Globals.ScaleMaximum)
			{
				Globals.Scale += Math.Min(
										Globals.ScaleMaximum - Globals.Scale,
										Globals.ScaleDelta);
				Scale();
			}
		}

		/// <summary>
		/// Zooms out.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		internal void OnScaleOutClick(object sender, EventArgs e)
		{
			if (Globals.Scale > Globals.ScaleMinimum)
			{
				Globals.Scale -= Math.Min(
										Globals.Scale - Globals.ScaleMinimum,
										Globals.ScaleDelta);
				Scale();
			}
		}

		/// <summary>
		/// Zooms in/out.
		/// </summary>
		private void Scale()
		{
			Globals.AutoScale = false;
			ObserverManager.ToolFactory.DecheckAutoscale();

			_underlay.SetOverlaySize();
			_underlay.UpdateScrollers();

			Invalidate();
		}

		/// <summary>
		/// Handles a click on the toolstrip's auto-scale button.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		internal void OnAutoScaleClick(object sender, EventArgs e)
		{
			if (Globals.AutoScale = ObserverManager.ToolFactory.ToggleAutoscale())
				_underlay.SetScale();

			_underlay.UpdateScrollers();
		}


		/// <summary>
		/// Searches the <c><see cref="MapTree"/></c> for a given string.
		/// </summary>
		/// <param name="text"></param>
		internal void Search(string text)
		{
			if (!String.IsNullOrEmpty(text))
			{
				TreeNode
					start0,
					start = null;

				if (Searched != null)
					start0 = Searched;
				else
					start0 = MapTree.SelectedNode;

				if (start0 != null)
				{
					start = start0;

					if (start.Nodes.Count != 0)
					{
						start = start.Nodes[0];
					}
					else if (start.NextNode != null)
					{
						start = start.NextNode;
					}
					else if (start.Parent != null)
					{
						if (start.Parent.NextNode != null)
						{
							start = start.Parent.NextNode;
						}
						else if (start.Parent.Parent != null
							&& start.Parent.Parent.NextNode != null)
						{
							start = start.Parent.Parent.NextNode;
						}
						else
							start = MapTree.Nodes[0];
					}
					else
						start = MapTree.Nodes[0];

					if (start != null) // jic.
					{
						TreeNode node = SearchTreeview(
													text.ToLower(),
													MapTree.Nodes,
													start,
													start0);
						if (node != null)
						{
							Searched = node;
							Searched.EnsureVisible();
							MapTree.Invalidate();
						}

						_active   =
						_hardstop = false;
					}
				}
			}
		}

		bool _active, _hardstop;

		/// <summary>
		/// Searches through the <c><see cref="MapTree"/></c> given a node to
		/// start at.
		/// </summary>
		/// <param name="text">the text to search for (lowercase)</param>
		/// <param name="nodes">the collection of nodes to search through</param>
		/// <param name="start">the node to start at</param>
		/// <param name="start0">the node to stop at</param>
		/// <returns>a found node or null</returns>
		private TreeNode SearchTreeview(
				string text,
				TreeNodeCollection nodes,
				TreeNode start,
				TreeNode start0)
		{
			if (!_hardstop)
			{
				TreeNode child;

				foreach (TreeNode node in nodes)
				{
					if (!_active)
					{
						_active = (node == start);
					}
					else if (node == start0)
					{
						_hardstop = true;	// <- whatever you were doing in the way of recursions stop it.
						return null;		// -> not found after wrapping. NOTE: Does not highlight the
					}						// current node even if that node has the searched for text.

					if (_active && node.Text.ToLower().Contains(text))
					{
						return node;
					}

					if (node.Nodes.Count != 0)
					{
						if ((child = SearchTreeview(text, node.Nodes, start, start0)) != null)
						{
							return child;
						}
					}
					else if (                             node              .NextNode == null	// if no more nodes at the current level
						&& (node.Parent        == null || node.Parent       .NextNode == null)	// and no parent OR parent is last node at its level
						&& (node.Parent.Parent == null || node.Parent.Parent.NextNode == null))	// and no parent-of-parent OR parent-of-parent is last node at its level
					{
						return SearchTreeview(text, MapTree.Nodes, MapTree.Nodes[0], start0);
					}
				}
			}
			return null;
		}

		/// <summary>
		/// Clears the searched/found/highlighted treenode.
		/// </summary>
		internal void ClearSearched()
		{
			Searched = null;
			MapTree.Invalidate();
		}
		#endregion Events


		#region Events (tree)
		// __Sequence of Events__
		// MainViewF.OnMaptreeMouseDown()
		// MainViewF.OnMaptreeNodeMouseClick()
		// MainViewF.OnMaptreeBeforeSelect()
		// MainViewF.OnMaptreeAfterSelect()
		// MainViewF.LoadSelectedDescriptor()

		/// <summary>
		/// Tracks the currently loaded treenode.
		/// </summary>
		private TreeNode _loaded;

		/// <summary>
		/// just because you have billions of dollars doesn't mean you can beep
		/// </summary>
		private DontBeepType _dontbeeptype;
		private enum DontBeepType
		{
			OpenContext,
			LoadDescriptor,
			MapBrowserDialog
		}

		/// <summary>
		/// This flag is used to bypass checks when accessing the
		/// <c><see cref="TilesetEditor"/></c> while maintaining the current
		/// state of the actual changed-flags.
		/// </summary>
		private bool _bypassSavealert;

		/// <summary>
		/// To allow the <c><see cref="MapTree"/></c> to be navigated by
		/// keyboard without loading every darn tileset whose treenode gets
		/// selected during keyboard navigation this value shall be <c>false</c>
		/// unless
		/// <list type="bullet">
		/// <item>a <c>TreeNode</c> is clicked explicitly</item>
		/// <item><c><see cref="SelectTilesetNode()">SelectTilesetNode()</see></c></item>
		/// </list>
		/// </summary>
		/// <remarks>The value is checked in
		/// <c><see cref="OnMaptreeAfterSelect()">OnMaptreeAfterSelect()</see></c>.</remarks>
		private bool _loadready;


		/// <summary>
		/// Bypasses ornery system-beeps that can happen on keydown events.
		/// <list type="bullet">
		/// <item><c>[Space]</c> opens the Context menu</item>
		/// <item><c>[Enter]</c> loads a <c><see cref="Descriptor"/></c></item>
		/// <item><c>[Shift+Enter]</c> opens a <c>FolderBrowserDialog</c> if a
		/// tileset's files are invalid; will also load a <c>Descriptor</c> if
		/// the files are valid.</item>
		/// </list>
		/// </summary>
		/// <seealso cref="OnKeyDown()"><c>OnKeyDown()</c></seealso>
		private void DontBeep()
		{
			//Logfile.Log("MainViewF.DontBeep() _dontbeeptype= " + _dontbeeptype);

			switch (_dontbeeptype)
			{
				case DontBeepType.OpenContext:		// [Space]
				{
					Rectangle nodebounds = MapTree.SelectedNode.Bounds;
					var args = new MouseEventArgs(
												MouseButtons.Right,
												1,
												nodebounds.X + 15, nodebounds.Y + 5,
												0);
					OnMaptreeMouseDown(null, args);
					break;
				}

				case DontBeepType.LoadDescriptor:	// [Enter]
				{
					OnMaptreeAfterSelect(null, new TreeViewEventArgs(MapTree.SelectedNode));
					break;
				}

				case DontBeepType.MapBrowserDialog:	// [Shift+Enter]
				{
					var args = new TreeNodeMouseClickEventArgs(
															MapTree.SelectedNode,
															MouseButtons.Left,
															0, 0,0);
					OnMaptreeNodeMouseClick(null, args);
					break;
				}
			}
		}


		/// <summary>
		/// Opens the <c><see cref="MapTree">MapTree's</see></c> context on
		/// rightclick.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// <remarks>A <c>MouseDown</c> event occurs before <c>TreeView's</c>
		/// <c>BeforeSelect</c> and <c>AfterSelected</c> events occur .... A
		/// <c>MouseClick</c> event occurs after <c>TreeView's</c>
		/// <c>BeforeSelect</c> and <c>AfterSelected</c> events occur. So the
		/// selected Map will change before a context-menu is shown, which is
		/// good. A <c>MouseClick</c> event won't work if the tree is blank. So
		/// use <c>MouseDown</c>.</remarks>
		/// <seealso cref="OnMaptreeNodeMouseClick()"><c>OnMaptreeNodeMouseClick()</c></seealso>
		private void OnMaptreeMouseDown(object sender, MouseEventArgs e)
		{
			//Logfile.Log("MainViewF.OnMaptreeMouseDown()");

			if (e.Button == MouseButtons.Right && MapTree.SelectedNode != null // safety.
				&& (MapTree.SelectedNode.Level != TREELEVEL_TILESET
					|| MapTree.SelectedNode == _loaded))
			{
				// prevent a bunch of problems like looping dialogs when returning from
				// the Tileset Editor and the Maptree-node gets re-selected causing
				// this class-object to react as if a different Map is going to load ...
				// vid. LoadSelectedDescriptor()

				if (MapFile == null
					|| _bypassSavealert
					|| (   !MapFile.MapChanged
						&& !MapFile.RoutesChanged))
				{
					_bypassSavealert = false;

					cms_MapTreeContext.Items.Clear();

					cms_MapTreeContext.Items.Add("Add Group ...", null, OnAddGroupClick);

					if (MapTree.SelectedNode != null)
					{
						switch (MapTree.SelectedNode.Level)
						{
							case TREELEVEL_GROUP:
								cms_MapTreeContext.Items.Add(new ToolStripSeparator());
								cms_MapTreeContext.Items.Add("Edit Group ...",   null, OnEditGroupClick);
								cms_MapTreeContext.Items.Add("Delete Group",     null, OnDeleteGroupClick);
								cms_MapTreeContext.Items.Add(new ToolStripSeparator());
								cms_MapTreeContext.Items.Add("Add Category ...", null, OnAddCategoryClick);
								break;

							case TREELEVEL_CATEGORY:
								cms_MapTreeContext.Items.Add(new ToolStripSeparator());
								cms_MapTreeContext.Items.Add("Edit Category ...", null, OnEditCategoryClick);
								cms_MapTreeContext.Items.Add("Delete Category",   null, OnDeleteCategoryClick);
								cms_MapTreeContext.Items.Add(new ToolStripSeparator());
								cms_MapTreeContext.Items.Add("Add Tileset ...",   null, OnAddTilesetClick);
								break;

							case TREELEVEL_TILESET:
								cms_MapTreeContext.Items.Add(new ToolStripSeparator());
								cms_MapTreeContext.Items.Add("Edit Tileset ...", null, OnEditTilesetClick);
								cms_MapTreeContext.Items.Add("Delete Tileset",   null, OnDeleteTilesetClick);
								break;
						}
					}

					cms_MapTreeContext.Show(MapTree, e.Location);
				}
				else // MapFile != null && !_bypassSavealert && (MapFile.MapChanged || MapFile.RoutesChanged)
				{
					string info = GetChangedInfo();

					string head = Infobox.SplitString("Modifying the Maptree can cause the Tilesets"
								+ " to reload. The current " + info + " should be saved or else any"
								+ " changes will be lost. How do you wish to proceed?", 80);

					string copyable = "retry  - save changes and show the Maptree-menu"        + Environment.NewLine
									+ "ok     - risk losing changes and show the Maptree-menu" + Environment.NewLine
									+ "cancel - return to state";

					using (var f = new Infobox(
											"Changes detected",
											head,
											copyable,
											InfoboxType.Warn,
											InfoboxButton.CancelOkayRetry))
					{
						switch (f.ShowDialog(this))
						{
							default: // DialogResult.Cancel:
								return;

							case DialogResult.Retry:
								if (MapFile.MapChanged && MapFile.SaveMap())
								{
									MapChanged = false;

									if (MapFile.ForceReload)	// NOTE: Forcing reload is probably not necessary here
									{							// because the current Map is *probably* going to change. I think ...
										MapFile.ForceReload = false;
										LoadSelectedDescriptor(false, true);
									}
								}

								if (MapFile.RoutesChanged && MapFile.SaveRoutes())
								{
									RouteView.RoutesChangedCoordinator = false;
								}
								break;

							case DialogResult.OK:
								_bypassSavealert = true;
								break;
						}
					}

					OnMaptreeMouseDown(sender, e); // RECURSE^
				}
			}
		}


		/// <summary>
		/// Adds a group to the <c><see cref="MapTree"/></c>.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void OnAddGroupClick(object sender, EventArgs e)
		{
			using (var ib = new MapTreeInputBox(
											"Enter the label for a new Map group."
												+ " It needs to start with UFO or TFTD (case insensitive) since"
												+ " the prefix will set the default path and palette of its tilesets.",
											"Note that groups that do not contain tilesets will not be saved.",
											TreeboxType.AddGroup,
											String.Empty))
			{
				if (ib.ShowDialog(this) == DialogResult.OK)
				{
					MaptreeChanged = true;
					TileGroupManager.AddTileGroup(ib.Label);
					CreateTree();
					SelectGroupNode(ib.Label);
				}
			}
		}

		/// <summary>
		/// Edits the label of a group on the <c><see cref="MapTree"/></c>.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void OnEditGroupClick(object sender, EventArgs e)
		{
			using (var ib = new MapTreeInputBox(
											"Enter a new label for the Map group."
												+ " It needs to start with UFO or TFTD (case insensitive) since"
												+ " the prefix will set the default path and palette of its tilesets.",
											"Note that groups that do not contain tilesets will not be saved.",
											TreeboxType.EditGroup,
											String.Empty))
			{
				string labelGroup = MapTree.SelectedNode.Text;

				ib.Label = labelGroup;
				if (ib.ShowDialog(this) == DialogResult.OK)
				{
					MaptreeChanged = true;
					TileGroupManager.EditTileGroup(ib.Label, labelGroup);
					CreateTree();
					SelectGroupNode(ib.Label);
				}
			}
		}

		/// <summary>
		/// Deletes a group from the <c><see cref="MapTree"/></c>.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void OnDeleteGroupClick(object sender, EventArgs e)
		{
			// TODO: Make a custom box for delete Group/Category/Tileset.

			string head = Infobox.SplitString("Are you sure you want to remove"
						+ " this Map group? This will also remove all its categories"
						+ " and tilesets, but files on disk are unaffected.");

			string labelGroup = MapTree.SelectedNode.Text;

			using (var ib = new Infobox(
									"Warning",
									head,
									"group - " + labelGroup,
									InfoboxType.Warn,
									InfoboxButton.CancelOkay))
			{
				if (ib.ShowDialog(this) == DialogResult.OK)
				{
					if (MapFile != null)
					{
						MapChanged = RouteView.RoutesChangedCoordinator = false;
						OnCloseClick(null, EventArgs.Empty);
					}

					MaptreeChanged = true;
					TileGroupManager.DeleteTileGroup(labelGroup);
					CreateTree();
				}
			}
		}

		/// <summary>
		/// Adds a category to a group on the <c><see cref="MapTree"/></c>.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void OnAddCategoryClick(object sender, EventArgs e)
		{
			string labelGroup = MapTree.SelectedNode.Text;

			using (var ib = new MapTreeInputBox(
											"Enter the label for a new Map category.",
											"Note that categories that do not contain tilesets will not be saved.",
											TreeboxType.AddCategory,
											labelGroup))
			{
				if (ib.ShowDialog(this) == DialogResult.OK)
				{
					MaptreeChanged = true;
					TileGroupManager.TileGroups[labelGroup].AddCategory(ib.Label);
					CreateTree();
					SelectCategoryNode(labelGroup, ib.Label);
				}
			}
		}

		/// <summary>
		/// Edits the label of a category on the <c><see cref="MapTree"/></c>.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void OnEditCategoryClick(object sender, EventArgs e)
		{
			string labelGroup = MapTree.SelectedNode.Parent.Text;

			using (var ib = new MapTreeInputBox(
											"Enter a new label for the Map category.",
											"Note that categories that do not contain tilesets will not be saved.",
											TreeboxType.EditCategory,
											labelGroup))
			{
				string labelCategory = MapTree.SelectedNode.Text;

				ib.Label = labelCategory;
				if (ib.ShowDialog(this) == DialogResult.OK)
				{
					MaptreeChanged = true;
					TileGroupManager.TileGroups[labelGroup].EditCategory(ib.Label, labelCategory);
					CreateTree();
					SelectCategoryNode(labelGroup, ib.Label);
				}
			}
		}

		/// <summary>
		/// Deletes a category from the <c><see cref="MapTree"/></c>.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void OnDeleteCategoryClick(object sender, EventArgs e)
		{
			// TODO: Make a custom box for delete Group/Category/Tileset.

			string head = Infobox.SplitString("Are you sure you want to remove"
						+ " this Map category? This will also remove all its"
						+ " tilesets, but files on disk are unaffected.");

			string labelGroup    = MapTree.SelectedNode.Parent.Text;
			string labelCategory = MapTree.SelectedNode.Text;

			string copyable = "group    - " + labelGroup + Environment.NewLine
							+ "category - " + labelCategory;

			using (var ib = new Infobox(
									"Warning",
									head,
									copyable,
									InfoboxType.Warn,
									InfoboxButton.CancelOkay))
			{
				if (ib.ShowDialog(this) == DialogResult.OK)
				{
					if (MapFile != null)
					{
						MapChanged = RouteView.RoutesChangedCoordinator = false;
						OnCloseClick(null, EventArgs.Empty);
					}

					MaptreeChanged = true;
					TileGroupManager.TileGroups[labelGroup].DeleteCategory(labelCategory);
					CreateTree();
					SelectGroupNode(labelGroup);
				}
			}
		}

		/// <summary>
		/// Adds a tileset and its characteristics to the
		/// <c><see cref="MapTree"/></c>.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void OnAddTilesetClick(object sender, EventArgs e)
		{
			string labelGroup = MapTree.SelectedNode.Parent.Text;
			if (isGrouptypeConfigured(labelGroup))
			{
				string labelCategory = MapTree.SelectedNode.Text;
				string labelTileset  = String.Empty;

				using (var te = new TilesetEditor(
											TilesetEdit.CreateDescriptor,
											labelGroup,
											labelCategory,
											labelTileset))
				{
					if (te.ShowDialog(this) == DialogResult.OK)
					{
						MaptreeChanged = _bypassSavealert = true;

						CreateTree();
						SelectTilesetNode(labelGroup, labelCategory, te.TilesetLabel);
						_overlay.Invalidate();
					}
				}
			}
		}

		/// <summary>
		/// Edits the characteristics of a tileset on the
		/// <c><see cref="MapTree"/></c>.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void OnEditTilesetClick(object sender, EventArgs e)
		{
			string labelGroup = MapTree.SelectedNode.Parent.Parent.Text;
			if (isGrouptypeConfigured(labelGroup))
			{
				string labelCategory = MapTree.SelectedNode.Parent.Text;
				string labelTileset  = MapTree.SelectedNode.Text;

				using (var te = new TilesetEditor(
											TilesetEdit.DescriptorExists,
											labelGroup,
											labelCategory,
											labelTileset))
				{
					if (te.ShowDialog(this) == DialogResult.OK)
					{
						MaptreeChanged = _bypassSavealert = true;

						CreateTree();
						SelectTilesetNode(labelGroup, labelCategory, te.TilesetLabel);
						_overlay.Invalidate();
					}
				}
			}
		}

		/// <summary>
		/// Checks that the group-type is configured so that
		/// <c><see cref="TilesetEditor"/></c> doesn't explode. Shows an error
		/// if not configured.
		/// </summary>
		/// <param name="labelGroup">the label of the group</param>
		/// <returns><c>true</c> if okay to proceed</returns>
		private bool isGrouptypeConfigured(string labelGroup)
		{
			TileGroup @group = TileGroupManager.TileGroups[labelGroup];

			string key;
			if (@group.GroupType == GroupType.Tftd)
				key = SharedSpace.ResourceDirectoryTftd;
			else
				key = SharedSpace.ResourceDirectoryUfo;

			if (SharedSpace.GetShareString(key) == null)
			{
				if (@group.GroupType == GroupType.Tftd) key = "TFTD";
				else                                    key = "UFO";

				using (var ib = new Infobox(
										"Error",
										key + " is not configured.",
										null,
										InfoboxType.Error))
				{
					ib.ShowDialog(this);
				}
				return false;
			}
			return true;
		}

		/// <summary>
		/// Deletes a tileset from the <c><see cref="MapTree"/></c>.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void OnDeleteTilesetClick(object sender, EventArgs e)
		{
			// TODO: Make a custom box for delete Group/Category/Tileset.

			string head = Infobox.SplitString("Are you sure you want to remove"
						+ " this Map tileset? Files on disk are unaffected.");

			string labelGroup    = MapTree.SelectedNode.Parent.Parent.Text;
			string labelCategory = MapTree.SelectedNode.Parent.Text;
			string labelTileset  = MapTree.SelectedNode.Text;

			string copyable = "group    - " + labelGroup    + Environment.NewLine
							+ "category - " + labelCategory + Environment.NewLine
							+ "tileset  - " + labelTileset;

			using (var ib = new Infobox(
									"Warning",
									head,
									copyable,
									InfoboxType.Warn,
									InfoboxButton.CancelOkay))
			{
				if (ib.ShowDialog(this) == DialogResult.OK)
				{
					if (MapFile != null)
					{
						MapChanged = RouteView.RoutesChangedCoordinator = false;
						OnCloseClick(null, EventArgs.Empty);
					}

					MaptreeChanged = true;
					TileGroupManager.TileGroups[labelGroup].DeleteTileset(labelTileset, labelCategory);
					CreateTree();
					SelectCategoryNode(labelGroup, labelCategory);
				}
			}
		}


		/// <summary>
		/// Selects a treenode in the <c><see cref="MapTree"/></c> given
		/// <paramref name="labelGroup"/>.
		/// </summary>
		/// <param name="labelGroup"></param>
		private void SelectGroupNode(string labelGroup)
		{
			foreach (TreeNode nodeGroup in MapTree.Nodes)
			{
				if (nodeGroup.Text == labelGroup)
				{
					(MapTree.SelectedNode = nodeGroup).Expand();
					return;
				}
			}
		}

		/// <summary>
		/// Selects a treenode in the <c><see cref="MapTree"/></c> given
		/// <paramref name="labelGroup"/> and <paramref name="labelCategory"/>.
		/// </summary>
		/// <param name="labelGroup"></param>
		/// <param name="labelCategory"></param>
		private void SelectCategoryNode(string labelGroup, string labelCategory)
		{
			foreach (TreeNode nodeGroup in MapTree.Nodes)
			{
				if (nodeGroup.Text == labelGroup)
				{
					foreach (TreeNode nodeCategory in nodeGroup.Nodes)
					{
						if (nodeCategory.Text == labelCategory)
						{
							(MapTree.SelectedNode = nodeCategory).Expand();
							return;
						}
					}

					(MapTree.SelectedNode = nodeGroup).Expand(); // safety ->
					return;
				}
			}
		}

		/// <summary>
		/// Selects a treenode in the <c><see cref="MapTree"/></c> given
		/// <paramref name="labelGroup"/> and <paramref name="labelCategory"/>
		/// and <paramref name="labelTileset"/>.
		/// </summary>
		/// <param name="labelGroup"></param>
		/// <param name="labelCategory"></param>
		/// <param name="labelTileset"></param>
		private void SelectTilesetNode(string labelGroup, string labelCategory, string labelTileset)
		{
			foreach (TreeNode nodeGroup in MapTree.Nodes)
			{
				if (nodeGroup.Text == labelGroup)
				{
					foreach (TreeNode nodeCategory in nodeGroup.Nodes)
					{
						if (nodeCategory.Text == labelCategory)
						{
							foreach (TreeNode nodeTileset in nodeCategory.Nodes)
							{
								if (nodeTileset.Text == labelTileset)
								{
									_loadready = true;
									MapTree.SelectedNode = nodeTileset;
									return;
								}
							}

							(MapTree.SelectedNode = nodeCategory).Expand(); // safety ->
							return;
						}
					}

					(MapTree.SelectedNode = nodeGroup).Expand(); // safety ->
					return;
				}
			}
		}


		/// <summary>
		/// For an ungodly reason when the <c><see cref="MapTree"/></c>
		/// gains/loses focus
		/// <c><see cref="OnMaptreeDrawNode()">OnMaptreeDrawNode()</see></c>
		/// re-colors selected/ focused treenodes correctly but does not
		/// re-color a <c><see cref="Searched"/></c> treenode or a
		/// <c><see cref="_loaded"/></c> treenode if it's not also selected/
		/// focused.
		/// </summary>
		/// <param name="sender"><c><see cref="MapTree"/></c></param>
		/// <param name="e"></param>
		private void OnMaptreeFocusChanged(object sender, EventArgs e)
		{
			//Logfile.Log("MainViewF.OnMaptreeFocusChanged()");

			if ((_loaded != null && MapTree.SelectedNode != _loaded)
				|| Searched != null)
			{
				MapTree.Invalidate();
			}
		}

		/// <summary>
		/// If user clicks on an already selected treenode for which the Mapfile
		/// has not been loaded
		/// <c><see cref="MapFileService"/>.LoadDescriptor()</c> offers to show
		/// a dialog for the user to browse to the file. Otherwise it just loads
		/// the Mapfile for the clicked treenode.
		/// </summary>
		/// <param name="sender"><c><see cref="MapTree"/></c></param>
		/// <param name="e"></param>
		/// <remarks><paramref name="sender"/> is <c>null</c> on
		/// <c>[Shift+Enter]</c> keypress.</remarks>
		/// <seealso cref="OnMaptreeMouseDown()"><c>OnMaptreeMouseDown()</c></seealso>
		private void OnMaptreeNodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
		{
			//Logfile.Log("MainViewF.OnMaptreeNodeMouseClick() " + MapTree.SelectedNode);

			if (e.Button == MouseButtons.Left
				&& e.Node.Level == TREELEVEL_TILESET
				&& (sender == null // [Shift+Enter]
					|| MapTree.HitTest(e.Location).Location == TreeViewHitTestLocations.Label))
			{
				if (MapTree.SelectedNode != null && e.Node == MapTree.SelectedNode
					&& (MapFile == null || MapFile.Descriptor != MapTree.SelectedNode.Tag as Descriptor))
				{
					//Logfile.Log(". browseForMapfile");
					LoadSelectedDescriptor(true);
				}
				else
					_loadready = true;
			}
		}

		/// <summary>
		/// Asks user to save Map and Routes before selecting a treenode if
		/// applicable.
		/// </summary>
		/// <param name="sender"><c><see cref="MapTree"/></c></param>
		/// <param name="e"></param>
		private void OnMaptreeBeforeSelect(object sender, CancelEventArgs e)
		{
			//Logfile.Log("MainViewF.OnMaptreeBeforeSelect() " + MapTree.SelectedNode);

			if (!_bypassSavealert) // is true on TilesetEditor DialogResult.OK
			{
				e.Cancel  = (SaveAlertMap()    == DialogResult.Cancel);
				e.Cancel |= (SaveAlertRoutes() == DialogResult.Cancel); // NOTE: that bitwise had better execute ....
			}
//			else
//				_bypassSavealert = false;
		}

		/// <summary>
		/// Loads the currently selected treenode's tileset or closes the
		/// currently open tileset if the selected node's level is not
		/// <c><see cref="MainViewF.TREELEVEL_TILESET">MainViewF.TREELEVEL_TILESET</see></c>.
		/// </summary>
		/// <param name="sender"><c><see cref="MapTree"/></c></param>
		/// <param name="e"></param>
		/// <remarks><paramref name="sender"/> is <c>null</c> on <c>[Enter]</c>
		/// keypress.</remarks>
		private void OnMaptreeAfterSelect(object sender, TreeViewEventArgs e)
		{
			//Logfile.Log("MainViewF.OnMaptreeAfterSelect() " + MapTree.SelectedNode);

			if (e.Node.Level == TREELEVEL_TILESET
				&& (sender == null || _loadready))
			{
				LoadSelectedDescriptor();
			}
			else if (MapFile != null)
			{
				OnCloseClick(null, EventArgs.Empty);
			}
		}
		#endregion Events (tree)


		#region Methods
		/// <summary>
		/// Tries to load the tileset that's selected in the
		/// <c><see cref="MapTree"/></c>.
		/// </summary>
		/// <param name="browseMapfile"><c>true</c> to force the find Mapfile
		/// dialog</param>
		/// <param name="keepRoutes"><c>true</c> to keep the current
		/// <c><see cref="RouteNodes"/></c> - pass <c>true</c> only when
		/// crippled <c><see cref="Tilepart">Tileparts</see></c> got wiped
		/// during a successfully save of the <c><see cref="MapFile"/></c></param>
		private void LoadSelectedDescriptor(bool browseMapfile = false, bool keepRoutes = false)
		{
			//Logfile.Log("MainViewF.LoadSelectedDescriptor() " + MapTree.SelectedNode);

			ClearSearched();
			_bypassSavealert = false;

			TopView.ClosePartslotTestDialog();

			var descriptor = MapTree.SelectedNode.Tag as Descriptor; // Descriptor can be null if !Mapfile.FileValid
			if (descriptor != null)
			{
				//Logfile.Log(". descriptor Valid");

				RouteNodes routes; bool routesChanged;
				if (keepRoutes)
				{
					routes        = MapFile.Routes;
					routesChanged = MapFile.RoutesChanged;
				}
				else
				{
					routes = null;
					routesChanged = false;
				}


				// try this in case MapFile.LoadMapfile() needs to show a
				// dialog about partIds exceeding the allocated terrainset.
				// I think the crippled sprites aren't ready to go yet and
				// .net tries to draw the Map and throws up when returning
				// from that dialog. Then likely due to a redundancy of
				// calls to the draw-routine the Map gets drawn correctly
				// anyway after the crippled sprites are then ready ->

				Dontdrawyougits = true;

				MapFile file = MapFileService.LoadDescriptor(
														descriptor,
														ref browseMapfile,
														Optionables.IgnoreRecordsExceeded,
														routes,
														MapTree.SelectedNode,
														!ObserverManager.TopView.Control.it_Floor.Checked);
				if (!MaptreeChanged && browseMapfile) MaptreeChanged = true;

				Dontdrawyougits = false;


				if (file != null)
				{
					//Logfile.Log(". file Valid");

					_loaded = MapTree.SelectedNode;

					descriptor.FileValid = true;

					if (file.TerrainsetCountExceeded != 0) // don't ask why 'cause I don't know ...
						MapTree.Invalidate();


					EnableMenus(true);
					EnableObservers(true);

					if (descriptor.GroupType == GroupType.Tftd)
					{
						ViewersMenuManager.EnableScanG(SpritesetManager.GetScanGtftd() != null);
						Palette.MonoBrushes = Palette.BrushesTftdBattle; // used by Mono only
					}
					else // default to ufo-battle palette
					{
						ViewersMenuManager.EnableScanG(SpritesetManager.GetScanGufo() != null);
						Palette.MonoBrushes = Palette.BrushesUfoBattle; // used by Mono only
					}


					MapFile = file; // clears '_overlay.FirstClick'

					ObserverManager.ToolFactory.EnableAutoscale(true);
					ObserverManager.ToolFactory.EnableLevelers(file.Level, file.Levs);

					Text = TITLE + " " + descriptor.Basepath;
					if (MaptreeChanged) MaptreeChanged = MaptreeChanged; // maniacal laugh YOU figure it out.

					tsslMapLabel     .Text = descriptor.Label;
					tsslDimensions   .Text = file.SizeString;
					tsslPosition     .Text =
					tsslSelectionSize.Text = String.Empty;

					if (!file.MapChanged)
						MapChanged = (file.TerrainsetCountExceeded != 0);

					file.TerrainsetCountExceeded = 0; // TODO: Perhaps do that when the Mapfile is saved.

					RouteView.ClearSelectedInfo();

					Options[MainViewOptionables.str_OpenDoors].Value = // close doors; not necessary but keeps user's head on straight.
					Optionables.OpenDoors = false;
					SetDoorSpritesFullPhase(false);
					if (_foptions != null && _foptions.Visible)
						_foptions.propertyGrid.Refresh();

					SelectToner(); // create toned spriteset(s) for selected-tile(s)

					if (!menuViewers.Enabled) // show the forms that are flagged to show (in MainView's Options).
						ViewersMenuManager.StartSecondStageBoosters();

					ObserverManager.AssignMapfile(file); // reset all observers' Mapfile var

					RouteCheckService.SetBase1( // send the base1-count options to 'XCom' ->
											MainViewF.Optionables.Base1_xy,
											MainViewF.Optionables.Base1_z);

					if (RouteCheckService.CheckNodeBounds(file) == DialogResult.Yes)
					{
						RouteView.RoutesChangedCoordinator = true;

						foreach (RouteNode node in RouteCheckService.Invalids)
							file.Routes.DeleteNode(node);
					}

					if (routesChanged && !file.RoutesChanged)
						RouteView.RoutesChangedCoordinator = true;

					Globals.Scale = Globals.Scale; // enable/disable the scale-in/scale-out buttons

					if (ScanG != null) // update ScanG viewer if open
						ScanG.LoadMapfile(file);

					McdInfoF fMcdInfo = ObserverManager.TileView.Control.McdInfo; // update MCD Info if open ->
					if (fMcdInfo != null)
						fMcdInfo.UpdateData();

					if (RouteView.SpawnInfo != null) // update SpawnInfo if open ->
						RouteView.SpawnInfo.Initialize(file);

					ResetQuadrantPanel(); // update the Quadrant panel

					FirstActivated = false;
					Activate();

					return;
				}

				descriptor.FileValid = false;
				//Logfile.Log(". file NOT Valid");
			}
			//else Logfile.Log(". descriptor NOT Valid");

			if (MapFile != null)
				OnCloseClick(null, EventArgs.Empty);
		}

		/// <summary>
		/// Enables all relevant its under the
		/// <c><see cref="mmMain">MainMenu</see></c> when a
		/// <c><see cref="MapFile"/></c> loads.
		/// </summary>
		/// <param name="enable"><c>true</c> to enable</param>
		private void EnableMenus(bool enable)
		{
			miSaveAll             .Enabled =
			miSaveMap             .Enabled =
			miSaveRoutes          .Enabled =
			miExport              .Enabled =
			miReload              .Enabled =
			miClose               .Enabled =
			miScreenshot          .Enabled =
			miModifySize          .Enabled =
			miTilepartSubstitution.Enabled =
			miTerrainSwap         .Enabled =
			miMapInfo             .Enabled = enable;
		}

		/// <summary>
		/// Dis/enables <c>ToolStrips</c> on the observers.
		/// </summary>
		/// <param name="enable"><c>true</c> to enable</param>
		private static void EnableObservers(bool enable)
		{
			ObserverManager.TileView    .Control     .Enable(enable);
			ObserverManager.TopView     .Control     .Enable(enable);
			ObserverManager.TopRouteView.ControlTop  .Enable(enable);
			ObserverManager.RouteView   .Control     .Enable(enable);
			ObserverManager.TopRouteView.ControlRoute.Enable(enable);
		}


		/// <summary>
		/// Selects the toned <c><see cref="Palette"/></c> to be used for
		/// drawing <c><see cref="Tilepart">Tileparts</see></c> of selected
		/// tiles.
		/// </summary>
		/// <remarks>See
		/// <c><see cref="MainViewOptionables.TONER_NONE">MainViewOptionables.TONER_*</see></c>
		/// for the toner constants.</remarks>
		internal void SelectToner()
		{
			int toner = Optionables.SelectedTileToner;

			miNone .Checked = (toner == MainViewOptionables.TONER_NONE);
			miGray .Checked = (toner == MainViewOptionables.TONER_GRAY);
			miRed  .Checked = (toner == MainViewOptionables.TONER_RED);
			miGreen.Checked = (toner == MainViewOptionables.TONER_GREEN);
			miBlue .Checked = (toner == MainViewOptionables.TONER_BLUE);

			SetTonedPalette();
		}

		/// <summary>
		/// Sets the toned <c><see cref="Palette"/></c> to be used for drawing
		/// <c><see cref="Tilepart">Tileparts</see></c> of selected tiles based
		/// on
		/// <c><see cref="MainViewOptionables.SelectedTileToner">MainViewOptionables.SelectedTileToner</see></c>.
		/// </summary>
		internal static void SetTonedPalette()
		{
			if (SpritesetManager.Spritesets.Count != 0)
			{
				ColorPalette table;

				switch (Optionables.SelectedTileToner)
				{
					case MainViewOptionables.TONER_NONE:
						return; // sprites shall draw their standard, no-toned palette version

					// NOTE: All loaded spritesets will have the same palette.
					// - this demonstrates how redundant palette-pointers are.
					//   palette this palette that, here have a palette ...
					//   i know you want one

					default: // case TONER_GRAY
						table = SpritesetManager.Spritesets[0].Pal.GrayScale.Table;
						break;

					case MainViewOptionables.TONER_RED:
						table = SpritesetManager.Spritesets[0].Pal.RedScale.Table;
						break;

					case MainViewOptionables.TONER_GREEN:
						table = SpritesetManager.Spritesets[0].Pal.GreenScale.Table;
						break;

					case MainViewOptionables.TONER_BLUE:
						table = SpritesetManager.Spritesets[0].Pal.BlueScale.Table;
						break;
				}

				foreach (Spriteset spriteset in SpritesetManager.Spritesets)
				for (int id = 0; id != spriteset.Count; ++id)
				{
					(spriteset[id] as PckSprite).SpriteToned.Palette = table; // lovely.
				}
			}
		}

		/// <summary>
		/// Resets the <c><see cref="QuadrantControl"/></c> when either a Map
		/// loads or gets resized or gets closed.
		/// </summary>
		private static void ResetQuadrantPanel()
		{
			QuadrantControl p1 = ObserverManager.TopView     .Control   .QuadrantControl;
			QuadrantControl p2 = ObserverManager.TopRouteView.ControlTop.QuadrantControl;

			p1.Tile =
			p2.Tile = null;

			p1.SelectedLocation =
			p2.SelectedLocation = null;

			QuadrantDrawService.SelectedTilepart = ObserverManager.TileView.Control.SelectedTilepart;

			p1.Invalidate();
			p2.Invalidate();
		}


		/// <summary>
		/// Sets door-sprites to fullphase or firstphase.
		/// </summary>
		/// <param name="full">true to animate any doors</param>
		internal void SetDoorSpritesFullPhase(bool full)
		{
			if (MapFile != null) // NOTE: MapFile is null on MapView load.
			{
				foreach (Tilepart part in MapFile.Parts)
					part.ToggleDoorSprites(full);
			}
		}

		/// <summary>
		/// Sets door-sprites to their alternate sprite's firstphase.
		/// </summary>
		internal void SetDoorSpritesAlternate()
		{
			if (MapFile != null) // NOTE: MapFile is null on MapView load.
			{
				foreach (Tilepart part in MapFile.Parts)
					part.SetSprite1_altr();
			}
		}


		/// <summary>
		/// Shows the user a dialog-box asking to Save if the currently
		/// displayed Map has changed.
		/// </summary>
		/// <returns><c>DialogResult.OK</c> if things can proceed;
		/// <c>DialogResult.Cancel</c> if user chose to cancel or the Mapfile
		/// was not written successfully</returns>
		/// <remarks>Is called when either (a) MapView is closing (b) a Map is
		/// about to load/reload.</remarks>
		private DialogResult SaveAlertMap()
		{
			if (MapFile != null && MapFile.MapChanged)
			{
				using (var f = new Infobox(
										"Map Changed",
										"Do you want to save changes to the Map?",
										null,
										InfoboxType.Warn,
										InfoboxButton.CancelYesNo)) // cancel/ok/retry
				{
					switch (f.ShowDialog(this))
					{
						case DialogResult.Cancel:	// close dialog and maintain state
							return DialogResult.Cancel;

						case DialogResult.OK:		// Yes. save Mapfile and clear MapChanged flag
							if (MapFile.SaveMap())
								goto case DialogResult.Retry;

							return DialogResult.Cancel; // save failed.

						case DialogResult.Retry:	// No. don't save just clear MapChanged flag
							MapChanged = false;
							break;
					}
				}
			}
			return DialogResult.OK;
		}

		/// <summary>
		/// Shows the user a dialog-box asking to Save if the currently
		/// displayed Routes has changed.
		/// </summary>
		/// <returns><c>DialogResult.OK</c> if things can proceed;
		/// <c>DialogResult.Cancel</c> if user chose to cancel or the Routefile
		/// was not written successfully</returns>
		/// <remarks>Is called when either (a) MapView is closing (b) another
		/// Map is about to load.</remarks>
		private DialogResult SaveAlertRoutes()
		{
			if (MapFile != null && MapFile.RoutesChanged)
			{
				using (var f = new Infobox(
										"Routes Changed",
										"Do you want to save changes to the Routes?",
										null,
										InfoboxType.Warn,
										InfoboxButton.CancelYesNo)) // cancel/ok/retry
				{
					switch (f.ShowDialog(this))
					{
						case DialogResult.Cancel:	// close dialog and maintain state
							return DialogResult.Cancel;

						case DialogResult.OK:		// Yes. save Routes and clear RoutesChanged flag
							if (MapFile.SaveRoutes())
								goto case DialogResult.Retry;

							return DialogResult.Cancel; // save failed.

						case DialogResult.Retry:	// No. don't save just clear RoutesChanged flag
							RouteView.RoutesChangedCoordinator = false;
							break;
					}
				}
			}
			return DialogResult.OK;
		}

		/// <summary>
		/// Shows the user a dialog-box asking to save the
		/// <c><see cref="MapTree"/></c> if it has changed.
		/// </summary>
		/// <returns><c>DialogResult.OK</c> if things can proceed;
		/// <c>DialogResult.Cancel</c> if user chose to cancel or MapTilesets
		/// was not written successfully.</returns>
		/// <remarks>Is called when either (a) MapView is closing (b) MapView is
		/// reloading due to a configuration change (ie. only if resource-paths
		/// have been changed, since the only other relevant option - if the
		/// tilesets-config file - is changed then saving the current one is
		/// pointless).</remarks>
		private DialogResult SaveAlertMaptree()
		{
			if (MaptreeChanged)
			{
				using (var f = new Infobox(
										"Maptree Changed",
										"Do you want to save changes to the Map Tree?",
										null,
										InfoboxType.Warn,
										InfoboxButton.CancelYesNo)) // cancel/ok/retry
				{
					switch (f.ShowDialog(this))
					{
						case DialogResult.Cancel:	// close dialog and maintain state
							return DialogResult.Cancel;

						case DialogResult.OK:		// Yes. save MapTilesets then check MaptreeChanged flag
							OnSaveMaptreeClick(null, EventArgs.Empty);
							if (MaptreeChanged)
								return DialogResult.Cancel; // save failed.
							break;

						case DialogResult.Retry:	// No. don't save just clear MaptreeChanged flag
							MaptreeChanged = false;
							break;
					}
				}
			}
			return DialogResult.OK;
		}


		/// <summary>
		/// Prints the currently selected location to the statusbar.
		/// </summary>
		internal void sb_PrintPosition()
		{
			if (_overlay.FirstClick)
			{
				tsslPosition.Text = Globals.GetLocationString(
															MapFile.Location.Col,
															MapFile.Location.Row,
															MapFile.Level,
															MapFile.Levs);
			}
		}

		/// <summary>
		/// Prints the current scale to the statusbar.
		/// </summary>
		internal void sb_PrintScale()
		{
			tsslScale.Text = String.Format(
										CultureInfo.CurrentCulture,
										"scale {0:0.00}",
										Globals.Scale);
		}

		/// <summary>
		/// Prints the current selection-size to the statusbar.
		/// </summary>
		/// <param name="tx"></param>
		/// <param name="ty"></param>
		internal void sb_PrintSelectionSize(int tx, int ty)
		{
			tsslSelectionSize.Text = tx + " x " + ty;
			ssMain.Refresh(); // fast update for selection-size.
		}
		#endregion Methods
	}
}


		// TODO: consolidate the select node functions into a single function.

/*		/// <summary>
		/// Selects the top treenode in the <c><see cref="MapTree"/></c> if one
		/// exists.
		/// </summary>
		private void SelectGroupNodeTop()
		{
			if (MapTree.Nodes.Count != 0)
				(MapTree.SelectedNode = MapTree.Nodes[0]).Expand();
		} */

/*		/// <summary>
		/// Selects the top category treenode in the
		/// <c><see cref="MapTree"/></c> if one exists under a given group
		/// treenode.
		/// </summary>
		/// <param name="labelGroup"></param>
		/// <remarks>Assumes that the parent-group node is valid.</remarks>
		private void SelectCategoryNodeTop(string labelGroup)
		{
			foreach (TreeNode nodeGroup in MapTree.Nodes)
			{
				if (nodeGroup.Text == labelGroup)
				{
					if (nodeGroup.Nodes.Count != 0)
						MapTree.SelectedNode = nodeGroup.Nodes[0];
					else
						MapTree.SelectedNode = nodeGroup;

					MapTree.SelectedNode.Expand();
					return;
				}
			}
		} */

/*		/// <summary>
		/// Selects the top tileset treenode in the <c><see cref="MapTree"/></c>
		/// if one exists under a given category treenode.
		/// </summary>
		/// <param name="labelGroup"></param>
		/// <param name="labelCategory"></param>
		/// <remarks>Assumes that the parent-parent-group and parent-category
		/// nodes are valid.</remarks>
		private void SelectTilesetNodeTop(string labelGroup, string labelCategory)
		{
			foreach (TreeNode nodeGroup in MapTree.Nodes)
			{
				if (nodeGroup.Text == labelGroup)
				{
					foreach (TreeNode nodeCategory in nodeGroup.Nodes)
					{
						if (nodeCategory.Text == labelCategory)
						{
							if (nodeCategory.Nodes.Count != 0)
								MapTree.SelectedNode = nodeCategory.Nodes[0];
							else
								(MapTree.SelectedNode = nodeCategory).Expand();

							return;
						}
					}

					(MapTree.SelectedNode = nodeGroup).Expand(); // safety ->
					return;
				}
			}
		} */
