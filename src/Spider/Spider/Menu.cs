using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;
using System.Reflection;

namespace Spider
{
    delegate void OnClick();
    delegate void OnButtonClicked(MenuButton button);

    class Menu
    {
        private static Texture2D spiderCardTex;
        private static Rectangle spiderCardBounds;

        private static SpriteFont menuFont;
        private static SpriteFont menuSubFont;
        private static SpriteFont menuTrialDetailFont;
        private static SpriteFont menuBackgroundFont;

        private static Texture2D oneSuitTex;
        private static Texture2D twoSuitTex;
        private static Texture2D fourSuitTex;

        private static Texture2D trialBannerTex;

        public static int ContentCount() { return 9 + StatisticsView.ContentCount() + OptionsView.ContentCount() + AboutView.ContentCount(); }
        public static void LoadContent(ContentManager content, ContentLoadNotificationDelegate callback)
        {
            spiderCardTex = content.Load<Texture2D>(@"Menu\SpiderCard");
            callback();
            menuFont = content.Load<SpriteFont>(@"Menu\MainMenuFont");
            callback();
            menuSubFont = content.Load<SpriteFont>(@"Menu\MenuSubFont");
            callback();
            menuTrialDetailFont = content.Load<SpriteFont>(@"Menu\MenuTrialDetailFont");
            callback();
            menuBackgroundFont = content.Load<SpriteFont>(@"Menu\MenuBackgroundFont");
            callback();

            oneSuitTex = content.Load<Texture2D>(@"Menu\OneSuit");
            callback();
            twoSuitTex = content.Load<Texture2D>(@"Menu\TwoSuits");
            callback();
            fourSuitTex = content.Load<Texture2D>(@"Menu\FourSuits");
            callback();

            if (GameStateManager.IsTrial)
                trialBannerTex = content.Load<Texture2D>(@"Menu\TrialBanner");
            callback();

            StatisticsView.LoadContent(content, callback);
            OptionsView.LoadContent(content, callback);
            AboutView.LoadContent(content, callback);
        }

        private Rectangle viewRect;
        private List<MenuButton> buttons = new List<MenuButton>();
        private List<MenuButtonAnimation> animations = new List<MenuButtonAnimation>();

        private DateTime startTime;
        private DateTime currentTime;

        private MenuSubView activeSubView;
        private TrialWindow trialWindow;

        const int BackgroundTextScrollTime = 90;
        
