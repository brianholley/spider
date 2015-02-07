using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Serialization;
using Microsoft.Phone.Reactive;

namespace Spider
{
	class Analytics
	{
		public enum EventType
		{
			Boot,
			Shutdown,
			NewGame,
			ResumeGame,
			WinGame,
			ViewStatistics,
			ResetStatistics,
			ViewOptions,
			ViewAbout
		}

		public static void RegisterEvent(EventType eventType, string data = null)
		{
			// t=event,exception
			// ec=category
			// ea=action
			// el=label
			// ev=value
			var ev = new AnalyticsEvent();
			ev.Args["t"] = "event";
			ev.Args["ea"] = eventType.ToString().ToLowerInvariant();
			switch (eventType)
			{
				case EventType.Boot:
					ev.Args["ec"] = "session";
					ev.Args["av"] = Assembly.GetExecutingAssembly().GetName().Version.ToString();
					ev.Args["sr"] = (int)Application.Current.Host.Content.ActualWidth + "x" + (int)Application.Current.Host.Content.ActualHeight;
					ev.Args["ul"] = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
					break;
				case EventType.Shutdown:
					ev.Args["ec"] = "session";
					break;
				case EventType.NewGame:
					ev.Args["ec"] = "game";
					ev.Args["el"] = "suits";
					ev.Args["ev"] = data;
					break;
				case EventType.ResumeGame:
					ev.Args["ec"] = "game";
					break;
				case EventType.WinGame:
					ev.Args["ec"] = "game";
					break;
				case EventType.ViewStatistics:
					ev.Args["ec"] = "extras";
					break;
				case EventType.ResetStatistics:
					ev.Args["ec"] = "extras";
					break;
				case EventType.ViewOptions:
					ev.Args["ec"] = "extras";
					break;
				case EventType.ViewAbout:
					ev.Args["ec"] = "extras";
					break;
			}

			Task.Run(() =>
			{
				if (!_running || !SendAnalytics(ev).Wait(30000))
				{
					lock (_eventQueue)
					{
						_eventQueue.Enqueue(ev);
					}
				}
			});
			
		}

		public static void RegisterException(Exception e)
		{
			var ev = new AnalyticsEvent();
			ev.Args["t"] = "exception";
		}

		private static Queue<AnalyticsEvent> _eventQueue;
		private static bool _running;

		private const string TrackingId = "";
		private static Guid _clientGuid;

		static Analytics()
		{
			_clientGuid = new Guid();
			_eventQueue = new Queue<AnalyticsEvent>();
			
			Load();

			_running = true;
			new Thread(SendEventsThread).Start();
		}

		public static void Shutdown()
		{
			lock (_eventQueue)
			{
				_running = false;
			}
			RegisterEvent(EventType.Shutdown);
			lock (_eventQueue)
			{
				Save();
			}
		}

		#region Send

		private static void SendEventsThread()
		{
			while (true)
			{
				AnalyticsEvent ev = null;
				bool anyLeft = false;
				lock (_eventQueue)
				{
					if (!_running)
						return;

					ev = _eventQueue.Dequeue();
					anyLeft = (_eventQueue.Count > 0);
				}
				if (ev != null && !SendAnalytics(ev).Wait(30000))
				{
					lock (_eventQueue)
					{
						_eventQueue.Enqueue(ev);
					}
				}
				if (anyLeft)
					Thread.Sleep(60000);
			}
		}

		private static async Task<bool> SendAnalytics(AnalyticsEvent ev)
		{
			var args = new Dictionary<string, string>(ev.Args);
			args["v"] = "1";
			args["tid"] = TrackingId;
			args["cid"] = _clientGuid.ToString("D");

			var uri = new Uri("www.google-analytics.com/collect");
			var request = WebRequest.CreateHttp(new Uri(uri, "?" + string.Join("&", args.Select(p => p.Key + "=" + p.Value))));

			try
			{
				var response = (HttpWebResponse)await Task<WebResponse>.Factory.FromAsync(request.BeginGetResponse, request.EndGetResponse, request);
				if (response.StatusCode == HttpStatusCode.OK)
					return true;
			}
			catch (Exception e)
			{
				System.Diagnostics.Debug.WriteLine("Analytics send failed: {0}", e);
			}

			return false;
		}

		#endregion

		#region Load/Save

		private const string Filename = "Analytics.json";

		private static void Load()
		{
			try
			{
				var isoFile = IsolatedStorageFile.GetUserStoreForApplication();
				if (isoFile.FileExists(Filename))
				{
					using (var stream = IsolatedStorageFile.GetUserStoreForApplication().OpenFile(Filename, FileMode.Open))
					{
						var serializer = new DataContractJsonSerializer(typeof(CachedAnalytics));
						var cache = (CachedAnalytics)serializer.ReadObject(stream);
						_clientGuid = cache.ClientGuid;
						lock (_eventQueue)
						{
							_eventQueue = new Queue<AnalyticsEvent>(cache.Events);
						}
					}
				}
			}
			catch (Exception exc)
			{
				System.Diagnostics.Debug.WriteLine(exc);
			}
		}

		private static void Save()
		{
			try
			{
				var cache = new CachedAnalytics();
				cache.ClientGuid = _clientGuid;
				lock (_eventQueue)
				{
					cache.Events = _eventQueue.ToList();
				}

				var isoFile = IsolatedStorageFile.GetUserStoreForApplication();
				using (var stream = IsolatedStorageFile.GetUserStoreForApplication().OpenFile(Filename, FileMode.Open))
				{
					var serializer = new DataContractJsonSerializer(typeof(CachedAnalytics));
					serializer.WriteObject(stream, cache);
				}
			}
			catch (Exception exc)
			{
				System.Diagnostics.Debug.WriteLine(exc);
			}	
		}

		[DataContract]
		private class CachedAnalytics
		{
			[DataMember] public Guid ClientGuid;
			[DataMember] public List<AnalyticsEvent> Events;
		}
		#endregion
	}

	class AnalyticsEvent
	{
		public AnalyticsEvent()
		{
			Args = new Dictionary<string, string>();
		}

		public Dictionary<string, string> Args { get; private set; } 
	}
}
