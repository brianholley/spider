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
        public const int TrialGamesLimit = 10;

        public static void LaunchMarketplace()
        {
            Microsoft.Xna.Framework.GamerServices.Guide.ShowMarketplace(PlayerIndex.One);
        }
    }

    class TrialWindow
    {
        private static Texture2D backgroundTex;
        private static SpriteFont font;
        
        public static int ContentCount() { return 2; }
        public static void LoadContent(ContentManager content, ContentLoadNotificationDelegate callback)
        {
            backgroundTex = content.Load<Texture2D>(@"Menu\TrialMessage");
            callback();
            font = content.Load<SpriteFont>(@"Menu\TrialFont");
            callback();
        }

        Rectangle viewRect;
        Rectangle windowRect;
        Vector2 textPos;
        string windowText;

        public TrialWindow(Rectangle viewRect, string text)
        {
            this.viewRect = viewRect;
            windowText = text;

            Vector2 textSize = font.MeasureString(windowText);
            textPos = new Vector2((viewRect.Width - textSize.X) / 2, (viewRect.Height - textSize.Y) / 2);

            int xPadding = (int)(textSize.X * 0.15);
            int yPadding = (int)(textSize.Y * 0.15);
            windowRect = new Rectangle(
                (int)textPos.X - xPadding,
                (int)textPos.Y - yPadding,
                (int)textSize.X + xPadding * 2,
                (int)textSize.Y + yPadding * 2);
        }

        public bool Update()
        {
            foreach (TouchLocation touchLoc in TouchPanel.GetState())
            {
                Point pt = new Point((int)touchLoc.Position.X, (int)touchLoc.Position.Y);
                if (touchLoc.State == TouchLocationState.Released)
                {
                    if (windowRect.Contains(pt))
                    {
                        TrialMode.LaunchMarketplace();
                        return false;
                    }
                }
            }
            return true;
        }

        public void Render(Rectangle rect, SpriteBatch batch)
        {
            Color overlayColor = Color.Multiply(Color.Black, 0.8f);

            batch.Begin();

            batch.Draw(CardResources.BlankTex, viewRect, overlayColor);
            batch.Draw(backgroundTex, windowRect, Color.White);
            batch.DrawString(font, windowText, textPos, Color.White);
            
            batch.End();
        }
    }
}
