using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using System.Diagnostics;
using System.IO.IsolatedStorage;
using System.Xml;
using System.Xml.Serialization;
using System.IO;

namespace Spider
{
    class Board
    {
        public const int CardCount = 104;
        public const int StackCount = 10;
        public const int ExtrasCount = 6;
        
        public const int StartingScore = 500;
        public const int ScorePerRun = 100;
        public const int ScorePerMove = 1;

        public static int SuitCount { get; set; }
	    private List<CardStack> stacks = new List<CardStack>(StackCount);
	    private List<Card>[] extras = new List<Card>[ExtrasCount];
	    private List<Card> completed = new List<Card>(CardCount / 13);
	
	    private bool cleared = false;
	
	    private UndoStack undoStack;
	
	    public int MoveCount { get; private set; }
	    public int Score { get; private set; }

        public BoardView View { get; set; }

        public Board()
        {
            for (int i=0; i < StackCount; i++)
		        stacks.Add(new CardStack(i));
	
	        undoStack = new UndoStack(this);
        }

        public void StartNewGame()
        {
            Reset();

            int seed = (int)DateTime.Now.Ticks;
            Random random = new Random(seed);
            Debug.WriteLine("Seeding game with value " + seed);
	
            List<Card> cards = new List<Card>(CardCount);
	        int decks = (CardCount / 13 / SuitCount);
	
	        for (int n=0; n < decks; n++)
	        {
		        for (int s=0; s < SuitCount; s++)
		        {
			        for (int v=0; v < 13; v++)
			        {
				        Card card = new Card((Suit)s, (Value)v, random);
                        card.View = new CardView(card);
				        cards.Add(card);
			        }
		        }
	        }
	
            Shuffle(cards);
		
	        int i=0;
	        for (i=0; i < CardCount - StackCount * ExtrasCount; i++)
	        {
		        Card card = cards[i];
		        stacks[i % StackCount].Add(card);
	        }
	
	        for (; i < CardCount; i++)
	        {
		        Card card = cards[i];
		        int extra = (i - (CardCount - StackCount * ExtrasCount)) / StackCount;
		        if (extras[extra] == null)
			        extras[extra] = new List<Card>(StackCount);
		        extras[extra].Add(card);
	        }
	
	        MoveCount = 0;
	        Score = StartingScore;

            Statistics.TotalGames++;
            switch (SuitCount)
            {
                case 1:
                    Statistics.EasyGames++;
                    break;
                case 2:
                    Statistics.MediumGames++;
                    break;
                case 4:
                    Statistics.HardGames++;
                    break;
            }
        }
        
        public void Reset()
        {
            for (int i=0; i < StackCount; i++)
		        stacks[i].Clear();
	
	        for (int i=0; i < ExtrasCount; i++)
	            extras[i] = null;
	        
	        completed.Clear();
	        cleared = false;
	
	        undoStack.Clear();

            MoveCount = 0;
            Score = 0;
        }

        private static void Shuffle(List<Card> cards)
        {
            cards.Sort(CompareCards);
        }

        private static int CompareCards(Card a, Card b)
        {
            if (a.RandomSeed < b.RandomSeed)
                return -1;
            if (a.RandomSeed > b.RandomSeed)
                return 1;
            return 0;
        }

        public void Deal()
        {
            int left = CountOfExtraDealingsLeft();
	        if (left <= 0)
		        return;
	
	        List<Card> next = extras[left - 1];
	        extras[left - 1] = null;
		
	        for (int i=0; i < StackCount; i++)
	        {
                Card card = next[i];
		        card.Reveal();
                stacks[i].Add(card);
	        }
        }

        public bool CanUndo()
        {
            return undoStack.CanUndo;
        }

        public void Undo()
        {
            undoStack.Undo();
        }

        public UndoAction NextUndoAction()
        {
            if (CanUndo())
                return undoStack.stack[undoStack.stack.Count - 1];
            return null;
        }

