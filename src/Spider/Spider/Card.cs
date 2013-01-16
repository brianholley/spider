using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using System.Resources;
using System.Reflection;

namespace Spider
{
    enum Suit
    {
	    Spade,
	    Diamond,
	    Club,
	    Heart,
    }

    enum Value
    {
	    Ace,
	    Two,
	    Three,
	    Four,
	    Five,
	    Six,
	    Seven,
	    Eight,
	    Nine,
	    Ten,
	    Jack,
	    Queen,
	    King,
    }

    // TODO:Later: Refactor these into a better grouping of classes
    class CardResources
    {
        //public static string[] valueNames = { @"Ace", @"Two", @"Three", @"Four", @"Five", @"Six", @"Seven", @"Eight", @"Nine", @"Ten", @"Jack", @"Queen", @"King" };
        private static string[] valueNamesShort = { @"A", @"2", @"3", @"4", @"5", @"6", @"7", @"8", @"9", @"10", @"J", @"Q", @"K" };
        //public static string[] suitNames = { @"Spades", @"Diamonds", @"Clubs", @"Hearts" };
        private static string[] suitNamesShort = { @"Spade", @"Diamond", @"Club", @"Heart" };
        
        public static Texture2D CardTex { get; private set; }
        public static Texture2D CardBackTex { get; private set; }
        public static Texture2D HighlightEndTex { get; private set; }
        public static Texture2D HightlightCenterTex { get; private set; }
        public static Texture2D PlaceholderTex { get; private set; }
        public static List<Texture2D> SuitTex { get; private set; }
        public static List<Texture2D> ValueTex { get; private set; }
        public static Texture2D GradientTex { get; private set; }
        public static Texture2D BlankTex { get; private set; }

        public static Texture2D UndoTex { get; private set; }

        public static SpriteFont WinFont { get; private set; }
        public static SpriteFont AgainFont { get; private set; }
        public static Texture2D RocketTex { get; private set; }
        public static List<Texture2D> PuffTex { get; private set; }
        public static List<Texture2D> FireworkParticleTex { get; private set; }

        public static int ContentCount() { return 1 + 1 + 3 + suitNamesShort.Length + valueNamesShort.Length + 8 + 2; }
        public static void LoadResources(GraphicsDevice graphicsDevice, ContentManager content, ContentLoadNotificationDelegate callback)
        {
            CardTex = content.Load<Texture2D>(@"Card\Card");
            callback();

            CardBackTex = content.Load<Texture2D>(@"Card\CardBack_White");
            callback();

            HighlightEndTex = content.Load<Texture2D>(@"Card\Highlight_End");
            callback();
            HightlightCenterTex = content.Load<Texture2D>(@"Card\Highlight_Center");
            callback();
            PlaceholderTex = content.Load<Texture2D>(@"Card\Placeholder");
            callback();

            List<Texture2D> suitTex = new List<Texture2D>(suitNamesShort.Length);
            foreach (string suit in suitNamesShort)
            {
                suitTex.Add(content.Load<Texture2D>(@"Card\" + suit));
                callback();
            }
            SuitTex = suitTex;

            List<Texture2D> valueTex = new List<Texture2D>(valueNamesShort.Length);
            foreach (string value in valueNamesShort)
            {
                valueTex.Add(content.Load<Texture2D>(@"Card\" + value));
                callback();
            }
            ValueTex = valueTex;

            GradientTex = content.Load<Texture2D>("Gradient");
            callback();
            BlankTex = new Texture2D(graphicsDevice, 1, 1, false, SurfaceFormat.Color);
            BlankTex.SetData<Color>(new Color[] { Color.White });
            callback();

            UndoTex = content.Load<Texture2D>("Undo");
            callback();

            WinFont = content.Load<SpriteFont>(@"Win\WinFont");
            callback();
            AgainFont = content.Load<SpriteFont>(@"Win\AgainFont");
            callback();
            RocketTex = content.Load<Texture2D>(@"Win\Rocket");
            callback();
            List<Texture2D> puffTex = new List<Texture2D>();
            for (int i = 0; i < 2; i++)
            {
                puffTex.Add(content.Load<Texture2D>(@"Win\Puff" + (i + 1)));
                callback();
            }
            PuffTex = puffTex;
            List<Texture2D> particleTex = new List<Texture2D>();
            for (int i = 0; i < 2; i++)
            {
                particleTex.Add(content.Load<Texture2D>(@"Win\Firework" + (i + 1)));
                callback();
            }
            FireworkParticleTex = particleTex;
        }
    }

    class Card
    {
	    public Suit Suit { get; private set; }
	    public Value Value { get; private set; }
	    public bool Visible { get; set; }

        public CardView View { get; set; }
	
	    public double RandomSeed { get; private set; }	// For shuffling purposes

        public Card(Suit suit, Value value, Random random)
        {
            Suit = suit;
            Value = value;

            RandomSeed = random.NextDouble();
        }

        public Card(Card cardOrig)
        {
            Suit = cardOrig.Suit;
            Value = cardOrig.Value;
            Visible = cardOrig.Visible;
        }

        public void Reveal()
        {
            Visible = true;
        }
    }
}
