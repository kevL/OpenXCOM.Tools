using System;
using System.Collections.Generic;
using System.IO;


namespace DSShared
{
	public static class SharedSpace
	{
		#region Fields (static)
		public const string ApplicationDirectory  = "ApplicationDirectory";
		public const string SettingsDirectory     = "SettingsDirectory";
		public const string ResourceDirectoryUfo  = "ResourceDirectoryUfo";
		public const string ResourceDirectoryTftd = "ResourceDirectoryTftd";

		public static string CursorFilePrefix     = "UFOGRAPH" + Path.DirectorySeparatorChar + "CURSOR"; // the cursor is determined in XCMainWindow.cTor
		public static string ScanGfile            = "GEODATA"  + Path.DirectorySeparatorChar + "SCANG.DAT";
		public static string LoftfileUfo          = "GEODATA"  + Path.DirectorySeparatorChar + "LOFTEMPS.DAT";
		public static string LoftfileTftd         = "TERRAIN"  + Path.DirectorySeparatorChar + "LOFTEMPS.DAT"; // *snap* TFTD's LoFTs could be in GEODATA also ... not sure.


		private static readonly Dictionary<string, object> _shares =
							new Dictionary<string, object>();
		#endregion Fields (static)


		// TODO: Since SharedSpace holds only string-values factor away the
		// boxing and just use strings. Actually, Palettes is a dictionary ...
		// but it should be changed into a variable that's local to PckView
		// anyway.
		//
		// NOTE: which means that SharedSpace and PathInfo have very similar
		// usages and ought be merged.
		//
		// NOTE: PathInfo objects are returned as objects also.


		#region Methods (static)
		/// <summary>
		/// Allocates a key-val pair in the SharedSpace. This does not replace
		/// the value of an existing key unless its value is null.
		/// </summary>
		/// <param name="key">the key to look for</param>
		/// <param name="val">the object to add if the key doesn't exist or its
		/// value is null</param>
		public static void SetShare(string key, object val)
		{
			if (!_shares.ContainsKey(key))
			{
				_shares.Add(key, val);
			}
			else if (_shares[key] == null)
			{
				_shares[key] = val;
			}
		}

		/// <summary>
		/// Gets the value of a specified key as an object.
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		public static object GetShareObject(string key)
		{
			object val;
			if (!String.IsNullOrEmpty(key) && _shares.TryGetValue(key, out val))
				return val;

			return null;
		}

		/// <summary>
		/// Gets the value of a specified key as a string.
		/// </summary>
		/// <param name="key"></param>
		/// <returns>the value associated with the key; null if key is
		/// invalid</returns>
		public static string GetShareString(string key)
		{
			object val;
			if (!String.IsNullOrEmpty(key) && _shares.TryGetValue(key, out val))
				return val as String;

			return null;
		}
		#endregion Methods (static)
	}
}