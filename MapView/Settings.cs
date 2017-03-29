using System;
using System.Collections.Generic;
using System.Drawing;
using System.Reflection;

using XCom;


namespace MapView
{
	public delegate string ConvertObject(object value);
	public delegate void ValueChangedEventHandler(object sender, string key, object value); // TODO: FxCop CA1009.


	/// <summary>
	/// A wrapper around a Hashtable for Setting objects. Setting objects are
	/// intended to use with the CustomPropertyGrid.
	/// </summary>
	public class Settings
	{
		private Dictionary<string, Setting> _settings;
		private Dictionary<string, PropertyObject> _propertyObject;

		private static Dictionary<Type,ConvertObject> _converters;

		public static void AddConverter(Type type, ConvertObject obj)
		{
			if (_converters == null)
				_converters = new Dictionary<Type, ConvertObject>();

			_converters[type] = obj;
		}


		public Settings()
		{
			_settings = new Dictionary<string, Setting>();
			_propertyObject = new Dictionary<string, PropertyObject>();

			if (_converters == null)
			{
				_converters = new Dictionary<Type,ConvertObject>();
				_converters[typeof(Color)] = new ConvertObject(ConvertColor);
			}
		}


		public static void ReadSettings(
				Varidia vars,
				KeyvalPair keyval,
				Settings settings)
		{
			while ((keyval = vars.ReadLine()) != null)
			{
				switch (keyval.Keyword)
				{
					case "{": // starting out
						break;

					case "}": // all done
						return;

					default:
						if (settings[keyval.Keyword] != null)
						{
							settings[keyval.Keyword].Value = keyval.Value;
							settings[keyval.Keyword].FireUpdate(keyval.Keyword);
						}
						break;
				}
			}
		}

		/// <summary>
		/// Gets the key collection for this Settings object.
		/// </summary>
		public Dictionary<string, Setting>.KeyCollection Keys
		{
			get { return _settings.Keys; }
		}

		/// <summary>
		/// Gets/Sets the Setting object tied to the input string.
		/// </summary>
		public Setting this[string key]
		{
			get
			{
				key = key.Replace(" ", String.Empty);
				return (_settings.ContainsKey(key)) ? _settings[key]
													: null;
			}

			set
			{
				key = key.Replace(" ", String.Empty);
				if (!_settings.ContainsKey(key))
					_settings.Add(key, value);
				else
				{
					_settings[key] = value;
					value.Name = key;
				}
			}
		}

		/// <summary>
		/// Adds a setting to a specified object.
		/// </summary>
		/// <param name="name">property name</param>
		/// <param name="value">start value of the property</param>
		/// <param name="desc">property description</param>
		/// <param name="category">property category</param>
		/// <param name="update">event handler to receive the PropertyValueChanged event</param>
		/// <param name="reflect">if true, an internal event handler will be created - the refObj
		/// must not be null and the name must be the name of a property of the type that refObj is</param>
		/// <param name="refValue">the object that will receive the changed property values</param>
		public void AddSetting(
				string name,
				object value,
				string desc,
				string category,
				ValueChangedEventHandler update,
				bool reflect,
				object refValue)
		{
			name = name.Replace(" ", String.Empty);

			Setting setting;
			if (!_settings.ContainsKey(name))
			{
				setting = new Setting(value, desc, category);
				_settings[name] = setting;
			}
			else
			{
				setting = _settings[name];
				setting.Value = value;
				setting.Description = desc;
			}

			if (update != null)
				setting.ValueChanged += update;

			if (reflect && refValue != null)
			{
				_propertyObject[name] = new PropertyObject(refValue, name);
				this[name].ValueChanged += ReflectEvent;
			}
		}

		/// <summary>
		/// Gets the object tied to the string. If there is no object one will
		/// be created with the value specified.
		/// </summary>
		/// <param name="key">the name of the setting object</param>
		/// <param name="value">if there is no Setting object tied to the
		/// string, a Setting will be created with this as its Value</param>
		/// <returns>the Setting object tied to the string</returns>
		public Setting GetSetting(string key, object value)
		{
			if (!_settings.ContainsKey(key))
			{
				var setting = new Setting(value, null, null);
				_settings.Add(key, setting);
				setting.Name = key;
			}
			return _settings[key];
		}

