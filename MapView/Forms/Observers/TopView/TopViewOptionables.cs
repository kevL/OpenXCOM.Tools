﻿using System;
using System.ComponentModel;
using System.Drawing;

using MapView.Forms.MainView;


namespace MapView.Forms.Observers
{
	/// <summary>
	/// Properties for <see cref="TopView"/> that appear in TopView's Options.
	/// </summary>
	internal sealed class TopViewOptionables
	{
		internal static void DisposeOptionables()
		{
			DSShared.Logfile.Log("TopViewOptionables.DisposeOptionables() static");
			foreach (var pair in TopControl.TopPens)
				pair.Value.Dispose();

			foreach (var pair in TopControl.TopBrushes)
				pair.Value.Dispose();

			TopControl.ToolWest   .Dispose();
			TopControl.ToolNorth  .Dispose();
			TopControl.ToolContent.Dispose();

			QuadrantDrawService.Brush.Dispose();
		}


		#region Properties (optionable)
		// NOTE: The Properties are public for Reflection.

//		[DisplayName(...)]
//		[Description(...)]
//		[Category(...)]
//		[TypeConverter(...)]
//		[ReadOnly(...)]
//		[Browsable(...)]
//		[DefaultValue(...)]
//		[Editor(...)]

		private const string cat_Grid = "Grid";

		internal const string str_GridLineColor = "GridLineColor";
		private static Color  def_GridLineColor = Color.Black;

		private Color _gridLineColor = def_GridLineColor;
		[Category(cat_Grid)]
		[Description("Color of the grid lines (default Black)")]
		[DefaultValue(typeof(Color), "Black")]
		public Color GridLineColor
		{
			get { return _gridLineColor; }
			set { _gridLineColor = value; }
		}

		private const string str_GridLineWidth = "GridLineWidth";
		private const int    def_GridLineWidth = 1;

		private int _gridLineWidth = def_GridLineWidth;
		[Category(cat_Grid)]
		[Description("Width of the grid lines in pixels (1..5 default 1)")]
		[DefaultValue(def_GridLineWidth)]
		public int GridLineWidth
		{
			get { return _gridLineWidth; }
			set
			{
				if ((TopView._foptions as OptionsForm) == null) // on load
				{
					TopView.Options[str_GridLineWidth].Value =
					_gridLineWidth = value.Viceroy(1,5);
				}
				else if ((_gridLineWidth = value.Viceroy(1,5)) != value) // on user-changed
				{
					TopView.Options[str_GridLineWidth].Value = _gridLineWidth;
				}
			}
		}


		internal const string str_GridLine10Color = "GridLine10Color";
		private static Color  def_GridLine10Color = Color.Black;

		private Color _gridLine10Color = def_GridLine10Color;
		[Category(cat_Grid)]
		[Description("Color of every tenth grid line (default Black)")]
		[DefaultValue(typeof(Color), "Black")]
		public Color GridLine10Color
		{
			get { return _gridLine10Color; }
			set { _gridLine10Color = value; }
		}

		private const string str_GridLine10Width = "GridLine10Width";
		private const int    def_GridLine10Width = 2;

		private int _gridLine10Width = def_GridLine10Width;
		[Category(cat_Grid)]
		[Description("Width of every tenth grid line in pixels (1..5 default 2)")]
		[DefaultValue(def_GridLine10Width)]
		public int GridLine10Width
		{
			get { return _gridLine10Width; }
			set
			{
				if ((TopView._foptions as OptionsForm) == null) // on load
				{
					TopView.Options[str_GridLine10Width].Value =
					_gridLine10Width = value.Viceroy(1,5);
				}
				else if ((_gridLine10Width = value.Viceroy(1,5)) != value) // on user-changed
				{
					TopView.Options[str_GridLine10Width].Value = _gridLine10Width;
				}
			}
		}



		private const string cat_Blobs = "Blobs";

		internal const string str_FloorColor = "FloorColor"; // The key to the Option shall be identical to its Property label.
		private static Color  def_FloorColor = Color.BurlyWood;

		private Color _floorColor = def_FloorColor;
		[Category(cat_Blobs)]
		[Description("Color of the floor indicator (default BurlyWood)")]
		[DefaultValue(typeof(Color), "BurlyWood")]
		public Color FloorColor
		{
			get { return _floorColor; }
			set { _floorColor = value; }
		}


		internal const string str_WestColor = "WestColor";
		private static Color  def_WestColor = Color.Khaki;

