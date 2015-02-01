using System;
using Microsoft.Phone.Shell;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Spider
{
	/// <summary>
	/// This is the main type for your game
	/// </summary>
	public class SpiderGame : Game
	{
		private GraphicsDeviceManager _graphics;
		private SpriteBatch _spriteBatch;

		public SpiderGame()
		{
			_graphics = new GraphicsDeviceManager(this) {IsFullScreen = true};
			Content.RootDirectory = "Content";

			// Frame rate is 30 fps by default for Windows Phone.
			TargetElapsedTime = TimeSpan.FromTicks(333333);

			//PhoneApplicationService appService = PhoneApplicationService.Current;
			//appService.Launching += OnAppServiceLaunching;
			//appService.Activated += OnAppServiceActivated;
			//appService.Deactivated += OnAppServiceDeactivated;
			//appService.Closing += OnAppServiceClosing;
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
			_spriteBatch = new SpriteBatch(GraphicsDevice);
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
			LoadOnActivated();
		}

		protected override void OnDeactivated(object sender, EventArgs args)
		{
			GameStateManager.CurrentState.Save();
			if (GameStateManager.CurrentState.State() != GameState.Loading)
			{
				PhoneApplicationService.Current.State["GameState"] = GameStateManager.CurrentState.State();
			}
		}

		protected void LoadOnActivated()
		{
			var loadState = GameStateManager.CurrentState as LoadGameState;
			if (loadState != null)
			{
				try
				{
					if (PhoneApplicationService.Current != null && // wtf
					    PhoneApplicationService.Current.State != null && // wtf2
					    PhoneApplicationService.Current.State.ContainsKey("GameState"))
					{
						loadState.SetResuming((GameState) PhoneApplicationService.Current.State["GameState"]);
					}
				}
				catch (Exception e)
				{
					// Note: Suspect if the app deactives/resumes quickly we can hit here with an InvalidOperationException
					Console.Error.WriteLine("Resuming state threw exception: " + e);
				}
			}

#if DEBUG
			//Microsoft.Xna.Framework.GamerServices.Guide.SimulateTrialMode = true;
#endif
			GameStateManager.RefreshTrialStatus();
		}

		/// <summary>
		/// Allows the game to run logic such as updating the world,
		/// checking for collisions, gathering input, and playing audio.
		/// </summary>
		/// <param name="gameTime">Provides a snapshot of timing values.</param>
		protected override void Update(GameTime gameTime)
		{
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
			GameStateManager.CurrentState.Render(_spriteBatch);
			base.Draw(gameTime);
		}
	}
}