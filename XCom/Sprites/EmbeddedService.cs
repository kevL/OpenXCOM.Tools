﻿using System;
using System.Reflection;

using DSShared;


namespace XCom
{
	public static class EmbeddedService
	{
		/// <summary>
		/// Creates a monotone <c><see cref="Spriteset"/></c> from the embedded
		/// <c>MONOTONE.PCK/TAB</c> files.
		/// </summary>
		/// <param name="label">"Monotone" is the label of the spriteset for
		/// TopView's blank quads/TileView's eraser and "Monotone_crippled" is
		/// the label for MainView's crippled tileparts.</param>
		/// <returns>a monotone <c>Spriteset</c></returns>
		public static Spriteset CreateMonotoneSpriteset(string label)
		{
			var ass = Assembly.GetExecutingAssembly();
			using (var strPck = ass.GetManifestResourceStream("XCom._Embedded.MONOTONE" + GlobalsXC.PckExt))
			using (var strTab = ass.GetManifestResourceStream("XCom._Embedded.MONOTONE" + GlobalsXC.TabExt))
			{
				var bytesPck = new byte[strPck.Length];
				var bytesTab = new byte[strTab.Length];

				strPck.Read(bytesPck, 0, (int)strPck.Length);
				strTab.Read(bytesTab, 0, (int)strTab.Length);

				return new Spriteset( // bypass error-checking
								label,
								Palette.UfoBattle,
								bytesPck,
								bytesTab);
			}
		}
	}
}
