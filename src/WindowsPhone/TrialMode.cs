using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;

namespace Spider
{
    class TrialMode
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
                System.Console.Error.WriteLine("ShowMarketplace resulted in exception: " + e);
                return false;
            }
        }
    }

    class TrialWindow : MessageWindow
    {
        bool launchSuccessful = true;

        public TrialWindow(Rectangle viewRect, string text) : base(viewRect, text)
        {
            ClickDelegate = OnClick;
        }

        public override bool Update()
        {
            bool launchPrev = launchSuccessful;
            bool update = base.Update();
            if (!update && !launchSuccessful && launchPrev)
            {
                SetText(Strings.Menu_TrialNavFailed);
                return true;
            }
            return update;
        }
        
        public void OnClick()
        {
            if (launchSuccessful)
                launchSuccessful = TrialMode.LaunchMarketplace();
        }

    }
}