        public void ClearUndoStack()
        {
            undoStack.Clear();
        }

        public CardStack GetStack(int stack)
        {
            return stacks[stack];
        }

        public void RemoveCompleteRun(CardStack stack, bool revealCard, bool addToComplete)
        {
            List<Card> runCards = stack.RemoveCompleteRun(revealCard);
            if (addToComplete)
            {
                Card king = runCards[0];
                completed.Add(king);

                Score += ScorePerRun;

                // Check for cleared board
                if (!cleared && completed.Count == CardCount / 13)
                    SetBoardClear();

#if DEBUG
                bool check = true;
                for (int i = 0; i < StackCount && check; i++)
                {
                    if (GetStack(i).Count > 0)
                        check = false;
                }
                if (CountOfExtraDealingsLeft() > 0)
                    check = false;

                if (check != cleared)
                {
                    System.Diagnostics.Debug.WriteLine("BUG warning!!! Completed stack count does not match full board check!!!");
                    throw new Exception();
                }
#endif
            }
        }

        public int CountOfExtraDealingsLeft()
        {
            int i = 0;
            for (i = 0; i < ExtrasCount; i++)
            {
                if (extras[i] == null)
                    return i;
            }
            return ExtrasCount;
        }

        public List<Card> CardsInNextDeal()
        {
	        int nextDeal = CountOfExtraDealingsLeft() - 1;
	        return extras[nextDeal];
        }

        public int CompletedCount()
        {
            return completed.Count;
        }

        public Card GetCompletedStack(int stack)
        {
            return completed[stack];
        }

        public bool IsBoardClear()
        {
            return cleared;
        }

        public void SetBoardClear()
        {
            cleared = true;

            Statistics.TotalGamesWon++;

            switch (SuitCount)
            {
                case 1:
                    Statistics.EasyGamesWon++;
                    break;
                case 2:
                    Statistics.MediumGamesWon++;
                    break;
                case 4:
                    Statistics.HardGamesWon++;
                    break;
            }
        }

        public bool CanMoveCardToStack(Card card, CardStack stack)
        {
	        if (stack.Count == 0)
		        return true;
	
	        Card topCard = stack.GetLastCard();
	        if (card.Value == topCard.Value - 1)
		        return true;
	        return false;
        }

        public void MoveCards(CardStack stackSrc, int posSrc, CardStack stackDest)
        {
	        if (posSrc < 0)
		        return;

            if (!stackSrc.CanPickupRun(posSrc))
		        Console.WriteLine("Asked to move cards that are not in a sequential run - bug?");
            Card sourceCard = stackSrc.GetCard(posSrc);
	        if (!CanMoveCardToStack(sourceCard, stackDest))
		        Console.WriteLine(@"Asked to move cards to an invalid location - bug?");

            for (int i = posSrc; i < stackSrc.Count; i++)
	        {
                Card card = stackSrc.GetCard(i);
		        stackDest.Add(card);
	        }
	        int cardCount = stackSrc.Count - posSrc;
	        stackSrc.RemoveRange(posSrc, cardCount);
	
	        bool revealedCard = false;
	        if (stackSrc.Count > 0)
	        {
                Card revealed = stackSrc.GetCard(posSrc - 1);
		        if (!revealed.Visible)
		        {
			        revealedCard = true;
			        revealed.Reveal();
		        }
	        }

	        MoveCount++;
	        Score -= ScorePerMove;
            undoStack.AddMoveOfCards(cardCount, stackSrc, stackDest, revealedCard);
        }

