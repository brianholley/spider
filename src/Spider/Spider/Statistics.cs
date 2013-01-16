using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Serialization;
using System.IO;
using System.IO.IsolatedStorage;

namespace Spider
{
    class Statistics
    {
        public static bool Load()
        {
            // If already loaded...
            if (stats != null)
                return true;

            try
            {
                IsolatedStorageFile isoFile = IsolatedStorageFile.GetUserStoreForApplication();
                if (isoFile.FileExists(Filename))
                {
                    IsolatedStorageFileStream stream = IsolatedStorageFile.GetUserStoreForApplication().OpenFile(Filename, FileMode.Open);
                    TextReader reader = new StreamReader(stream);
                    XmlSerializer serializer = new XmlSerializer(typeof(SerializedStatistics));
                    stats = serializer.Deserialize(reader) as SerializedStatistics;
                    reader.Close();
                }
                else
                {
                    stats = new SerializedStatistics();
                }
                return true;
            }
            catch (FileNotFoundException)
            {
                // No statistics file found - that's okay
                return true;
            }
            catch (Exception exc)
            {
                System.Diagnostics.Debug.WriteLine(exc);
                stats = new SerializedStatistics();
                return false;
            }
        }

        public static void Save()
        {
            // If stats never loaded, they wouldn't have changed...
            if (stats == null)
                return;

            try
            {
                IsolatedStorageFileStream stream = IsolatedStorageFile.GetUserStoreForApplication().OpenFile(Filename, FileMode.Create);
                StreamWriter writer = new StreamWriter(stream);
                XmlSerializer serializer = new XmlSerializer(typeof(SerializedStatistics));
                serializer.Serialize(writer, stats);
                writer.Close();
            }
            catch
            {
                // Couldn't save stats file - crap
            }
        }

        public static void Reset()
        {
            stats = new SerializedStatistics();
        }

        public static int TotalGames { get { return stats.TotalGames; } set { stats.TotalGames = value; } }
        public static int EasyGames { get { return stats.EasyGames; } set { stats.EasyGames = value; } }
        public static int MediumGames { get { return stats.MediumGames; } set { stats.MediumGames = value; } }
        public static int HardGames { get { return stats.HardGames; } set { stats.HardGames = value; } }

        public static int TotalGamesWon { get { return stats.TotalGamesWon; } set { stats.TotalGamesWon = value; } }
        public static int EasyGamesWon { get { return stats.EasyGamesWon; } set { stats.EasyGamesWon = value; } }
        public static int MediumGamesWon { get { return stats.MediumGamesWon; } set { stats.MediumGamesWon = value; } }
        public static int HardGamesWon { get { return stats.HardGamesWon; } set { stats.HardGamesWon = value; } }

        public static long TotalTimePlayed { get { return stats.TotalTimePlayed; } set { stats.TotalTimePlayed = value; } }

        protected static string Filename = "Stats.xml";
        protected static SerializedStatistics stats;
    }

    [XmlRootAttribute("Statistics")]
    public class SerializedStatistics
    {
        [XmlAttribute]
        public int StatsVersion = 1;

        public int TotalGames = 0;
        public int EasyGames = 0;
        public int MediumGames = 0;
        public int HardGames = 0;

        public int TotalGamesWon = 0;
        public int EasyGamesWon = 0;
        public int MediumGamesWon = 0;
        public int HardGamesWon = 0;

        public long TotalTimePlayed = 0;
    }
}
