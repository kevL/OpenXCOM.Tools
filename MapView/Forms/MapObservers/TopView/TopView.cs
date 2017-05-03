using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

//using Microsoft.Win32;

using MapView.Forms.MainWindow;

using XCom;


namespace MapView.Forms.MapObservers.TopViews
{
	internal sealed partial class TopView
		:
			MapObserverControl0
	{
		#region Fields
		private event EventHandler VisibleTileChangedEvent;

		private readonly TopViewPanel _topViewPanel;
		internal TopViewPanel TopViewPanel
		{
			get { return _topViewPanel; }
		}

		private EditButtonsFactory _editButtonsFactory;

		private Dictionary<string, Pen> _topPens;
		private Dictionary<string, SolidBrush> _topBrushes;

//		private readonly Dictionary<ToolStripMenuItem, int> _visQuadsDictionary = new Dictionary<ToolStripMenuItem, int>();
		#endregion


		#region Properties
		internal QuadrantPanel QuadrantsPanel
		{
			get { return quadrants; }
		}

		public bool GroundVisible
		{
			get { return _topViewPanel.Ground.Checked; }
		}

		public bool NorthVisible
		{
			get { return _topViewPanel.North.Checked; }
		}

		public bool WestVisible
		{
			get { return _topViewPanel.West.Checked; }
		}

		public bool ContentVisible
		{
			get { return _topViewPanel.Content.Checked; }
		}
		#endregion


		#region cTor
		/// <summary>
		/// cTor. Instantiates the TopView viewer and its components/controls.
		/// </summary>
		internal TopView()
		{
			InitializeComponent();

			SuspendLayout();

			_topViewPanel = new TopViewPanel();
			_topViewPanel.Dock = DockStyle.Fill;
//			_topViewPanel.Width  = 100;//pMain.Width;
//			_topViewPanel.Height = 100;//pMain.Height;

			pMain.AutoScroll = true;
			pMain.Controls.Add(_topViewPanel);

			pMain.Resize += (sender, e) => _topViewPanel.ResizeObserver(pMain.Width, pMain.Height);

			var visQuads = tsddbVisibleQuads.DropDown.Items;

			_topViewPanel.Ground = new ToolStripMenuItem("Floor");
			visQuads.Add(_topViewPanel.Ground);
			_topViewPanel.Ground.ShortcutKeys = Keys.F1;
			_topViewPanel.Ground.Checked = true;
//			_visQuadsDictionary[_topViewPanel.Ground] = 0;

			_topViewPanel.West = new ToolStripMenuItem("West");
			visQuads.Add(_topViewPanel.West);
			_topViewPanel.West.ShortcutKeys = Keys.F2;
			_topViewPanel.West.Checked = true;
//			_visQuadsDictionary[_topViewPanel.West] = 1;

			_topViewPanel.North = new ToolStripMenuItem("North");
			visQuads.Add(_topViewPanel.North);
			_topViewPanel.North.ShortcutKeys = Keys.F3;
			_topViewPanel.North.Checked = true;
//			_visQuadsDictionary[_topViewPanel.North] = 2;

			_topViewPanel.Content = new ToolStripMenuItem("Content");
			visQuads.Add(_topViewPanel.Content);
			_topViewPanel.Content.ShortcutKeys = Keys.F4;
			_topViewPanel.Content.Checked = true;
//			_visQuadsDictionary[_topViewPanel.Content] = 3;

			foreach (ToolStripMenuItem it in visQuads)
				it.Click += OnToggleQuadrantVisibilityClick;

			_topViewPanel.QuadrantsPanel = QuadrantsPanel;

			MoreObservers.Add("QuadrantsPanel", QuadrantsPanel);
			MoreObservers.Add("TopViewPanel", _topViewPanel);

			ResumeLayout();
		}
		#endregion


		#region EventCalls
		/// <summary>
		/// Handles a click on any of the quadrant-visibility menuitems.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void OnToggleQuadrantVisibilityClick(object sender, EventArgs e)
		{
			var it = sender as ToolStripMenuItem;
			it.Checked = !it.Checked;

			if (VisibleTileChangedEvent != null)
				VisibleTileChangedEvent(this, new EventArgs());

			MainViewUnderlay.Instance.Refresh();
			Refresh();
		}


		private Form _foptions;
		private bool _closing;

		/// <summary>
		/// Handles a click on the Options menuitem.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void OnOptionsClick(object sender, EventArgs e)
		{
			var it = (ToolStripMenuItem)sender;
			if (!it.Checked)
			{
				it.Checked = true;

				_foptions = new OptionsForm("TopViewOptions", Settings);
				_foptions.Text = "Top View Options";

				_foptions.Show();

				_foptions.Closing += (sender1, e1) =>
				{
					if (!_closing)
						OnOptionsClick(sender, e);

					_closing = false;
				};
			}
			else
			{
				_closing = true;

				it.Checked = false;
				_foptions.Close();
			}
		}

		/// <summary>
		/// Saves the Map on a Ctrl-S keydown event.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void OnKeyDown(object sender, KeyEventArgs e) // TODO: vet that.
		{
//			if (MapBase != null
//				&& e.Control && e.KeyCode == Keys.S)
//			{
//				MapBase.Save();
//				e.Handled = true; // why.
//			}
		}
		#endregion


		#region Methods
		/// <summary>
		/// Initializes the edit-buttons toolstrip.
		/// </summary>
		/// <param name="editButtons"></param>
		internal void InitializeEditStrip(EditButtonsFactory editButtons)
		{
			_editButtonsFactory = editButtons;
			_editButtonsFactory.CreateEditorStrip(tsEdit);
		}

		/// <summary>
		/// Selects a quadrant in the QuadrantsPanel given a selected tiletype.
		/// </summary>
		/// <param name="tileType"></param>
		internal void SelectQuadrant(TileType tileType)
		{
			switch (tileType)
			{
				case TileType.Ground:
					QuadrantsPanel.SelectedQuadrant = QuadrantType.Ground;
					break;

				case TileType.WestWall:
					QuadrantsPanel.SelectedQuadrant = QuadrantType.West;
					break;

				case TileType.NorthWall:
					QuadrantsPanel.SelectedQuadrant = QuadrantType.North;
					break;

				case TileType.Object:
					QuadrantsPanel.SelectedQuadrant = QuadrantType.Content;
					break;
			}
		}
		#endregion


		#region Settings
		internal const string FloorColor        = "FloorColor";
		internal const string WestColor         = "WestColor";
		internal const string NorthColor        = "NorthColor";
		internal const string ContentColor      = "ContentColor";

		private  const string WestWidth         = "WestWidth";
		private  const string NorthWidth        = "NorthWidth";

		internal const string SelectorColor     = "SelectorColor";
		internal const string SelectorWidth     = "SelectorWidth";

		internal const string SelectedColor     = "SelectedColor";
		private  const string SelectedWidth     = "SelectedWidth";

		private const string SelectedPartColor = "SelectedPartColor";

		internal const string GridColor         = "GridColor";
		private  const string GridWidth         = "GridWidth";

//		internal const string TileMinHeight     = "TileMinHeight";


		/// <summary>
		/// Loads default settings for TopView in TopRouteView screens.
		/// </summary>
		protected internal override void LoadControl0Settings()
		{
			_topBrushes = new Dictionary<string, SolidBrush>();
			_topPens    = new Dictionary<string, Pen>();

			_topBrushes.Add(FloorColor, new SolidBrush(Color.BurlyWood));
			_topBrushes.Add(ContentColor, new SolidBrush(Color.MediumSeaGreen));
			_topBrushes.Add(SelectedPartColor, QuadrantsPanel.SelectColor);

			var penWest = new Pen(new SolidBrush(Color.Khaki), 4);
			_topPens.Add(WestColor, penWest);
			_topPens.Add(WestWidth, penWest);

			var penNorth = new Pen(new SolidBrush(Color.Wheat), 4);
			_topPens.Add(NorthColor, penNorth);
			_topPens.Add(NorthWidth, penNorth);

			var penOver = new Pen(new SolidBrush(Color.Black), 2);
			_topPens.Add(SelectorColor, penOver);
			_topPens.Add(SelectorWidth, penOver);

			var penSelected = new Pen(new SolidBrush(Color.RoyalBlue), 2);
			_topPens.Add(SelectedColor, penSelected);
			_topPens.Add(SelectedWidth, penSelected);

			var penGrid = new Pen(new SolidBrush(Color.Black), 1);
			_topPens.Add(GridColor, penGrid);
			_topPens.Add(GridWidth, penGrid);

			ValueChangedEventHandler bc = OnBrushChanged;
			ValueChangedEventHandler pc = OnPenColorChanged;
			ValueChangedEventHandler pw = OnPenWidthChanged;
//			ValueChangedEventHandler dh = OnDiamondHeight;

			Settings.AddSetting(FloorColor,        Color.BurlyWood,                 "Color of the floor tile indicator",           "Tile",   bc);
			Settings.AddSetting(WestColor,         Color.Khaki,                     "Color of the west tile indicator",            "Tile",   pc);
			Settings.AddSetting(NorthColor,        Color.Wheat,                     "Color of the north tile indicator",           "Tile",   pc);
			Settings.AddSetting(ContentColor,      Color.MediumSeaGreen,            "Color of the content tile indicator",         "Tile",   bc);
			Settings.AddSetting(WestWidth,         3,                               "Width of the west tile indicator in pixels",  "Tile",   pw);
			Settings.AddSetting(NorthWidth,        3,                               "Width of the north tile indicator in pixels", "Tile",   pw);

			Settings.AddSetting(SelectorColor,     Color.Black,                     "Color of the mouse-over indicator",           "Select", pc);
			Settings.AddSetting(SelectorWidth,     2,                               "Width of the mouse-over indicator in pixels", "Select", pw);
			Settings.AddSetting(SelectedColor,     Color.RoyalBlue,                 "Color of the selection line",                 "Select", pc);
			Settings.AddSetting(SelectedWidth,     2,                               "Width of the selection line in pixels",       "Select", pw);
			Settings.AddSetting(SelectedPartColor, Color.LightBlue,                 "Background color of the selected tiletype",   "Select", bc);

			Settings.AddSetting(GridColor,         Color.Black,                     "Color of the grid lines",                     "Grid",   pc);
			Settings.AddSetting(GridWidth,         1,                               "Width of the grid lines in pixels",           "Grid",   pw);
//			Settings.AddSetting(TileMinHeight,     _topViewPanel.TileLozengeHeight, "Minimum height of the grid tiles in pixels",  "Grid",   dh);

			QuadrantsPanel.Pens   =
			_topViewPanel.TopPens = _topPens;

			QuadrantsPanel.Brushes   =
			_topViewPanel.TopBrushes = _topBrushes;

			Invalidate();
		}

		/// <summary>
		/// Fires when a brush-color changes in Settings.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="key"></param>
		/// <param name="val"></param>
		private void OnBrushChanged(object sender, string key, object val)
		{
			_topBrushes[key].Color = (Color)val;

			if (key == SelectedPartColor)
				QuadrantsPanel.SelectColor = _topBrushes[key];

			Refresh();
		}

		/// <summary>
		/// Fires when a pen-color changes in Settings.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="key"></param>
		/// <param name="val"></param>
		private void OnPenColorChanged(object sender, string key, object val)
		{
			_topPens[key].Color = (Color)val;
			Refresh();
		}

		/// <summary>
		/// Fires when a pen-width changes in Settings.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="key"></param>
		/// <param name="val"></param>
		private void OnPenWidthChanged(object sender, string key, object val)
		{
			_topPens[key].Width = (int)val;
			Refresh();
		}

		/// <summary>
		/// Gets the brushes/colors for the Floor and Content blobs.
		/// Used by the Help screen.
		/// </summary>
		/// <returns>a hashtable of the brushes</returns>
		internal Dictionary<string, SolidBrush> GetFloorContentBrushes()
		{
			return _topBrushes;
		}

		/// <summary>
		/// Gets the pens/colors for the Westwall and Northwall blobs.
		/// Used by the Help screen.
		/// </summary>
		/// <returns>a hashtable of the brushes</returns>
		internal Dictionary<string, Pen> GetWallPens()
		{
			return _topPens;
		}

//		/// <summary>
//		/// Fires when the minimum diamond-height changes in Settings.
//		/// </summary>
//		/// <param name="sender"></param>
//		/// <param name="keyword"></param>
//		/// <param name="val"></param>
//		private void OnDiamondHeight(object sender, string keyword, object val)
//		{
//			_topViewPanel.TileLozengeHeight = (int)val;
//		}
		#endregion
	}
}

