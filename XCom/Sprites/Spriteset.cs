using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

using DSShared;


namespace XCom
{
	/// <summary>
	/// a <c>Spriteset</c>. A collection of images that is usually created of
	/// <c>PCK+TAB</c> terrain file data but can also be units or bigobs or a
	/// ScanG or LoFT iconset.
	/// </summary>
	/// <remarks>Only PckView maintains ScanG and/or LoFT iconsets as
	/// <c>Spritesets</c>. MapView and McdView use ScanG and LoFT icons in
	/// nonjagged 2d-arrays of color-indices which are used to instantiate and
	/// draw <c>Bitmaps</c> on-the-fly.
	/// <br/><br/>
	/// This object is disposable but eff their <c>IDisposable crap</c>.</remarks>
	public sealed class Spriteset
	{
		#region Methods (disposable)
		/// <summary>
		/// Disposes all <c><see cref="XCImage">XCImages</see></c> in
		/// <c><see cref="Sprites"/></c> and clears the list.
		/// </summary>
		/// <remarks>This <c>Spriteset</c> itself remains valid along with the
		/// cleared <c>Sprites</c> list.</remarks>
		public void Dispose()
		{
			//Logfile.Log("Spriteset.Dispose() Label= " + Label);
			foreach (var sprite in Sprites)
				sprite.Dispose();

			Sprites.Clear();
		}
		#endregion Methods (disposable)


		#region Fields (static)
		public const int SpriteWidth32  = 32; // terrain, units, bigobs
		public const int SpriteHeight40 = 40; // terrain, units

		public const int SpriteHeight48 = 48; // bigobs

		public const int ScanGside      =  4;
		public const int LoFTside       = 16;
		#endregion Fields (static)


		#region Properties
		private readonly IList<XCImage> _sprites = new List<XCImage>();
		public IList<XCImage> Sprites
		{
			get { return _sprites; }
		}

		public int Count
		{
			get { return Sprites.Count; }
		}

		public string Label
		{ get; private set; }

		public int TabwordLength
		{ get; private set; }


		private Palette _pal;
		/// <summary>
		/// This <c>Spriteset's</c> reference to a <c><see cref="Palette"/></c>.
		/// </summary>
		/// <remarks>Changing the palette requires re-assigning the changed
		/// <c><see cref="System.Drawing.Imaging.ColorPalette"/></c> to all
		/// sprites in this <c>Spriteset</c>.</remarks>
		public Palette Pal
		{
			get { return _pal; }
			set
			{
				_pal = value;

				foreach (XCImage sprite in Sprites)
					sprite.Pal = Pal;
			}
		}


		/// <summary>
		/// The width of the sprites in this <c>Spriteset</c>.
		/// </summary>
		internal int SpriteWidth
		{ get; private set; }

		/// <summary>
		/// The height of the sprites in this <c>Spriteset</c>.
		/// </summary>
		internal int SpriteHeight
		{ get; private set; }


		/// <summary>
		/// Stores a possible <c><see cref="SpritesetFail"/></c> state when
		/// loading this <c>Spriteset</c>.
		/// </summary>
		/// <remarks><c><see cref="SpritesetFail.non">SpritesetFail.non</see></c>
		/// if loading is successful. The caller shall set this <c>Spriteset</c>
		/// to <c>null</c> if not <c>SpritesetFail.non</c>. Only
		/// <c><see cref="SpritesetFail.pck">SpritesetFail.pck</see></c> needs
		/// to call <c><see cref="Dispose()">Dispose()</see></c>.</remarks>
		public SpritesetFail Failreason
		{ get; internal set; }

		/// <summary>
		/// Count of sprites detected in a Pckfile. Is used only if this
		/// <c>Spriteset</c> fails to load due to a <c>PCK</c> vs <c>TAB</c>
		/// mismatch error. It's printed in the errorbox as an aid for
		/// debugging.
		/// </summary>
		internal int CountSprites
		{ get; private set; }

		/// <summary>
		/// Count of offsets detected in a Tabfile. Is used only if this
		/// <c>Spriteset</c> fails to load due to a <c>PCK</c> vs <c>TAB</c>
		/// mismatch error. It's printed in the errorbox as an aid for
		/// debugging.
		/// </summary>
		internal int CountOffsets
		{ get; private set; }

