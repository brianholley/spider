using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Phone.Marketplace;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input.Touch;
using Microsoft.Xna.Framework.Content;
using System.Resources;
using System.Threading;
using Microsoft.Xna.Framework.Input;

namespace Spider
{
    enum GameState
    {
        None,
        Loading,
        Menu,
        Playing
    }

    class GameStateManager
    {
        private static GameState state;
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

            state = newState;
        }

        public static GraphicsDevice GraphicsDevice { get; set; }
        public static ContentManager Content { get; set; }
        public static Rectangle ViewRect { get { return GraphicsDevice.Viewport.Bounds; } }

        public static bool IsTrial { get; set; }
        public static void RefreshTrialStatus()
        {
	        IsTrial = Windows.ApplicationModel.Store.CurrentApp.LicenseInformation.IsTrial;
        }
    }

    abstract class GameStateBase
    {
        public abstract GameState State();

        public abstract void Init(object arg);
        public abstract void Update(Game game);
        public abstract void Render(SpriteBatch spriteBatch);

        public abstract void Save();
    }
    
    public delegate void ContentLoadNotificationDelegate();

    class LoadGameState : GameStateBase
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
            loadingTextSize = new Point((int)size.X, (int)size.Y);

            backgroundTex = GameStateManager.Content.Load<Texture2D>("SplashScreenLoad");
            progressBarTex = GameStateManager.Content.Load<Texture2D>("ProgressBar");

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
            loadingTextSize = new Point((int)size.X, (int)size.Y);

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
            spriteBatch.DrawString(splashFont, loadingText, new Vector2((GameStateManager.ViewRect.Width - loadingTextSize.X) / 2, GameStateManager.ViewRect.Height / 4 - loadingTextSize.Y), Color.White);

            Rectangle progressRect = new Rectangle(GameStateManager.ViewRect.Width / 4, GameStateManager.ViewRect.Height * 3 / 4, GameStateManager.ViewRect.Width / 2, GameStateManager.ViewRect.Height / 24);
            spriteBatch.Draw(progressBarTex, progressRect, progressBarTex.Bounds, new Color(96, 96, 96));

            float progress = (float)contentLoaded / (float)contentTotal;
            progressRect.Width = (int)(progressRect.Width * progress);
            Rectangle sourceRect = new Rectangle(progressBarTex.Bounds.X, progressBarTex.Bounds.Y, (int)(progressBarTex.Bounds.Width * progress), progressBarTex.Bounds.Height);
            spriteBatch.Draw(progressBarTex, progressRect, sourceRect, Color.White);

            Rectangle loadRect = new Rectangle(GameStateManager.ViewRect.Width / 3, GameStateManager.ViewRect.Height / 3, GameStateManager.ViewRect.Width / 3, GameStateManager.ViewRect.Height / 3);
            Color loadImageColor = Color.White * progress;
            spriteBatch.Draw(backgroundTex, loadRect, loadImageColor);
            
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
        private Texture2D progressBarTex;
        private int contentTotal;
        private int contentLoaded;
        private GameState nextGameState;
    }

    class MenuGameState : GameStateBase
    {
        Menu menu;

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

    class PlayGameState : GameStateBase
    {
        Board board;
        DateTime startTime;

        public override GameState State()
        {
            return GameState.Playing;
        }

        public override void Init(object arg)
        {
            Statistics.Load();
            startTime = DateTime.Now;

            bool resume = (arg != null && arg is bool && (bool)arg);

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
                Point pt = new Point((int)touchLoc.Position.X, (int)touchLoc.Position.Y);
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

            Statistics.TotalTimePlayed += (long)(DateTime.Now - startTime).TotalSeconds;
            Statistics.Save();
        }
    }
}