        public void MoveCardsWithoutChecking(CardStack stackSrc, int posSrc, CardStack stackDest)
        {
            if (!stackSrc.CanPickupRun(posSrc))
		        Console.WriteLine("Asked to move cards that are not in a sequential run - bug?");

            for (int i = posSrc; i < stackSrc.Count; i++)
	        {
                Card card = stackSrc.GetCard(i);
		        stackDest.Add(card);
	        }
            int cardCount = stackSrc.Count - posSrc;
            stackSrc.RemoveRange(posSrc, cardCount);

            if (stackSrc.Count > 0)
            {
                Card revealed = stackSrc.GetCard(posSrc - 1);
                revealed.Reveal();
            }
	
	        MoveCount++;
	        Score -= ScorePerMove;
        }

        #region Load/Save
        protected const string ResumeGameFilename = "InProgressGame.xml";

        public static bool ResumeGameExists()
        {
            IsolatedStorageFile storage = IsolatedStorageFile.GetUserStoreForApplication();
            return storage.FileExists(ResumeGameFilename);
        }

        protected static void RemoveResumeGame()
        {
            IsolatedStorageFile storage = IsolatedStorageFile.GetUserStoreForApplication();
            if (storage.FileExists(ResumeGameFilename))
                storage.DeleteFile(ResumeGameFilename);
        }

        public void Save()
        {
            // TODO: Ensure animation finished

            if (IsBoardClear())
            {
                RemoveResumeGame();
                return;
            }

            // Build serialized model of the game
            GameSerialization gameSerialization = new GameSerialization() { Suits = Board.SuitCount, MoveCount = this.MoveCount, Score = this.Score };
            foreach (CardStack cardStack in stacks)
            {
                CardStackSerialization stackSerialization = new CardStackSerialization();
                foreach (Card card in cardStack.GetCards())
                {
                    CardSerialization cardSerialization = new CardSerialization() { Suit = (int)card.Suit, Value = (int)card.Value, Visible = card.Visible };
                    stackSerialization.Cards.Add(cardSerialization);
                }
                gameSerialization.Board.Stacks.Add(stackSerialization);
            }
            foreach (List<Card> extraList in extras)
            {
                if (extraList != null)
                {
                    CardStackSerialization stackSerialization = new CardStackSerialization();
                    foreach (Card card in extraList)
                    {
                        CardSerialization cardSerialization = new CardSerialization() { Suit = (int)card.Suit, Value = (int)card.Value, Visible = card.Visible };
                        stackSerialization.Cards.Add(cardSerialization);
                    }
                    gameSerialization.Board.Extras.Add(stackSerialization);
                }
            }
            foreach (Card card in completed)
            {
                CardStackSerialization stackSerialization = new CardStackSerialization();
                CardSerialization cardSerialization = new CardSerialization() { Suit = (int)card.Suit, Value = (int)card.Value, Visible = card.Visible };
                stackSerialization.Cards.Add(cardSerialization);
                gameSerialization.Board.Completed.Add(stackSerialization);
            }

            gameSerialization.UndoStack.Actions = new List<UndoActionSerialization>();
            foreach (UndoAction action in undoStack.stack)
            {
                UndoActionSerialization actionSerialization = new UndoActionSerialization() { CardCount = action.CardCount, SourceStack = action.SourceStack.Index, DestStack = action.DestStack.Index, RevealedCard = action.RevealedCard };
                gameSerialization.UndoStack.Actions.Add(actionSerialization);
            }
            
            try
            {
                IsolatedStorageFile storage = IsolatedStorageFile.GetUserStoreForApplication();
                IsolatedStorageFileStream stream = storage.CreateFile(ResumeGameFilename);

                StreamWriter writer = new StreamWriter(stream);
                XmlSerializer serializer = new XmlSerializer(typeof(GameSerialization));
                serializer.Serialize(writer, gameSerialization);
                writer.Close();
            }
            catch
            {
                // Couldn't save stats file - crap
            }
        }

#if OLD_SERIALIZATION
        public void Save()
        {
            // TODO: Ensure animation finished

            if (IsBoardClear())
                return;

            IsolatedStorageFile storage = IsolatedStorageFile.GetUserStoreForApplication();
            IsolatedStorageFileStream stream = storage.CreateFile("InProgressGame.xml");

            StringBuilder sb = new StringBuilder();
            XmlWriter writer = XmlWriter.Create(sb);

            int cardsWritten = 0;

            writer.WriteStartDocument();
            writer.WriteStartElement("SpiderGame");
            writer.WriteAttributeString("Suits", Board.SuitCount.ToString());
            writer.WriteAttributeString("MoveCount", MoveCount.ToString());
            writer.WriteAttributeString("Score", Score.ToString());
            {
                writer.WriteStartElement("Board");
                {
                    foreach (CardStack stack in stacks)
                    {
                        writer.WriteStartElement("Stack");
                        writer.WriteAttributeString("Count", stack.Count.ToString());
                        {
                            foreach (Card card in stack.GetCards())
                            {
                                SaveCard(card, writer);
                                cardsWritten++;
                            }
                        }
                        writer.WriteEndElement();
                    }
                }
                writer.WriteEndElement();
                writer.WriteStartElement("Extras");
                writer.WriteAttributeString("Count", CountOfExtraDealingsLeft().ToString());
                {
                    foreach (List<Card> cards in extras)
                    {
                        if (cards != null && cards.Count > 0)
                        {
                            writer.WriteStartElement("Deal");
                            {
                                foreach (Card card in cards)
                                {
                                    SaveCard(card, writer);
                                    cardsWritten++;
                                }
                            }
                            writer.WriteEndElement();
                        }
                    }
                }
                writer.WriteEndElement();
                writer.WriteStartElement("Completed");
                writer.WriteAttributeString("Count", completed.Count.ToString());
                {
                    foreach (Card card in completed)
                    {
                        SaveCard(card, writer);
                        cardsWritten += 13;
                    }
                }
                writer.WriteEndElement();

                writer.WriteStartElement("UndoStack");
                undoStack.Save(writer);
                writer.WriteEndElement();
            }
            writer.WriteEndElement();
            writer.WriteEndDocument();

            writer.Close();

            if (cardsWritten != Board.CardCount)
                throw new Exception("Failure to save!  Saved " + cardsWritten + "/" + Board.CardCount + " cards!");

            byte[] bytes = System.Text.Encoding.Unicode.GetBytes(sb.ToString());
            stream.Write(bytes, 0, bytes.Length);
            stream.Close();
        }