		/// <summary>
		/// This value can be set to a sprite-id if uncompressing a <c>PCK</c>
		/// sprite or spriteset fails.
		/// </summary>
		internal int Failid
		{ get; set; }
		#endregion Properties


		#region Indexers
		/// <summary>
		/// Gets/Sets the <c><see cref="XCImage"/></c> at a specified id in
		/// <c><see cref="Sprites"/></c>. Adds a sprite to the end of the set
		/// if the specified id falls outside the bounds of the list.
		/// </summary>
		public XCImage this[int id]
		{
			get
			{
				if (id > -1 && id < Count)
					return Sprites[id];

				return null;
			}
			set
			{
				if (id > -1 && id < Count)
					Sprites[id] = value;
				else
				{
					value.Id = Count;
					Sprites.Add(value);
				}
			}
		}
		#endregion Indexers


		#region cTor
		/// <summary>
		/// cTor[0]. Creates a quick and dirty blank <c>Spriteset</c>.
		/// </summary>
		/// <param name="label">typically the file w/out path or extension</param>
		/// <param name="pal">the <c><see cref="Palette"/></c> to use (typically
		/// <c><see cref="Palette.UfoBattle">Palette.UfoBattle</see></c> for
		/// UFO-sprites or <c><see cref="Palette.TftdBattle">Palette.TftdBattle</see></c>
		/// for TFTD-sprites)</param>
		/// <param name="tabwordLength"><c><see cref="SpritesetManager.TAB_WORD_LENGTH_2">SpritesetManager.TAB_WORD_LENGTH_2</see></c>
		/// for terrains/bigobs/ufo-units, <c><see cref="SpritesetManager.TAB_WORD_LENGTH_4">SpritesetManager.TAB_WORD_LENGTH_4</see></c>
		/// for tftd-units</param>
		/// <param name="spritewidth"></param>
		/// <param name="spriteheight"></param>
		/// <remarks>A spriteset is created by
		/// <list type="number">
		/// <item><c>PckView.PckViewF.OnCreateClick()</c></item>
		/// <item><c>McdView.TerrainPanel_main.addPart()</c></item>
		/// </list></remarks>
		public Spriteset(
				string label,
				Palette pal,
				int spritewidth   = SpriteWidth32,
				int spriteheight  = SpriteHeight40,
				int tabwordLength = SpritesetManager.TAB_WORD_LENGTH_2)
		{
			Label = label;
			Pal = pal;

			SpriteWidth  = spritewidth;
			SpriteHeight = spriteheight;

			TabwordLength = tabwordLength;
		}

