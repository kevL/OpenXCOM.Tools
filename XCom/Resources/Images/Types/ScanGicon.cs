﻿using System;

using XCom.Interfaces;


namespace XCom
{
	internal sealed class ScanGicon
		:
			XCImage
	{
		#region Fields
		private readonly SpriteCollection _spriteset; // currently used only for ToString() override.
		#endregion


		#region cTor
		/// <summary>
		/// Instantiates a ScanG icon, based on an XCImage.
		/// </summary>
		/// <param name="bindata">the ScanG.Dat source data</param>
		/// <param name="spriteset">the sprite-collection this icon belongs to</param>
		internal ScanGicon(
				byte[] bindata,
				SpriteCollection spriteset)
			:
				base(
					bindata,
					XCImage.SpriteWidth,
					XCImage.SpriteHeight,
					null, // do *not* pass 'pal' in here. See XCImage..cTor
					-1)
		{
			_spriteset = spriteset; // for ToString() only.

			Pal = Palette.UfoBattle; // default: icons have no integral palette.


			Sprite = BitmapService.CreateColorized(
												XCImage.SpriteWidth,
												XCImage.SpriteHeight,
												Bindata,
												Pal.ColorTable);
		}
		#endregion
	}
}