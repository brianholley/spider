using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;

namespace Spider
{
	internal delegate void AnimationCompleteCallback(Animation animation);

	internal abstract class Animation
	{
		public abstract bool Update();
		public abstract void Render(SpriteBatch batch);

		public AnimationCompleteCallback OnAnimationCompleted;
	}

	internal class DealAnimation : Animation
	{
		private Board _board;
		private readonly List<CardAnimationView> _cardAnimations;
		private readonly DateTime _stopTime;

		public DealAnimation(Board board, List<Card> cardsToDeal)
		{
			_board = board;

			_cardAnimations = new List<CardAnimationView>(Board.StackCount);

			// TODO: Revive this rule at some point?
			/*int cardCount = 0;
			int stacksEmpty = 0;
			for (int i = 0; i < Board.StackCount; i++)
			{
				int stackSize = board.GetStack(i).Count;
				if (stackSize == 0)
					stacksEmpty++;
				cardCount += stackSize;
			}

			if (stacksEmpty > 0 && cardCount >= Board.StackCount)
			{
				string errorMsg = CardResources.Strings.GetString("EmptyStacksDealError");
				_board.View.AddError(errorMsg);
				return;
			}*/

			int dealPos = board.CountOfExtraDealingsLeft() - 1;

			const double delay = 0.1;
			const double duration = 0.2;

			var bounds = board.View.GetViewArea();
			var cardSize = board.View.GetCardSize();
			var startPoint = new Point(bounds.Width - cardSize.X - dealPos*25, bounds.Height - cardSize.Y);

			for (int i = 0; i < cardsToDeal.Count; i++)
			{
				Card cardDealt = cardsToDeal[i];
				cardDealt.Reveal();
				cardDealt.View.Animating = true;

				Point destPoint = board.View.GetLocationOfCardInStack(board.GetStack(i), board.GetStack(i).Count);
				var animation = new CardAnimationView(cardDealt, i*delay, duration, startPoint, destPoint, cardSize.X,
					cardSize.Y);
				_cardAnimations.Insert(0, animation);
			}

			_stopTime = DateTime.Now.AddSeconds(cardsToDeal.Count*delay + duration);
			UpdateCardAnimations();
		}

		public override bool Update()
		{
			return UpdateCardAnimations();
		}

		private bool UpdateCardAnimations()
		{
			foreach (var animation in _cardAnimations)
				animation.Update();

			if (_stopTime <= DateTime.Now || _cardAnimations.Count == 0)
			{
				foreach (var animation in _cardAnimations)
					animation.Card.View.Animating = false;
				return false;
			}
			return true;
		}

		public override void Render(SpriteBatch batch)
		{
			foreach (var animation in _cardAnimations)
				animation.Render(batch);
		}
	}

	internal class StackExpandAnimation : Animation
	{
		public List<CardAnimationView> CardAnimations;

		private readonly Board _board;
		private readonly DateTime _startTime;
		private readonly DateTime _stopTime;

		private const double Duration = 0.2;
		private const int CardsPerRow = 6;

		public StackExpandAnimation(Board board, CardStack stack, Point ptExpand)
		{
			_board = board;

			_startTime = DateTime.Now;

			var cardsInStack = stack.GetCards();
			int top = stack.GetTopOfSequentialRun();
			cardsInStack.RemoveRange(0, top);

			int rows = (cardsInStack.Count + (CardsPerRow - 1))/CardsPerRow;
			int cols = (cardsInStack.Count < CardsPerRow ? cardsInStack.Count : CardsPerRow);

			var bounds = board.View.GetViewArea();
			var cardSize = board.View.GetCardSize();
			int spacing = (bounds.Width - cardSize.X*CardsPerRow)/(CardsPerRow - 1);
			spacing = Math.Min(spacing, 10);

			int xAnchor = ptExpand.X - cols/2*cardSize.X - (cols - 1)/2*spacing;
			xAnchor = Math.Max(xAnchor, 0); // Left edge collision
			xAnchor = Math.Min(xAnchor, bounds.Width - cardSize.X*cols - spacing*(cols - 1)); // Right edge collision
			int yAnchor = ptExpand.Y - cardSize.Y/2;
			yAnchor = Math.Max(yAnchor, 0); // Top edge collision
			yAnchor = Math.Min(yAnchor, bounds.Height - cardSize.Y*rows - spacing*(rows - 1)); // Bottom edge collision

			CardAnimations = new List<CardAnimationView>(cardsInStack.Count);
			for (int i = 0; i < cardsInStack.Count; i++)
			{
				var card = cardsInStack[i];
				card.View.Animating = true;
				var startPoint = board.View.GetLocationOfCardInStack(stack, i + top);

				int row = i/CardsPerRow;
				int col = i%CardsPerRow;

				int x = xAnchor + (cardSize.X + spacing)*col;
				int y = yAnchor + (cardSize.Y + spacing)*row;
				var destPoint = new Point(x, y);

				var animation = new CardAnimationView(card, 0, Duration, startPoint, destPoint, cardSize.X, cardSize.Y);
				CardAnimations.Add(animation);
			}

			_stopTime = DateTime.Now.AddSeconds(Duration);
			UpdateCardAnimations();
		}