		private Color _westColor = def_WestColor;
		[Category(cat_Blobs)]
		[Description("Color of the westwall indicator (default Khaki)")]
		[DefaultValue(typeof(Color), "Khaki")]
		public Color WestColor
		{
			get { return _westColor; }
			set { _westColor = value; }
		}

		private const string str_WestWidth = "WestWidth";
		private const int    def_WestWidth = 3;

		private int _westWidth = def_WestWidth;
		[Category(cat_Blobs)]
		[Description("Width of the westwall indicator in pixels (1..10 default 3)")]
		[DefaultValue(def_WestWidth)]
		public int WestWidth
		{
			get { return _westWidth; }
			set
			{
				if ((TopView._foptions as OptionsForm) == null) // on load
				{
					TopView.Options[str_WestWidth].Value =
					_westWidth = value.Viceroy(1,10);
				}
				else if ((_westWidth = value.Viceroy(1,10)) != value) // on user-changed
				{
					TopView.Options[str_WestWidth].Value = _westWidth;
				}
			}
		}


		internal const string str_NorthColor = "NorthColor";
		private static Color  def_NorthColor = Color.Khaki;

		private Color _northColor = def_NorthColor;
		[Category(cat_Blobs)]
		[Description("Color of the northwall indicator (default Khaki)")]
		[DefaultValue(typeof(Color), "Khaki")]
		public Color NorthColor
		{
			get { return _northColor; }
			set { _northColor = value; }
		}

		private const string str_NorthWidth = "NorthWidth";
		private const int    def_NorthWidth = 3;

		private int _northWidth = def_NorthWidth;
		[Category(cat_Blobs)]
		[Description("Width of the northwall indicator in pixels (1..10 default 3)")]
		[DefaultValue(def_NorthWidth)]
		public int NorthWidth
		{
			get { return _northWidth; }
			set
			{
				if ((TopView._foptions as OptionsForm) == null) // on load
				{
					TopView.Options[str_NorthWidth].Value =
					_northWidth = value.Viceroy(1,10);
				}
				else if ((_northWidth = value.Viceroy(1,10)) != value) // on user-changed
				{
					TopView.Options[str_NorthWidth].Value = _northWidth;
				}
			}
		}


		internal const string str_ContentColor = "ContentColor";
		private static Color  def_ContentColor = Color.MediumSeaGreen;

		private Color _contentColor = def_ContentColor;
		[Category(cat_Blobs)]
		[Description("Color of the content indicator (default MediumSeaGreen)")]
		[DefaultValue(typeof(Color), "MediumSeaGreen")]
		public Color ContentColor
		{
			get { return _contentColor; }
			set { _contentColor = value; }
		}



		private const string cat_Selects = "Selects";

		internal const string str_SelectorColor = "SelectorColor";
		private static Color  def_SelectorColor = Color.Black;

		private Color _selectorColor = def_SelectorColor;
		[Category(cat_Selects)]
		[Description("Color of the tile selector (default Black)")]
		[DefaultValue(typeof(Color), "Black")]
		public Color SelectorColor
		{
			get { return _selectorColor; }
			set { _selectorColor = value; }
		}

		private const string str_SelectorWidth = "SelectorWidth";
		private const int    def_SelectorWidth = 2;

		private int _selectorWidth = def_SelectorWidth;
		[Category(cat_Selects)]
		[Description("Width of the tile selector in pixels (1..5 default 2)")]
		[DefaultValue(def_SelectorWidth)]
		public int SelectorWidth
		{
			get { return _selectorWidth; }
			set
			{
				if ((TopView._foptions as OptionsForm) == null) // on load
				{
					TopView.Options[str_SelectorWidth].Value =
					_selectorWidth = value.Viceroy(1,5);
				}
				else if ((_selectorWidth = value.Viceroy(1,5)) != value) // on user-changed
				{
					TopView.Options[str_SelectorWidth].Value = _selectorWidth;
				}
			}
		}


		internal const string str_SelectedColor = "SelectedColor";
		private static Color  def_SelectedColor = Color.RoyalBlue;

		private Color _selectedColor = def_SelectedColor;
		[Category(cat_Selects)]
		[Description("Color of the selection border (default RoyalBlue)")]
		[DefaultValue(typeof(Color), "RoyalBlue")]
		public Color SelectedColor
		{
			get { return _selectedColor; }
			set { _selectedColor = value; }
		}

		private const string str_SelectedWidth = "SelectedWidth";
		private const int    def_SelectedWidth = 2;

