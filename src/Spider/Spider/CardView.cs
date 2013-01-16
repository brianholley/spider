using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Spider
{
    class CardView
    {
        public static Color MutedBackColor { get; set; }

        public Card Card { get; private set; }
        public Rectangle Rect { get; set; }
        public bool Animating { get; set; }
        public bool Highlighted { get; set; }

        public CardView(Card card)
        {
            Card = card;
            Highlighted = true;
        }

        public void Render(SpriteBatch batch)
        {
            Color color = (Highlighted ? Color.White : Color.DarkGray);
            Color backColor = (Highlighted ? Options.CardBackColor : CardView.MutedBackColor);

            // TODO:Later: Face cards?
            if (Card.Visible)
            {
                batch.Begin();
                batch.Draw(CardResources.CardTex, Rect, color);

                int valueWidth = Rect.Width / 6 + 3;
                int valueHeight = valueWidth * 55 / 45;
                Rectangle numberRect = new Rectangle(Rect.X + 5, Rect.Y + 5, valueWidth, valueHeight);
                batch.Draw(CardResources.ValueTex[(int)Card.Value], numberRect, color);

                Rectangle suitSmallRect = new Rectangle(numberRect.X + numberRect.Width, numberRect.Y, numberRect.Height, numberRect.Height);
                batch.Draw(CardResources.SuitTex[(int)Card.Suit], suitSmallRect, color);

                Rectangle numberTexRect = CardResources.ValueTex[(int)Card.Value].Bounds;
                Rectangle numberBottomRect = new Rectangle(Rect.Right - numberRect.Width - 5, Rect.Bottom - numberRect.Height - 5, numberRect.Width, numberRect.Height);
                batch.Draw(CardResources.ValueTex[(int)Card.Value], numberBottomRect, null, color, 0.0f, Vector2.Zero, SpriteEffects.FlipHorizontally | SpriteEffects.FlipVertically, 0);

                Rectangle suitTexRect = CardResources.SuitTex[(int)Card.Suit].Bounds;
                Rectangle suitBottomRect = new Rectangle(numberBottomRect.Left - suitSmallRect.Width, Rect.Bottom - suitSmallRect.Height - 5, suitSmallRect.Width, suitSmallRect.Height);
                batch.Draw(CardResources.SuitTex[(int)Card.Suit], suitBottomRect, null, color, 0.0f, Vector2.Zero, SpriteEffects.FlipHorizontally | SpriteEffects.FlipVertically, 0);
                
                Rectangle suitLargeRect = new Rectangle(Rect.X + Rect.Width / 6, Rect.Y + Rect.Height / 2 - (Rect.Width / 3), 2 * Rect.Width / 3, 2 * Rect.Width / 3);
                batch.Draw(CardResources.SuitTex[(int)Card.Suit], suitLargeRect, color);

                batch.End();
            }
            else
            {
                batch.Begin();
                batch.Draw(CardResources.CardBackTex, Rect, backColor);
                batch.End();
            }
        }
    }
}
