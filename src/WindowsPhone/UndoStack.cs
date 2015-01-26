using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace Spider
{
    class UndoStack
    {
        public Board board;
	    public List<UndoAction> stack;

        public bool CanUndo { get { return stack.Count > 0; } }

        public UndoStack(Board board)
        {
            this.board = board;
            stack = new List<UndoAction>();
        }

        public void Clear()
        {
            stack.Clear();
        }

        public void AddMoveOfCards(int cardCount, CardStack stackSrc, CardStack stackDest, bool revealedCard)
        {
            stack.Add(new UndoAction(cardCount, stackSrc, stackDest, revealedCard));
        }

        public void Undo()
        {
	        if (stack.Count > 0)
	        {
		        UndoAction action = stack[stack.Count - 1];
		        if (action.RevealedCard)
		        {
			        Card lastCard = action.SourceStack.GetLastCard();
			        lastCard.Visible = false;
		        }
		        int pos = action.DestStack.Count - action.CardCount;
		        board.MoveCardsWithoutChecking(action.DestStack, pos, action.SourceStack);
		        stack.Remove(action);
	        }
        }
    }

    class UndoAction
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