        public Menu(Rectangle rc)
        {
            if (GameStateManager.IsTrial)
                Statistics.Load();

            viewRect = rc;

            Color switchedColor = (GameStateManager.IsTrial ? Color.DimGray : Color.White);

            float aspectRatio = (float)spiderCardTex.Bounds.Width / (float)spiderCardTex.Bounds.Height;
            int spiderCardHeight = Math.Min(GameStateManager.ViewRect.Height - 80, (int)((GameStateManager.ViewRect.Width / 2 - 80) / aspectRatio));
            spiderCardBounds = new Rectangle(40, (GameStateManager.ViewRect.Height - spiderCardHeight) / 2, (int)(spiderCardHeight * aspectRatio), spiderCardHeight);

            int x = viewRect.Width / 2 - 40;
            int y = (int)(viewRect.Height * 0.06);
            int xSpacing = (int)(viewRect.Width / 8);
            int ySpacing = (int)(viewRect.Height * 0.13);
            int xImgSpacing = (int)(viewRect.Width * 0.03);

            TextMenuButton newGameButton = new TextMenuButton() { Text = Strings.NewGame, Font = menuFont };
            Vector2 newGameSize = newGameButton.Font.MeasureString(newGameButton.Text);
            newGameButton.Rect = new Rectangle(x, y, (int)newGameSize.X, newGameButton.Font.LineSpacing);
            newGameButton.ButtonClickDelegate = OnNewGameClicked;
            buttons.Add(newGameButton);

            int imageWidth = GameStateManager.ViewRect.Width / 8 + 20;
            int imageHeight = imageWidth * oneSuitTex.Height / oneSuitTex.Width;

            ImageMenuButton oneSuitButton = new ImageMenuButton() { Texture = oneSuitTex, Visible = false, Enabled = false };
            oneSuitButton.Rect = new Rectangle(newGameButton.Rect.X, newGameButton.Rect.Bottom + 10, imageWidth, imageHeight);
            oneSuitButton.ButtonClickDelegate = OnSuitImageClicked;
            buttons.Add(oneSuitButton);

            ImageMenuButton twoSuitButton = new ImageMenuButton() { Texture = twoSuitTex, Visible = false, Enabled = false, Color = switchedColor };
            twoSuitButton.Rect = new Rectangle(newGameButton.Rect.X + imageWidth + xImgSpacing, newGameButton.Rect.Bottom + 10, imageWidth, imageHeight);
            twoSuitButton.ButtonClickDelegate = OnSuitImageClicked;
            buttons.Add(twoSuitButton);

            ImageMenuButton fourSuitButton = new ImageMenuButton() { Texture = fourSuitTex, Visible = false, Enabled = false, Color = switchedColor };
            fourSuitButton.Rect = new Rectangle(newGameButton.Rect.X + imageWidth * 2 + xImgSpacing * 2, newGameButton.Rect.Bottom + 10, imageWidth, imageHeight);
            fourSuitButton.ButtonClickDelegate = OnSuitImageClicked;
            buttons.Add(fourSuitButton);

            bool resume = Board.ResumeGameExists();
            TextMenuButton resumeButton = new TextMenuButton() { Text = Strings.Resume, Font = menuFont, Enabled = resume };
            if (!resume)
                resumeButton.Color = new Color(64, 64, 64);
            Vector2 resumeSize = resumeButton.Font.MeasureString(resumeButton.Text);
            resumeButton.Rect = new Rectangle(x, y + ySpacing * 2, (int)resumeSize.X, resumeButton.Font.LineSpacing);
            resumeButton.ButtonClickDelegate = OnResumeClicked;
            buttons.Add(resumeButton);

            TextMenuButton optionsButton = new TextMenuButton() { Text = Strings.Options, Font = menuSubFont };
            Vector2 optionsSize = optionsButton.Font.MeasureString(optionsButton.Text);
            optionsButton.Rect = new Rectangle(x, y + ySpacing * 4, (int)optionsSize.X, optionsButton.Font.LineSpacing);
            optionsButton.ButtonClickDelegate = OnOptionsClicked;
            buttons.Add(optionsButton);

            TextMenuButton statsButton = new TextMenuButton() { Text = Strings.Statistics, Font = menuSubFont, Color = switchedColor };
            Vector2 statsSize = statsButton.Font.MeasureString(statsButton.Text);
            statsButton.Rect = new Rectangle(x, y + ySpacing * 5, (int)statsSize.X, statsButton.Font.LineSpacing);
            statsButton.ButtonClickDelegate = OnStatisticsClicked;
            buttons.Add(statsButton);

            TextMenuButton aboutButton = new TextMenuButton() { Text = Strings.About, Font = menuSubFont };
            Vector2 aboutSize = aboutButton.Font.MeasureString(aboutButton.Text);
            aboutButton.Rect = new Rectangle(x, y + ySpacing * 6, (int)aboutSize.X, aboutButton.Font.LineSpacing);
            aboutButton.ButtonClickDelegate = OnAboutClicked;
            buttons.Add(aboutButton);

            if (GameStateManager.IsTrial)
            {
                int bannerSize = (int)(viewRect.Height * 0.45);
                ImageMenuButton trialBannerButton = new ImageMenuButton() { Texture = trialBannerTex };
                trialBannerButton.Rect = new Rectangle(viewRect.Right - bannerSize, viewRect.Bottom - bannerSize, bannerSize, bannerSize);
                trialBannerButton.ButtonClickDelegate = OnTrialBannerClicked;
                buttons.Add(trialBannerButton);

                TextMenuButton bannerTextButton = new TextMenuButton() { Text = Strings.Menu_TrialBanner, Font = menuSubFont, Color = Color.Black, Rotation = (float)(-Math.PI / 4) };
                Vector2 bannerTextSize = bannerTextButton.Font.MeasureString(bannerTextButton.Text);
                bannerTextButton.Rect = new Rectangle(trialBannerButton.Rect.X + 3 * (int)bannerTextSize.Y / 4, viewRect.Bottom - 3 * (int)bannerTextSize.Y / 4, (int)bannerTextSize.X, (int)bannerTextSize.Y);
                buttons.Add(bannerTextButton);
            }

            startTime = DateTime.Now;
            currentTime = DateTime.Now;
        }

        public void Update(Game game)
        {
            if (trialWindow != null)
            {
                if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
                {
                    trialWindow = null;
                    return;
                }

                if (trialWindow.Update() == false)
                {
                    CheckTrialStatus();
                    trialWindow = null;
                    return;
                }
            }
            else if (activeSubView != null)
            {
                if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
                {
                    activeSubView.OnClose();
                    activeSubView = null;
                    return;
                }

                activeSubView.Update();
            }
            else
            {
                if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
                    game.Exit();

                currentTime = DateTime.Now;

                foreach (TouchLocation touchLoc in TouchPanel.GetState())
                {
                    Point pt = new Point((int)touchLoc.Position.X, (int)touchLoc.Position.Y);

                    if (touchLoc.State == TouchLocationState.Released)
                    {
                        foreach (MenuButton button in buttons)
                        {
                            if (button.Rect.Contains(pt) && button.Enabled)
                            {
                                if (button.ButtonClickDelegate != null)
                                    button.ButtonClickDelegate(button);
                                break;
                            }
                        }
                    }
                }

                List<MenuButtonAnimation> finishedAnimations = new List<MenuButtonAnimation>();
                foreach (MenuButtonAnimation animation in animations)
                {
                    if (!animation.Update())
                        finishedAnimations.Add(animation);
                }
                foreach (MenuButtonAnimation animation in finishedAnimations)
                    animations.Remove(animation);
            }
        }