		/// <summary>
		/// cTor[1]. Parses a Pckfile into a collection of sprites according to
		/// its Tabfile.
		/// </summary>
		/// <param name="label">typically the file w/out path or extension</param>
		/// <param name="pal">the <c><see cref="Palette"/></c> to use (typically
		/// <c><see cref="Palette.UfoBattle">Palette.UfoBattle</see></c> for
		/// UFO-sprites or
		/// <c><see cref="Palette.TftdBattle">Palette.TftdBattle</see></c> for
		/// TFTD-sprites)</param>
		/// <param name="bytesPck">byte array of the Pckfile</param>
		/// <param name="bytesTab">byte array of the Tabfile</param>
		/// <param name="spritewidth"></param>
		/// <param name="spriteheight"></param>
		/// <param name="tabwordLength"><c><see cref="SpritesetManager.TAB_WORD_LENGTH_2">SpritesetManager.TAB_WORD_LENGTH_2</see></c>
		/// for terrains/bigobs/ufo-units,
		/// <c><see cref="SpritesetManager.TAB_WORD_LENGTH_4">SpritesetManager.TAB_WORD_LENGTH_4</see></c>
		/// for tftd-units</param>
		/// <param name="createToned"><c>true</c> to create
		/// <c><see cref="PckSprite.SpriteToned">PckSprite.SpriteToned</see></c>
		/// sprites for MapView</param>
		/// <remarks>Check that <paramref name="bytesPck"/> and
		/// <paramref name="bytesTab"/> are valid before call.
		/// <br/><br/>
		/// A <c>Spriteset</c> is loaded by
		/// <list type="number">
		/// <item><c>MapView.MainViewF()</c><br/>
		/// > <c><see cref="EmbeddedService.CreateMonotoneSpriteset()">EmbeddedService.CreateMonotoneSpriteset()</see></c>
		/// partype icons</item>
		/// <item><c>MapView.MainViewF()</c><br/>
		/// > <c><see cref="SpritesetManager.CreateSpriteset(string, string, int, Palette, bool)">SpritesetManager.CreateSpriteset()</see></c>
		/// ufo-cursor<br/>
		/// > <c><see cref="SpritesetManager.CreateSpriteset(string, string, int, Palette, bool)">SpritesetManager.CreateSpriteset()</see></c>
		/// tftd-cursor</item>
		/// <item><c>MapView.MainViewF.LoadSelectedDescriptor()</c><br/>
		/// > <c><see cref="MapFileService.LoadDescriptor(Descriptor, ref bool, bool, bool, RouteNodes)">MapFileService.LoadDescriptor()</see></c><br/>
		/// > <c><see cref="Descriptor.CreateTerrain(int)">Descriptor.CreateTerrain()</see></c><br/>
		/// > <c><see cref="SpritesetManager.CreateSpriteset(string, string, int, Palette, bool)">SpritesetManager.CreateSpriteset()</see></c>
		/// tilepart sprites</item>
		/// <item><c>MapView.MainViewF.LoadSelectedDescriptor()</c><br/>
		/// > <c><see cref="MapFileService.LoadDescriptor(Descriptor, ref bool, bool, bool, RouteNodes)">MapFileService.LoadDescriptor()</see></c><br/>
		/// > <c><see cref="MapFile(Descriptor, IList, RouteNodes)">MapFile.MapFile()</see></c><br/>
		/// > <c>MapFile.LoadMapfile()</c><br/>
		/// > <c>MapFile.CreateTile()</c><br/>
		/// > <c><see cref="Tilepart.Cripple(PartType)">Tilepart.Cripple()</see></c><br/>
		/// > <c>Tilepart.CreateCrippledSprites()</c><br/>
		/// > <c><see cref="EmbeddedService.CreateMonotoneSpriteset()">EmbeddedService.CreateMonotoneSpriteset()</see></c>
		/// crippled sprites</item>
		/// <item><c>PckView.PckViewF.LoadSpriteset(string, bool)</c></item>
		/// <item><c>McdView.McdViewF.OnClick_Create()</c></item>
		/// <item><c>McdView.McdViewF.LoadTerrain()</c></item>
		/// <item><c>McdView.McdViewF.OnClick_Reload()</c></item>
		/// <item><c>McdView.McdViewF.LoadRecords()</c></item>
		/// <item><c>McdView.McdViewF.OpenCopier()</c></item>
		/// </list></remarks>
		public Spriteset(
				string label,
				Palette pal,
				byte[] bytesPck,
				byte[] bytesTab,
				int spritewidth   = SpriteWidth32,
				int spriteheight  = SpriteHeight40,
				int tabwordLength = SpritesetManager.TAB_WORD_LENGTH_2,
				bool createToned  = false)
		{
			//Logfile.Log("Spriteset label= " + label + " pal= " + pal + " tabwordLength= " + tabwordLength);
			//Logfile.Log(". bytesTab.Length= " + bytesTab.Length);
			//Logfile.Log(". tabwordLength= " + tabwordLength);

			Label = label;
			Pal = pal;

			SpriteWidth  = spritewidth;
			SpriteHeight = spriteheight;

			TabwordLength = tabwordLength;


			CountOffsets = bytesTab.Length / TabwordLength;
			//Logfile.Log(". CountOffsets= " + CountOffsets);
			var offsets = new uint[CountOffsets + 1];	// NOTE: the last entry will be set to the total length of
														// the input-bindata to deter the length of the final sprite.
			var buffer = new byte[TabwordLength];
			uint b;
			int pos = 0;

			bool le = BitConverter.IsLittleEndian; // computer architecture. But nobody expects the BigEndian anymore.

			if (TabwordLength == SpritesetManager.TAB_WORD_LENGTH_4)
			{
				while (pos != bytesTab.Length)
				{
					for (b = 0; b != TabwordLength; ++b)
						buffer[b] = bytesTab[pos + b];

					if (!le) Array.Reverse(buffer);
					offsets[pos / TabwordLength] = BitConverter.ToUInt32(buffer, 0);

					pos += TabwordLength;
				}
			}
			else // TabwordLength == SpritesetManager.TAB_WORD_LENGTH_2
			{
				while (pos != bytesTab.Length)
				{
					for (b = 0; b != TabwordLength; ++b)
						buffer[b] = bytesTab[pos + b];

					if (!le) Array.Reverse(buffer);
					offsets[pos / TabwordLength] = BitConverter.ToUInt16(buffer, 0);

					pos += TabwordLength;
				}
			}


			// TODO: Apparently MCDEdit can and will output a 1-byte Pck sprite
			// w/ only "FF" if a blank sprite is in its internal spriteset when
			// a save happens.
			//
			// But unfortunately the official OxC code @ Engine/SurfaceSet.cpp
			// void SurfaceSet::loadPck(const std::string &pck, const std::string &tab)
			// expects the first byte of a compressed PCK sprite to be the count
			// of transparent rows.
//			if (bytesPck.Length == 1)
//			{}
//			else

			if (bytesPck.Length > 1)
			{
				for (int i = 1; i != bytesPck.Length; ++i)
				{
					if (   bytesPck[i]     == PckSprite.MarkerEos
						&& bytesPck[i - 1] != PckSprite.MarkerRle)
					{
						++CountSprites; // qty of bytes in 'bytesPck' w/ value 0xFF (ie. qty of sprites)
					}
				}
				//Logfile.Log(". CountSprites= " + CountSprites);

				// test ->
/*				if (true) // rewrite the Tabfile ->
				{
					string dir = Path.GetDirectoryName(System.Windows.Forms.Application.ExecutablePath);
					string pfe = Path.Combine(dir, "file.TAB");

					using (var fsTab = FileService.CreateFile(pfe))
					if (fsTab != null)
					using (var bwTab = new BinaryWriter(fsTab))
					{
						pos = 0;

						uint u = 0;
						for (int id = 0; id != CountSprites; ++id)
						{
							if (u > UInt16.MaxValue) // bork. Psst, happens at ~150 sprites.
							{
								// "The size of the encoded sprite-data has grown too large to
								// be stored correctly in a Tab file. Try deleting sprite(s)
								// or (less effective) using more transparency in the sprites."
								return;
							}

							bwTab.Write((ushort)u);

							while (++pos != bytesPck.Length && bytesPck[pos - 1] != 0xFF) // note does not handle "FE FF"
								++u;

							++u;
						}
					}
				} */
				// <- test end.

				if (CountSprites != CountOffsets) // avoid throwing 1 or 15000 exceptions ...
				{
					//Logfile.Log("SpritesetFail.qty");
					Failreason = SpritesetFail.qty;
					return;
				}


				offsets[offsets.Length - 1] = (uint)bytesPck.Length;

				byte[] bindata;

				for (int i = 0; i != offsets.Length - 1; ++i)
				{
					bindata = new byte[offsets[i + 1] - offsets[i]];

					for (int j = 0; j != bindata.Length; ++j)
						bindata[j] = bytesPck[offsets[i] + j];

					if (bindata[bindata.Length - 1] != PckSprite.MarkerEos)
					{
						//Logfile.Log("#" + i + ". SpritesetFail.pck val= " + bindata[bindata.Length - 1]);
						Failreason = SpritesetFail.pck;
						Failid = i;
						return;
					}

					//Logfile.Log("sprite #" + i);
					var sprite = new PckSprite(
											bindata,
											SpriteWidth,
											SpriteHeight,
											Pal,
											i,
											this,
											createToned);

					if (   Failreason == SpritesetFail.ovr
						|| Failreason == SpritesetFail.eos)
					{
						//Logfile.Log("error: SpritesetFail." + Failreason);
						return;
					}

					Sprites.Add(sprite);
				}
			}
			else
			{
				// a spriteset needs at least 2 bytes: one for the count of
				// transparent lines in the first sprite and another for its
				// End_of_Sprite

				using (var f = new Infobox(
										"Error",
										Label + " is corrupt.",
										null,
										InfoboxType.Error))
				{
					f.ShowDialog();
				}
			}
		}

