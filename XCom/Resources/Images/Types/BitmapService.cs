using System;
//using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
//using System.IO;

using XCom.Interfaces;

// now why did I just *know* that the nutcase who writes the low-level code for
// image-handling was going to cram everything together ....
// And I'm the nutcase who just went through the whole thing adding whitespace.


namespace XCom
{
	/// <summary>
	/// Static methods for dealing with Bitmaps and sprites.
	/// </summary>
	public static class BitmapService
	{
		/// <summary>
		/// Ensures there aren't any StopBytes or TransparencyBytes in the
		/// returned XCImage data.
		/// Helper for CreateSheetSprites().
		/// Also called by PckViewForm's contextmenu:
		/// - OnAddSpritesClick()
		/// - InsertSprites()
		/// - OnReplaceSpriteClick()
		/// </summary>
		/// <param name="b">a 32x40 indexed Bitmap</param>
		/// <param name="id">an appropriate set-id</param>
		/// <param name="pal">an XCOM Palette-object</param>
		/// <param name="width">the width of the image</param>
		/// <param name="height">the height of the image</param>
		/// <param name="isScanG">true if creating a ScanG icon</param>
		/// <param name="x">used by spritesheets only</param>
		/// <param name="y">used by spritesheets only</param>
		/// <returns>an XCImage-object (base of PckImage)</returns>
		public static XCImage CreateSprite(
				Bitmap b,
				int id,
				Palette pal,
				int width,
				int height,
				bool isScanG = false,
				int x = 0,
				int y = 0)
		{
			var bindata = new byte[width * height]; // image data in uncompressed 8-bpp (color-indexed) format

			var locked = b.LockBits(
								new Rectangle(x, y, width, height),
								ImageLockMode.ReadOnly,
								PixelFormat.Format8bppIndexed);
			var start = locked.Scan0;

			unsafe // change any palette-indices 0xFF or 0xFE to 0xFD if *not* a ScanG icon ->
			{
				// kL_note: I suspect any of this negative-stride stuff is redundant.

				byte* pos;
				if (locked.Stride > 0)
					pos = (byte*)start.ToPointer();
				else
					pos = (byte*)start.ToPointer() + locked.Stride * (b.Height - 1);

				uint stride = (uint)Math.Abs(locked.Stride);

				int i = -1;
				for (uint row = 0; row != height; ++row)
				for (uint col = 0; col != width;  ++col)
				{
					byte palid = *(pos + row * stride + col);

					if (!isScanG)
					{
						switch (palid)
						{
							case PckImage.MarkerRle:	// #254
							case PckImage.MarkerEos:	// #255
								palid = PckImage.MxId;	// #253
								break;
						}
					}
					bindata[++i] = palid;
				}
			}
			b.UnlockBits(locked);

			return new XCImage(bindata, width, height, pal, id); // note: XCImage..cTor calls CreateColorized() below.
		}

		/// <summary>
		/// Called by PckViewForm.OnImportSpritesheetClick()
		/// </summary>
		/// <param name="b">an indexed Bitmap of a spritesheet</param>
		/// <param name="pal">an XCOM Palette-object</param>
		/// <param name="width">the width of a sprite in the collection</param>
		/// <param name="height">the height of a sprite in the collection</param>
		/// <param name="isScanG">true if creating a ScanG iconset</param>
		/// <param name="pad">padding between sprites</param>
		/// <returns>a spriteset, the entries of which will be repurposed to
		/// another spriteset</returns>
		public static SpriteCollection CreateSpriteCollection(
				Bitmap b,
				Palette pal,
				int width,
				int height,
				bool isScanG,
				int pad = 0)
		{
			var spriteset = new SpriteCollection(String.Empty, pal);

			int cols = (b.Width  + pad) / (width  + pad);
			int rows = (b.Height + pad) / (height + pad);

			int id = -1;

			for (int i = 0; i != cols * rows; ++i)
			{
				int x = (i % cols) * (width  + pad);
				int y = (i / cols) * (height + pad);
				spriteset.Sprites.Add(CreateSprite(
												b,
												++id,
												pal,
												width, height,
												isScanG,
												x, y));
			}
			return spriteset;
		}


