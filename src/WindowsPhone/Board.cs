using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.IsolatedStorage;
using System.Xml.Serialization;

namespace Spider
{
	internal class Board
	{
		public const int CardCount = 104;
		public const int StackCount = 10;
		public const int ExtrasCount = 6;

		public const int StartingScore = 500;
		public const int ScorePerRun = 100;
		public const int ScorePerMove = 1;

		public static int SuitCount { get; set; }
		private readonly List<CardStack> _stacks = new List<CardStack>(StackCount);
		private readonly List<Card>[] _extras = new List<Card>[ExtrasCount];
		private readonly List<Card> _completed = new List<Card>(CardCount/13);

		private bool _cleared;

		private readonly UndoStack _undoStack;

		public int MoveCount { get; private set; }
		public int Score { get; private set; }

		public BoardView View { get; set; }

		public Board()
		{
			for (int i = 0; i < StackCount; i++)
				_stacks.Add(new CardStack(i));

			_undoStack = new UndoStack(this);
		}

		public void StartNewGame()
		{
			Reset();

			var seed = (int) DateTime.Now.Ticks;
			var random = new Random(seed);
			Debug.WriteLine("Seeding game with value " + seed);

			var cards = new List<Card>(CardCount);
			int decks = (CardCount/13/SuitCount);

			for (int n = 0; n < decks; n++)
			{
				for (int s = 0; s < SuitCount; s++)
				{
					for (int v = 0; v < 13; v++)
					{
						var card = new Card((Suit) s, (Value) v, random);
						card.View = new CardView(card);
						cards.Add(card);
					}
				}
			}

			Shuffle(cards);

			int i;
			for (i = 0; i < CardCount - StackCount*ExtrasCount; i++)
			{
				Card card = cards[i];
				_stacks[i%StackCount].Add(card);
			}

			for (; i < CardCount; i++)
			{
				Card card = cards[i];
				int extra = (i - (CardCount - StackCount*ExtrasCount))/StackCount;
				if (_extras[extra] == null)
					_extras[extra] = new List<Card>(StackCount);
				_extras[extra].Add(card);
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
			for (int i = 0; i < StackCount; i++)
				_stacks[i].Clear();

			for (int i = 0; i < ExtrasCount; i++)
				_extras[i] = null;

			_completed.Clear();
			_cleared = false;

			_undoStack.Clear();

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

			List<Card> next = _extras[left - 1];
			_extras[left - 1] = null;

			for (int i = 0; i < StackCount; i++)
			{
				Card card = next[i];
				card.Reveal();
				_stacks[i].Add(card);
			}
		}

		public bool CanUndo()
		{
			return _undoStack.CanUndo;
		}

		public void Undo()
		{
			_undoStack.Undo();
		}

		public UndoAction NextUndoAction()
		{
			if (CanUndo())
				return _undoStack.Stack[_undoStack.Stack.Count - 1];
			return null;
		}

		public void ClearUndoStack()
		{
			_undoStack.Clear();
		}

		public CardStack GetStack(int stack)
		{
			return _stacks[stack];
		}

		public void RemoveCompleteRun(CardStack stack, bool revealCard, bool addToComplete)
		{
			List<Card> runCards = stack.RemoveCompleteRun(revealCard);
			if (addToComplete)
			{
				Card king = runCards[0];
				_completed.Add(king);

				Score += ScorePerRun;

				// Check for cleared board
				if (!_cleared && _completed.Count == CardCount/13)
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

				if (check != _cleared)
				{
					Debug.WriteLine("BUG warning!!! Completed stack count does not match full board check!!!");
					throw new Exception();
				}
#endif
			}
		}

		public int CountOfExtraDealingsLeft()
		{
			int i;
			for (i = 0; i < ExtrasCount; i++)
			{
				if (_extras[i] == null)
					return i;
			}
			return ExtrasCount;
		}

		public List<Card> CardsInNextDeal()
		{
			int nextDeal = CountOfExtraDealingsLeft() - 1;
			return _extras[nextDeal];
		}

		public int CompletedCount()
		{
			return _completed.Count;
		}

		public Card GetCompletedStack(int stack)
		{
			return _completed[stack];
		}

		public bool IsBoardClear()
		{
			return _cleared;
		}

		public void SetBoardClear()
		{
			_cleared = true;

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
			_undoStack.AddMoveOfCards(cardCount, stackSrc, stackDest, revealedCard);
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
			var gameSerialization = new GameSerialization {Suits = SuitCount, MoveCount = MoveCount, Score = Score};
			foreach (CardStack cardStack in _stacks)
			{
				var stackSerialization = new CardStackSerialization();
				foreach (Card card in cardStack.GetCards())
				{
					var cardSerialization = new CardSerialization
					{
						Suit = (int) card.Suit,
						Value = (int) card.Value,
						Visible = card.Visible
					};
					stackSerialization.Cards.Add(cardSerialization);
				}
				gameSerialization.Board.Stacks.Add(stackSerialization);
			}
			foreach (var extraList in _extras)
			{
				if (extraList != null)
				{
					var stackSerialization = new CardStackSerialization();
					foreach (Card card in extraList)
					{
						var cardSerialization = new CardSerialization
						{
							Suit = (int) card.Suit,
							Value = (int) card.Value,
							Visible = card.Visible
						};
						stackSerialization.Cards.Add(cardSerialization);
					}
					gameSerialization.Board.Extras.Add(stackSerialization);
				}
			}
			foreach (Card card in _completed)
			{
				var stackSerialization = new CardStackSerialization();
				var cardSerialization = new CardSerialization
				{
					Suit = (int) card.Suit,
					Value = (int) card.Value,
					Visible = card.Visible
				};
				stackSerialization.Cards.Add(cardSerialization);
				gameSerialization.Board.Completed.Add(stackSerialization);
			}

			gameSerialization.UndoStack.Actions = new List<UndoActionSerialization>();
			foreach (UndoAction action in _undoStack.Stack)
			{
				var actionSerialization = new UndoActionSerialization
				{
					CardCount = action.CardCount,
					SourceStack = action.SourceStack.Index,
					DestStack = action.DestStack.Index,
					RevealedCard = action.RevealedCard
				};
				gameSerialization.UndoStack.Actions.Add(actionSerialization);
			}

			try
			{
				var storage = IsolatedStorageFile.GetUserStoreForApplication();
				var stream = storage.CreateFile(ResumeGameFilename);

				var writer = new StreamWriter(stream);
				var serializer = new XmlSerializer(typeof (GameSerialization));
				serializer.Serialize(writer, gameSerialization);
				writer.Close();
			}
			catch
			{
				// Couldn't save stats file - crap
			}
		}

		public bool Load()
		{
			if (ResumeGameExists())
			{
				Reset();

				GameSerialization gameSerialization;
				try
				{
					var storage = IsolatedStorageFile.GetUserStoreForApplication();
					var stream = storage.OpenFile(ResumeGameFilename, FileMode.Open, FileAccess.Read);

					TextReader reader = new StreamReader(stream);
					var serializer = new XmlSerializer(typeof (GameSerialization));
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
					Debug.WriteLine(exc);
					return false;
				}

				if (gameSerialization == null)
					return false;

				SuitCount = gameSerialization.Suits;
				MoveCount = gameSerialization.MoveCount;
				Score = gameSerialization.Score;

				for (int i = 0; i < StackCount; i++)
				{
					CardStackSerialization stackSerialization = gameSerialization.Board.Stacks[i];
					CardStack stack = GetStack(i);
					foreach (CardSerialization cardSerialization in stackSerialization.Cards)
					{
						var card = new Card((Suit) cardSerialization.Suit, (Value) cardSerialization.Value, new Random())
						{
							Visible = cardSerialization.Visible
						};
						card.View = new CardView(card);
						stack.Add(card);
					}
				}

				for (int i = 0; i < gameSerialization.Board.Extras.Count; i++)
				{
					CardStackSerialization extraSerialization = gameSerialization.Board.Extras[i];
					_extras[i] = new List<Card>();
					foreach (CardSerialization cardSerialization in extraSerialization.Cards)
					{
						var card = new Card((Suit) cardSerialization.Suit, (Value) cardSerialization.Value, new Random())
						{
							Visible = cardSerialization.Visible
						};
						card.View = new CardView(card);
						_extras[i].Add(card);
					}
				}

				for (int i = 0; i < gameSerialization.Board.Completed.Count; i++)
				{
					CardStackSerialization completedSerialization = gameSerialization.Board.Completed[i];
					CardSerialization cardSerialization = completedSerialization.Cards[0];
					var card = new Card((Suit) cardSerialization.Suit, (Value) cardSerialization.Value, new Random())
					{
						Visible = cardSerialization.Visible
					};
					card.View = new CardView(card);
					_completed.Add(card);
				}

				for (int i = 0; i < gameSerialization.UndoStack.Actions.Count; i++)
				{
					UndoActionSerialization actionSerialization = gameSerialization.UndoStack.Actions[i];
					var action = new UndoAction(actionSerialization.CardCount, GetStack(actionSerialization.SourceStack),
						GetStack(actionSerialization.DestStack), actionSerialization.RevealedCard);
					_undoStack.Stack.Add(action);
				}

				for (int i = 0; i < StackCount; i++)
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

		#endregion

		public void DumpToLog()
		{
			for (int stack = 0; stack < StackCount; stack++)
			{
				Console.WriteLine("Stack #{0}:", stack + 1);
				for (int i = 0; i < _stacks[stack].Count; i++)
				{
					Card card = _stacks[stack].GetCard(i);
					Console.WriteLine("    {0} of {1} {2}", card.Value, card.Suit, card.Visible ? "(visible)" : "");
				}
			}

			Console.WriteLine();

			for (int extra = 0; extra < ExtrasCount; extra++)
			{
				Console.WriteLine("Extra #{0}:", extra + 1);
				if (_extras[extra] == null)
					continue;

				for (int i = 0; i < _extras[extra].Count; i++)
				{
					Card card = _extras[extra][i];
					Console.WriteLine("    {0} of {1} {2}", card.Value, card.Suit, card.Visible ? "(visible)" : "");
				}
			}
		}

		public void DumpDeckToLog(List<Card> cards)
		{
			Console.WriteLine("Deck:");
			foreach (Card card in cards)
			{
				Console.WriteLine("    {0} of {1} {2}", card.Value, card.Suit, card.Visible ? "(visible)" : "");
			}
		}
	}
}