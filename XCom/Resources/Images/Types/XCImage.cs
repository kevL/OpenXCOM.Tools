using System;
using System.Drawing;


namespace XCom.Interfaces
{
	/// <summary>
	/// A container for storing data about an XCOM image.
	/// </summary>
	public class XCImage
	{
		#region Fields (static)
		public const  int SpriteWidth32  = 32;	// for MapView, so I don't have to recode a bunch of crap there.
		public const  int SpriteHeight40 = 40;	// for MapView, so I don't have to recode a bunch of crap there.

		public static int SpriteWidth    = 32;
		public static int SpriteHeight   = 40;	// terrain & units 40px / bigobs 48px / scang 4px
		#endregion Fields (static)				// NOTE: Bigobs and ScanG shall be supported only by PckView.


		#region Properties
		public byte[] Bindata
		{ get; protected set; }

		public int Id
		{ get; set; }

		public Bitmap Sprite
		{ get; set; }

		public Bitmap SpriteGr
		{ get; protected set; }

		private Palette _palette;
		public Palette Pal
		{
			get { return _palette; }
			set
			{
				_palette = value;

				if (Sprite != null)
					Sprite.Palette = _palette.ColorTable;
			}
		}
		#endregion Properties


		#region cTors
		/// <summary>
		/// cTor[1]. Creates an XCImage.
		/// @note Binary data must not be compressed.
		/// </summary>
		/// <param name="bindata">the uncompressed source data</param>
		/// <param name="width"></param>
		/// <param name="height"></param>
		/// <param name="pal">pass in null to *bypass* creating the 'Image'; ie,
		/// the PckImage..cTor has already unravelled the compressed image-data
		/// instead</param>
		/// <param name="id"></param>
		internal XCImage(
				byte[] bindata,
				int width,
				int height,
				Palette pal,
				int id)
		{
			Id = id;

			Bindata = bindata;
			Pal     = pal;

			if (Pal != null)											// NOTE: this is to check for a call by BitmapService.CreateSprite()
				Sprite = BitmapService.CreateColorized(					// which is called by
													width,				// BitmapService.CreateSpriteset() and
													height,				// several PckViewForm contextmenu events
													Bindata,			// BUT: the call by PckImage..cTor initializer needs to decode
													Pal.ColorTable);	// the file-data first, then it creates its own 'Image'.
		}																// that's why i prefer pizza.

		/// <summary>
		/// cTor[2]. For clone. See PckImage..cTor[2] and .Clone().
		/// </summary>
		protected XCImage()
		{}
		#endregion cTors
	}
}
