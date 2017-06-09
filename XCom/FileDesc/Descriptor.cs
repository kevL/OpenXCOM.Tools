using System;
using System.Collections.Generic;
using System.IO;

using XCom.Resources.Map;


namespace XCom
{
	/// <summary>
	/// Descriptors describe a tileset: a Map, its route-nodes, and terrain. It
	/// also holds the path to its files' parent directory.
	/// A descriptor is accessed *only* through a Group and Category, and is
	/// identified by its tileset-label. This allows multiple tilesets (ie. with
	/// the same label) to be configured differently according to Category and
	/// Group.
	/// </summary>
	public sealed class Descriptor // *snap*
	{
		public const string PathTerrain = "TERRAIN";

		#region Properties
		public string Label
		{ get; private set; }

		public string BasePath
		{ get; private set; }

		public List<string> Terrains
		{ get; set; }

		public Palette Pal
		{ get; private set; }
		#endregion


		#region cTor
		/// <summary>
		/// cTor.
		/// </summary>
		/// <param name="tileset"></param>
		/// <param name="terrains"></param>
		/// <param name="basepath"></param>
		/// <param name="palette"></param>
		public Descriptor(
				string tileset,
				List<string> terrains,
				string basepath,
				Palette palette)
		{
			//LogFile.WriteLine("Descriptor cTor tileset= " + tileset);
			//LogFile.WriteLine("");

			Label    = tileset;
			Terrains = terrains;
			BasePath = basepath;
			Pal      = palette;
		}
		#endregion


		#region Methods
		/// <summary>
		/// Gets the MCD-records for a given terrain in this Descriptor.
		/// </summary>
		/// <returns></returns>
		public McdRecordCollection GetTerrainRecords(string terrain)
		{
//			string dirTerrain = Path.Combine(BasePath, PathTerrain);
			string dirTerrain = (Pal == Palette.UfoBattle) ? SharedSpace.ResourceDirectoryUfo
														   : SharedSpace.ResourceDirectoryTftd;
			dirTerrain = Path.Combine(SharedSpace.Instance.GetShare(dirTerrain), PathTerrain);

			var tiles = XCTileFactory.CreateRecords(
												terrain,
												dirTerrain,
												GetTerrainSpriteset(terrain));
			return new McdRecordCollection(tiles);
		}

		/// <summary>
		/// Gets the spriteset for a given terrain in this Descriptor.
		/// </summary>
		/// <returns></returns>
		public SpriteCollection GetTerrainSpriteset(string terrain)
		{
//			string dirTerrain = Path.Combine(BasePath, PathTerrain);
			string dirTerrain = (Pal == Palette.UfoBattle) ? SharedSpace.ResourceDirectoryUfo
														   : SharedSpace.ResourceDirectoryTftd;
			dirTerrain = Path.Combine(SharedSpace.Instance.GetShare(dirTerrain), PathTerrain);

			return ResourceInfo.LoadSpriteset(terrain, dirTerrain, 2, Pal); // TODO: Should the '2' be '4' for TFTD ...
		}

		/// <summary>
		/// Overrides Object.ToString()
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return Label;
		}
		#endregion
	}
}