		/// <summary>
		/// cTor[2]. Creates a <c>Spriteset</c> of ScanG or LoFT icons.
		/// ScanG chops bindata into 16-byte icons (4x4 256-color indexed).
		/// LoFT chops bindata into 256-bit icons (16x16 2-color true/false bits).
		/// </summary>
		/// <param name="label">typically the file w/out path or extension</param>
		/// <param name="fs">a <c>Stream</c> of the <c>SCANG.DAT</c> or
		/// <c>LOFTEMPS.DAT</c> file</param>
		/// <param name="setType"><c><see cref="SpritesetType.LoFT">SpritesetType.LoFT</see></c>
		/// if LoFT data,
		/// <c><see cref="SpritesetType.ScanG">SpritesetType.ScanG</see></c>
		/// if ScanG</param>
		/// <remarks>cf
		/// <list type="bullet">
		/// <item><c><see cref="SpritesetManager.LoadScanGufo()">SpritesetManager.LoadScanGufo()</see></c></item>
		/// <item><c><see cref="SpritesetManager.LoadScanGtftd()">SpritesetManager.LoadScanGtftd()</see></c></item>
		/// <item><c><see cref="SpritesetManager.LoadLoFTufo()">SpritesetManager.LoadLoFTufo()</see></c></item>
		/// <item><c><see cref="SpritesetManager.LoadLoFTtftd()">SpritesetManager.LoadLoFTtftd()</see></c></item>
		/// <item><c>McdviewF.LoadScanGufo()</c></item>
		/// <item><c>McdviewF.LoadScanGtftd()</c></item>
		/// <item><c>McdviewF.LoadLoFTufo()</c></item>
		/// <item><c>McdviewF.LoadLoFTtftd()</c></item>
		/// </list></remarks>
		public Spriteset(string label, Stream fs, SpritesetType setType)
		{
			Label         = label;
			TabwordLength = SpritesetManager.TAB_WORD_LENGTH_0;

			byte[] bindata; int iconCount;

			switch (setType)
			{
				case SpritesetType.ScanG:
					SpriteWidth = SpriteHeight = ScanGside;

					bindata = new byte[(int)fs.Length];
					fs.Read(bindata, 0, bindata.Length);

					iconCount = bindata.Length / ScanGicon.Length_ScanG;
					for (int i = 0; i != iconCount; ++i)
					{
						var icondata = new byte[ScanGicon.Length_ScanG];

						for (int j = 0; j != ScanGicon.Length_ScanG; ++j)
							icondata[j] = bindata[i * ScanGicon.Length_ScanG + j];

						Sprites.Add(new ScanGicon(icondata, i));
					}
					Pal = Palette.UfoBattle; // <- default
					break;

				case SpritesetType.LoFT:
					SpriteWidth = SpriteHeight = LoFTside;

					bindata = new byte[(int)fs.Length];
					fs.Read(bindata, 0, bindata.Length);

					int posByte = 0, posBit;

					iconCount = bindata.Length / LoFTicon.Length_LoFT;
					for (int id = 0; id != iconCount; ++id)
					{
						// 32 bytes in a loft
						// 256 bits in a loft

						var icondata = new BitArray(LoFTicon.Length_LoFT_bits); // init to Falses

						// read the data as little-endian unsigned shorts - you gotta be kidding
						// who decided to write LoFTemps.dat as SHORTS
						// eg. C0 01 -> 01 C0

						posBit = -1;
						for (int i = 0; i != LoFTicon.Length_LoFT; i += 2, posByte += 2)
						{
							for (byte j = 0x80; j != 0x00; j >>= 1) // 1000 0000 - iterate over bits in each even byte
							{
								icondata[++posBit] = ((bindata[posByte + 1] & j) != 0);
							}

							for (byte j = 0x80; j != 0x00; j >>= 1) // iterate over bits in each odd byte
							{
								icondata[++posBit] = ((bindata[posByte] & j) != 0);
							}
						}

						// convert to binary palette-ids and store the data to a
						// byte-array in 'XCImage' ->
						var bytes = new byte[icondata.Length];
						for (int i = 0; i != icondata.Length; ++i)
						{
							if (icondata[i])
								bytes[i] = (byte)1;
							else
								bytes[i] = (byte)0;
						}
						Sprites.Add(new LoFTicon(bytes, id));
					}
					Pal = Palette.Binary;
					break;
			}
		}
		#endregion cTor