		private void ReflectEvent(object sender, string key, object val)
		{
//			System.Windows.Forms.PropertyValueChangedEventArgs pe = (System.Windows.Forms.PropertyValueChangedEventArgs)e;
			_propertyObject[key].SetValue(val);
		}

		public void Save(string line, System.IO.TextWriter sw)
		{
			sw.WriteLine(line);
			sw.WriteLine("{");

			foreach (string st in _settings.Keys)
				sw.WriteLine("\t" + st + ":" + Convert(this[st].Value));

			sw.WriteLine("}");
		}

		private static string Convert(object obj)
		{
			return (_converters.ContainsKey(obj.GetType())) ? _converters[obj.GetType()](obj)
															: obj.ToString();
		}

		private static string ConvertColor(object obj)
		{
			var color = (Color)obj;
			if (color.IsKnownColor || color.IsNamedColor || color.IsSystemColor)
				return color.Name;

			return string.Format(
							System.Globalization.CultureInfo.InvariantCulture,
							"{0},{1},{2},{3}",
							color.A, color.R, color.G, color.B);
		}
	}

	/// <summary>
	/// Stores information to be used in the CustomPropertyGrid.
	/// </summary>
	public class Setting
	{
		private object _value;

		private static Dictionary<Type, parseString> _converters;

		public event ValueChangedEventHandler ValueChanged;

		private delegate object parseString(string st);

		private static object parseBoolString(string st)
		{
			return bool.Parse(st);
		}

		private static object parseIntString(string st)
		{
			return int.Parse(st, System.Globalization.CultureInfo.InvariantCulture);
		}

		private static object parseColorString(string st)
		{
			string[] vals = st.Split(',');

			switch (vals.Length)
			{
				case 1:
					return Color.FromName(st);

				case 3:
					return Color.FromArgb(
									int.Parse(vals[0], System.Globalization.CultureInfo.InvariantCulture),
									int.Parse(vals[1], System.Globalization.CultureInfo.InvariantCulture),
									int.Parse(vals[2], System.Globalization.CultureInfo.InvariantCulture));
			}

			return Color.FromArgb(
								int.Parse(vals[0], System.Globalization.CultureInfo.InvariantCulture),
								int.Parse(vals[1], System.Globalization.CultureInfo.InvariantCulture),
								int.Parse(vals[2], System.Globalization.CultureInfo.InvariantCulture),
								int.Parse(vals[3], System.Globalization.CultureInfo.InvariantCulture));
		}


		public Setting(object value, string desc, string category)
		{
			_value = value;
			Description = desc;
			Category = category;

			if (_converters == null)
			{
				_converters = new Dictionary<Type, parseString>();

				_converters[typeof(int)]   = parseIntString;
				_converters[typeof(Color)] = parseColorString;
				_converters[typeof(bool)]  = parseBoolString;
			}
		}


		public bool IsBoolean
		{
			get
			{
				if (Value is bool)
					return (bool)Value;

				return false;
			}
		}

		public object Value
		{
			get { return _value; }
			set
			{
				if (_value != null)
				{
					var type = _value.GetType();
					if (_converters.ContainsKey(type))
					{
						var val = value as string;
						if (val != null)
						{
							_value = _converters[type](val);
							return;
						}
					}
				}
				_value = value;
			}
		}

		public string Description
		{ get; set; }

		public string Category
		{ get; set; }

		public string Name
		{ get; set; }

		public void FireUpdate(string key, object value) // FxCop CA1030:UseEventsWhereAppropriate
		{
			if (ValueChanged != null)
				ValueChanged(this, key, value);
		}

		public void FireUpdate(string key) // FxCop CA1030:UseEventsWhereAppropriate
		{
			if (ValueChanged != null)
				ValueChanged(this, key, _value);
		}
	}


	/// <summary>
	/// struct PropertyObject
	/// </summary>
	internal struct PropertyObject
	{
		public PropertyInfo _info;
		public object _obj;


		public PropertyObject(object obj, string property)
		{
			_obj  = obj;
			_info = obj.GetType().GetProperty(property);
		}


		public void SetValue(object obj)
		{
			_info.SetValue(_obj, obj, new object[]{});
		}
	}
}