        public void Render(Rectangle rect, SpriteBatch batch)
        {
            if (activeSubView != null)
            {
                activeSubView.Render(rect, batch);
            }
            else
            {
                TimeSpan elapsed = currentTime - startTime;
                float scale = 3.0f;
                Vector2 backgroundSize = menuBackgroundFont.MeasureString(Strings.AppName) * scale;
                float x = (float)(-backgroundSize.X * (elapsed.TotalSeconds / BackgroundTextScrollTime));
                float y = -(backgroundSize.Y - GameStateManager.ViewRect.Height) / 2;
                Vector2 offset = new Vector2(x, y);

                batch.Begin();
                batch.DrawString(menuBackgroundFont, Strings.AppName, offset, new Color(32, 32, 32), 0.0f, Vector2.Zero, scale, SpriteEffects.None, 1);
                if (offset.X + backgroundSize.X < GameStateManager.ViewRect.Width)
                {
                    offset.X += backgroundSize.X;
                    batch.DrawString(menuBackgroundFont, Strings.AppName, offset, new Color(32, 32, 32), 0.0f, Vector2.Zero, scale, SpriteEffects.None, 1);
                }

                batch.Draw(spiderCardTex, spiderCardBounds, Color.White);

                foreach (MenuButton button in buttons)
                {
                    if (button.Visible)
                    {
                        TextMenuButton textButton = button as TextMenuButton;
                        ImageMenuButton imageButton = button as ImageMenuButton;
                        if (textButton != null)
                        {
                            Vector2 pos = new Vector2(button.Rect.X, button.Rect.Y);
                            batch.DrawString(textButton.Font, textButton.Text, pos, button.Color, textButton.Rotation, Vector2.Zero, 1.0f, SpriteEffects.None, 0);
                        }
                        if (imageButton != null)
                        {
                            batch.Draw(imageButton.Texture, button.Rect, button.Color);
                        }
                    }
                }

                batch.End();
            }

            if (trialWindow != null)
            {
                trialWindow.Render(rect, batch);
            }
        }

        private void OnNewGameClicked(MenuButton button)
        {
            MenuButton oneSuitButton = null;
            MenuButton twoSuitButton = null;
            MenuButton fourSuitButton = null;
            foreach (MenuButton menuButton in buttons)
            {
                ImageMenuButton imageButton = menuButton as ImageMenuButton;
                if (imageButton != null)
                {
                    if (imageButton.Texture == oneSuitTex)
                        oneSuitButton = menuButton;
                    else if (imageButton.Texture == twoSuitTex)
                        twoSuitButton = menuButton;
                    else if (imageButton.Texture == fourSuitTex)
                        fourSuitButton = menuButton;
                }
            }

            if (!oneSuitButton.Visible)
                animations.Add(new MenuButtonAnimation(oneSuitButton, oneSuitButton.Rect.Location, oneSuitButton.Rect.Location, 0.0f, 1.0f));
            if (!twoSuitButton.Visible)
                animations.Add(new MenuButtonAnimation(twoSuitButton, twoSuitButton.Rect.Location, twoSuitButton.Rect.Location, 0.0f, 1.0f));
            if (!fourSuitButton.Visible)
                animations.Add(new MenuButtonAnimation(fourSuitButton, fourSuitButton.Rect.Location, fourSuitButton.Rect.Location, 0.0f, 1.0f));
        }

        private void OnSuitImageClicked(MenuButton button)
        {
            ImageMenuButton imageButton = button as ImageMenuButton;
            if (imageButton.Texture == oneSuitTex)
                Board.SuitCount = 1;
            else if (!GameStateManager.IsTrial)
            {
                if (imageButton.Texture == twoSuitTex)
                    Board.SuitCount = 2;
                else if (imageButton.Texture == fourSuitTex)
                    Board.SuitCount = 4;
            }
            else
            {
                trialWindow = new TrialWindow(viewRect, Strings.DisabledInTrial);
                return;
            }
            GameStateManager.ChangeGameState(GameState.Playing);
        }

        private void OnResumeClicked(MenuButton button)
        {
            GameStateManager.ChangeGameState(GameState.Playing, true /*resume*/);
        }

        private void OnOptionsClicked(MenuButton button)
        {
            activeSubView = new OptionsView(viewRect);
        }

        private void OnStatisticsClicked(MenuButton button)
        {
            if (!GameStateManager.IsTrial)
                activeSubView = new StatisticsView(viewRect);
            else
                trialWindow = new TrialWindow(viewRect, Strings.DisabledInTrial);
        }

        private void OnAboutClicked(MenuButton button)
        {
            activeSubView = new AboutView(viewRect);
        }

        private void OnTrialBannerClicked(MenuButton button)
        {
            trialWindow = new TrialWindow(viewRect, Strings.Menu_TrialBannerNav);
        }

        // TODO: Probably get rid of this
        private void CheckTrialStatus()
        {
            GameStateManager.RefreshTrialStatus();
            if (!GameStateManager.IsTrial)
            {
                MenuButton twoSuitButton = null;
                MenuButton fourSuitButton = null;
                foreach (MenuButton menuButton in buttons)
                {
                    ImageMenuButton imageButton = menuButton as ImageMenuButton;
                    if (imageButton != null)
                    {
                        if (imageButton.Texture == twoSuitTex)
                            twoSuitButton = menuButton;
                        else if (imageButton.Texture == fourSuitTex)
                            fourSuitButton = menuButton;
                    }
                }

                twoSuitButton.Color = Color.White;
                fourSuitButton.Color = Color.White;
            }
        }
    }