		public override bool Update()
		{
			return UpdateCardAnimations();
		}

		private bool UpdateCardAnimations()
		{
			foreach (var animation in CardAnimations)
				animation.Update();

			if (_stopTime <= DateTime.Now)
			{
				foreach (var animation in CardAnimations)
					animation.Card.View.Animating = false;
				return false;
			}
			return true;
		}

		public override void Render(SpriteBatch batch)
		{
			var elapsed = DateTime.Now - _startTime;
			var duration = _stopTime - _startTime;
			if (elapsed > duration)
				elapsed = duration;

			double tfactor = elapsed.TotalSeconds/duration.TotalSeconds;

			byte alpha = (byte) (128*tfactor);
			batch.Begin();
			batch.Draw(CardResources.BlankTex, _board.View.GetViewArea(), new Color(0, 0, 0, alpha));
			batch.End();

			foreach (var animation in CardAnimations)
				animation.Render(batch);
		}
	}

	internal class StackCollapseAnimation : Animation
	{
		private readonly Board _board;
		private readonly List<CardAnimationView> _cardAnimations;
		private readonly DateTime _startTime;
		private readonly DateTime _stopTime;

		private const double Duration = 0.2;

		public StackCollapseAnimation(Board board, int stack, List<CardAnimationView> expandedCards)
		{
			_board = board;

			_startTime = DateTime.Now;

			var cardSize = board.View.GetCardSize();
			int stackBase = board.GetStack(stack).Count - expandedCards.Count;

			_cardAnimations = new List<CardAnimationView>(expandedCards.Count);
			for (int i = 0; i < expandedCards.Count; i++)
			{
				var card = expandedCards[i].Card;
				card.View.Animating = true;

				var destPoint = board.View.GetLocationOfCardInStack(board.GetStack(stack), i + stackBase);

				var animation = new CardAnimationView(card, 0, Duration, expandedCards[i].CurrentRect.Location,
					destPoint, cardSize.X, cardSize.Y);
				_cardAnimations.Add(animation);
			}

			_stopTime = DateTime.Now.AddSeconds(Duration);
			UpdateCardAnimations();
		}

		public override bool Update()
		{
			return UpdateCardAnimations();
		}

		private bool UpdateCardAnimations()
		{
			foreach (var animation in _cardAnimations)
				animation.Update();

			if (_stopTime <= DateTime.Now)
			{
				foreach (var animation in _cardAnimations)
					animation.Card.View.Animating = false;
				return false;
			}
			return true;
		}

		public override void Render(SpriteBatch batch)
		{
			var elapsed = DateTime.Now - _startTime;
			var duration = _stopTime - _startTime;
			if (elapsed > duration)
				elapsed = duration;

			double tfactor = 1 - elapsed.TotalSeconds/duration.TotalSeconds;

			byte alpha = (byte) (128*tfactor);
			batch.Begin();
			batch.Draw(CardResources.BlankTex, _board.View.GetViewArea(), new Color(0, 0, 0, alpha));
			batch.End();

			foreach (var animation in _cardAnimations)
				animation.Render(batch);
		}
	}

	internal class ClearRunAnimation : Animation
	{
		public int Stack { get; set; }
		private readonly List<CardAnimationView> _cardAnimations;
		private readonly List<CardAnimationView> _completedAnimations;
		private readonly DateTime _stopTime;

		private const double Delay = 0.1;
		private const double Duration = 0.1;

		public ClearRunAnimation(Board board, int stack, Point destPoint)
		{
			Stack = stack;

			var cardSize = board.View.GetCardSize();
			int stackSize = board.GetStack(stack).Count;

			_cardAnimations = new List<CardAnimationView>(13);
			for (int i = 0; i < 13; i++)
			{
				int pos = stackSize - 13 + i;
				var card = board.GetStack(stack).GetCard(pos);
				card.View.Animating = true;
				var startPoint = board.View.GetLocationOfCardInStack(board.GetStack(stack), pos);

				var animation = new CardAnimationView(card, (13 - i)*Delay, Duration, startPoint, destPoint, cardSize.X, cardSize.Y);
				_cardAnimations.Add(animation);
			}

			_stopTime = DateTime.Now.AddSeconds(13*Delay + Duration);
			_completedAnimations = new List<CardAnimationView>(13);
			UpdateCardAnimations();
		}