		private int _selectedWidth = def_SelectedWidth;
		[Category(cat_Selects)]
		[Description("Width of the selection border in pixels (1..5 default 2)")]
		[DefaultValue(def_SelectedWidth)]
		public int SelectedWidth
		{
			get { return _selectedWidth; }
			set
			{
				if ((TopView._foptions as OptionsForm) == null) // on load
				{
					TopView.Options[str_SelectedWidth].Value =
					_selectedWidth = value.Viceroy(1,5);
				}
				else if ((_selectedWidth = value.Viceroy(1,5)) != value) // on user-changed
				{
					TopView.Options[str_SelectedWidth].Value = _selectedWidth;
				}
			}
		}


		private const string str_SelectedQuadColor = "SelectedQuadColor";
		private static Color def_SelectedQuadColor = Color.PowderBlue;

		private Color _selectedQuadColor = def_SelectedQuadColor;
		[Category(cat_Selects)]
		[Description("Background color of the selected parttype (default PowderBlue)")]
		[DefaultValue(typeof(Color), "PowderBlue")]
		public Color SelectedQuadColor
		{
			get { return _selectedQuadColor; }
			set { _selectedQuadColor = value; }
		}



		private const string cat_General = "General";

		private const string str_EnableRightClickWaitTimer = "EnableRightClickWaitTimer";
		private const bool   def_EnableRightClickWaitTimer = false;

		private bool _enableWaitTimer = def_EnableRightClickWaitTimer;
		[Category(cat_General)]
		[Description("If true then right-clicking or double right-clicking on"
			+ " either a tile or a quadrant slot causes a very short delay"
			+ " in order to detect whether a single or double click happened."
			+ " This delay is used to bypass a single-click event [place"
			+ " tilepart] if a double-click [clear tilepart] is detected"
			+ " instead. If this option is false then a double right-click"
			+ " causes a tilepart to be placed and cleared in rapid succession."
			+ " WARNING: Enabling this option could cause TopView to exhibit"
			+ " unstable behavior (default False)")]
		[DefaultValue(def_EnableRightClickWaitTimer)]
		public bool EnableRightClickWaitTimer
		{
			get { return _enableWaitTimer; }
			set { _enableWaitTimer = value; }
		}



		private const string cat_nonBrowsable = "nonBrowsable";

		private const string str_DescriptionHeight = "DescriptionHeight";
		private const int    def_DescriptionHeight = 82; // header(22) + 5 line(12)

		private int _descriptionHeight = def_DescriptionHeight;
		[Browsable(false)]
		[Category(cat_nonBrowsable)]
		[Description("The height of the Description area at the bottom of Options")]
		[DefaultValue(def_DescriptionHeight)]
		public int DescriptionHeight
		{
			get { return _descriptionHeight; }
			set
			{
				TopView.Options[str_DescriptionHeight].Value =
				_descriptionHeight = value;
			}
		}
		#endregion Properties (optionable)


