using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using System.Resources;

namespace Spider
{
    delegate void AnimationCompleteCallback(Animation animation);

    abstract class Animation
    {
        public abstract bool Update();
        public abstract void Render(SpriteBatch batch);

        public AnimationCompleteCallback OnAnimationCompleted;
    }

    class DealAnimation : Animation
    {
        private Board board;
        private List<CardAnimationView> cardAnimations;
        private DateTime stopTime;

        public DealAnimation(Board board, List<Card> cardsToDeal)
        {
            this.board = board;

            int cardCount = 0;
	        int stacksEmpty = 0;
	        for (int i=0; i < Board.StackCount; i++)
	        {
		        int stackSize = board.GetStack(i).Count;
		        if (stackSize == 0)
			        stacksEmpty++;
		        cardCount += stackSize;
	        }

            cardAnimations = new List<CardAnimationView>(Board.StackCount);

            // TODO: Revive this rule at some point?
	        /*if (stacksEmpty > 0 && cardCount >= Board.StackCount)
            {
                string errorMsg = CardResources.Strings.GetString("EmptyStacksDealError");
                board.View.AddError(errorMsg);
                return;
            }*/
	
	        int dealPos = board.CountOfExtraDealingsLeft() - 1;
	
	        double delay = 0.1;
	        double duration = 0.2;

            Rectangle bounds = board.View.GetViewArea();
            Point cardSize = board.View.GetCardSize();
            Point startPoint = new Point(bounds.Width - cardSize.X - dealPos * 25, bounds.Height - cardSize.Y);
	        int spacing = (bounds.Width - cardSize.X * Board.StackCount) / (Board.StackCount - 1);

            for (int i=0; i < cardsToDeal.Count; i++)
	        {
		        Card cardDealt = cardsToDeal[i];
		        cardDealt.Reveal();
                cardDealt.View.Animating = true;
		
                Point destPoint = board.View.GetLocationOfCardInStack(board.GetStack(i), board.GetStack(i).Count);
                CardAnimationView animation = new CardAnimationView(cardDealt, i * delay, duration, startPoint, destPoint, cardSize.X, cardSize.Y);
                cardAnimations.Insert(0, animation);
	        }
	
	        stopTime = DateTime.Now.AddSeconds(cardsToDeal.Count * delay + duration);
            Update();
        }

        public override bool Update()
        {
            foreach (CardAnimationView animation in cardAnimations)
                animation.Update();

            if (stopTime <= DateTime.Now || cardAnimations.Count == 0)
            {
                foreach (CardAnimationView animation in cardAnimations)
                    animation.Card.View.Animating = false;
                return false;
            }
            return true;
        }

        public override void Render(SpriteBatch batch)
        {
            foreach (CardAnimationView animation in cardAnimations)
                animation.Render(batch);
        }
    }

    class StackExpandAnimation : Animation
    {
        public List<CardAnimationView> cardAnimations;
        
        private Board board;
        private DateTime startTime;
        private DateTime stopTime;

        private const double duration = 0.2;
        private const int cardsPerRow = 6;

        public StackExpandAnimation(Board board, CardStack stack, Point ptExpand)
        {
            this.board = board;

            startTime = DateTime.Now;

            List<Card> cardsInStack = stack.GetCards();
            int top = stack.GetTopOfSequentialRun();
            cardsInStack.RemoveRange(0, top);

            int rows = (cardsInStack.Count + (cardsPerRow - 1)) / cardsPerRow;
            int cols = (cardsInStack.Count < cardsPerRow ? cardsInStack.Count : cardsPerRow);

            Rectangle bounds = board.View.GetViewArea();
            Point cardSize = board.View.GetCardSize();
            int spacing = (bounds.Width - cardSize.X * cardsPerRow) / (cardsPerRow - 1);
            spacing = Math.Min(spacing, 10);

            int xAnchor = ptExpand.X - cols / 2 * cardSize.X - (cols - 1) / 2 * spacing;
            xAnchor = Math.Max(xAnchor, 0);                                                       // Left edge collision
            xAnchor = Math.Min(xAnchor, bounds.Width - cardSize.X * cols - spacing * (cols - 1)); // Right edge collision
            int yAnchor = ptExpand.Y - cardSize.Y / 2;
            yAnchor = Math.Max(yAnchor, 0);                                                       // Top edge collision
            yAnchor = Math.Min(yAnchor, bounds.Height - cardSize.Y * rows - spacing * (rows - 1));// Bottom edge collision

            cardAnimations = new List<CardAnimationView>(cardsInStack.Count);
            for (int i = 0; i < cardsInStack.Count; i++)
            {
                Card card = cardsInStack[i];
                card.View.Animating = true;
                Point startPoint = board.View.GetLocationOfCardInStack(stack, i + top);
            
                int row = i / cardsPerRow;
                int col = i % cardsPerRow;
                
                int x = xAnchor + (cardSize.X + spacing) * col;
                int y = yAnchor + (cardSize.Y + spacing) * row;
                Point destPoint = new Point(x, y);

                CardAnimationView animation = new CardAnimationView(card, 0, duration, startPoint, destPoint, cardSize.X, cardSize.Y);
                cardAnimations.Add(animation);
            }

            stopTime = DateTime.Now.AddSeconds(duration);
            Update();
        }