		/// <summary>
		/// Saves a sprite after setting colorid #0 transparent.
		/// </summary>
		/// <param name="fullpath"></param>
		/// <param name="b"></param>
		public static void ExportSprite(string fullpath, Bitmap b)
		{
			ColorPalette pal = b.Palette;
			pal.Entries[Palette.TranId] = Color.Transparent;
			b.Palette = pal;

			b.Save(fullpath, ImageFormat.Png);
		}

		/// <summary>
		/// Saves a spriteset as a PNG spritesheet.
		/// @note Check that spriteset is not null or blank before call.
		/// @note DO NOT PASS IN 0 COLS idiot.
		/// </summary>
		/// <param name="fullpath">fullpath of the output file</param>
		/// <param name="spriteset">spriteset</param>
		/// <param name="pal">palette</param>
		/// <param name="cols">quantity of cols</param>
		/// <param name="pad">padding between sprites in the spritesheet</param>
		public static void ExportSpritesheet(
				string fullpath,
				SpriteCollection spriteset,
				Palette pal,
				int cols,
				int pad = 0)
		{
			if (spriteset.Count < cols)
				cols = spriteset.Count;

			using (var b = CreateTransparent(
										cols * (XCImage.SpriteWidth + pad) - pad,
										((spriteset.Count + (cols - 1)) / cols) * (XCImage.SpriteHeight + pad) - pad,
										pal.ColorTable))
			{
				for (int i = 0; i != spriteset.Count; ++i)
				{
					int x = i % cols * (XCImage.SpriteWidth  + pad);
					int y = i / cols * (XCImage.SpriteHeight + pad);

					Insert(spriteset[i].Sprite, b, x, y);
				}
				ExportSprite(fullpath, b);
			}
		}


		/// <summary>
		/// Creates an 8-bit indexed bitmap from the specified byte-array.
		/// </summary>
		/// <param name="width">width of final Bitmap</param>
		/// <param name="height">height of final Bitmap</param>
		/// <param name="bindata">image data</param>
		/// <param name="pal">palette to color the image with</param>
		/// <returns></returns>
		public static Bitmap CreateColorized(
				int width,
				int height,
				byte[] bindata,
				ColorPalette pal)
		{
			var b = new Bitmap(
							width, height,
							PixelFormat.Format8bppIndexed);

			var locked = b.LockBits(
								new Rectangle(0,0, width, height),
								ImageLockMode.WriteOnly,
								PixelFormat.Format8bppIndexed);

			// Write to the temporary buffer that is provided by LockBits().
			// Copy the pixels from the source image in this loop.
			// Because you want an index, convert RGB to the appropriate
			// palette index here.
			var start = locked.Scan0;

			unsafe
			{
				byte* pos;
				if (locked.Stride > 0)
				{
					pos = (byte*)start.ToPointer();
				}
				else
				{
					// If the stride is negative, Scan0 points to the last
					// scanline in the buffer. To normalize the loop, obtain
					// a pointer to the front of the buffer that is located
					// (Height-1) scanlines previous.
					pos = (byte*)start.ToPointer() + locked.Stride * ((height > 0) ? height - 1 : 0); // satiate FxCop CA2233.
				}
				uint stride = (uint)Math.Abs(locked.Stride); // wtf.

				int i = 0;
				for (uint row = 0; row != height; ++row)
				for (uint col = 0; col != width && i != bindata.Length; ++col, ++i)
				{
					// The destination pixel.
					// The pointer to the color index byte of the destination;
					// this real pointer causes this code to be considered unsafe.
					byte* pixel = pos + row * stride + col;
					*pixel = bindata[i];
				}
			}
			b.UnlockBits(locked);

			b.Palette = pal;

			return b;
		}