		#region Methods
		/// <summary>
		/// Instantiates pens, brushes, and colortools used by TopView's draw
		/// routines with default values. Adds default keyval pairs to
		/// TopView's optionables and an option-changer is assigned to each.
		/// The default values were assigned to TopView's optionable properties
		/// when those properties were instantiated above.
		/// </summary>
		/// <param name="options"><c><see cref="TopView.Options">TopView.Options</see></c></param>
		internal void LoadDefaults(Options options)
		{
			//DSShared.Logfile.Log("TopViewOptionables.LoadDefaults()");

			Pen pen; SolidBrush brush;

			pen = new Pen(def_GridLineColor, def_GridLineWidth);
			TopControl.TopPens.Add(str_GridLineColor, pen);

			pen = new Pen(def_GridLine10Color, def_GridLine10Width);
			TopControl.TopPens.Add(str_GridLine10Color, pen);

			TopControl.TopBrushes.Add(str_FloorColor, new SolidBrush(def_FloorColor));

			pen = new Pen(def_WestColor, def_WestWidth);
			TopControl.TopPens.Add(str_WestColor, pen);
			TopControl.ToolWest = new BlobColorTool(pen, "TopToolWest");

			pen = new Pen(def_NorthColor, def_NorthWidth);
			TopControl.TopPens.Add(str_NorthColor, pen);
			TopControl.ToolNorth = new BlobColorTool(pen, "TopToolNorth");

			brush = new SolidBrush(def_ContentColor);
			TopControl.TopBrushes.Add(str_ContentColor, brush);
			TopControl.ToolContent = new BlobColorTool(brush, BlobDrawService.LINEWIDTH_CONTENT, "TopToolContent");

			pen = new Pen(def_SelectorColor, def_SelectorWidth);
			TopControl.TopPens.Add(str_SelectorColor, pen);

			pen = new Pen(def_SelectedColor, def_SelectedWidth);
			TopControl.TopPens.Add(str_SelectedColor, pen);

			QuadrantDrawService.Brush = new SolidBrush(def_SelectedQuadColor);


			OptionChangedEvent changer0 = OnOptionChanged;
			OptionChangedEvent changer1 = OnQuadColorChanged;
			OptionChangedEvent changer2 = OnFlagChanged;
			OptionChangedEvent changer3 = OnDescriptionHeightChanged;

			options.CreateOptionDefault(str_GridLineColor,             def_GridLineColor,             changer0);
			options.CreateOptionDefault(str_GridLineWidth,             def_GridLineWidth,             changer0);
			options.CreateOptionDefault(str_GridLine10Color,           def_GridLine10Color,           changer0);
			options.CreateOptionDefault(str_GridLine10Width,           def_GridLine10Width,           changer0);

			options.CreateOptionDefault(str_FloorColor,                def_FloorColor,                changer0);
			options.CreateOptionDefault(str_WestColor,                 def_WestColor,                 changer0);
			options.CreateOptionDefault(str_WestWidth,                 def_WestWidth,                 changer0);
			options.CreateOptionDefault(str_NorthColor,                def_NorthColor,                changer0);
			options.CreateOptionDefault(str_NorthWidth,                def_NorthWidth,                changer0);
			options.CreateOptionDefault(str_ContentColor,              def_ContentColor,              changer0);

			options.CreateOptionDefault(str_SelectorColor,             def_SelectorColor,             changer0);
			options.CreateOptionDefault(str_SelectorWidth,             def_SelectorWidth,             changer0);
			options.CreateOptionDefault(str_SelectedColor,             def_SelectedColor,             changer0);
			options.CreateOptionDefault(str_SelectedWidth,             def_SelectedWidth,             changer0);
			options.CreateOptionDefault(str_SelectedQuadColor,         def_SelectedQuadColor,         changer1);

			options.CreateOptionDefault(str_EnableRightClickWaitTimer, def_EnableRightClickWaitTimer, changer2);

			options.CreateOptionDefault(str_DescriptionHeight,         def_DescriptionHeight,         changer3);
		}
		#endregion Methods


		#region Events
		/// <summary>
		/// Sets the value of an optionable property and invalidates the TopView
		/// and TopRouteView(Top) controls.
		/// </summary>
		/// <param name="key">one of the standard keys of an optionable</param>
		/// <param name="val">the value to set it to</param>
		private void OnOptionChanged(string key, object val)
		{
			bool invalidateQuads = false;

			switch (key)
			{
				case str_FloorColor:      FloorColor      = (Color)val; ChangeBruColor(key, val); invalidateQuads = true; break;
				case str_WestColor:       WestColor       = (Color)val; ChangePenColor(key, val); invalidateQuads = true; break;
				case str_WestWidth:       WestWidth       =   (int)val; ChangePenWidth(key, val); break;
				case str_NorthColor:      NorthColor      = (Color)val; ChangePenColor(key, val); invalidateQuads = true; break;
				case str_NorthWidth:      NorthWidth      =   (int)val; ChangePenWidth(key, val); break;
				case str_ContentColor:    ContentColor    = (Color)val; ChangeBruColor(key, val); invalidateQuads = true; break;
				case str_SelectorColor:   SelectorColor   = (Color)val; ChangePenColor(key, val); break;
				case str_SelectorWidth:   SelectorWidth   =   (int)val; ChangePenWidth(key, val); break;
				case str_SelectedColor:   SelectedColor   = (Color)val; ChangePenColor(key, val); break;
				case str_SelectedWidth:   SelectedWidth   =   (int)val; ChangePenWidth(key, val); break;
				case str_GridLineColor:   GridLineColor   = (Color)val; ChangePenColor(key, val); break;
				case str_GridLineWidth:   GridLineWidth   =   (int)val; ChangePenWidth(key, val); break;
				case str_GridLine10Color: GridLine10Color = (Color)val; ChangePenColor(key, val); break;
				case str_GridLine10Width: GridLine10Width =   (int)val; ChangePenWidth(key, val); break;
			}

			ObserverManager.InvalidateTopControls();

			if (invalidateQuads)
				ObserverManager.InvalidateQuadrantControls();
		}