        public override bool Update()
        {
            foreach (CardAnimationView animation in cardAnimations)
                animation.Update();

            if (stopTime <= DateTime.Now)
            {
                foreach (CardAnimationView animation in cardAnimations)
                    animation.Card.View.Animating = false;
                return false;
            }
            return true;
        }

        public override void Render(SpriteBatch batch)
        {
            TimeSpan elapsed = DateTime.Now - startTime;
            TimeSpan duration = stopTime - startTime;
            if (elapsed > duration)
                elapsed = duration;

            double tfactor = elapsed.TotalSeconds / duration.TotalSeconds;

            byte alpha = (byte)(128 * tfactor);
            batch.Begin();
            batch.Draw(CardResources.BlankTex, board.View.GetViewArea(), new Color(0, 0, 0, alpha));
            batch.End();

            foreach (CardAnimationView animation in cardAnimations)
                animation.Render(batch);
        }
    }

    class StackCollapseAnimation : Animation
    {
        private Board board;
        private List<CardAnimationView> cardAnimations;
        private DateTime startTime;
        private DateTime stopTime;

        private const double duration = 0.2;
        
        public StackCollapseAnimation(Board board, int stack, List<CardAnimationView> expandedCards)
        {
            this.board = board;

            startTime = DateTime.Now;

            Rectangle bounds = board.View.GetViewArea();
            Point cardSize = board.View.GetCardSize();
            int stackBase = board.GetStack(stack).Count - expandedCards.Count;
            
            cardAnimations = new List<CardAnimationView>(expandedCards.Count);
            for (int i = 0; i < expandedCards.Count; i++)
            {
                Card card = expandedCards[i].Card;
                card.View.Animating = true;

                Point destPoint = board.View.GetLocationOfCardInStack(board.GetStack(stack), i + stackBase);

                CardAnimationView animation = new CardAnimationView(card, 0, duration, expandedCards[i].CurrentRect.Location, destPoint, cardSize.X, cardSize.Y);
                cardAnimations.Add(animation);
            }

            stopTime = DateTime.Now.AddSeconds(duration);
            Update();
        }

        public override bool Update()
        {
            foreach (CardAnimationView animation in cardAnimations)
                animation.Update();

            if (stopTime <= DateTime.Now)
            {
                foreach (CardAnimationView animation in cardAnimations)
                    animation.Card.View.Animating = false;
                return false;
            }
            return true;
        }

        public override void Render(SpriteBatch batch)
        {
            TimeSpan elapsed = DateTime.Now - startTime;
            TimeSpan duration = stopTime - startTime;
            if (elapsed > duration)
                elapsed = duration;

            double tfactor = 1 - elapsed.TotalSeconds / duration.TotalSeconds;

            byte alpha = (byte)(128 * tfactor);
            batch.Begin();
            batch.Draw(CardResources.BlankTex, board.View.GetViewArea(), new Color(0, 0, 0, alpha));
            batch.End();

            foreach (CardAnimationView animation in cardAnimations)
                animation.Render(batch);
        }
    }

    class ClearRunAnimation : Animation
    {
        private Board board;
        public int Stack { get; set; }
        private List<CardAnimationView> cardAnimations;
        private List<CardAnimationView> completedAnimations;
        private DateTime stopTime;

        const double delay = 0.1;
        const double duration = 0.1;

        public ClearRunAnimation(Board board, int stack, Point destPoint)
        {
            this.board = board;
            Stack = stack;

            Rectangle bounds = board.View.GetViewArea();
            Point cardSize = board.View.GetCardSize();
	        int stackSize = board.GetStack(stack).Count;

	        cardAnimations = new List<CardAnimationView>(13);
	        for (int i=0; i < 13; i++)
	        {
		        int pos = stackSize - 13 + i;
                Card card = board.GetStack(stack).GetCard(pos);
                card.View.Animating = true;
                Point startPoint = board.View.GetLocationOfCardInStack(board.GetStack(stack), pos);

                CardAnimationView animation = new CardAnimationView(card, (13 - i) * delay, duration, startPoint, destPoint, cardSize.X, cardSize.Y);
                cardAnimations.Add(animation);
	        }
	
	        stopTime = DateTime.Now.AddSeconds(13 * delay + duration);
            completedAnimations = new List<CardAnimationView>(13);
            Update();
        }