    class MenuButton
    {
        public Rectangle Rect { get; set; }
        public Color Color { get; set; }
        public bool Visible { get; set; }
        public bool Enabled { get; set; }
        public OnButtonClicked ButtonClickDelegate { get; set; }

        public MenuButton()
        {
            Color = Color.White;
            Visible = true;
            Enabled = true;
        }
    }

    class TextMenuButton : MenuButton
    {
        public string Text { get; set; }
        public SpriteFont Font { get; set; }
        public float Rotation { get; set; }
    }

    class ImageMenuButton : MenuButton
    {
        public Texture2D Texture { get; set; }
    }

    class MenuButtonAnimation
    {
        const double duration = 0.3;

        MenuButton button;
        Color endColor;
        Point ptStart;
        Point ptEnd;
        float alphaStart;
        float alphaEnd;
        DateTime timeStart;
        DateTime timeEnd;

        public MenuButtonAnimation(MenuButton button, Point ptStart, Point ptEnd, float alphaStart, float alphaEnd)
        {
            this.button = button;
            this.endColor = button.Color;
            this.ptStart = ptStart;
            this.ptEnd = ptEnd;
            this.alphaStart = alphaStart;
            this.alphaEnd = alphaEnd;
            this.timeStart = DateTime.Now;
            this.timeEnd = timeStart.AddSeconds(duration);

            button.Visible = true;
        }

        public bool Update()
        {
            double t = (DateTime.Now - timeStart).TotalSeconds / duration;
            button.Rect = new Rectangle(
                (int)((ptEnd.X - ptStart.X) * t) + ptStart.X, 
                (int)((ptEnd.Y - ptStart.Y) * t) + ptStart.Y,
                button.Rect.Width,
                button.Rect.Height);
            button.Color = Color.Multiply(endColor, (float)((alphaEnd - alphaStart) * t) + alphaStart);

            if (DateTime.Now > timeEnd)
            {
                button.Enabled = true;
                button.Color = endColor;
                return false;
            }
            return true;
        }
    }

    interface MenuSubView
    {
        void Update();
        void Render(Rectangle rect, SpriteBatch batch);
        void OnClose();
    }

    class StatisticsView : MenuSubView
    {
        private static SpriteFont titleFont;
        private static SpriteFont itemFont;
        private static SpriteFont resetFont;
        
        public static int ContentCount() { return 3; }
        public static void LoadContent(ContentManager content, ContentLoadNotificationDelegate callback)
        {
            titleFont = content.Load<SpriteFont>(@"Menu\MainMenuFont");
            callback();
            itemFont = content.Load<SpriteFont>(@"Menu\StatisticsFont");
            callback();
            resetFont = content.Load<SpriteFont>(@"Menu\MenuSubFont");
            callback();
        }
        private Rectangle viewRect;
        private List<MenuButton> labels = new List<MenuButton>();
        private TextMenuButton resetButton;
        
        public StatisticsView(Rectangle rc)
        {
            Statistics.Load();
            viewRect = rc;

            InitControls();
        }