		/// <summary>
		/// Sets the value of SelectedQuadColor and invalidates the TopView
		/// and TopRouteView(Top) controls.
		/// </summary>
		/// <param name="key">one of the standard keys of an optionable</param>
		/// <param name="val">the value to set it to</param>
		private void OnQuadColorChanged(string key, object val)
		{
			SelectedQuadColor =
			QuadrantDrawService.Brush.Color = (Color)val;

			ObserverManager.InvalidateQuadrantControls();
		}

		/// <summary>
		/// Sets the value of an optionable property but doesn't invalidate
		/// anything.
		/// </summary>
		/// <param name="key">one of the standard keys of an optionable</param>
		/// <param name="val">the value to set it to</param>
		private void OnFlagChanged(string key, object val)
		{
			EnableRightClickWaitTimer = (bool)val;
		}

		/// <summary>
		/// Stores the property panel's Description area's height when the user
		/// changes it.
		/// </summary>
		/// <param name="key"></param>
		/// <param name="val"></param>
		private void OnDescriptionHeightChanged(string key, object val)
		{
			DescriptionHeight = (int)val;
		}
		#endregion Events


		#region Methods (static)
		/// <summary>
		/// Fires when a brush-color changes in Options.
		/// </summary>
		/// <param name="key"></param>
		/// <param name="val"></param>
		private static void ChangeBruColor(string key, object val)
		{
			TopControl.TopBrushes[key].Color = (Color)val;

			if (key == str_ContentColor)
			{
				TopControl.ToolContent.Dispose();
				TopControl.ToolContent = new BlobColorTool(
														TopControl.TopBrushes[key],
														BlobDrawService.LINEWIDTH_CONTENT,
														"TopToolContent");
			}

			if (MainViewF.that._fcolors != null)
				MainViewF.that._fcolors.UpdateTopViewBlobColors();
		}

		/// <summary>
		/// Fires when a pen-color changes in Options.
		/// </summary>
		/// <param name="key"></param>
		/// <param name="val"></param>
		private static void ChangePenColor(string key, object val)
		{
			TopControl.TopPens[key].Color = (Color)val;

			bool updateColorhelp = false;

			switch (key)
			{
				case str_WestColor:
					TopControl.ToolWest.Dispose();
					TopControl.ToolWest = new BlobColorTool(TopControl.TopPens[key], "TopToolWest");
					updateColorhelp = true;
					break;

				case str_NorthColor:
					TopControl.ToolNorth.Dispose();
					TopControl.ToolNorth = new BlobColorTool(TopControl.TopPens[key], "TopToolNorth");
					updateColorhelp = true;
					break;
			}

			if (updateColorhelp && MainViewF.that._fcolors != null)
				MainViewF.that._fcolors.UpdateTopViewBlobColors();
		}

		/// <summary>
		/// Fires when a pen-width changes in Options.
		/// </summary>
		/// <param name="key"></param>
		/// <param name="val"></param>
		private static void ChangePenWidth(string key, object val)
		{
			TopControl.TopPens[key = WidthToColor(key)].Width = (int)val;

			switch (key)
			{
				case str_WestColor:
					TopControl.ToolWest.Dispose();
					TopControl.ToolWest = new BlobColorTool(TopControl.TopPens[key], "TopToolWest");
					break;

				case str_NorthColor:
					TopControl.ToolNorth.Dispose();
					TopControl.ToolNorth = new BlobColorTool(TopControl.TopPens[key], "TopToolNorth");
					break;
			}
		}

		/// <summary>
		/// Maps a pen's width-key to its corresponding color-key. Because
		/// TopPens are stored and accessed with their color-keys not their
		/// width-keys.
		/// </summary>
		/// <param name="width">width-key</param>
		/// <returns>the width-keys corresponding color-key</returns>
		private static string WidthToColor(string width)
		{
			switch (width)
			{
				case str_WestWidth:       return str_WestColor;
				case str_NorthWidth:      return str_NorthColor;
				case str_SelectorWidth:   return str_SelectorColor;
				case str_SelectedWidth:   return str_SelectedColor;
				case str_GridLineWidth:   return str_GridLineColor;
				case str_GridLine10Width: return str_GridLine10Color;
			}
			return null;
		}
		#endregion Methods (static)
	}
}