        public override bool Update()
        {
            List<CardAnimationView> newlyCompletedAnimations = new List<CardAnimationView>();
            foreach (CardAnimationView animation in cardAnimations)
            {
                if (animation.Update() == false)
                    newlyCompletedAnimations.Add(animation);
            }
            foreach (CardAnimationView animation in newlyCompletedAnimations)
                cardAnimations.Remove(animation);
            completedAnimations.AddRange(newlyCompletedAnimations);

            if (stopTime <= DateTime.Now)
            {
                foreach (CardAnimationView animation in cardAnimations)
                    animation.Card.View.Animating = false;
                return false;
            }
            return true;
        }

        public override void Render(SpriteBatch batch)
        {
            foreach (CardAnimationView animation in completedAnimations)
                animation.Render(batch);
            foreach (CardAnimationView animation in cardAnimations)
                animation.Render(batch);
        }
    }

    class ShowMoveAnimation : Animation
    {
        public ShowMoveAnimation(Board board, int stackSrc, int stackDest)
        {
            Update();
        }

        public override bool Update()
        {
            return false;
        }

        public override void Render(SpriteBatch batch)
        {
        }
    }

    class WinAnimation : Animation
    {
        Animation winAnimation;

        public WinAnimation(Board board)
        {
            Random r = new Random();
            if (r.Next() % 2 == 0)
                winAnimation = new RocketWinAnimation(board);
            else
                winAnimation = new FireworksWinAnimation(board);

            Update();
        }

        public override bool Update()
        {
            return winAnimation.Update();
        }

        public override void Render(SpriteBatch batch)
        {
            winAnimation.Render(batch);
        }
    }

    class RocketWinAnimation : Animation
    {
        private Board board;
        private DateTime startTime;

        Random random;
        private int radius;
        private Rectangle rocketRect;
        private float rocketRot;

        private List<RocketPuff> rocketPuffs;
        private DateTime nextPuffTime = DateTime.MinValue;

        const float rocketDuration = 4.0f;
        const float minPuffDelay = 0.04f;
        const float maxPuffDelay = 0.12f;

        public RocketWinAnimation(Board board)
        {
            this.board = board;

            startTime = DateTime.Now;
            random = new Random();
            rocketPuffs = new List<RocketPuff>(20);

            Rectangle bounds = board.View.GetViewArea();
            radius = (int)(Math.Min(bounds.Height, bounds.Width / 2) * 0.9);
            Update();
        }

        public override bool Update()
        {
            TimeSpan elapsed = DateTime.Now - startTime;

            List<RocketPuff> invisiblePuffs = new List<RocketPuff>(rocketPuffs.Count);
            foreach (RocketPuff puff in rocketPuffs)
            {
                if (!puff.Update())
                    invisiblePuffs.Add(puff);
            }
            foreach (RocketPuff puff in invisiblePuffs)
                rocketPuffs.Remove(puff);

            if (elapsed.TotalSeconds < rocketDuration)
            {
                Rectangle bounds = board.View.GetViewArea();
                double theta = Math.PI * (elapsed.TotalSeconds / rocketDuration);
                int x = (int)(Math.Cos(theta) * radius + bounds.Width / 2);
                int y = bounds.Height - (int)(Math.Sin(theta) * radius) + 60;

                rocketRect = new Rectangle(x, y, 60, 60);
                rocketRot = -(float)theta;

                if (DateTime.Now > nextPuffTime)
                {
                    theta = Math.PI * ((elapsed.TotalSeconds - 0.1) / rocketDuration);
                    x = (int)(Math.Cos(theta) * radius + bounds.Width / 2) - 10;
                    y = bounds.Height - (int)(Math.Sin(theta) * radius) - 10 + 60;

                    int index = new Random().Next(CardResources.PuffTex.Count) + 1;

                    Rectangle rect = new Rectangle(x, y, 20, 20);
                    float scale = 1.5f;
                    RocketPuff puff = new RocketPuff(rect, scale, CardResources.PuffTex[random.Next(CardResources.PuffTex.Count)]);
                    rocketPuffs.Add(puff);

                    nextPuffTime = DateTime.Now.AddSeconds(random.NextDouble() * (maxPuffDelay - minPuffDelay) + minPuffDelay);
                }
            }

            if (elapsed.TotalSeconds >= rocketDuration && rocketPuffs.Count == 0)
            {
                return false;
            }
            return true;
        }