        protected void InitControls()
        {
            labels.Clear();

            int x = 40;
            int y = viewRect.Height / 10;
            int xSpacing = (int)(viewRect.Width / 20);
            int ySpacing = (int)(viewRect.Height * 0.1);
            int xMaxLabel = 0;

            TextMenuButton titleLabel = new TextMenuButton() { Text = Strings.Statistics, Font = titleFont };
            Vector2 titleSize = titleLabel.Font.MeasureString(titleLabel.Text);
            titleLabel.Rect = new Rectangle(x, y, (int)titleSize.X, (int)titleSize.Y);
            if (titleLabel.Rect.Right > xMaxLabel)
                xMaxLabel = titleLabel.Rect.Right;
            labels.Add(titleLabel);

            TextMenuButton totalGamesLabel = new TextMenuButton() { Text = Strings.Stats_TotalGamesLabel, Font = itemFont };
            Vector2 totalGamesSize = totalGamesLabel.Font.MeasureString(totalGamesLabel.Text);
            totalGamesLabel.Rect = new Rectangle(x + xSpacing, y + ySpacing * 2, (int)totalGamesSize.X, (int)totalGamesSize.Y);
            if (totalGamesLabel.Rect.Right > xMaxLabel)
                xMaxLabel = totalGamesLabel.Rect.Right;
            labels.Add(totalGamesLabel);

            TextMenuButton easyGamesLabel = new TextMenuButton() { Text = Strings.Stats_EasyGamesLabel, Font = itemFont };
            Vector2 easyGamesSize = easyGamesLabel.Font.MeasureString(easyGamesLabel.Text);
            easyGamesLabel.Rect = new Rectangle(x + xSpacing, y + ySpacing * 3, (int)easyGamesSize.X, (int)easyGamesSize.Y);
            if (easyGamesLabel.Rect.Right > xMaxLabel)
                xMaxLabel = easyGamesLabel.Rect.Right;
            labels.Add(easyGamesLabel);

            TextMenuButton mediumGamesLabel = new TextMenuButton() { Text = Strings.Stats_MediumGamesLabel, Font = itemFont };
            Vector2 mediumGamesSize = mediumGamesLabel.Font.MeasureString(mediumGamesLabel.Text);
            mediumGamesLabel.Rect = new Rectangle(x + xSpacing, y + ySpacing * 4, (int)mediumGamesSize.X, (int)mediumGamesSize.Y);
            if (mediumGamesLabel.Rect.Right > xMaxLabel)
                xMaxLabel = mediumGamesLabel.Rect.Right;
            labels.Add(mediumGamesLabel);

            TextMenuButton hardGamesLabel = new TextMenuButton() { Text = Strings.Stats_HardGamesLabel, Font = itemFont };
            Vector2 hardGamesSize = hardGamesLabel.Font.MeasureString(hardGamesLabel.Text);
            hardGamesLabel.Rect = new Rectangle(x + xSpacing, y + ySpacing * 5, (int)hardGamesSize.X, (int)hardGamesSize.Y);
            if (hardGamesLabel.Rect.Right > xMaxLabel)
                xMaxLabel = hardGamesLabel.Rect.Right;
            labels.Add(hardGamesLabel);

            TextMenuButton totalTimeLabel = new TextMenuButton() { Text = Strings.Stats_TotalTimeLabel, Font = itemFont };
            Vector2 totalTimeSize = totalTimeLabel.Font.MeasureString(totalTimeLabel.Text);
            totalTimeLabel.Rect = new Rectangle(x + xSpacing, y + ySpacing * 6, (int)totalTimeSize.X, (int)totalTimeSize.Y);
            if (totalTimeLabel.Rect.Right > xMaxLabel)
                xMaxLabel = totalTimeLabel.Rect.Right;
            labels.Add(totalTimeLabel);

            {
                TextMenuButton info = new TextMenuButton() { Text = BuildGameStatsString(Statistics.TotalGamesWon, Statistics.TotalGames), Font = itemFont };
                Vector2 size = info.Font.MeasureString(info.Text);
                info.Rect = new Rectangle(xMaxLabel + xSpacing, y + ySpacing * 2, (int)size.X, (int)size.Y);
                labels.Add(info);
            }

            {
                TextMenuButton info = new TextMenuButton() { Text = BuildGameStatsString(Statistics.EasyGamesWon, Statistics.EasyGames), Font = itemFont };
                Vector2 size = info.Font.MeasureString(info.Text);
                info.Rect = new Rectangle(xMaxLabel + xSpacing, y + ySpacing * 3, (int)size.X, (int)size.Y);
                labels.Add(info);
            }

            {
                TextMenuButton info = new TextMenuButton() { Text = BuildGameStatsString(Statistics.MediumGamesWon, Statistics.MediumGames), Font = itemFont };
                Vector2 size = info.Font.MeasureString(info.Text);
                info.Rect = new Rectangle(xMaxLabel + xSpacing, y + ySpacing * 4, (int)size.X, (int)size.Y);
                labels.Add(info);
            }

            {
                TextMenuButton info = new TextMenuButton() { Text = BuildGameStatsString(Statistics.HardGamesWon, Statistics.HardGames), Font = itemFont };
                Vector2 size = info.Font.MeasureString(info.Text);
                info.Rect = new Rectangle(xMaxLabel + xSpacing, y + ySpacing * 5, (int)size.X, (int)size.Y);
                labels.Add(info);
            }

            {
                TextMenuButton info = new TextMenuButton() { Text = TimeSpan.FromSeconds(Statistics.TotalTimePlayed).ToString(), Font = itemFont };
                Vector2 size = info.Font.MeasureString(info.Text);
                info.Rect = new Rectangle(xMaxLabel + xSpacing, y + ySpacing * 6, (int)size.X, (int)size.Y);
                labels.Add(info);
            }

            resetButton = new TextMenuButton() { Text = Strings.Stats_ResetButton, Font = resetFont };
            Vector2 resetSize = resetButton.Font.MeasureString(resetButton.Text);
            resetButton.Rect = new Rectangle(x + xSpacing, y + ySpacing * 7, (int)resetSize.X, (int)resetSize.Y);
            resetButton.ButtonClickDelegate = OnResetClicked;
        }

        protected string BuildGameStatsString(int won, int total)
        {
            return string.Format("{0}/{1} ({2}%)", won, total, (total == 0 ? 0 : (int)((float)won / total * 100)));
        }

        public void Update()
        {
            foreach (TouchLocation touchLoc in TouchPanel.GetState())
            {
                Point pt = new Point((int)touchLoc.Position.X, (int)touchLoc.Position.Y);

                if (touchLoc.State == TouchLocationState.Released)
                {
                    if (resetButton.Rect.Contains(pt))
                    {
                        if (resetButton.ButtonClickDelegate != null)
                            resetButton.ButtonClickDelegate(resetButton);
                        break;
                    }
                }
            }
        }

