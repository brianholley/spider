using System;
using System.Xml.Serialization;
using System.IO;
using System.IO.IsolatedStorage;
using Microsoft.Xna.Framework;

namespace Spider
{
	internal class Options
	{
		public static bool Load()
		{
			// If already loaded...
			if (_options != null)
				return true;

			try
			{
				var isoFile = IsolatedStorageFile.GetUserStoreForApplication();
				if (isoFile.FileExists(Filename))
				{
					var stream = IsolatedStorageFile.GetUserStoreForApplication().OpenFile(Filename, FileMode.Open);
					TextReader reader = new StreamReader(stream);
					var serializer = new XmlSerializer(typeof (SerializedOptions));
					_options = serializer.Deserialize(reader) as SerializedOptions;
					reader.Close();
				}
				return true;
			}
			catch (FileNotFoundException)
			{
				// No options file found - that's okay
				return true;
			}
			catch (Exception exc)
			{
				System.Diagnostics.Debug.WriteLine(exc);
				return false;
			}
			finally
			{
				if (_options == null)
					_options = new SerializedOptions();
			}
		}

		public static void Save()
		{
			// If options never loaded, they wouldn't have changed...
			if (_options == null)
				return;

			try
			{
				IsolatedStorageFileStream stream = IsolatedStorageFile.GetUserStoreForApplication()
					.OpenFile(Filename, FileMode.Create);
				var writer = new StreamWriter(stream);
				var serializer = new XmlSerializer(typeof (SerializedOptions));
				serializer.Serialize(writer, _options);
				writer.Close();
			}
			catch
			{
				// Couldn't save options file - crap
			}
		}

		public static void Reset()
		{
			_options = new SerializedOptions();
		}

		public static Color CardBackColor
		{
			get { return _options.CardBackColor; }
			set { _options.CardBackColor = value; }
		}

		public static string ThemePack
		{
			get { return _options.ThemePack; }
			set { _options.ThemePack = value; }
		}

		private const string Filename = "Options.xml";
		private static SerializedOptions _options;
	}

	[XmlRootAttribute("Options")]
	public class SerializedOptions
	{
		[XmlAttribute] public int OptionsVersion = 2;

		public Color CardBackColor = Color.CornflowerBlue;
		public string ThemePack = "Original";
	}
}