		#region Methods
		/// <summary>
		/// Saves this <c>Spriteset</c> to <c>PCK+TAB</c>.
		/// </summary>
		/// <param name="pf">the directory and label w/out extension to save to</param>
		/// <returns><c>true</c> if mission was successful; <c>false</c> if a
		/// 2-byte tabword-length offset exceeds <c>UInt16.MaxValue</c>. Also
		/// <c>false</c> if the Pck- or Tab-file could not be created.</returns>
		public bool WriteSpriteset(string pf)
		{
			string pfePck = pf + GlobalsXC.PckExt;
			string pfeTab = pf + GlobalsXC.TabExt;

			string pfePckT, pfeTabT;
			if (File.Exists(pfePck) || File.Exists(pfeTab))
			{
				pfePckT = pfePck + GlobalsXC.TEMPExt;
				pfeTabT = pfeTab + GlobalsXC.TEMPExt;
			}
			else
			{
				pfePckT = pfePck;
				pfeTabT = pfeTab;
			}


			bool fail = true;
			using (var fsPck = FileService.CreateFile(pfePckT))
			if (fsPck != null)
			using (var fsTab = FileService.CreateFile(pfeTabT))
			if (fsTab != null)
			{
				fail = false;
				using (var bwPck = new BinaryWriter(fsPck))
				using (var bwTab = new BinaryWriter(fsTab))
				{
					switch (TabwordLength)
					{
						case SpritesetManager.TAB_WORD_LENGTH_2:
						{
							uint pos = 0;
							for (int id = 0; id != Count; ++id)
							{
								if (pos > UInt16.MaxValue) // bork. Psst, happens at ~150 sprites.
								{
									// "The size of the encoded sprite-data has grown too large to
									// be stored correctly in a Tab file. Try deleting sprite(s)
									// or (less effective) using more transparency in the sprites."
									return false;
								}

								bwTab.Write((ushort)pos);
								pos += (Sprites[id] as PckSprite).Write(bwPck);
							}
							break;
						}

						case SpritesetManager.TAB_WORD_LENGTH_4:
						{
							uint pos = 0;
							for (int id = 0; id != Count; ++id)
							{
								bwTab.Write(pos);
								pos += (Sprites[id] as PckSprite).Write(bwPck);
							}
							break;
						}
					}
				}
			}

			if (!fail && pfePckT != pfePck)
			{
				return FileService.ReplaceFile(pfePck)
					&& FileService.ReplaceFile(pfeTab);
			}

			return !fail;
		}


