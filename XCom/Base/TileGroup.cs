using System;


namespace XCom.Base
{
	#region Enumerations
	public enum GameType
	{
		Ufo,
		Tftd
	}
	// TODO: Replace all the if(Palette==Ufo.Palette) stuff with GameType.
	#endregion Enumerations



	public class TileGroup
		:
			TileGroupBase
	{
		#region Properties
		public GameType GroupType // TODO: 'GroupType' can/should be superceded by 'Pal' - or vice versa ...
		{ get; private set; }

		public Palette Pal
		{ get; set; }
		#endregion Properties


		#region cTor
		/// <summary>
		/// cTor. Load from YAML.
		/// </summary>
		internal TileGroup(string labelGroup)
			:
				base(labelGroup)
		{
			if (labelGroup.StartsWith("tftd", StringComparison.OrdinalIgnoreCase))
			{
				GroupType = GameType.Tftd;
			}
			else //if (labelGroup.StartsWith("ufo", StringComparison.OrdinalIgnoreCase))
			{
				GroupType = GameType.Ufo;	// NOTE: if the prefix "tftd" is not found at the beginning of
			}								// the group-label then default to UFO basepath and palette.

			switch (GroupType)
			{
				case GameType.Ufo:  Pal = Palette.UfoBattle;  break;
				case GameType.Tftd: Pal = Palette.TftdBattle; break;
			}
			// custom Palette = Palette.GetPalette(val)
		}
		#endregion cTor
	}
}