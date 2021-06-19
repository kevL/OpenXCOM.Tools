﻿using System;
using System.ComponentModel;
using System.Drawing;

using MapView.Forms.MainView;
using MapView.ExternalProcess;

using XCom;


namespace MapView.Forms.Observers
{
	/// <summary>
	/// Properties for <c><see cref="TileView"/></c> that appear in TileView's
	/// <c><see cref="Options"/></c>.
	/// </summary>
	internal sealed class TileViewOptionables
	{
		internal static void DisposeOptionables()
		{
			DSShared.LogFile.WriteLine("TileViewOptionables.DisposeOptionables() static");
			foreach (var pair in TilePanel.SpecialBrushes)
				pair.Value.Dispose();
		}


		#region Fields
		private readonly TileView _tileView;
		#endregion Fields


		#region cTor
		/// <summary>
		/// cTor.
		/// </summary>
		/// <param name="tileView"></param>
		internal TileViewOptionables(TileView tileView)
		{
			_tileView = tileView;
		}
		#endregion cTor


		#region Fields (static)
		/// <summary>
		/// These are default colors for the SpecialProperty of a tilepart.
		/// TileView will load these colors when the app loads, then any colors
		/// of SpecialType that were customized will be set and accessed by
		/// TilePanel and/or the Help|Colorhelp dialog later.
		/// </summary>
		private static readonly Color[] def_SpecialColors =
		{							//      __UFO__			__TFTD__
			Color.NavajoWhite,		//  0 - Standard
			Color.Lavender,			//  1 - EntryPoint
			Color.IndianRed,		//  2 - PowerSource		IonBeamAccel
			Color.Turquoise,		//  3 - Navigation
			Color.Khaki,			//  4 - Construction
			Color.Snow,				//  5 - Food			Cryo
			Color.Aquamarine,		//  6 - Reproduction	Clon
			Color.DeepSkyBlue,		//  7 - Entertainment	LearnArrays
			Color.Thistle,			//  8 - Surgery			Implant
			Color.YellowGreen,		//  9 - Examination
			Color.Orchid,			// 10 - Alloys			Plastics
			Color.LightSteelBlue,	// 11 - Habitat			Re-anim
			Color.Aqua,				// 12 - Destroyed
			Color.BurlyWood,		// 13 - ExitPoint
			Color.LightCoral		// 14 - MustDestroy
		};
		#endregion Fields (static)


		#region Properties (optionable)
		// NOTE: The Properties are public for Reflection. These property-vars
		// shall be identified with labels that are identical to the constants
		// in the enum XCom.SpecialType.

//		[DisplayName(...)]
//		[Description(...)]
//		[Category(...)]
//		[TypeConverter(...)]
//		[ReadOnly(...)]
//		[Browsable(...)]
//		[DefaultValue(...)]
//		[Editor(...)]

		private const string cat_SpecialPropertyColors = "SpecialPropertyColors";