        public override void Render(SpriteBatch batch)
        {
            foreach (RocketPuff puff in rocketPuffs)
                puff.Render(batch);

            Vector2 origin = new Vector2(CardResources.RocketTex.Width / 2, CardResources.RocketTex.Height / 2);
            batch.Begin();
            batch.Draw(CardResources.RocketTex, rocketRect, CardResources.RocketTex.Bounds, Color.White, rocketRot, origin, SpriteEffects.None, 0);
            batch.End();
        }
    }
    
    class FireworksWinAnimation : Animation
    {
        private Board board;
        private DateTime startTime;

        private Random random;
        private Firework firework;
        private List<FireworkParticle> particles;
        private DateTime lastFireworkLaunchTime = DateTime.MinValue;

        Color[] fireworkColors = new Color[] { Color.Red, Color.Blue, Color.White, Color.Purple };

        const int particlesPerFirework = 300;
        Vector2 fireworkVelocity = new Vector2(0, -40);
        Vector2 fireworkAccel = new Vector2(0, -120);
        const float fireworkLifespan = 2.0f;
        Vector2 fireworkSize = new Vector2(40, 40);

        const float fireworkParticleStartVelocity = 450.0f;
        const float fireworkParticleFriction = 2.5f;
        Vector2 fireworkParticleSize = new Vector2(20, 20);
        const float particleLifespan = 1.8f;
        Vector2 fireworkParticleGravity = new Vector2(0, 60);

        public FireworksWinAnimation(Board board)
        {
            this.board = board;

            startTime = DateTime.Now;
            random = new Random();
            particles = new List<FireworkParticle>(particlesPerFirework);

            Update();
        }

        public override bool Update()
        {
            // Launch firework
            if (firework == null && particles.Count == 0)
            {
                Rectangle viewRect = board.View.GetViewArea();
                
                firework = new Firework(new Vector2(viewRect.Width / 2 - fireworkSize.X / 2, viewRect.Height), fireworkSize, fireworkVelocity, fireworkAccel, 0.0f, Color.White, Color.White, CardResources.RocketTex, fireworkLifespan);
            }

            // Explode firework
            if (firework != null)
            {
                if (!firework.Update())
                {
                    Color color = fireworkColors[random.Next(fireworkColors.Length)];
                    Color endColor = Color.Multiply(color, 0.3f);
                    Texture2D tex = CardResources.FireworkParticleTex[random.Next(2)];
                    for (int i = 0; i < particlesPerFirework; i++)
                    {
                        double theta = random.NextDouble() * Math.PI * 2.0;
                        float velMult = (float)random.NextDouble();
                        Vector2 vel = new Vector2((float)Math.Cos(theta), (float)Math.Sin(theta)) * fireworkParticleStartVelocity * velMult;

                        FireworkParticle particle = new FireworkParticle(firework.Position, fireworkParticleSize, vel, fireworkParticleGravity, fireworkParticleFriction, color, endColor, tex, particleLifespan);
                        particles.Add(particle);
                    }

                    firework = null;
                }
            }
            
            // Dead particles stay in the list - too much work to remove the dead ones every update
            bool someParticlesAlive = false;
            foreach (FireworkParticle particle in particles)
                someParticlesAlive |= particle.Update();

            if (firework == null && !someParticlesAlive)
            {
                return false;
            }
            return true;
        }

        public override void Render(SpriteBatch batch)
        {
            batch.Begin();
            
            if (firework != null)
                firework.Render(batch);

            foreach (FireworkParticle particle in particles)
                particle.Render(batch);
            
            batch.End();
        }
    }

    class CardAnimationView
    {
        public Card Card { get; private set; }
        public Rectangle CurrentRect { get; private set; }

        public bool Started { get { return DateTime.Now >= startTime; } }
        public bool Complted { get { return DateTime.Now >= destTime; } }
        
        private Point startPoint;
	    private DateTime startTime;
	    private Point destPoint;
	    private DateTime destTime;

        public CardAnimationView(Card c, double delay, double duration, Point start, Point end, int width, int height)
        {
            Card = c;
            startPoint = start;
            destPoint = end;
            startTime = DateTime.Now.AddSeconds(delay);
            destTime = startTime.AddSeconds(duration);
            CurrentRect = new Rectangle(startPoint.X, startPoint.Y, width, height);

            Update();
        }