        public void Render(Rectangle rect, SpriteBatch batch)
        {
            batch.Begin();
            foreach (MenuButton label in labels)
            {
                if (label.Visible)
                {
                    TextMenuButton textButton = label as TextMenuButton;
                    if (textButton != null)
                    {
                        Vector2 pos = new Vector2(label.Rect.X, label.Rect.Y);
                        batch.DrawString(textButton.Font, textButton.Text, pos, label.Color);
                    }
                }
            }
            
            {
                Vector2 pos = new Vector2(resetButton.Rect.X, resetButton.Rect.Y);
                batch.DrawString(resetButton.Font, resetButton.Text, pos, resetButton.Color);
            }

            batch.End();
        }

        private void OnResetClicked(MenuButton button)
        {
            Statistics.Reset();
            Statistics.Save();
            InitControls();
        }

        public void OnClose()
        {
        }
    }

    class OptionsView : MenuSubView
    {
        private static SpriteFont titleFont;
        private static SpriteFont itemFont;
        private static Texture2D cardTex;

        public static int ContentCount() { return 3; }
        public static void LoadContent(ContentManager content, ContentLoadNotificationDelegate callback)
        {
            titleFont = content.Load<SpriteFont>(@"Menu\MainMenuFont");
            callback();
            itemFont = content.Load<SpriteFont>(@"Menu\StatisticsFont");
            callback();
            cardTex = content.Load<Texture2D>(@"Card\CardBack_White");
            callback();
        }

        private static Color[] deckColors = new Color[] { Color.CornflowerBlue, Color.Crimson, Color.LightSlateGray, Color.Gold, Color.MediumPurple, Color.Silver };

        private Rectangle viewRect;
        private List<MenuButton> labels = new List<MenuButton>();
        private List<ImageMenuButton> deckColorButtons = new List<ImageMenuButton>();
        
        private int selectedDeckColor = 0;

        public OptionsView(Rectangle rc)
        {
            Options.Load();
            viewRect = rc;

            InitControls();

            for (int i = 0; i < deckColors.Length; i++)
            {
                if (deckColors[i] == Options.CardBackColor)
                {
                    selectedDeckColor = i;
                    break;
                }
            }
        }

        protected void InitControls()
        {
            labels.Clear();

            int x = 40;
            int y = viewRect.Height / 10;
            int xSpacing = (int)(viewRect.Width / 20);
            int ySpacing = (int)(viewRect.Height * 0.20);
            int xMaxLabel = 0;

            TextMenuButton titleLabel = new TextMenuButton() { Text = Strings.Options, Font = titleFont };
            Vector2 titleSize = titleLabel.Font.MeasureString(titleLabel.Text);
            titleLabel.Rect = new Rectangle(x, y, (int)titleSize.X, (int)titleSize.Y);
            if (titleLabel.Rect.Right > xMaxLabel)
                xMaxLabel = titleLabel.Rect.Right;
            labels.Add(titleLabel);

            TextMenuButton deckColorLabel = new TextMenuButton() { Text = Strings.Options_DeckColorLabel, Font = itemFont };
            Vector2 deckColorSize = deckColorLabel.Font.MeasureString(deckColorLabel.Text);
            deckColorLabel.Rect = new Rectangle(x + xSpacing, y + ySpacing, (int)deckColorSize.X, (int)deckColorSize.Y);
            if (deckColorLabel.Rect.Right > xMaxLabel)
                xMaxLabel = deckColorLabel.Rect.Right;
            labels.Add(deckColorLabel);

            int cardHeight = (int)(viewRect.Width / 8);
            int cardWidth = (int)(cardHeight * ((float)cardTex.Width / (float)cardTex.Height));
            for (int i=0; i < deckColors.Length; i++)
            {
                ImageMenuButton button = new ImageMenuButton() { Texture = cardTex, ButtonClickDelegate = OnDeckColorClicked, Color = deckColors[i] };
                button.Rect = new Rectangle(xMaxLabel - xSpacing + (cardWidth + xSpacing / 2) * i, deckColorLabel.Rect.Bottom + 20, cardWidth, cardHeight);
                deckColorButtons.Add(button);
            }
        }

        public void Update()
        {
            foreach (TouchLocation touchLoc in TouchPanel.GetState())
            {
                Point pt = new Point((int)touchLoc.Position.X, (int)touchLoc.Position.Y);

                if (touchLoc.State == TouchLocationState.Released)
                {
                    foreach (ImageMenuButton button in deckColorButtons)
                    {
                        if (button.Rect.Contains(pt))
                        {
                            if (button.ButtonClickDelegate != null)
                                button.ButtonClickDelegate(button);
                            break;
                        }
                    }
                }
            }
        }