		/// <summary>
		/// Used by ExportSpritesheet() and MapFileBase.SaveGifFile()
		/// </summary>
		/// <param name="width">width of final Bitmap</param>
		/// <param name="height">height of final Bitmap</param>
		/// <param name="pal">palette to color the image with</param>
		/// <returns>pointer to Bitmap</returns>
		internal static Bitmap CreateTransparent(
				int width,
				int height,
				ColorPalette pal)
		{
			var b = new Bitmap(
							width, height,
							PixelFormat.Format8bppIndexed);

			var locked = b.LockBits(
								new Rectangle(0,0, width, height),
								ImageLockMode.WriteOnly,
								PixelFormat.Format8bppIndexed);

			// Write to the temporary buffer that is provided by LockBits().
			// Copy the pixels from the source image in this loop.
			// Because you want an index, convert RGB to the appropriate
			// palette index here.
			var start = locked.Scan0;

			unsafe
			{
				byte* pos;
				if (locked.Stride > 0)
				{
					pos = (byte*)start.ToPointer();
				}
				else
				{
					// If the stride is negative, Scan0 points to the last
					// scanline in the buffer. To normalize the loop, obtain
					// a pointer to the front of the buffer that is located
					// (Height-1) scanlines previous.
					pos = (byte*)start.ToPointer() + locked.Stride * ((height > 0) ? height - 1 : 0); // satiate FxCop CA2233.
				}
				uint stride = (uint)Math.Abs(locked.Stride); // wtf.

				for (uint row = 0; row != height; ++row)
				for (uint col = 0; col != width;  ++col)
				{
					// The destination pixel.
					// The pointer to the color index byte of the destination;
					// this real pointer causes this code to be considered unsafe.
					byte* pixel = pos + row * stride + col;
					*pixel = Palette.TranId;
				}
			}
			b.UnlockBits(locked);

			b.Palette = pal;

			return b;
		}

		/// <summary>
		/// Used by ExportSpritesheet() and MapFileBase.SaveGifFile()
		/// </summary>
		/// <param name="src"></param>
		/// <param name="dst"></param>
		/// <param name="x"></param>
		/// <param name="y"></param>
		internal static void Insert(
				Bitmap src,
				Bitmap dst,
				int x,
				int y)
		{
			var dstLocked = dst.LockBits(
									new Rectangle(0,0, dst.Width, dst.Height),
									ImageLockMode.WriteOnly,
									PixelFormat.Format8bppIndexed);

			var srcLocked = src.LockBits(
									new Rectangle(0,0, src.Width, src.Height),
									ImageLockMode.ReadOnly,
									PixelFormat.Format8bppIndexed);

			var srcStart = srcLocked.Scan0;
			var dstStart = dstLocked.Scan0;

			unsafe
			{
				byte* srcPos;
				if (srcLocked.Stride > 0)
					srcPos = (byte*)srcStart.ToPointer();
				else
					srcPos = (byte*)srcStart.ToPointer() + srcLocked.Stride * (src.Height - 1);

				uint srcStride = (uint)Math.Abs(srcLocked.Stride); // wtf.

				byte* dstPos;
				if (dstLocked.Stride > 0)
					dstPos = (byte*)dstStart.ToPointer();
				else
					dstPos = (byte*)dstStart.ToPointer() + dstLocked.Stride * (dst.Height - 1);

				uint dstStride = (uint)Math.Abs(dstLocked.Stride); // wtf.

				for (uint row = 0; row != src.Height; ++row)
				for (uint col = 0; col != src.Width;  ++col)
				{
					byte* srcPixel = srcPos +  row      * srcStride +  col;
					byte* dstPixel = dstPos + (row + y) * dstStride + (col + x);

					if (*srcPixel != Palette.TranId && row + y < dst.Height)
						*dstPixel = *srcPixel;
				}
			}
			src.UnlockBits(srcLocked);
			dst.UnlockBits(dstLocked);
		}

