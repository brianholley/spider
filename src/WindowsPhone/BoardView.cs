using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Spider
{
    class BoardView
    {
        private Board board;
        private Rectangle viewRect;

        private Point cardSize;

        private List<Animation> currentAnimations;

        private enum CardAction
        {
            None,
            Moving,
            Selecting,
            Dragging
        }

        private CardAction currentAction = CardAction.None;
        private int currentStack = 0;
        private List<Card> cardsInAction;
        private List<CardAnimationView> cardsSelectionAnimation;

        private class DragInfo
        {
            public Point startPos;
            public Point offset;
            public Point currentPos;

            public int stack;
            public List<Card> cardsToDrag;
        }
        private DragInfo dragInfo;

        private bool completed = false;

        public List<string> errors;
        
        Rectangle undoButtonRect;

        private const int MiniumumDragDistanceSq = 10 * 10;

        // TODO: Show errors + alerts (strings, buzzer, etc.)
        public BoardView(Board board, Rectangle viewRect)
        {
            this.board = board;
            this.viewRect = viewRect;

            int cardWidth = viewRect.Width / 10 - 4;
            int cardHeight = (int)(cardWidth * (3.5 / 2.5));
            cardSize = new Point(cardWidth, cardHeight);

            currentAnimations = new List<Animation>();
            errors = new List<string>();

            undoButtonRect = new Rectangle(10, viewRect.Height - cardHeight + (cardHeight - cardWidth) / 2, cardWidth, cardWidth);

            // Ensure that we have the deck color options loaded
            Options.Load();
            
            Color mutedBackColor = Options.CardBackColor * (169.0f / 255.0f);
            mutedBackColor.A = 255;    // Fix premultiplied alpha value
            CardView.MutedBackColor = mutedBackColor;
        }

        #region Position and Size Helpers
        public Rectangle GetViewArea()
        {
            return viewRect;
        }

        public Rectangle GetCardArea()
        {
            return new Rectangle(0, 0, viewRect.Width, viewRect.Height - cardSize.Y);
        }

        public Point GetCardSize()
        {
            return cardSize;
        }

        public Rectangle GetAreaOfStack(CardStack stack)
        {
            return GetRectOfCardRunInStack(stack, 0, stack.Count);
        }

        public CardStack GetStackAtPoint(Point pt)
        {
            int spacing = (viewRect.Width - cardSize.X * Board.StackCount) / (Board.StackCount - 1);
            int index = pt.X / (cardSize.X + spacing);
            if (index >= 0 && index < Board.StackCount)
            {
                Rectangle stackArea = GetAreaOfStack(board.GetStack(index));
                if (stackArea.Contains(pt))
                    return board.GetStack(index);
            }
            return null;
        }

        public Point GetLocationOfStack(CardStack stack)
        {
            int spacing = (viewRect.Width - cardSize.X * Board.StackCount) / (Board.StackCount - 1);
            return new Point(stack.Index * (cardSize.X + spacing), 0);
        }

        public Point GetLocationOfCardInStack(CardStack stack, int card)
        {
            Point stackLoc = GetLocationOfStack(stack);
            int hidden = stack.GetCountOfHiddenCards();
            int delta = GetVisibleCardDelta(stack);
            int y = (card < hidden ? card * HiddenCardDelta : hidden * HiddenCardDelta + (card - hidden) * delta);
			return new Point(stackLoc.X, y);
        }

        public Rectangle GetRectOfCardRunInStack(CardStack stack, int card, int cards)
        {
            Point loc = GetLocationOfCardInStack(stack, card);
            int hidden = stack.GetCountOfHiddenCards();
            int hiddenUsed = Math.Max(Math.Min(hidden, card + cards) - card, 0);
            int delta = GetVisibleCardDelta(stack);
            int height = hiddenUsed * HiddenCardDelta + (cards - hiddenUsed - 1) * delta + cardSize.Y;
            return new Rectangle(loc.X, loc.Y, cardSize.X, height);
        }

        public int CardDelta { get { return 25; } }
        public int HiddenCardDelta { get { return 10; } }

        private int GetVisibleCardDelta(CardStack stack)
        {
            if (stack.Count > 0)
            {
                int hidden = stack.GetCountOfHiddenCards();
                int delta = (stack.Count > hidden ? (viewRect.Height - cardSize.Y * 2 - hidden * HiddenCardDelta) / (stack.Count - hidden) : CardDelta);
                return Math.Min(delta, CardDelta);
            }
            else
            {
                return 0;
            }
        }

        public Rectangle GetExtraStackArea()
        {
            return new Rectangle(viewRect.Width - cardSize.X - Board.ExtrasCount * 25, viewRect.Height - cardSize.Y, cardSize.X + Board.ExtrasCount * 25, cardSize.Y);
        }

        public Point GetLocationOfExtraStack(int stack)
        {
            return new Point(viewRect.Width - cardSize.X - stack * 25, viewRect.Height - cardSize.Y);
        }
        #endregion

        public void StartNewGame()
        {
            Reset();

            board.StartNewGame();

            for (int i = 0; i < Board.StackCount; i++)
                FixupStackCardViewRects(board.GetStack(i));
        }

        public void Reset()
        {
            currentAnimations = new List<Animation>();

            currentAction = CardAction.None;
            currentStack = 0;
            cardsInAction = null;
            cardsSelectionAnimation = null;

            completed = false;

            errors.Clear();
        }

        public void Update()
        {
            for (int i=0; i < currentAnimations.Count; )
            {
                if (currentAnimations[i].Update())
                {
                    i++;
                }
                else
                {
                    AnimationCompleteCallback onCompleteCallback = currentAnimations[i].OnAnimationCompleted;
                    Animation completeAnimation = currentAnimations[i];
                    currentAnimations.RemoveAt(i);
                    if (onCompleteCallback != null)
                        onCompleteCallback(completeAnimation);
                }
            }
        }

        public void Render(Rectangle rc, SpriteBatch batch)
        {
            // TODO:V2: Render move count and score? 
            batch.Begin();
            batch.Draw(CardResources.GradientTex, rc, Color.Green);
            batch.End();

            int cardWidth = rc.Width / 10 - 4;
            int cardHeight = (int)(cardWidth * (3.5 / 2.5));
            int spacing = (rc.Width - cardWidth * Board.StackCount) / (Board.StackCount - 1);

            // Draw the game board
            for (int i = 0; i < Board.StackCount; i++)
            {
                int stackSize = board.GetStack(i).Count;
                int stackSizeForDelta = stackSize;
                //if (drag.isDragging && i == drag.source.stack)
                //    stackSize = drag.source.pos;
                if (stackSize == 0)
                {
                    Rectangle cardRect = new Rectangle(i * (cardWidth + spacing), 0, cardWidth, cardHeight);
                    batch.Begin();
                    batch.Draw(CardResources.PlaceholderTex, cardRect, Color.White);
                    batch.End();
                }
                else
                {
                    int stackCount = board.GetStack(i).Count - (currentAction == CardAction.Dragging && i == currentStack ? cardsInAction.Count : 0);
                    for (int c = 0; c < stackSize; c++)
                    {
                        Card card = board.GetStack(i).GetCard(c);
                        if (!card.View.Animating && c < stackCount)
                        {
                            card.View.Render(batch);
                        }
                    }
                }
            }

            // Draw the current mover overlay
            if (currentAction == CardAction.Moving)
            {
                CardStack stack = board.GetStack(currentStack);
                int stackSize = stack.Count;
                Point pt = board.View.GetLocationOfCardInStack(board.GetStack(currentStack), stackSize - cardsInAction.Count);
                Point pt2 = board.View.GetLocationOfCardInStack(board.GetStack(currentStack), stackSize - 1);
                Point size = board.View.GetCardSize();
                Rectangle overlayRect = new Rectangle(pt.X, pt.Y, size.X, pt2.Y - pt.Y + size.Y);
                overlayRect.Inflate(size.X / 20, size.Y / 20);

                Rectangle topRect = new Rectangle(overlayRect.Left, overlayRect.Top, overlayRect.Width, size.Y / 4);
                Rectangle bottomRect = new Rectangle(overlayRect.Left, overlayRect.Bottom - topRect.Height, overlayRect.Width, topRect.Height);
                Rectangle centerRect = new Rectangle(overlayRect.Left, overlayRect.Top + topRect.Height, overlayRect.Width, overlayRect.Height - topRect.Height * 2);

                batch.Begin();
                batch.Draw(CardResources.HighlightEndTex, topRect, Color.White);
                batch.Draw(CardResources.HighlightEndTex, bottomRect, null, Color.White, 0.0f, Vector2.Zero, SpriteEffects.FlipVertically, 0.0f);
                batch.Draw(CardResources.HightlightCenterTex, centerRect, Color.White);
                batch.End();
            }

            // Draw the extra stacks
            for (int i = 0; i < board.CountOfExtraDealingsLeft(); i++)
            {
                Rectangle cardRect = new Rectangle(rc.Width - cardWidth - i * 25, rc.Height - cardHeight, cardWidth, cardHeight);
                batch.Begin();
                batch.Draw(CardResources.CardBackTex, cardRect, Options.CardBackColor);
                batch.End();
            }

            // Draw the undo button
            Color undoColor = (board.CanUndo() ? Color.White : Color.Multiply(Color.White, 0.4f));
            batch.Begin();
            batch.Draw(CardResources.UndoTex, undoButtonRect, undoColor);
            batch.End();

            // Draw the completed stacks
            for (int i = 0; i < board.CompletedCount(); i++)
            {
                Card completedCard = board.GetCompletedStack(i);
                completedCard.View.Render(batch);
            }
                

            // Draw the cards being dragged
            if (currentAction == CardAction.Dragging)
            {
                for (int i=0; i < cardsInAction.Count; i++)
                {
                    Card card = cardsInAction[i];
                    card.View.Render(batch);
                }
            }

            if (board.IsBoardClear())
            {
                Vector2 winTextSize = CardResources.WinFont.MeasureString("You Win!");
                Vector2 winPos = new Vector2(viewRect.Width / 2 - winTextSize.X / 2, viewRect.Height / 2 - winTextSize.Y / 2);

                batch.Begin();
                batch.DrawString(CardResources.WinFont, "You Win!", winPos, Color.Black);
                batch.End();

                if (completed)
                {
                    Vector2 newGameSize = CardResources.AgainFont.MeasureString("Tap to start a new game");
                    Vector2 pos = new Vector2(viewRect.Width / 2 - newGameSize.X / 2, viewRect.Height / 2 - newGameSize.Y / 2 + winTextSize.Y);

                    batch.Begin();
                    batch.DrawString(CardResources.AgainFont, "Tap to start a new game", pos, Color.Black);
                    batch.End();
                }
            }
            else if (errors.Count > 0)
            {
            }
            

            foreach (Animation animation in currentAnimations)
                animation.Render(batch);

            if (currentAction == CardAction.Selecting)
            {
                batch.Begin();
                batch.Draw(CardResources.BlankTex, board.View.GetViewArea(), new Color(0, 0, 0, 128));
                batch.End();

                foreach (CardAnimationView animation in cardsSelectionAnimation)
                    animation.Render(batch);
            }
        }

        public void FixupStackCardViewRects(CardStack stack)
        {
            for (int c = 0; c < stack.Count; c++)
            {
                Card card = stack.GetCard(c);
                card.View.Rect = GetRectOfCardRunInStack(stack, c, 1);
            }
        }

        public void FixupCompletedCardViewRects()
        {
            Point cardSize = GetCardSize();
            for (int i = 0; i < board.CompletedCount(); i++)
            {
                Card completedCard = board.GetCompletedStack(i);
                completedCard.View.Rect = new Rectangle(i * 25 + undoButtonRect.Width + 20, viewRect.Height - cardSize.Y, cardSize.X, cardSize.Y);
            }
        }

        public void FixupCardHighlights()
        {
            if (cardsInAction != null && currentAction != CardAction.None)
            {
                for (int i = 0; i < Board.StackCount; i++)
                {
                    CardStack stack = board.GetStack(i);
                    foreach (Card card in stack.GetCards())
                    {
                        card.View.Highlighted = false;
                    }
                    foreach (Card cardInAction in cardsInAction)
                    {
                        if (board.CanMoveCardToStack(cardInAction, stack))
                        {
                            if (stack.Count > 0)
                                stack.GetLastCard().View.Highlighted = true;
                            break;
                        }
                    }
                }
            }
            else
            {
                for (int i = 0; i < Board.StackCount; i++)
                {
                    CardStack stack = board.GetStack(i);
                    foreach (Card card in stack.GetCards())
                    {
                        card.View.Highlighted = true;
                    }
                }
            }

            // For now, ensure that all completed cards are highlighted
            for (int i = 0; i < board.CompletedCount(); i++)
            {
                Card completedCard = board.GetCompletedStack(i);
                completedCard.View.Highlighted = true;
            }
        }

        public void AddError(string error)
        {
            errors.Add(error);
        }

        #region Events
        public void Touch(Point pt)
        {
            if (completed)
            {
                StartNewGame();
                Deal();
            }
            else if (currentAction == CardAction.Selecting)
            {
                currentAction = CardAction.None;

                for (int i = 0; i < cardsSelectionAnimation.Count; i++)
                {
                    if (cardsSelectionAnimation[i].CurrentRect.Contains(pt))
                    {
                        currentAction = CardAction.Moving;
                        cardsInAction = new List<Card>();
                        for (; i < cardsSelectionAnimation.Count; i++)
                            cardsInAction.Add(cardsSelectionAnimation[i].Card);
                    }
                }
                CloseStack(board.GetStack(currentStack), cardsSelectionAnimation);
                cardsSelectionAnimation = null;
            }
            else if (currentAction == CardAction.Dragging)
            {
                currentAction = CardAction.None;

                CardStack destStack;
                Rectangle card0Rect = cardsInAction[0].View.Rect;
                CardStack destStack1 = GetStackAtPoint(card0Rect.Location);
                CardStack destStack2 = GetStackAtPoint(new Point(card0Rect.Right, card0Rect.Top));
				CardStack destStack3 = GetStackAtPoint(pt);
                if (destStack1 != null && destStack2 != null) // Check based on card position first
                {
                    Rectangle destRect1 = GetAreaOfStack(destStack1);
                    Rectangle destRect2 = GetAreaOfStack(destStack2);

                    if (destRect1.Width - (card0Rect.Left - destRect1.Left) > card0Rect.Right - destRect2.Left)
                        destStack = destStack1;
                    else
                        destStack = destStack2;
                }
                else if (destStack1 != null) // Prefer card location instead of top right
                {
                    destStack = destStack1;
                }
				else if (destStack2 != null) // Prefer top right instead of touch pos
				{
					destStack = destStack2;
				}
				else // Accept touch pos if we don't have a better option
				{
					destStack = destStack3;
				}

                CardStack srcStack = board.GetStack(currentStack);
                bool movedRun = false;
                for (int t = 0; t < 2 && !movedRun; t++)
                {
                    if (destStack != null && destStack.Index != currentStack)
                    {
                        for (int i = 0; i < cardsInAction.Count; i++)
                        {
                            if (board.CanMoveCardToStack(cardsInAction[i], destStack))
                            {
                                MoveCards(srcStack, srcStack.Count - cardsInAction.Count + i, destStack);
                                if (destStack.ContainsCompleteRun())
                                    ClearRun(destStack);
                                movedRun = true;
                                break;
                            }
                        }
                    }
                    destStack = (destStack == destStack1 ? destStack2 : destStack1);
                }
                if (!movedRun)
                {
                    FixupStackCardViewRects(srcStack);
                }
                cardsInAction = null;
            }
            else if (currentAction == CardAction.Moving)
            {
                currentAction = CardAction.None;

                CardStack stack = GetStackAtPoint(pt);
                if (stack != null)
                {
                    if (stack.Index == currentStack)
                    {
                        PickFromStack(stack, pt);
                    }
                    else
                    {
                        for (int i=0; i < cardsInAction.Count; i++)
                        {
                            if (board.CanMoveCardToStack(cardsInAction[i], stack))
                            {
                                MoveCards(board.GetStack(currentStack), board.GetStack(currentStack).Count - cardsInAction.Count + i, stack);
                                if (stack.ContainsCompleteRun())
                                    ClearRun(stack);
                                cardsInAction = null;
                                break;
                            }
                        }
                        
                        if (cardsInAction != null)
                        {
                            cardsInAction = null;
                            if (stack.Count > 0)
                                SelectStack(stack);
                        }
                    }
                }
            }
            else if (GetExtraStackArea().Contains(pt))
            {
                if (board.CountOfExtraDealingsLeft() > 0)
                    Deal();
            }
            else if (GetCardArea().Contains(pt))
            {
                CardStack stack = GetStackAtPoint(pt);
                if (stack != null)
                {
                    if (stack.Count > 0)
                        SelectStack(stack);
                }
            }
            else if (undoButtonRect.Contains(pt) && board.CanUndo())
            {
                UndoAction nextAction = board.NextUndoAction();
                board.Undo();
                FixupStackCardViewRects(nextAction.SourceStack);
                FixupStackCardViewRects(nextAction.DestStack);
            }

            dragInfo = null;
            FixupCardHighlights();
        }

        public void StartDrag(Point pt)
        {
            CardStack cardStack = GetStackAtPoint(pt);
            if (cardStack != null && cardStack.Count > 0 && (currentAction == CardAction.None || currentAction == CardAction.Moving))
            {
                dragInfo = new DragInfo();
                
                int top = cardStack.GetTopOfSequentialRun();
                dragInfo.cardsToDrag = cardStack.GetCards();
                dragInfo.cardsToDrag.RemoveRange(0, top);

                dragInfo.stack = cardStack.Index;

                Point cardOrigin = GetLocationOfCardInStack(cardStack, cardStack.Count - dragInfo.cardsToDrag.Count);
                dragInfo.startPos = pt;
                dragInfo.offset = new Point(dragInfo.startPos.X - cardOrigin.X, dragInfo.startPos.Y - cardOrigin.Y);
            }
        }

        public void ContinueDrag(Point pt)
        {
            if (dragInfo != null && DistanceSq(pt, dragInfo.startPos) > MiniumumDragDistanceSq && currentAction != CardAction.Dragging)
            {
                currentAction = CardAction.Dragging;
                cardsInAction = dragInfo.cardsToDrag;
                currentStack = dragInfo.stack;

                FixupCardHighlights();
            }

            if (currentAction == CardAction.Dragging)
            {
                dragInfo.currentPos = pt;
                Point origin = new Point(dragInfo.currentPos.X - dragInfo.offset.X, dragInfo.currentPos.Y - dragInfo.offset.Y);
                int delta = GetVisibleCardDelta(board.GetStack(dragInfo.stack));
                for (int i = 0; i < cardsInAction.Count; i++)
                {
                    cardsInAction[i].View.Rect = new Rectangle(origin.X, origin.Y + i * delta, cardSize.X, cardSize.Y);
                }
            }
        }
        #endregion

        #region Animation Triggers
        public void Deal()
        {
            // Only 1 deal allowed at a time
            foreach (Animation animation in currentAnimations)
            {
                if (animation is DealAnimation)
                    return;
            }

            board.ClearUndoStack();

            Animation dealAnimation = new DealAnimation(board, board.CardsInNextDeal()) { OnAnimationCompleted = OnCompletedDealAnimation };
            currentAnimations.Add(dealAnimation);
        }

        public void SelectStack(CardStack stack)
        {
            if (stack.Count > 0)
            {
                currentAction = CardAction.Moving;
                int top = stack.GetTopOfSequentialRun();
                cardsInAction = stack.GetCards();
                cardsInAction.RemoveRange(0, top);
           
                currentStack = stack.Index;
            }
        }

        public void PickFromStack(CardStack stack, Point ptExpand)
        {
            if (stack.Count > 0)
            {
                int top = stack.GetTopOfSequentialRun();
                if (top < stack.Count - 1)
                {
                    Animation expandAnimation = new StackExpandAnimation(board, stack, ptExpand) { OnAnimationCompleted = OnCompletedExpandAnimation };
                    currentAnimations.Add(expandAnimation);
                }
                else
                {
                    SelectStack(stack);
                }
                currentStack = stack.Index;
            }
        }

        public void CloseStack(CardStack stack, List<CardAnimationView> expandedCards)
        {
            Animation collapseAnimation = new StackCollapseAnimation(board, stack.Index, expandedCards) { OnAnimationCompleted = OnCompletedCollapseAnimation };
            currentAnimations.Add(collapseAnimation);
        }

        public void MoveCards(CardStack stackSrc, int posSrc, CardStack stackDest)
        {
            board.MoveCards(stackSrc, posSrc, stackDest);
            FixupStackCardViewRects(stackSrc);
            FixupStackCardViewRects(stackDest);
        }

        public void ClearRun(CardStack stack)
        {
            // Need to account for any other run clearing in progress
            int clearAnimations = 0;
            foreach (Animation animation in currentAnimations)
            {
                if (animation is ClearRunAnimation)
                    clearAnimations++;
            }

            board.ClearUndoStack();

            Point destPoint = new Point((board.CompletedCount() + clearAnimations) * 25 + undoButtonRect.Width + 20, viewRect.Height - cardSize.Y);
            Animation clearAnimation = new ClearRunAnimation(board, stack.Index, destPoint) { OnAnimationCompleted = OnCompletedClearRunAnimation };
            currentAnimations.Add(clearAnimation);
        }

        public void FinishGame()
        {
            Animation winAnimation = new WinAnimation(board) { OnAnimationCompleted = OnCompletedWinAnimation };
            currentAnimations.Add(winAnimation);
        }
        #endregion

        #region Animation Callbacks
        private void OnCompletedDealAnimation(Animation animation)
        {
            board.Deal();
            
            for (int i=0; i < Board.StackCount; i++)
            {
                CardStack stack = board.GetStack(i);
                FixupStackCardViewRects(stack);
            }

            for (int i = 0; i < Board.StackCount; i++)
            {
                if (board.GetStack(i).ContainsCompleteRun())
                    ClearRun(board.GetStack(i));
            }
        }

        private void OnCompletedExpandAnimation(Animation animation)
        {
            currentAction = CardAction.Selecting;
            cardsSelectionAnimation = (animation as StackExpandAnimation).cardAnimations;
        }

        private void OnCompletedCollapseAnimation(Animation animation)
        {
        }

        private void OnCompletedClearRunAnimation(Animation animation)
        {
            CardStack stack = board.GetStack((animation as ClearRunAnimation).Stack);
            board.RemoveCompleteRun(stack, true, true);
            FixupStackCardViewRects(stack);
            FixupCompletedCardViewRects();

            if (board.IsBoardClear())
            {
                FinishGame();
            }
        }

        private void OnCompletedWinAnimation(Animation animation)
        {
            completed = true;
        }
        #endregion

        private int DistanceSq(Point a, Point b)
        {
            return ((a.X - b.X) * (a.X - b.X)) + ((a.Y - b.Y) * (a.Y - b.Y));
        }
    }
}