		private Color _colorStandard = def_SpecialColors[0];
		[Category(cat_SpecialPropertyColors)]
		[Description(@"Color of Standard parts
(default NavajoWhite)")]
		[DefaultValue(typeof(Color), "NavajoWhite")]
		public Color Standard
		{
			get { return _colorStandard; }
			set { _colorStandard = value; }
		}

		private Color _colorEntryPoint = def_SpecialColors[1];
		[Category(cat_SpecialPropertyColors)]
		[Description(@"Color of Entry Point parts
(default Lavender)")]
		[DefaultValue(typeof(Color), "Lavender")]
		public Color EntryPoint
		{
			get { return _colorEntryPoint; }
			set { _colorEntryPoint = value; }
		}

		private Color _colorPowerSource = def_SpecialColors[2];
		[Category(cat_SpecialPropertyColors)]
		[Description(@"Color of UFO Power Source parts
Color of TFTD Ion-beam Accelerators parts
(default IndianRed)")]
		[DefaultValue(typeof(Color), "IndianRed")]
		public Color PowerSource
		{
			get { return _colorPowerSource; }
			set { _colorPowerSource = value; }
		}

		private Color _colorNavigation = def_SpecialColors[3];
		[Category(cat_SpecialPropertyColors)]
		[Description(@"Color of UFO Navigation parts
Color of TFTD Magnetic Navigation parts
(default Turquoise)")]
		[DefaultValue(typeof(Color), "Turquoise")]
		public Color Navigation
		{
			get { return _colorNavigation; }
			set { _colorNavigation = value; }
		}

		private Color _colorConstruction = def_SpecialColors[4];
		[Category(cat_SpecialPropertyColors)]
		[Description(@"Color of UFO Construction parts
Color of TFTD Alien Sub Construction parts
(default Khaki)")]
		[DefaultValue(typeof(Color), "Khaki")]
		public Color Construction
		{
			get { return _colorConstruction; }
			set { _colorConstruction = value; }
		}

		private Color _colorFood = def_SpecialColors[5];
		[Category(cat_SpecialPropertyColors)]
		[Description(@"Color of UFO Alien Food parts
Color of TFTD Alien Cryogenics parts
(default Snow)")]
		[DefaultValue(typeof(Color), "Snow")]
		public Color Food
		{
			get { return _colorFood; }
			set { _colorFood = value; }
		}

		private Color _colorReproduction = def_SpecialColors[6];
		[Category(cat_SpecialPropertyColors)]
		[Description(@"Color of UFO Alien Reproduction parts
Color of TFTD Alien Cloning parts
(default Aquamarine)")]
		[DefaultValue(typeof(Color), "Aquamarine")]
		public Color Reproduction
		{
			get { return _colorReproduction; }
			set { _colorReproduction = value; }
		}

		private Color _colorEntertainment = def_SpecialColors[7];
		[Category(cat_SpecialPropertyColors)]
		[Description(@"Color of UFO Alien Entertainment parts
Color of TFTD Alien Learning Arrays parts
(default DeepSkyBlue)")]
		[DefaultValue(typeof(Color), "DeepSkyBlue")]
		public Color Entertainment
		{
			get { return _colorEntertainment; }
			set { _colorEntertainment = value; }
		}

		private Color _colorSurgery = def_SpecialColors[8];
		[Category(cat_SpecialPropertyColors)]
		[Description(@"Color of UFO Alien Surgery parts
Color of TFTD Alien Implanter parts
(default Thistle)")]
		[DefaultValue(typeof(Color), "Thistle")]
		public Color Surgery
		{
			get { return _colorSurgery; }
			set { _colorSurgery = value; }
		}

		private Color _colorExamination = def_SpecialColors[9];
		[Category(cat_SpecialPropertyColors)]
		[Description(@"Color of Examination Room parts
(default YellowGreen)")]
		[DefaultValue(typeof(Color), "YellowGreen")]
		public Color Examination
		{
			get { return _colorExamination; }
			set { _colorExamination = value; }
		}

		private Color _colorAlloys = def_SpecialColors[10];
		[Category(cat_SpecialPropertyColors)]
		[Description(@"Color of UFO Alien Alloys parts
Color of TFTD Aqua Plastics parts
(default Orchid)")]
		[DefaultValue(typeof(Color), "Orchid")]
		public Color Alloys
		{
			get { return _colorAlloys; }
			set { _colorAlloys = value; }
		}

		private Color _colorHabitat = def_SpecialColors[11];
		[Category(cat_SpecialPropertyColors)]
		[Description(@"Color of UFO Alien Habitat parts
Color of TFTD Alien Re-animation Zone parts
(default LightSteelBlue)")]
		[DefaultValue(typeof(Color), "LightSteelBlue")]
		public Color Habitat
		{
			get { return _colorHabitat; }
			set { _colorHabitat = value; }
		}

		private Color _colorDestroyed = def_SpecialColors[12];
		[Category(cat_SpecialPropertyColors)]
		[Description(@"Color of Destroyed Alloys/Plastics parts
(default Aqua)")]
		[DefaultValue(typeof(Color), "Aqua")]
		public Color Destroyed
		{
			get { return _colorDestroyed; }
			set { _colorDestroyed = value; }
		}

		private Color _colorExitPoint = def_SpecialColors[13];
		[Category(cat_SpecialPropertyColors)]
		[Description(@"Color of Exit Point parts
(default BurlyWood)")]
		[DefaultValue(typeof(Color), "BurlyWood")]
		public Color ExitPoint
		{
			get { return _colorExitPoint; }
			set { _colorExitPoint = value; }
		}

		private Color _colorMustDestroy = def_SpecialColors[14];
		[Category(cat_SpecialPropertyColors)]
		[Description(@"Color of Must Destroy parts
eg. UFO Alien Brain parts
eg. TFTD T'leth Power Cylinders parts
(default LightCoral)")]
		[DefaultValue(typeof(Color), "LightCoral")]
		public Color MustDestroy
		{
			get { return _colorMustDestroy; }
			set { _colorMustDestroy = value; }
		}


		private const string cat_External = "External";

		private string _process = String.Empty;
		[Category(cat_External)]
		[Description(@"Path to external process
The path specified can be used to start an application or to open a specified"
			+ " file with its associated application.")]
		[DefaultValue("")]
		public string ExternalProcess
		{
			get { return _process; }
			set { _process = value; }
		}



		private const string cat_nonBrowsable = "nonBrowsable";

		private const string str_DescriptionHeight = "DescriptionHeight";
		private const int    def_DescriptionHeight = 70; // header(22) + 4 line(12)

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
				ObserverManager.TileView.Control.Options[str_DescriptionHeight].Value =
				_descriptionHeight = value;
			}
		}
		#endregion Properties (optionable)


		#region Methods
		/// <summary>
		/// Instantiates brushes used by TileView's draw routines with default
		/// values. Adds default keyval pairs to TileView's optionables and an
		/// option-changer is assigned to each. The default values were assigned
		/// to TileView's optionable properties when those properties were
		/// instantiated above. Also prepares the Volutar service.
		/// </summary>
		/// <param name="options">TileView's options</param>
		internal void LoadDefaults(Options options)
		{
			//DSShared.LogFile.WriteLine("TileViewOptionables.LoadDefaults()");

			OptionChangedEvent changer = OnSpecialPropertyColorChanged;

			Color color;
			foreach (SpecialType key in Enum.GetValues(typeof(SpecialType)))
			{
				color = def_SpecialColors[(int)key];
				TilePanel.SpecialBrushes[key] = new SolidBrush(color);
				options.CreateOptionDefault(
										Enum.GetName(typeof(SpecialType), key),
										color,
										changer);
			}

			options.CreateOptionDefault(ExternalProcessService.PROCESS, String.Empty, OnExternalProcessChanged);

			options.CreateOptionDefault(str_DescriptionHeight, def_DescriptionHeight, OnDescriptionHeightChanged);
		}
		#endregion Methods


		#region Events
		/// <summary>
		/// Sets a color for a <see cref="TilePanel.SpecialBrushes">SpecialBrush</see>
		/// and invalidates the current <see cref="TileView"/> panel.
		/// </summary>
		/// <param name="key">one of the SpecialType keys</param>
		/// <param name="val">its color</param>
		private void OnSpecialPropertyColorChanged(string key, object val)
		{
			var color = (Color)val;

			var special = (SpecialType)Enum.Parse(typeof(SpecialType), key);
			switch (special)
			{
				case SpecialType.Standard:      Standard      = color; break;
				case SpecialType.EntryPoint:    EntryPoint    = color; break;
				case SpecialType.PowerSource:   PowerSource   = color; break;
				case SpecialType.Navigation:    Navigation    = color; break;
				case SpecialType.Construction:  Construction  = color; break;
				case SpecialType.Food:          Food          = color; break;
				case SpecialType.Reproduction:  Reproduction  = color; break;
				case SpecialType.Entertainment: Entertainment = color; break;
				case SpecialType.Surgery:       Surgery       = color; break;
				case SpecialType.Examination:   Examination   = color; break;
				case SpecialType.Alloys:        Alloys        = color; break;
				case SpecialType.Habitat:       Habitat       = color; break;
				case SpecialType.Destroyed:     Destroyed     = color; break;
				case SpecialType.ExitPoint:     ExitPoint     = color; break;
				case SpecialType.MustDestroy:   MustDestroy   = color; break;
			}

			TilePanel.SpecialBrushes[special].Color = color;
			_tileView.GetSelectedPanel().Invalidate();

			if (MainViewF.that._fcolors != null)
				MainViewF.that._fcolors.UpdateSpecialPropertyColors();
		}

		/// <summary>
		/// Volutar never changes.
		/// </summary>
		/// <param name="key"></param>
		/// <param name="val"></param>
		private void OnExternalProcessChanged(string key, object val)
		{
			ExternalProcess = (String)val;
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
	}
}