        public bool Update()
        {
            TimeSpan elapsed = DateTime.Now - startTime;
            TimeSpan duration = destTime - startTime;
            bool running = (elapsed <= duration);
            if (elapsed < TimeSpan.Zero)
                elapsed = TimeSpan.Zero;
            if (elapsed > duration)
                elapsed = duration;

            double tfactor = elapsed.TotalSeconds / duration.TotalSeconds;
            Point currentPoint = new Point(
                (int)((destPoint.X - startPoint.X) * tfactor + startPoint.X),
                (int)((destPoint.Y - startPoint.Y) * tfactor + startPoint.Y));
            CurrentRect = new Rectangle(currentPoint.X, currentPoint.Y, CurrentRect.Width, CurrentRect.Height);
            Card.View.Rect = CurrentRect;
            return running;
        }

        public void Render(SpriteBatch batch)
        {
            Card.View.Render(batch);
        }
    }

    class RocketPuff
    {
        private Rectangle rect;
        private Texture2D tex;
        private float alpha;
        private float scaleFactor;
        private DateTime startTime;
        
        const float duration = 0.6f;

        public RocketPuff(Rectangle rect, float scale, Texture2D tex)
        {
            this.rect = rect;
            this.tex = tex;
            alpha = 1.0f;
            scaleFactor = scale;
            startTime = DateTime.Now;
        }

        public bool Update()
        {
	        double t = (DateTime.Now - startTime).TotalSeconds;
	        alpha = (float)(1.0 - t / duration);
	        double currentScale = 1.0 + (t / duration) * (scaleFactor - 1.0);
	        int size = (int)(20 * currentScale);
            Point center = new Point(rect.X + rect.Width / 2, rect.Y + rect.Height / 2);
	        rect = new Rectangle(center.X - size / 2, center.Y - size / 2, size, size);
	        if (t > duration)
		        return false;
	        return true;
        }

        public void Render(SpriteBatch batch)
        {
            batch.Begin();
            batch.Draw(tex, rect, Color.Multiply(Color.White, alpha));
            batch.End();
        }
    }

    class Particle
    {
        protected Vector2 pos;
        protected Vector2 size;
        protected Vector2 vel;
        protected Vector2 accel;
        protected float friction;
        protected Color startColor;
        protected Color endColor;
        protected Texture2D tex;
        protected float lifespan;

        bool alive;
        DateTime startTime;
        DateTime lastUpdateTime;
        Color currentColor;

        public Particle(Vector2 pos, Vector2 size, Vector2 vel, Vector2 accel, float friction, Color startColor, Color endColor, Texture2D tex, float lifespan)
        {
            this.pos = pos;
            this.size = size;
            this.vel = vel;
            this.accel = accel;
            this.friction = friction;
            this.startColor = startColor;
            this.endColor = endColor;
            this.tex = tex;
            this.lifespan = lifespan;

            this.alive = true;
            this.startTime = DateTime.Now;
            this.lastUpdateTime = DateTime.Now;
            this.currentColor = this.startColor;
        }

        public bool Update()
        {
            if (!alive)
                return false;

            double t = (DateTime.Now - startTime).TotalSeconds;
            double percent = (t / lifespan);
            double delta = (DateTime.Now - lastUpdateTime).TotalSeconds;

            pos += vel * (float)delta;
            vel += accel * (float)delta;
            vel *= 1 - Math.Min(friction * (float)delta, 1.0f);

            currentColor = new Color((endColor.ToVector4() - startColor.ToVector4()) * (float)percent + startColor.ToVector4());
            lastUpdateTime = DateTime.Now;

            if (t > lifespan)
                alive = false;
            return alive;
        }

        public void Render(SpriteBatch batch)
        {
            if (alive)
            {
                Rectangle rect = new Rectangle((int)pos.X, (int)pos.Y, (int)size.X, (int)size.Y);
                batch.Draw(tex, rect, currentColor);
            }
        }
    }

    class Firework : Particle
    {
        public Firework(Vector2 pos, Vector2 size, Vector2 vel, Vector2 accel, float friction, Color startColor, Color endColor, Texture2D tex, float lifespan) :
            base(pos, size, vel, accel, friction, startColor, endColor, tex, lifespan)
        {
        }

        public Vector2 Position { get { return pos; } }
    }

    class FireworkParticle : Particle
    {
        public FireworkParticle(Vector2 pos, Vector2 size, Vector2 vel, Vector2 accel, float friction, Color startColor, Color endColor, Texture2D tex, float lifespan) :
            base(pos, size, vel, accel, friction, startColor, endColor, tex, lifespan)
        {
        }
    }
}