        private void SaveCard(Card card, XmlWriter writer)
        {
            writer.WriteStartElement("Card");
            writer.WriteAttributeString("Value", ((int)card.Value).ToString());
            writer.WriteAttributeString("Suit", ((int)card.Suit).ToString());
            writer.WriteAttributeString("Visible", card.Visible.ToString());
            writer.WriteEndElement();
        }
#endif
        public bool Load()
        {
            if (ResumeGameExists())
            {
                Reset();

                GameSerialization gameSerialization;
                try
                {
                    IsolatedStorageFile storage = IsolatedStorageFile.GetUserStoreForApplication();
                    IsolatedStorageFileStream stream = storage.OpenFile(ResumeGameFilename, System.IO.FileMode.Open, System.IO.FileAccess.Read);

                    TextReader reader = new StreamReader(stream);
                    XmlSerializer serializer = new XmlSerializer(typeof(GameSerialization));
                    gameSerialization = serializer.Deserialize(reader) as GameSerialization;
                    reader.Close();
                }
                catch (FileNotFoundException)
                {
                    // We thought we had a file...?
                    return false;
                }
                catch (Exception exc)
                {
                    // Something else bad happened.  Crap.
                    System.Diagnostics.Debug.WriteLine(exc);
                    return false;
                }

                Board.SuitCount = gameSerialization.Suits;
                MoveCount = gameSerialization.MoveCount;
                Score = gameSerialization.Score;

                for (int i=0; i < Board.StackCount; i++)
                {
                    CardStackSerialization stackSerialization = gameSerialization.Board.Stacks[i];
                    CardStack stack = GetStack(i);
                    foreach (CardSerialization cardSerialization in stackSerialization.Cards)
                    {
                        Card card = new Card((Suit)cardSerialization.Suit, (Value)cardSerialization.Value, new Random()) { Visible = cardSerialization.Visible };
                        card.View = new CardView(card);
                        stack.Add(card);
                    }
                }

                for (int i = 0; i < gameSerialization.Board.Extras.Count; i++)
                {
                    CardStackSerialization extraSerialization = gameSerialization.Board.Extras[i];
                    extras[i] = new List<Card>();
                    foreach (CardSerialization cardSerialization in extraSerialization.Cards)
                    {
                        Card card = new Card((Suit)cardSerialization.Suit, (Value)cardSerialization.Value, new Random()) { Visible = cardSerialization.Visible };
                        card.View = new CardView(card);
                        extras[i].Add(card);
                    }
                }

                for (int i = 0; i < gameSerialization.Board.Completed.Count; i++)
                {
                    CardStackSerialization completedSerialization = gameSerialization.Board.Completed[i];
                    CardSerialization cardSerialization = completedSerialization.Cards[0];
                    Card card = new Card((Suit)cardSerialization.Suit, (Value)cardSerialization.Value, new Random()) { Visible = cardSerialization.Visible };
                    card.View = new CardView(card);
                    completed.Add(card);
                }

                for (int i = 0; i < gameSerialization.UndoStack.Actions.Count; i++)
                {
                    UndoActionSerialization actionSerialization = gameSerialization.UndoStack.Actions[i];
                    UndoAction action = new UndoAction(actionSerialization.CardCount, GetStack(actionSerialization.SourceStack), GetStack(actionSerialization.DestStack), actionSerialization.RevealedCard);
                    undoStack.stack.Add(action);
                }

                for (int i = 0; i < Board.StackCount; i++)
                    View.FixupStackCardViewRects(GetStack(i));

                // Stats were reset with a suspended game - bump the count since we have a game in progress
                if (Statistics.TotalGames == 0)
                {
                    Statistics.TotalGames++;
                    switch (SuitCount)
                    {
                        case 1:
                            Statistics.EasyGames++;
                            break;
                        case 2:
                            Statistics.MediumGames++;
                            break;
                        case 4:
                            Statistics.HardGames++;
                            break;
                    }
                }
                return true;
            }
            return false;
        }

#if OLD_SERIALIZATION
        public bool Load()
        {
            if (ResumeGameExists())
            {
                Reset();

                IsolatedStorageFile storage = IsolatedStorageFile.GetUserStoreForApplication();
                IsolatedStorageFileStream stream = storage.OpenFile("InProgressGame.xml", System.IO.FileMode.Open, System.IO.FileAccess.Read);

#if DEBUG
                byte[] bytes = new byte[stream.Length];
                stream.Read(bytes, 0, bytes.Length);
                string s = System.Text.Encoding.Unicode.GetString(bytes, 0, bytes.Length);
                stream.Seek(0, System.IO.SeekOrigin.Begin);
#endif

                try
                {
                    XmlReader reader = XmlReader.Create(stream);
                    reader.Read();

                    // Doc element
                    reader.Read();

                    // SpiderGame
                    reader.MoveToFirstAttribute();
                    Board.SuitCount = int.Parse(reader["Suits"]);
                    MoveCount = int.Parse(reader["MoveCount"]);
                    Score = int.Parse(reader["Score"]);
                    reader.MoveToElement();
                    reader.ReadStartElement("SpiderGame");
                    {
                        // Board
                        reader.ReadStartElement("Board");
                        {
                            // Stack
                            for (int i = 0; i < StackCount; i++)
                            {
                                reader.MoveToFirstAttribute();
                                int count = int.Parse(reader["Count"]);
                                reader.MoveToElement();
                                reader.ReadStartElement("Stack");

                                CardStack stack = GetStack(i);
                                for (int j = 0; j < count; j++)
                                    stack.Add(LoadCard(reader));
                            }
                        }
                        reader.ReadEndElement();

                        // Extras
                        reader.MoveToFirstAttribute();
                        int extraCount = int.Parse(reader["Count"]);
                        reader.MoveToElement();
                        reader.ReadStartElement("Extras");
                        {
                            for (int i = 0; i < extraCount; i++)
                            {
                                // Deal
                                reader.ReadStartElement("Deal");
                                {
                                    extras[i] = new List<Card>();
                                    for (int j = 0; j < StackCount; j++)
                                        extras[i].Add(LoadCard(reader));
                                }
                            }
                        }
                        reader.ReadEndElement();

                        // Completed
                        reader.MoveToFirstAttribute();
                        int completedCount = int.Parse(reader["Count"]);
                        reader.MoveToElement();
                        reader.ReadStartElement("Completed");
                        {
                            for (int i = 0; i < completedCount; i++)
                                completed.Add(LoadCard(reader));
                        }
                    }
                    reader.ReadEndElement();
                    reader.Close();
                }
                catch (XmlException e)
                {
                    Debug.WriteLine("XmlException resuming game: " + e);
                    stream.Close();
                    return false;
                }
                stream.Close();

                for (int i = 0; i < Board.StackCount; i++)
                    View.FixupStackCardViewRects(GetStack(i));

                // Stats were reset with a suspended game - bump the count since we have a game in progress
                if (Statistics.TotalGames == 0)
                {
                    Statistics.TotalGames++;
                    switch (SuitCount)
                    {
                        case 1:
                            Statistics.EasyGames++;
                            break;
                        case 2:
                            Statistics.MediumGames++;
                            break;
                        case 4:
                            Statistics.HardGames++;
                            break;
                    }
                }

                return true;
            }
            return false;
        }