		/// <summary>
		/// Used by MapFileBase.SaveGifFile()
		/// </summary>
		/// <param name="b"></param>
		/// <param name="transparent"></param>
		/// <returns></returns>
		internal static Rectangle GetCloseRectangle(Bitmap b, int transparent)
		{
			var locked = b.LockBits(
								new Rectangle(0,0, b.Width, b.Height),
								ImageLockMode.ReadOnly,
								PixelFormat.Format8bppIndexed);

			var start = locked.Scan0;

			int rowMin, rowMax, colMin, colMax;
			unsafe
			{
				byte* pos;
				if (locked.Stride > 0)
					pos = (byte*)start.ToPointer();
				else
					pos = (byte*)start.ToPointer() + locked.Stride * (b.Height - 1);
				
				uint stride = (uint)Math.Abs(locked.Stride); // wtf.

				for (rowMin = 0; rowMin != b.Height; ++rowMin)
				for (int col = 0; col != b.Width; ++col)
				{
					byte id = *(pos + rowMin * stride + col);
					if (id != transparent)
						goto outLoop1;
				}

			outLoop1:
				for (colMin = 0; colMin != b.Width; ++colMin)
				for (int row = rowMin; row < b.Height; ++row)
				{
					byte id = *(pos + row * stride + colMin);
					if (id != transparent)
						goto outLoop2;
				}

			outLoop2:
				for (rowMax = b.Height - 1; rowMax > rowMin; --rowMax)
				for (int col = colMin; col < b.Width; ++col)
				{
					byte id = *(pos + rowMax * stride + col);
					if (id != transparent)
						goto outLoop3;
				}

			outLoop3:
				for (colMax = b.Width - 1; colMax > colMin; --colMax)
				for (int row = rowMin; row < rowMax; ++row)
				{
					byte id = *(pos + row * stride + colMax);
					if (id != transparent)
						goto outLoop4;
				}
			}
			outLoop4:
			b.UnlockBits(locked);

			return new Rectangle(
							colMin - 1, rowMin - 1,
							colMax - colMin + 3, rowMax - rowMin + 3);
		}

		/// <summary>
		/// Used by MapFileBase.SaveGifFile()
		/// </summary>
		/// <param name="src"></param>
		/// <param name="rect"></param>
		/// <returns></returns>
		internal static Bitmap Crop(Bitmap src, Rectangle rect)
		{
			var dst = CreateTransparent(rect.Width, rect.Height, src.Palette);

			var dstLocked = dst.LockBits(
									new Rectangle(0,0, dst.Width, dst.Height),
									ImageLockMode.WriteOnly,
									PixelFormat.Format8bppIndexed);

			var srcLocked = src.LockBits(
									new Rectangle(0,0, src.Width, src.Height),
									ImageLockMode.ReadOnly,
									PixelFormat.Format8bppIndexed);

			var srcStart = srcLocked.Scan0;
			var dstStart = dstLocked.Scan0;

			unsafe
			{
				byte* srcPos;
				if (srcLocked.Stride > 0)
					srcPos = (byte*)srcStart.ToPointer();
				else
					srcPos = (byte*)srcStart.ToPointer() + srcLocked.Stride * (src.Height - 1);

				uint srcStride = (uint)Math.Abs(srcLocked.Stride); // wtf.

				byte* dstPos;
				if (dstLocked.Stride > 0)
					dstPos = (byte*)dstStart.ToPointer();
				else
					dstPos = (byte*)dstStart.ToPointer() + dstLocked.Stride * (dst.Height - 1);

				uint dstStride = (uint)Math.Abs(dstLocked.Stride); // wtf.

				for (uint row = 0; row != rect.Height; ++row)
				for (uint col = 0; col != rect.Width;  ++col) // why all these effin uints is there a point
				{
					byte* srcPixel = srcPos + (row + rect.Y) * srcStride + (col + rect.X);
					byte* dstPixel = dstPos +  row           * dstStride +  col;

//					if (*srcPixel != PckImage.TransparentIndex && row + y < dst.Height)
					*dstPixel = *srcPixel;
				}
			}
			src.UnlockBits(srcLocked);
			dst.UnlockBits(dstLocked);

			return dst;
		}
	}
}