		public override bool Update()
		{
			return UpdateCardAnimations();
		}

		private bool UpdateCardAnimations()
		{
			var newlyCompletedAnimations = new List<CardAnimationView>();
			foreach (CardAnimationView animation in _cardAnimations)
			{
				if (animation.Update() == false)
					newlyCompletedAnimations.Add(animation);
			}
			foreach (var animation in newlyCompletedAnimations)
				_cardAnimations.Remove(animation);
			_completedAnimations.AddRange(newlyCompletedAnimations);

			if (_stopTime <= DateTime.Now)
			{
				foreach (var animation in _cardAnimations)
					animation.Card.View.Animating = false;
				return false;
			}
			return true;
		}

		public override void Render(SpriteBatch batch)
		{
			foreach (var animation in _completedAnimations)
				animation.Render(batch);
			foreach (var animation in _cardAnimations)
				animation.Render(batch);
		}
	}

	internal class ShowMoveAnimation : Animation
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

	internal class WinAnimation : Animation
	{
		private readonly Animation _winAnimation;

		public WinAnimation(Board board)
		{
			var r = new Random();
			if (r.Next()%2 == 0)
				_winAnimation = new RocketWinAnimation(board);
			else
				_winAnimation = new FireworksWinAnimation(board);

			Update();
		}

		public override bool Update()
		{
			return _winAnimation.Update();
		}

		public override void Render(SpriteBatch batch)
		{
			_winAnimation.Render(batch);
		}
	}

	internal class RocketWinAnimation : Animation
	{
		private readonly Board _board;
		private readonly DateTime _startTime;

		private readonly Random _random;
		private readonly int _radius;
		private Rectangle _rocketRect;
		private float _rocketRot;

		private readonly List<RocketPuff> _rocketPuffs;
		private DateTime _nextPuffTime = DateTime.MinValue;

		private const float RocketDuration = 4.0f;
		private const float MinPuffDelay = 0.04f;
		private const float MaxPuffDelay = 0.12f;
		private const float PuffScale = 1.5f;
					
		public RocketWinAnimation(Board board)
		{
			_board = board;

			_startTime = DateTime.Now;
			_random = new Random();
			_rocketPuffs = new List<RocketPuff>(20);

			Rectangle bounds = board.View.GetViewArea();
			_radius = (int) (Math.Min(bounds.Height, bounds.Width/2)*0.9);
			Update();
		}

		public override bool Update()
		{
			var elapsed = DateTime.Now - _startTime;

			var invisiblePuffs = new List<RocketPuff>(_rocketPuffs.Count);
			foreach (RocketPuff puff in _rocketPuffs)
			{
				if (!puff.Update())
					invisiblePuffs.Add(puff);
			}
			foreach (var puff in invisiblePuffs)
				_rocketPuffs.Remove(puff);

			if (elapsed.TotalSeconds < RocketDuration)
			{
				var bounds = _board.View.GetViewArea();
				double theta = Math.PI*(elapsed.TotalSeconds/RocketDuration);
				int x = (int) (Math.Cos(theta)*_radius + bounds.Width/2.0f);
				int y = bounds.Height - (int) (Math.Sin(theta)*_radius) + 60;

				_rocketRect = new Rectangle(x, y, 60, 60);
				_rocketRot = -(float) theta;

				if (DateTime.Now > _nextPuffTime)
				{
					theta = Math.PI*((elapsed.TotalSeconds - 0.1)/RocketDuration);
					x = (int) (Math.Cos(theta)*_radius + bounds.Width/2.0f) - 10;
					y = bounds.Height - (int) (Math.Sin(theta)*_radius) - 10 + 60;

					var rect = new Rectangle(x, y, 20, 20);
					var puff = new RocketPuff(rect, PuffScale, CardResources.PuffTex[_random.Next(CardResources.PuffTex.Count)]);
					_rocketPuffs.Add(puff);

					_nextPuffTime = DateTime.Now.AddSeconds(_random.NextDouble()*(MaxPuffDelay - MinPuffDelay) + MinPuffDelay);
				}
			}

			if (elapsed.TotalSeconds >= RocketDuration && _rocketPuffs.Count == 0)
			{
				return false;
			}
			return true;
		}

