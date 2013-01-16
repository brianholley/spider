using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Serialization;
using System.IO;
using System.IO.IsolatedStorage;
using Microsoft.Xna.Framework;

namespace Spider
{
    class Options
    {
        public static bool Load()
        {
            // If already loaded...
            if (options != null)
                return true;

            try
            {
                IsolatedStorageFile isoFile = IsolatedStorageFile.GetUserStoreForApplication();
                if (isoFile.FileExists(Filename))
                {
                    IsolatedStorageFileStream stream = IsolatedStorageFile.GetUserStoreForApplication().OpenFile(Filename, FileMode.Open);
                    TextReader reader = new StreamReader(stream);
                    XmlSerializer serializer = new XmlSerializer(typeof(SerializedOptions));
                    options = serializer.Deserialize(reader) as SerializedOptions;
                    reader.Close();
                }
                else
                {
                    options = new SerializedOptions();
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
                options = new SerializedOptions();
                return false;
            }
        }

        public static void Save()
        {
            // If options never loaded, they wouldn't have changed...
            if (options == null)
                return;

            try
            {
                IsolatedStorageFileStream stream = IsolatedStorageFile.GetUserStoreForApplication().OpenFile(Filename, FileMode.Create);
                StreamWriter writer = new StreamWriter(stream);
                XmlSerializer serializer = new XmlSerializer(typeof(SerializedOptions));
                serializer.Serialize(writer, options);
                writer.Close();
            }
            catch
            {
                // Couldn't save options file - crap
            }
        }

        public static void Reset()
        {
            options = new SerializedOptions();
        }

        public static Color CardBackColor { get { return options.CardBackColor; } set { options.CardBackColor = value; } }

        protected static string Filename = "Options.xml";
        protected static SerializedOptions options;
    }

    [XmlRootAttribute("Options")]
    public class SerializedOptions
    {
        [XmlAttribute]
        public int OptionsVersion = 1;

        public Color CardBackColor = Color.CornflowerBlue;
    }
}