/*		/// <summary>
		/// Saves a sprite to a given path w/ format: MS Windows 3 Bitmap, uncompressed.
		/// </summary>
		/// <param name="fullpath"></param>
		/// <param name="bitmap"></param>
		public static void ExportSprite(string fullpath, Bitmap bitmap)
		{
			using (var bw = new BinaryWriter(new FileStream(fullpath, FileMode.Create)))
			{
				int pad = 0;
				while ((bitmap.Width + pad) % 4 != 0)
					++pad;

				int len = (bitmap.Width + pad) * bitmap.Height;

				bw.Write('B');
				bw.Write('M');
				bw.Write(1078 + len); // 14 + 40 + (4 * 256)
				bw.Write((int)0);
				bw.Write((int)1078);

				bw.Write((int)40);
				bw.Write((int)bitmap.Width);
				bw.Write((int)bitmap.Height);
				bw.Write((short)1);
				bw.Write((short)8);
				bw.Write((int)0);
				bw.Write((int)0);
				bw.Write((int)0);
				bw.Write((int)0);
				bw.Write((int)0);
				bw.Write((int)0);
				bw.Write((int)0);

//				byte[] bArr = new byte[256 * 4];
				var entries = bitmap.Palette.Entries;

				for (int colorId = 1; colorId != 256; ++colorId)
				{
//				for (int i = 0; i < bArr.Length; i += 4)
//				{
//					bArr[i]     = entries[i / 4].B;
//					bArr[i + 1] = entries[i / 4].G;
//					bArr[i + 2] = entries[i / 4].R;
//					bArr[i + 3] = 0;

					bw.Write(entries[colorId].B);
					bw.Write(entries[colorId].G);
					bw.Write(entries[colorId].R);
					bw.Write((byte)0);

//					bw.Write((byte)image.Palette.Entries[i].B);
//					bw.Write((byte)image.Palette.Entries[i].G);
//					bw.Write((byte)image.Palette.Entries[i].R);
//					bw.Write((byte)0);
				}
//				bw.Write(bArr);

				var colorTable = new Dictionary<Color, byte>();

				int id = 0;
				foreach(var colorId in bitmap.Palette.Entries)
					colorTable[colorId] = (byte)id++;

				colorTable[Color.FromArgb(0, 0, 0, 0)] = (byte)255;

				for (int i = bitmap.Height - 1; i != -1; --i)
				{
					for (int j = 0; j != bitmap.Width; ++j)
						bw.Write(colorTable[bitmap.GetPixel(j, i)]);

					for (int j = 0; j != pad; ++j)
						bw.Write((byte)0x00);
				}
			}
		} */


//		public static void Save24(string path, Bitmap image)
//		{
//			Save24(new FileStream(path, FileMode.Create), image);
//		}

//		public static void Save24(Stream str, Bitmap image)
//		{
//			var bw = new BinaryWriter(str);
//
//			int more = 0;
//			while ((image.Width * 3 + more) % 4 != 0)
//				more++;
//
//			int len = (image.Width * 3 + more) * image.Height;
//
//			bw.Write('B');					// must always be set to 'BM' to declare that this is a .bmp-file.
//			bw.Write('M');
//			bw.Write(14 + 40 + len);		// specifies the size of the file in bytes.
//			bw.Write((int)0);				// zero
//			bw.Write((int)14 + 40);			// specifies the offset from the beginning of the file to the bitmap data.
//
//			bw.Write((int)40);				// specifies the size of the BITMAPINFOHEADER structure, in bytes
//			bw.Write((int)image.Width);
//			bw.Write((int)image.Height);
//			bw.Write((short)1);				// specifies the number of planes of the target device
//			bw.Write((short)24);			// specifies the number of bits per pixel
//			bw.Write((int)0);
//			bw.Write((int)0);
//			bw.Write((int)0);
//			bw.Write((int)0);
//			bw.Write((int)0);
//			bw.Write((int)0);
//
//			for (int i = image.Height - 1; i >= 0; i--)
//			{
//				for (int j = 0; j < image.Width; j++)
//				{
//					var c = image.GetPixel(j, i);
//					bw.Write((byte)c.B);
//					bw.Write((byte)c.G);
//					bw.Write((byte)c.R);
//				}
//
//				for (int j = 0; j < more; j++)
//					bw.Write((byte)0x00);
//			}
//
//			bw.Flush();
//			bw.Close();
//		}