        public void Render(Rectangle rect, SpriteBatch batch)
        {
            batch.Begin();
            foreach (MenuButton label in labels)
            {
                if (label.Visible)
                {
                    TextMenuButton textButton = label as TextMenuButton;
                    if (textButton != null)
                    {
                        Vector2 pos = new Vector2(label.Rect.X, label.Rect.Y);
                        batch.DrawString(textButton.Font, textButton.Text, pos, label.Color);
                    }
                }
            }

            foreach (ImageMenuButton button in deckColorButtons)
            {
                batch.Draw(button.Texture, button.Rect, button.Color);
            }

            ImageMenuButton selectedButton = deckColorButtons[selectedDeckColor];
            {
                Rectangle overlayRect = new Rectangle(selectedButton.Rect.X, selectedButton.Rect.Y, selectedButton.Rect.Width, selectedButton.Rect.Height);
                overlayRect.Inflate(overlayRect.Width / 12, overlayRect.Height / 12);

                Rectangle topRect = new Rectangle(overlayRect.Left, overlayRect.Top, overlayRect.Width, overlayRect.Y / 4);
                Rectangle bottomRect = new Rectangle(overlayRect.Left, overlayRect.Bottom - topRect.Height, overlayRect.Width, topRect.Height);
                Rectangle centerRect = new Rectangle(overlayRect.Left, overlayRect.Top + topRect.Height, overlayRect.Width, overlayRect.Height - topRect.Height * 2);

                batch.Draw(CardResources.HighlightEndTex, topRect, Color.White);
                batch.Draw(CardResources.HighlightEndTex, bottomRect, null, Color.White, 0.0f, Vector2.Zero, SpriteEffects.FlipVertically, 0.0f);
                batch.Draw(CardResources.HightlightCenterTex, centerRect, Color.White);
            }

            batch.End();
        }

        private void OnDeckColorClicked(MenuButton button)
        {
            for (int i = 0; i < deckColorButtons.Count; i++)
            {
                if (button == deckColorButtons[i])
                    selectedDeckColor = i;
            }
            Options.CardBackColor = deckColors[selectedDeckColor];
        }

        public void OnClose()
        {
            Options.Save();
        }
    }

    class AboutView : MenuSubView
    {
        private static SpriteFont titleFont;
        private static SpriteFont itemFont;

        public static int ContentCount() { return 2; }
        public static void LoadContent(ContentManager content, ContentLoadNotificationDelegate callback)
        {
            titleFont = content.Load<SpriteFont>(@"Menu\MainMenuFont");
            callback();
            itemFont = content.Load<SpriteFont>(@"Menu\AboutFont");
            callback();
        }

        private Rectangle viewRect;
        private List<MenuButton> labels = new List<MenuButton>();
        MessageWindow messageWindow;
        
        public AboutView(Rectangle rc)
        {
            viewRect = rc;

            InitControls();
        }

        protected void InitControls()
        {
            labels.Clear();

            int x = 40;
            int y = viewRect.Height / 10;
            int xSpacing = (int)(viewRect.Width / 20);
            int ySpacing = (int)(viewRect.Height * 0.09);
            int xMaxLabel = 0;

            TextMenuButton titleLabel = new TextMenuButton() { Text = Strings.About_Title, Font = titleFont };
            Vector2 titleSize = titleLabel.Font.MeasureString(titleLabel.Text);
            titleLabel.Rect = new Rectangle(x, y, (int)titleSize.X, (int)titleSize.Y);
            labels.Add(titleLabel);

            TextMenuButton versionLabel = new TextMenuButton() { Text = Strings.About_VersionLabel, Font = itemFont };
            Vector2 versionSize = versionLabel.Font.MeasureString(versionLabel.Text);
            versionLabel.Rect = new Rectangle(x + xSpacing, y + ySpacing * 2, (int)versionSize.X, versionLabel.Font.LineSpacing);
            if (versionLabel.Rect.Right > xMaxLabel)
                xMaxLabel = versionLabel.Rect.Right;
            labels.Add(versionLabel);

            TextMenuButton copyrightLabel = new TextMenuButton() { Text = Strings.About_CopyrightLabel, Font = itemFont };
            Vector2 copyrightSize = copyrightLabel.Font.MeasureString(copyrightLabel.Text);
            copyrightLabel.Rect = new Rectangle(x + xSpacing, y + ySpacing * 3, (int)copyrightSize.X, copyrightLabel.Font.LineSpacing);
            if (copyrightLabel.Rect.Right > xMaxLabel)
                xMaxLabel = copyrightLabel.Rect.Right;
            labels.Add(copyrightLabel);

            TextMenuButton fontCopyrightLabel = new TextMenuButton() { Text = Strings.About_FontCopyrightLabel, Font = itemFont };
            Vector2 fontCopyrightSize = fontCopyrightLabel.Font.MeasureString(copyrightLabel.Text);
            fontCopyrightLabel.Rect = new Rectangle(x + xSpacing, y + ySpacing * 4, (int)fontCopyrightSize.X, fontCopyrightLabel.Font.LineSpacing);
            if (fontCopyrightLabel.Rect.Right > xMaxLabel)
                xMaxLabel = fontCopyrightLabel.Rect.Right;
            labels.Add(fontCopyrightLabel);

            Dictionary<string, string> assemblyInfo = GetAssemblyInfo();
            string versionStr = assemblyInfo["Version"];
            TextMenuButton versionInfo = new TextMenuButton() { Text = versionStr, Font = itemFont };
            Vector2 versionInfoSize = versionInfo.Font.MeasureString(versionInfo.Text);
            versionInfo.Rect = new Rectangle(xMaxLabel + xSpacing, y + ySpacing * 2, (int)versionInfoSize.X, versionInfo.Font.LineSpacing);
            labels.Add(versionInfo);

            TextMenuButton copyrightInfo = new TextMenuButton() { Text = Strings.About_CopyrightInfo, Font = itemFont };
            Vector2 copyrightInfoSize = copyrightInfo.Font.MeasureString(copyrightInfo.Text);
            copyrightInfo.Rect = new Rectangle(xMaxLabel + xSpacing, y + ySpacing * 3, (int)copyrightInfoSize.X, copyrightInfo.Font.LineSpacing);
            labels.Add(copyrightInfo);

            TextMenuButton fontCopyrightInfo = new TextMenuButton() { Text = Strings.About_FontCopyrightInfo, Font = itemFont };
            Vector2 fontCopyrightInfoSize = fontCopyrightInfo.Font.MeasureString(fontCopyrightInfo.Text);
            fontCopyrightInfo.Rect = new Rectangle(xMaxLabel + xSpacing, y + ySpacing * 4, (int)fontCopyrightInfoSize.X, fontCopyrightInfo.Font.LineSpacing);
            labels.Add(fontCopyrightInfo);

            if (GameStateManager.IsTrial)
            {
                TextMenuButton trialLabel = new TextMenuButton() { Text = Strings.About_TrialModeLabel, Font = itemFont };
                Vector2 trialSize = trialLabel.Font.MeasureString(trialLabel.Text);
                trialLabel.Rect = new Rectangle((viewRect.Width - (int)trialSize.X) / 2, y + ySpacing * 8, (int)trialSize.X, trialLabel.Font.LineSpacing);
                labels.Add(trialLabel);

                TextMenuButton upgradeLabel = new TextMenuButton() { Text = Strings.About_UpgradeLabel, Font = itemFont };
                Vector2 upgradeSize = upgradeLabel.Font.MeasureString(upgradeLabel.Text);
                upgradeLabel.Rect = new Rectangle((viewRect.Width - (int)upgradeSize.X) / 2, y + ySpacing * 9, (int)upgradeSize.X, upgradeLabel.Font.LineSpacing);
                upgradeLabel.ButtonClickDelegate = OnUpgradeClicked;
                labels.Add(upgradeLabel);
            }
        }