		// NOTE: A possible internal reason that a spriteset is invalid is that
		// if the total length of its compressed PCK-data exceeds 2^16 bits
		// (roughly). That is, the TAB file tracks the offsets and needs to
		// know the total length of the PCK file, but UFO's TAB file stores the
		// offsets in only 2-byte format (2^16 bits) so the arithmetic explodes
		// with an overflow as soon as an offset for one of the sprites becomes
		// too large. (Technically, the total PCK data can exceed 2^16 bits;
		// but the *start offset* for a sprite cannot -- at least that's how it
		// works in MapView I/II. Other apps like XCOM, OpenXcom, MCDEdit will
		// use their own routines.)
		// NOTE: It appears that TFTD's terrain files suffer this limitation
		// also (2-byte TabwordLength).

		/// <summary>
		/// Tests this <c>Spriteset</c> for validity of its Tabfile.
		/// </summary>
		/// <param name="result">a ref to hold the result as a string</param>
		/// <returns><c>true</c> if mission was successful</returns>
		/// <remarks>Test only <c>Spritesets</c> with a
		/// <c><see cref="TabwordLength"/></c> of
		/// <c><see cref="SpritesetManager.TAB_WORD_LENGTH_2">SpritesetManager.TAB_WORD_LENGTH_2</see></c>.</remarks>
		public bool TestTabOffsets(out string result)
		{
			uint pos = 0;
			for (int id = 0; id != Count; ++id)
			{
				if (pos > UInt16.MaxValue)
				{
					result = "Only " + id + " of " + Count
						   + " sprites can be indexed in the TAB file."
						   + Environment.NewLine
						   + "Failed at position " + pos;
					return false;
				}
				pos += (Sprites[id] as PckSprite).Write(); // test only.
			}

			result = "Sprite offsets are valid.";
			return true;
		}