        private Card LoadCard(XmlReader reader)
        {
            reader.MoveToFirstAttribute();
            Value val = (Value)int.Parse(reader["Value"]);
            Suit suit = (Suit)int.Parse(reader["Suit"]);
            bool visible = bool.Parse(reader["Visible"]);
            reader.MoveToElement();

            reader.ReadStartElement("Card");
            if (reader.NodeType == XmlNodeType.EndElement)
                reader.ReadEndElement();
            
            Random random = new Random();
            Card card = new Card(suit, val, random) { Visible = visible };
            card.View = new CardView(card);
            return card;
        }
#endif
        #endregion

        public void DumpToLog()
        {
            for (int stack=0; stack < StackCount; stack++)
	        {
		        Console.WriteLine("Stack #%d:", stack + 1);
		        for (int i=0; i < stacks[stack].Count; i++)
		        {
			        Card card = stacks[stack].GetCard(i);
			        Console.WriteLine("    %s of %s %s", card.Value.ToString(), card.Suit.ToString(), card.Visible ? "(visible)" : "");
		        }
	        }
	
	        Console.WriteLine();
	
	        for (int extra=0; extra < ExtrasCount; extra++)
	        {
		        Console.WriteLine("Extra #%d:", extra + 1);
		        if (extras[extra] == null)
			        continue;
		
		        for (int i=0; i < extras[extra].Count; i++)
		        {
			        Card card = extras[extra][i];
                    Console.WriteLine("    %s of %s %s\n", card.Value.ToString(), card.Suit.ToString(), card.Visible ? "(visible)" : "");
		        }
	        }
        }

        public void DumpDeckToLog(List<Card> cards)
        {
	        Console.WriteLine("Deck:");
	        for (int i=0; i < cards.Count; i++)
	        {
		        Card card = cards[i];
		        Console.WriteLine("    %s of %s %s", card.Value.ToString(), card.Suit.ToString(), card.Visible ? "(visible)" : "");
	        }
        }
    }
}