/*		/// <summary>
		/// Loads the VisibleQuadrants flags for the MenuItem-toggles.
		/// </summary>
		/// <param name="e"></param>
		protected override void OnExtraRegistrySettingsLoad(DSShared.Windows.RegistryEventArgs e)
		{
			switch (e.Key)
			{
				case "vis0":
					_topViewPanel.Ground.Checked = e.Value;
					break;
				case "vis1":
					_topViewPanel.West.Checked = e.Value;
					break;
				case "vis2":
					_topViewPanel.North.Checked = e.Value;
					break;
				case "vis3":
					_topViewPanel.Content.Checked = e.Value;
					break;
			}

//			QuadrantsPanel.Height = 74;

//			var regkey = e.OpenRegistryKey;
//			foreach (var it in _visQuadsDictionary.Keys)
//				it.Checked = Boolean.Parse((string)regkey.GetValue("vis" + _visQuadsDictionary[it], "true"));
		} */

/*		/// <summary>
		/// Saves the VisibleQuadrants flags for the menu.
		/// </summary>
		/// <param name="e"></param>
		protected override void OnExtraRegistrySettingsSave(DSShared.Windows.RegistryEventArgs e)
		{
//			var regkey = e.OpenRegistryKey;
//
//			foreach (var it in _visQuadsDictionary.Keys)
//				regkey.SetValue("vis" + _visQuadsDictionary[it], it.Checked);
		} */