		/// <summary>
		/// Deters a specified sprite-id's 2-byte TabOffset as well as the
		/// TabOffset of the next sprite.
		/// </summary>
		/// <param name="last">ref for the TabOffset of <paramref name="id"/></param>
		/// <param name="aftr">ref for the TabOffset of the next sprite</param>
		/// <param name="id"></param>
		/// <remarks>Check that <paramref name="id"/> is less than
		/// <c><see cref="Count"/></c> before call.</remarks>
		public void GetTabOffsets(
				out uint last,
				out uint aftr,
				int id)
		{
			last = aftr = 0;

			uint len;
			for (int i = 0; i <= id; ++i)
			{
				len = (Sprites[i] as PckSprite).Write(); // test only.
				if (i != id)
					last += len;
				else
					aftr = last + len;
			}
		}


		/// <summary>
		/// Saves this <c>Spriteset</c> to <c>SCANG.DAT</c>.
		/// </summary>
		/// <param name="pfe">the path-file-extension to save to</param>
		/// <returns><c>true</c> if mission was successful</returns>
		public bool WriteScanG(string pfe)
		{
			string pfeT;
			if (File.Exists(pfe))
				pfeT = pfe + GlobalsXC.TEMPExt;
			else
				pfeT = pfe;

			bool fail = true;
			using (var fs = FileService.CreateFile(pfeT))
			if (fs != null)
			{
				fail = false;

				byte[] bindata;
				for (int id = 0; id != Count; ++id)
				{
					bindata = Sprites[id].GetBindata();
					fs.Write(bindata, 0, bindata.Length);
				}
			}

			if (!fail && pfeT != pfe)
				return FileService.ReplaceFile(pfe);

			return !fail;
		}

		/// <summary>
		/// Saves this <c>Spriteset</c> to <c>LOFTEMPS.DAT</c>.
		/// </summary>
		/// <param name="pfe">the path-file-extension to save to</param>
		/// <returns><c>true</c> if mission was successful</returns>
		public bool WriteLoFT(string pfe)
		{
			string pfeT;
			if (File.Exists(pfe))
				pfeT = pfe + GlobalsXC.TEMPExt;
			else
				pfeT = pfe;

			bool fail = true;
			using (var fs = FileService.CreateFile(pfeT))
			if (fs != null)
			{
				fail = false;

				byte[] bindata;
				for (int id = 0; id != Count; ++id)
				{
					var buffer = new byte[1];
					BitArray bits;
					int b;

					bindata = Sprites[id].GetBindata();
					for (int i = 0; i != bindata.Length; i += 16)
					{
						// Look don't ask it appears to work ...

						b = 0;
						bits = new BitArray(8);
						for (int j = 15; j != 7; --j, ++b)
						{
							bits[b] = (bindata[i + j] != 0);
						}
						bits.CopyTo(buffer, 0);
						fs.Write(buffer, 0, 1);

						b = 0;
						bits = new BitArray(8);
						for (int j = 7; j != -1; --j, ++b)
						{
							bits[b] = (bindata[i + j] != 0);
						}
						bits.CopyTo(buffer, 0);
						fs.Write(buffer, 0, 1);
					}
				}
			}

			if (!fail && pfeT != pfe)
				return FileService.ReplaceFile(pfe);

			return !fail;
		}
		#endregion Methods


		#region Methods (override)
		/// <summary>
		/// Returns <c><see cref="Label"/></c>.
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return Label;
		}
		#endregion Methods (override)
	}
}