		public override void Render(SpriteBatch batch)
		{
			foreach (var puff in _rocketPuffs)
				puff.Render(batch);

			var origin = new Vector2(CardResources.RocketTex.Width/2.0f, CardResources.RocketTex.Height/2.0f);
			batch.Begin();
			batch.Draw(CardResources.RocketTex, _rocketRect, CardResources.RocketTex.Bounds, Color.White, _rocketRot, origin,
				SpriteEffects.None, 0);
			batch.End();
		}
	}

	internal sealed class FireworksWinAnimation : Animation
	{
		private readonly Board _board;

		private readonly Random _random;
		private Firework _firework;
		private readonly List<FireworkParticle> _particles;

		private readonly Color[] _fireworkColors = {Color.Red, Color.Blue, Color.White, Color.Purple};

		private const int ParticlesPerFirework = 300;
		private readonly Vector2 _fireworkVelocity = new Vector2(0, -40);
		private readonly Vector2 _fireworkAccel = new Vector2(0, -120);
		private const float FireworkLifespan = 2.0f;
		private readonly Vector2 _fireworkSize = new Vector2(40, 40);

		private const float FireworkParticleStartVelocity = 450.0f;
		private const float FireworkParticleFriction = 2.5f;
		private readonly Vector2 _fireworkParticleSize = new Vector2(20, 20);
		private const float ParticleLifespan = 1.8f;
		private readonly Vector2 _fireworkParticleGravity = new Vector2(0, 60);

		public FireworksWinAnimation(Board board)
		{
			_board = board;

			_random = new Random();
			_particles = new List<FireworkParticle>(ParticlesPerFirework);

			Update();
		}

		public override bool Update()
		{
			// Launch firework
			if (_firework == null && _particles.Count == 0)
			{
				Rectangle viewRect = _board.View.GetViewArea();

				_firework = new Firework(new Vector2(viewRect.Width/2.0f - _fireworkSize.X/2, viewRect.Height), _fireworkSize,
					_fireworkVelocity, _fireworkAccel, 0.0f, Color.White, Color.White, CardResources.RocketTex, FireworkLifespan);
			}

			// Explode firework
			if (_firework != null)
			{
				if (!_firework.Update())
				{
					var color = _fireworkColors[_random.Next(_fireworkColors.Length)];
					var endColor = Color.Multiply(color, 0.3f);
					var tex = CardResources.FireworkParticleTex[_random.Next(2)];
					for (int i = 0; i < ParticlesPerFirework; i++)
					{
						double theta = _random.NextDouble()*Math.PI*2.0;
						double velMult = _random.NextDouble();
						var vel = new Vector2((float) Math.Cos(theta), (float) Math.Sin(theta)) * FireworkParticleStartVelocity * (float)velMult;

						var particle = new FireworkParticle(_firework.Position, _fireworkParticleSize, vel, _fireworkParticleGravity, FireworkParticleFriction, color, endColor, tex, ParticleLifespan);
						_particles.Add(particle);
					}

					_firework = null;
				}
			}

			// Dead particles stay in the list - too much work to remove the dead ones every update
			bool someParticlesAlive = false;
			foreach (var particle in _particles)
				someParticlesAlive |= particle.Update();

			if (_firework == null && !someParticlesAlive)
			{
				return false;
			}
			return true;
		}

		public override void Render(SpriteBatch batch)
		{
			batch.Begin();

			if (_firework != null)
				_firework.Render(batch);

			foreach (var particle in _particles)
				particle.Render(batch);

			batch.End();
		}
	}

	internal class CardAnimationView
	{
		public Card Card { get; private set; }
		public Rectangle CurrentRect { get; private set; }

		public bool Started
		{
			get { return DateTime.Now >= _startTime; }
		}

		public bool Completed
		{
			get { return DateTime.Now >= _destTime; }
		}

		private Point _startPoint;
		private readonly DateTime _startTime;
		private Point _destPoint;
		private readonly DateTime _destTime;

		public CardAnimationView(Card c, double delay, double duration, Point start, Point end, int width, int height)
		{
			Card = c;
			_startPoint = start;
			_destPoint = end;
			_startTime = DateTime.Now.AddSeconds(delay);
			_destTime = _startTime.AddSeconds(duration);
			CurrentRect = new Rectangle(_startPoint.X, _startPoint.Y, width, height);

			Update();
		}

