using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;
using Microsoft.Xna.Framework.Media;
using System.Resources;

namespace Spider
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class SpiderGame : Microsoft.Xna.Framework.Game
    {
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;

        public SpiderGame()
        {
            graphics = new GraphicsDeviceManager(this);
            graphics.IsFullScreen = true;
            Content.RootDirectory = "Content";

            // Frame rate is 30 fps by default for Windows Phone.
            TargetElapsedTime = TimeSpan.FromTicks(333333);
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            GameStateManager.GraphicsDevice = GraphicsDevice;
            GameStateManager.Content = Content;
            GameStateManager.ChangeGameState(GameState.Loading);

            base.Initialize();
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);

            //Menu.LoadContent(this.Content);
            //CardResources.LoadResources(GraphicsDevice, this.Content);

            //GameStateManager.ChangeGameState(GameState.Menu);
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// all content.
        /// </summary>
        protected override void UnloadContent()
        {
        }

        protected override void OnActivated(object sender, EventArgs args)
        {
            LoadGameState loadState = GameStateManager.CurrentState as LoadGameState;
            if (loadState != null)
            {
                if (Microsoft.Phone.Shell.PhoneApplicationService.Current.State.ContainsKey("GameState"))
                    loadState.SetResuming((GameState)Microsoft.Phone.Shell.PhoneApplicationService.Current.State["GameState"]);
            }

#if DEBUG
            //Microsoft.Xna.Framework.GamerServices.Guide.SimulateTrialMode = true;
#endif
            GameStateManager.RefreshTrialStatus();
        }

        protected override void OnDeactivated(object sender, EventArgs args)
        {
            if (GameStateManager.CurrentState.State() != GameState.Loading)
            {
                Microsoft.Phone.Shell.PhoneApplicationService.Current.State["GameState"] = GameStateManager.CurrentState.State();
            }
        }

        /// <summary>
        /// Exiting the app
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        protected override void OnExiting(object sender, EventArgs args)
        {
            GameStateManager.CurrentState.Save();
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            // Allows the game to exit
            //if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
            //    this.Exit();

            GameStateManager.CurrentState.Update(this);

            base.Update(gameTime);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);
            GameStateManager.CurrentState.Render(spriteBatch);
            base.Draw(gameTime);
        }
    }
}
