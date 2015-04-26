using System;
using Microsoft.Xna.Framework;
using Spider.Resources;

namespace Spider
{
	internal class TrialMode
	{
		public static bool LaunchMarketplace()
		{
			try
			{
				Windows.ApplicationModel.Store.CurrentApp.RequestAppPurchaseAsync(false).AsTask().Wait();
				return true;
			}
			catch (Exception e)
			{
				Console.Error.WriteLine("ShowMarketplace resulted in exception: " + e);
				return false;
			}
		}
	}

	internal class TrialWindow : MessageWindow
	{
		private bool _launchSuccessful = true;

		public TrialWindow(Rectangle viewRect, string text) : base(viewRect, text)
		{
			ClickDelegate = OnClick;
		}

		public override bool Update()
		{
			bool launchPrev = _launchSuccessful;
			bool update = base.Update();
			if (!update && !_launchSuccessful && launchPrev)
			{
				SetText(Strings.Menu_TrialNavFailed);
				return true;
			}
			return update;
		}

		public void OnClick()
		{
			if (_launchSuccessful)
				_launchSuccessful = TrialMode.LaunchMarketplace();
		}
	}
}