		public bool Update()
		{
			var elapsed = DateTime.Now - _startTime;
			var duration = _destTime - _startTime;
			bool running = (elapsed <= duration);
			if (elapsed < TimeSpan.Zero)
				elapsed = TimeSpan.Zero;
			if (elapsed > duration)
				elapsed = duration;

			double tfactor = elapsed.TotalSeconds/duration.TotalSeconds;
			var currentPoint = new Point(
				(int) ((_destPoint.X - _startPoint.X)*tfactor + _startPoint.X),
				(int) ((_destPoint.Y - _startPoint.Y)*tfactor + _startPoint.Y));
			CurrentRect = new Rectangle(currentPoint.X, currentPoint.Y, CurrentRect.Width, CurrentRect.Height);
			Card.View.Rect = CurrentRect;
			return running;
		}

		public void Render(SpriteBatch batch)
		{
			Card.View.Render(batch);
		}
	}

	internal class RocketPuff
	{
		private Rectangle _rect;
		private readonly Texture2D _tex;
		private float _alpha;
		private readonly float _scaleFactor;
		private readonly DateTime _startTime;

		private const float Duration = 0.6f;

		public RocketPuff(Rectangle rect, float scale, Texture2D tex)
		{
			_rect = rect;
			_tex = tex;
			_alpha = 1.0f;
			_scaleFactor = scale;
			_startTime = DateTime.Now;
		}

		public bool Update()
		{
			double t = (DateTime.Now - _startTime).TotalSeconds;
			_alpha = (float) (1.0 - t/Duration);
			double currentScale = 1.0 + (t/Duration)*(_scaleFactor - 1.0);
			int size = (int) (20*currentScale);
			var center = new Point(_rect.X + _rect.Width/2, _rect.Y + _rect.Height/2);
			_rect = new Rectangle(center.X - size/2, center.Y - size/2, size, size);
			if (t > Duration)
				return false;
			return true;
		}

		public void Render(SpriteBatch batch)
		{
			batch.Begin();
			batch.Draw(_tex, _rect, Color.Multiply(Color.White, _alpha));
			batch.End();
		}
	}

	internal class Particle
	{
		protected Vector2 Pos;
		protected Vector2 Size;
		protected Vector2 Vel;
		protected Vector2 Accel;
		protected float Friction;
		protected Color StartColor;
		protected Color EndColor;
		protected Texture2D Tex;
		protected float Lifespan;

		private bool _alive;
		private readonly DateTime _startTime;
		private DateTime _lastUpdateTime;
		private Color _currentColor;

		public Particle(Vector2 pos, Vector2 size, Vector2 vel, Vector2 accel, float friction, Color startColor,
			Color endColor, Texture2D tex, float lifespan)
		{
			Pos = pos;
			Size = size;
			Vel = vel;
			Accel = accel;
			Friction = friction;
			StartColor = startColor;
			EndColor = endColor;
			Tex = tex;
			Lifespan = lifespan;

			_alive = true;
			_startTime = DateTime.Now;
			_lastUpdateTime = DateTime.Now;
			_currentColor = StartColor;
		}

		public bool Update()
		{
			if (!_alive)
				return false;

			double t = (DateTime.Now - _startTime).TotalSeconds;
			double percent = (t/Lifespan);
			double delta = (DateTime.Now - _lastUpdateTime).TotalSeconds;

			Pos += Vel*(float) delta;
			Vel += Accel*(float) delta;
			Vel *= 1 - Math.Min(Friction*(float) delta, 1.0f);

			_currentColor = new Color((EndColor.ToVector4() - StartColor.ToVector4())*(float) percent + StartColor.ToVector4());
			_lastUpdateTime = DateTime.Now;

			if (t > Lifespan)
				_alive = false;
			return _alive;
		}

		public void Render(SpriteBatch batch)
		{
			if (_alive)
			{
				var rect = new Rectangle((int) Pos.X, (int) Pos.Y, (int) Size.X, (int) Size.Y);
				batch.Draw(Tex, rect, _currentColor);
			}
		}
	}

	internal class Firework : Particle
	{
		public Firework(Vector2 pos, Vector2 size, Vector2 vel, Vector2 accel, float friction, Color startColor, Color endColor, Texture2D tex, float lifespan) :
			base(pos, size, vel, accel, friction, startColor, endColor, tex, lifespan)
		{
		}

		public Vector2 Position
		{
			get { return Pos; }
		}
	}

	internal class FireworkParticle : Particle
	{
		public FireworkParticle(Vector2 pos, Vector2 size, Vector2 vel, Vector2 accel, float friction, Color startColor, Color endColor, Texture2D tex, float lifespan) :
			base(pos, size, vel, accel, friction, startColor, endColor, tex, lifespan)
		{
		}
	}
}