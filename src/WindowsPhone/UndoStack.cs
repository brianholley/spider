using System.Collections.Generic;

namespace Spider
{
	internal class UndoStack
	{
		public Board Board;
		public List<UndoAction> Stack;

		public bool CanUndo
		{
			get { return Stack.Count > 0; }
		}

		public UndoStack(Board board)
		{
			Board = board;
			Stack = new List<UndoAction>();
		}

		public void Clear()
		{
			Stack.Clear();
		}

		public void AddMoveOfCards(int cardCount, CardStack stackSrc, CardStack stackDest, bool revealedCard)
		{
			Stack.Add(new UndoAction(cardCount, stackSrc, stackDest, revealedCard));
		}

		public void Undo()
		{
			if (Stack.Count > 0)
			{
				UndoAction action = Stack[Stack.Count - 1];
				if (action.RevealedCard)
				{
					Card lastCard = action.SourceStack.GetLastCard();
					lastCard.Visible = false;
				}
				int pos = action.DestStack.Count - action.CardCount;
				Board.MoveCardsWithoutChecking(action.DestStack, pos, action.SourceStack);
				Stack.Remove(action);
			}
		}
	}

	internal class UndoAction
	{
		public int CardCount { get; private set; }
		public CardStack SourceStack { get; private set; }
		public CardStack DestStack { get; private set; }
		public bool RevealedCard { get; private set; }

		public UndoAction(int cardCount, CardStack sourceStack, CardStack destStack, bool revealedCard)
		{
			CardCount = cardCount;
			SourceStack = sourceStack;
			DestStack = destStack;
			RevealedCard = revealedCard;
		}
	}
}