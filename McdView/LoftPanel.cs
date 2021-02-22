﻿using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Windows.Forms;

using DSShared;
using DSShared.Controls;

using XCom;


namespace McdView
{
	internal sealed class LoftPanel
		:
			BufferedPanel
	{
		#region Fields (static)
		private static McdviewF _f;
		#endregion Fields (static)


		#region Methods (static)
		/// <summary>
		/// Initializes '_f'.
		/// </summary>
		/// <param name="f"></param>
		internal static void SetStaticVars(McdviewF f)
		{
			_f = f;
		}
		#endregion Methods (static)


		#region Events (override)
		/// <summary>
		/// Draws a LoFT icon in this LoftPanel.
		/// </summary>
		/// <param name="e"></param>
		protected override void OnPaint(PaintEventArgs e)
		{
			if (McdviewF.isRunT // <- is set in McdviewF.cTor; prevents designer barf attacks.
				&& _f.Selid != -1 && _f.LoFT != null)
			{
				var graphics = e.Graphics;
				graphics.PixelOffsetMode = PixelOffsetMode.Half;

				var tb = Tag as TextBox;

				int loftid = Int32.Parse(tb.Text);
				if (loftid < _f.LoFT.Length / 256)
				{
					graphics.InterpolationMode = InterpolationMode.NearestNeighbor;

					loftid *= 256; // array id

					using (var loft = new Bitmap(16,16, PixelFormat.Format8bppIndexed))	// Format1bppIndexed <- uses only 1 BIT per pixel
					{																	// - causes probs setting the pixels below.
						var data = loft.LockBits(
											new Rectangle(0,0, loft.Width, loft.Height),
											ImageLockMode.WriteOnly,
											PixelFormat.Format8bppIndexed); // Format1bppIndexed
						var start = data.Scan0;

						unsafe
						{
							var pos = (byte*)start.ToPointer();

							byte palid;
							for (int row = 0; row != loft.Height; ++row)
							for (int col = 0; col != loft.Width;  ++col)
							{
								byte* pixel = pos + (row * data.Stride) + col;

								palid = Convert.ToByte(_f.LoFT[loftid + (row * loft.Width) + col]);
								*pixel = palid;
							}
						}
						loft.UnlockBits(data);

						Color color;
						int layer = Int32.Parse(tb.Tag.ToString());
						int track = _f.IsoLoftVal;

						if (track == layer * 2 + 1)
							color = Color.Gainsboro;
						else if (track <= layer * 2)
							color = Color.Silver;
						else
							color = Color.White;

						ColorPalette pal = loft.Palette;
						pal.Entries[Palette.LoFTclear] = Color.Black;
						pal.Entries[Palette.LoFTSolid] = color;
						loft.Palette = pal;

						graphics.DrawImage(
										loft,
										0,0,
										Width,Height);
					}
				}
				else
					graphics.FillRectangle(
										Colors.BrushInvalid,
										0,0,
										Width,Height);
			}
		}

		/// <summary>
		/// Opens the LoFT viewer when this LoftPanel is clicked.
		/// </summary>
		/// <param name="e"></param>
		/// <remarks>If user has the openfile dialog open and double-clicks to
		/// open a file that happens to be over the panel a mouse-up event
		/// fires. So use MouseDown here.</remarks>
		protected override void OnMouseDown(MouseEventArgs e)
		{
			_f.PartsPanel.Select(); // NOTE: Workaround 'bar_IsoLoft' flicker (flicker occurs iff 'bar_IsoLoft' is focused).

			if (_f.Selid != -1
				&& e.X > -1 && e.X < Width // NOTE: Bypass event if cursor moves off the panel before released.
				&& e.Y > -1 && e.Y < Height)
			{
				switch (e.Button)
				{
					case MouseButtons.Left:
						if (_f.LoFT != null)
						{
							var tb = Tag as TextBox;
							string id = tb.Text;

							using (var f = new LoftChooserF(
														_f,
														Int32.Parse(tb.Tag.ToString()),
														Int32.Parse(id)))
							{
								_f._pnlLoFT = this;
								f.ShowDialog();
							}
						}
						else
						{
							using (var f = new Infobox(
													"Error",
													"LoFT icons not found.",
													null,
													Infobox.BoxType.Error))
							{
								f.ShowDialog(this);
							}
						}
						break;

					case MouseButtons.Right:
					{
						string id = (Tag as TextBox).Text;
						if (_f.CanSetAllLofts(id)
							&& MessageBox.Show(
											this,
											"Set all LoFTs to #" + id,
											" Set all LoFTs",
											MessageBoxButtons.YesNo,
											MessageBoxIcon.Question,
											MessageBoxDefaultButton.Button1,
											0) == DialogResult.Yes)
						{
							_f.SetAllLofts(id);
						}
						break;
					}
				}

				_f.OnMouseClick_IsoLoft(null,null); // select the IsoLoFT's trackbar
			}
		}
		#endregion Events (override)
	}
}
