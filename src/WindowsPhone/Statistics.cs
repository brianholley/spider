using System;
using System.Xml.Serialization;
using System.IO;
using System.IO.IsolatedStorage;

namespace Spider
{
	internal class Statistics
	{
		public static bool Load()
		{
			// If already loaded...
			if (_stats != null)
				return true;

			try
			{
				var isoFile = IsolatedStorageFile.GetUserStoreForApplication();
				if (isoFile.FileExists(Filename))
				{
					var stream = IsolatedStorageFile.GetUserStoreForApplication().OpenFile(Filename, FileMode.Open);
					var reader = new StreamReader(stream);
					var serializer = new XmlSerializer(typeof (SerializedStatistics));
					_stats = serializer.Deserialize(reader) as SerializedStatistics;
					reader.Close();
				}
				else
				{
					_stats = new SerializedStatistics();
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
				_stats = new SerializedStatistics();
				return false;
			}
		}

		public static void Save()
		{
			// If stats never loaded, they wouldn't have changed...
			if (_stats == null)
				return;

			try
			{
				var stream = IsolatedStorageFile.GetUserStoreForApplication().OpenFile(Filename, FileMode.Create);
				var writer = new StreamWriter(stream);
				var serializer = new XmlSerializer(typeof (SerializedStatistics));
				serializer.Serialize(writer, _stats);
				writer.Close();
			}
			catch
			{
				// Couldn't save stats file - crap
			}
		}

		public static void Reset()
		{
			_stats = new SerializedStatistics();
		}

		public static int TotalGames
		{
			get { return _stats.TotalGames; }
			set { _stats.TotalGames = value; }
		}

		public static int EasyGames
		{
			get { return _stats.EasyGames; }
			set { _stats.EasyGames = value; }
		}

		public static int MediumGames
		{
			get { return _stats.MediumGames; }
			set { _stats.MediumGames = value; }
		}

		public static int HardGames
		{
			get { return _stats.HardGames; }
			set { _stats.HardGames = value; }
		}

		public static int TotalGamesWon
		{
			get { return _stats.TotalGamesWon; }
			set { _stats.TotalGamesWon = value; }
		}

		public static int EasyGamesWon
		{
			get { return _stats.EasyGamesWon; }
			set { _stats.EasyGamesWon = value; }
		}

		public static int MediumGamesWon
		{
			get { return _stats.MediumGamesWon; }
			set { _stats.MediumGamesWon = value; }
		}

		public static int HardGamesWon
		{
			get { return _stats.HardGamesWon; }
			set { _stats.HardGamesWon = value; }
		}

		public static long TotalTimePlayed
		{
			get { return _stats.TotalTimePlayed; }
			set { _stats.TotalTimePlayed = value; }
		}

		private const string Filename = "Stats.xml";
		private static SerializedStatistics _stats;
	}

	[XmlRootAttribute("Statistics")]
	public class SerializedStatistics
	{
		[XmlAttribute] public int StatsVersion = 1;

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