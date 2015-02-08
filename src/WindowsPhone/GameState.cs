using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input.Touch;
using Microsoft.Xna.Framework.Content;
using System.Threading;
using Microsoft.Xna.Framework.Input;

namespace Spider
{
	internal enum GameState
	{
		None,
		Loading,
		Menu,
		Playing
	}

	internal class GameStateManager
	{
		public static GameStateBase CurrentState { get; private set; }

		public static void ChangeGameState(GameState newState)
		{
			ChangeGameState(newState, null);
		}

		public static void ChangeGameState(GameState newState, object arg)
		{
			switch (newState)
			{
				case GameState.Loading:
					CurrentState = new LoadGameState();
					break;
				case GameState.Menu:
					CurrentState = new MenuGameState();
					break;
				case GameState.Playing:
					CurrentState = new PlayGameState();
					break;
			}
			CurrentState.Init(arg);
		}

		public static GraphicsDevice GraphicsDevice { get; set; }
		public static ContentManager Content { get; set; }

		public static Rectangle ViewRect
		{
			get { return GraphicsDevice.Viewport.Bounds; }
		}

		public static bool IsTrial { get; set; }

		public static void RefreshTrialStatus()
		{
			IsTrial = Windows.ApplicationModel.Store.CurrentApp.LicenseInformation.IsTrial;
		}
	}

	internal abstract class GameStateBase
	{
		public abstract GameState State();

		public abstract void Init(object arg);
		public abstract void Update(Game game);
		public abstract void Render(SpriteBatch spriteBatch);

		public abstract void Save();
	}

	public delegate void ContentLoadNotificationDelegate();

	internal class LoadGameState : GameStateBase
	{
		public override GameState State()
		{
			return GameState.Loading;
		}

		public override void Init(object arg)
		{
			splashFont = GameStateManager.Content.Load<SpriteFont>("SplashScreenFont");

			loadingText = Strings.Loading;
			Vector2 size = splashFont.MeasureString(loadingText);
			loadingTextSize = new Point((int) size.X, (int) size.Y);

			backgroundTex = GameStateManager.Content.Load<Texture2D>("SplashScreen");
			progressTex = GameStateManager.Content.Load<Texture2D>("SplashScreenLoad");
			
			contentTotal = 0;
			contentLoaded = 0;

			SpriteBatch spriteBatch = new SpriteBatch(GameStateManager.GraphicsDevice);
			GameStateManager.GraphicsDevice.Clear(Color.Black);
			Render(spriteBatch);
			GameStateManager.GraphicsDevice.Present();

			contentTotal += Menu.ContentCount();
			contentTotal += CardResources.ContentCount();
			contentTotal += MessageWindow.ContentCount();

			nextGameState = GameState.Menu;

			Thread thread = new Thread(this.LoadContent);
			thread.Start();
		}

		public void SetResuming(GameState resumeState)
		{
			loadingText = Strings.Resuming;
			Vector2 size = splashFont.MeasureString(loadingText);
			loadingTextSize = new Point((int) size.X, (int) size.Y);

			nextGameState = resumeState;
		}

		public override void Update(Game game)
		{
			if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
				game.Exit();

			if (contentLoaded == contentTotal)
				GameStateManager.ChangeGameState(nextGameState, true /*resume*/);
		}

		public override void Render(SpriteBatch spriteBatch)
		{
			spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Opaque);
			
			float aspect = (float) progressTex.Bounds.Width/progressTex.Bounds.Height;
			int height = (int) (GameStateManager.ViewRect.Height*0.6);
			int width = (int)(height*aspect); 

			Rectangle loadRect = new Rectangle(GameStateManager.ViewRect.Width/2 - width/2, GameStateManager.ViewRect.Height/2-height/2, width, height);
			spriteBatch.Draw(backgroundTex, loadRect, Color.White);

			float progress = (float) contentLoaded/(float) contentTotal;
			loadRect.Width = (int) (loadRect.Width * progress);
			Rectangle sourceRect = new Rectangle(progressTex.Bounds.X, progressTex.Bounds.Y,
				(int)(progressTex.Bounds.Width * progress), progressTex.Bounds.Height);
			spriteBatch.Draw(progressTex, loadRect, sourceRect, Color.White);

			spriteBatch.End();
		}

		public override void Save()
		{
		}

		protected void LoadContent(object data)
		{
			Menu.LoadContent(GameStateManager.Content, this.OnContentPieceLoaded);
			if (contentLoaded != Menu.ContentCount())
			{
				object obj = null;
				obj.ToString();
			}
			CardResources.LoadResources(GameStateManager.GraphicsDevice, GameStateManager.Content, this.OnContentPieceLoaded);
			if (contentLoaded != Menu.ContentCount() + CardResources.ContentCount())
			{
				object obj = null;
				obj.ToString();
			}

			MessageWindow.LoadContent(GameStateManager.Content, this.OnContentPieceLoaded);
			if (contentLoaded != contentTotal)
			{
				object obj = null;
				obj.ToString();
			}
		}

		protected void OnContentPieceLoaded()
		{
			contentLoaded++;

#if DEBUG
			Thread.Sleep(10);
#endif
		}

		private SpriteFont splashFont;
		private string loadingText;
		private Point loadingTextSize;
		private Texture2D backgroundTex;
		private Texture2D progressTex;
		private int contentTotal;
		private int contentLoaded;
		private GameState nextGameState;
	}

	internal class MenuGameState : GameStateBase
	{
		private Menu menu;

		public override GameState State()
		{
			return GameState.Menu;
		}

		public override void Init(object arg)
		{
			menu = new Menu(GameStateManager.ViewRect);
		}

		public override void Update(Game game)
		{
			menu.Update(game);
		}

		public override void Render(SpriteBatch spriteBatch)
		{
			menu.Render(GameStateManager.ViewRect, spriteBatch);
		}

		public override void Save()
		{
		}
	}

	internal class PlayGameState : GameStateBase
	{
		private Board board;
		private DateTime startTime;

		public override GameState State()
		{
			return GameState.Playing;
		}

		public override void Init(object arg)
		{
			Statistics.Load();
			startTime = DateTime.Now;

			bool resume = (arg != null && arg is bool && (bool) arg);

			board = new Board();
			board.View = new BoardView(board, GameStateManager.ViewRect);

			if (resume)
			{
				if (!board.Load())
					GameStateManager.ChangeGameState(GameState.Menu);
			}
			else
			{
				board.View.StartNewGame();
				board.View.Deal();
			}
		}

		public override void Update(Game game)
		{
			if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
			{
				Save();
				GameStateManager.ChangeGameState(GameState.Menu);
			}

			TouchCollection touchCollection = TouchPanel.GetState();
			foreach (TouchLocation touchLoc in touchCollection)
			{
				Point pt = new Point((int) touchLoc.Position.X, (int) touchLoc.Position.Y);
				if (touchLoc.State == TouchLocationState.Pressed)
					board.View.StartDrag(pt);
				else if (touchLoc.State == TouchLocationState.Moved)
					board.View.ContinueDrag(pt);
				else if (touchLoc.State == TouchLocationState.Released)
					board.View.Touch(pt);
			}

			board.View.Update();
		}

		public override void Render(SpriteBatch spriteBatch)
		{
			board.View.Render(GameStateManager.ViewRect, spriteBatch);
		}

		public override void Save()
		{
			board.Save();

			Statistics.TotalTimePlayed += (long) (DateTime.Now - startTime).TotalSeconds;
			Statistics.Save();
		}
	}
}