        protected Dictionary<string, string> GetAssemblyInfo()
        {
            Dictionary<string, string> assemblyInfo = new Dictionary<string, string>();

            string fullInfo = Assembly.GetExecutingAssembly().FullName;
            foreach (string v in fullInfo.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries))
            {
                string[] args = v.Split('=');
                assemblyInfo.Add(args[0], (args.Length > 1 ? args[1] : ""));
            }

            return assemblyInfo;
        }

        public void Update()
        {
            if (messageWindow != null)
            {
                if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || messageWindow.Update() == false)
                {
                    messageWindow = null;
                    return;
                }
            }
            else
            {
                foreach (TouchLocation touchLoc in TouchPanel.GetState())
                {
                    Point pt = new Point((int)touchLoc.Position.X, (int)touchLoc.Position.Y);

                    if (touchLoc.State == TouchLocationState.Released)
                    {
                        foreach (MenuButton button in labels)
                        {
                            if (button.Rect.Contains(pt))
                            {
                                if (button.ButtonClickDelegate != null)
                                    button.ButtonClickDelegate(button);
                                break;
                            }
                        }
                    }
                }
            }
        }

        public void Render(Rectangle rect, SpriteBatch batch)
        {
            batch.Begin();
            foreach (MenuButton label in labels)
            {
                if (label.Visible)
                {
                    TextMenuButton textButton = label as TextMenuButton;
                    if (textButton != null)
                    {
                        Vector2 pos = new Vector2(label.Rect.X, label.Rect.Y);
                        batch.DrawString(textButton.Font, textButton.Text, pos, label.Color);
                    }
                }
            }

            batch.End();

            if (messageWindow != null)
            {
                messageWindow.Render(rect, batch);
            }
        }

        public void OnClose()
        {
        }

        private void OnUpgradeClicked(MenuButton button)
        {
            TrialMode.LaunchMarketplace();
        }
    }
    
    class MessageWindow
    {
        private static Texture2D backgroundTex;
        private static SpriteFont font;

        public static int ContentCount() { return 2; }
        public static void LoadContent(ContentManager content, ContentLoadNotificationDelegate callback)
        {
            backgroundTex = content.Load<Texture2D>(@"Menu\MessageWindow");
            callback();
            font = content.Load<SpriteFont>(@"Menu\MessageFont");
            callback();
        }

        Rectangle viewRect;
        Rectangle windowRect;
        Vector2 textPos;
        string windowText;
        public OnClick ClickDelegate { get; set; }

        public MessageWindow(Rectangle viewRect, string text)
        {
            this.viewRect = viewRect;
            SetText(text);
        }

        protected void SetText(string text)
        {
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

        public virtual bool Update()
        {
            foreach (TouchLocation touchLoc in TouchPanel.GetState())
            {
                Point pt = new Point((int)touchLoc.Position.X, (int)touchLoc.Position.Y);
                if (touchLoc.State == TouchLocationState.Released)
                {
                    if (windowRect.Contains(pt))
                    {
                        ClickDelegate();
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