//		public static unsafe Bitmap HQ2X(/*Bitmap image*/)
//		{
//#if hq2xWorks
//			CImage in24 = new CImage();
//			in24.Init(image.Width, image.Height, 24);
//
//			for (int row = 0; row < image.Height; row++)
//				for (int col = 0; col < image.Width; col++)
//				{
//					Color c = image.GetPixel(col,row);
//					*(in24.m_pBitmap + (row * in24.m_Xres * 3) + (col * 3 + 0)) = c.B;
//					*(in24.m_pBitmap + (row * in24.m_Xres * 3) + (col * 3 + 1)) = c.G;
//					*(in24.m_pBitmap + (row * in24.m_Xres * 3) + (col * 3 + 2)) = c.R;
//				}
//
//			in24.ConvertTo16();
//
//			CImage out32 = new CImage();
//			out32.Init(in24.m_Xres * 2, in24.m_Yres * 2, 32);
//
//			CImage.InitLUTs();
//			CImage.hq2x_32(
//						in24.m_pBitmap,
//						out32.m_pBitmap,
//						in24.m_Xres,
//						in24.m_Yres,
//						out32.m_Xres * 4);
//
//			out32.ConvertTo24();
//
//			Bitmap b = new Bitmap(
//								out32.m_Xres, out32.m_Yres,
//								PixelFormat.Format24bppRgb);
//
////			Rectangle rect = new Rectangle(0, 0, b.Width, b.Height);
//			BitmapData bitmapData = b.LockBits(
//											new Rectangle(
//														0, 0,
//														b.Width, b.Height),
//											ImageLockMode.WriteOnly,
//											b.PixelFormat);
//
//			IntPtr pixels = bitmapData.Scan0;
//
//			byte* pBits;
//			if (bitmapData.Stride > 0)
//				pBits = (byte*)pixels.ToPointer();
//			else
//				pBits = (byte*)pixels.ToPointer() + bitmapData.Stride * (b.Height - 1);
//
//			byte* srcBits = out32.m_pBitmap;
//
//			for (int i = 0; i < b.Width * b.Height; i++)
//			{
//				*(pBits++) = *(srcBits++);
//				*(pBits++) = *(srcBits++);
//				*(pBits++) = *(srcBits++);
//			}
//
//			b.UnlockBits(bitmapData);
//
//			image.Dispose();
//			in24.__dtor();
//			out32.__dtor();
//
//			return b;
//#else
//			return null;
//#endif
//		}


//		public static XCImageCollection Load(string file, Type collectionType)
//		{
//			Bitmap b = new Bitmap(file);
//
//			MethodInfo mi = collectionType.GetMethod("FromBmp");
//			if (mi == null)
//				return null;
//			else
//				return (XCImageCollection)mi.Invoke(null, new object[]{ b });
//		}
//		public static XCImage LoadSingle(Bitmap src, int num, Palette pal, Type collectionType)
//		{
//			//return SpriteCollection.FromBmpSingle(src, num, pal);
//
//			MethodInfo mi = collectionType.GetMethod("FromBmpSingle");
//			if (mi == null)
//				return null;
//			else
//				return (XCImage)mi.Invoke(null, new object[]{ src, num, pal });
//		}
