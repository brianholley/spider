using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Spider
{
	internal class BoardView
	{
		private readonly Board _board;
		private Rectangle _viewRect;

		private Point _cardSize;

		private List<Animation> _currentAnimations;

		private enum CardAction
		{
			None,
			Moving,
			Selecting,
			Dragging
		}

		private CardAction _currentAction = CardAction.None;
		private int _currentStack;
		private List<Card> _cardsInAction;
		private List<CardAnimationView> _cardsSelectionAnimation;

		private class DragInfo
		{
			public Point StartPos;
			public Point Offset;
			public Point CurrentPos;

			public int Stack;
			public List<Card> CardsToDrag;
		}
		private DragInfo _dragInfo;

		private bool _completed;

		public List<string> Errors;

		private Rectangle _undoButtonRect;

		private const int MiniumumDragDistanceSq = 10*10;

		// TODO: Show errors + alerts (strings, buzzer, etc.)
		public BoardView(Board board, Rectangle viewRect)
		{
			_board = board;
			_viewRect = viewRect;

			int cardWidth = viewRect.Width/10 - 4;
			int cardHeight = (int) (cardWidth*(3.5/2.5));
			_cardSize = new Point(cardWidth, cardHeight);

			_currentAnimations = new List<Animation>();
			Errors = new List<string>();

			_undoButtonRect = new Rectangle(10, viewRect.Height - cardHeight + (cardHeight - cardWidth)/2, cardWidth, cardWidth);

			// Ensure that we have the deck color options loaded
			Options.Load();

			Color mutedBackColor = Options.CardBackColor*(169.0f/255.0f);
			mutedBackColor.A = 255; // Fix premultiplied alpha value
			CardView.MutedBackColor = mutedBackColor;
		}

		#region Position and Size Helpers

		public Rectangle GetViewArea()
		{
			return _viewRect;
		}

		public Rectangle GetCardArea()
		{
			return new Rectangle(0, 0, _viewRect.Width, _viewRect.Height - _cardSize.Y);
		}

		public Point GetCardSize()
		{
			return _cardSize;
		}

		public Rectangle GetAreaOfStack(CardStack stack)
		{
			return GetRectOfCardRunInStack(stack, 0, stack.Count);
		}

		public CardStack GetStackAtPoint(Point pt)
		{
			int spacing = (_viewRect.Width - _cardSize.X*Board.StackCount)/(Board.StackCount - 1);
			int index = pt.X/(_cardSize.X + spacing);
			if (index >= 0 && index < Board.StackCount)
			{
				Rectangle stackArea = GetAreaOfStack(_board.GetStack(index));
				if (stackArea.Contains(pt))
					return _board.GetStack(index);
			}
			return null;
		}

		public Point GetLocationOfStack(CardStack stack)
		{
			int spacing = (_viewRect.Width - _cardSize.X*Board.StackCount)/(Board.StackCount - 1);
			return new Point(stack.Index*(_cardSize.X + spacing), 0);
		}

		public Point GetLocationOfCardInStack(CardStack stack, int card)
		{
			Point stackLoc = GetLocationOfStack(stack);
			int hidden = stack.GetCountOfHiddenCards();
			int delta = GetVisibleCardDelta(stack);
			int y = (card < hidden ? card*HiddenCardDelta : hidden*HiddenCardDelta + (card - hidden)*delta);
			return new Point(stackLoc.X, y);
		}

		public Rectangle GetRectOfCardRunInStack(CardStack stack, int card, int cards)
		{
			Point loc = GetLocationOfCardInStack(stack, card);
			int hidden = stack.GetCountOfHiddenCards();
			int hiddenUsed = Math.Max(Math.Min(hidden, card + cards) - card, 0);
			int delta = GetVisibleCardDelta(stack);
			int height = hiddenUsed*HiddenCardDelta + (cards - hiddenUsed - 1)*delta + _cardSize.Y;
			return new Rectangle(loc.X, loc.Y, _cardSize.X, height);
		}

		public int CardDelta
		{
			get { return 25; }
		}

		public int HiddenCardDelta
		{
			get { return 10; }
		}

		private int GetVisibleCardDelta(CardStack stack)
		{
			if (stack.Count > 0)
			{
				int hidden = stack.GetCountOfHiddenCards();
				int delta = (stack.Count > hidden
					? (_viewRect.Height - _cardSize.Y*2 - hidden*HiddenCardDelta)/(stack.Count - hidden)
					: CardDelta);
				return Math.Min(delta, CardDelta);
			}
			else
			{
				return 0;
			}
		}

		public Rectangle GetExtraStackArea()
		{
			return new Rectangle(_viewRect.Width - _cardSize.X - Board.ExtrasCount*25, _viewRect.Height - _cardSize.Y,
				_cardSize.X + Board.ExtrasCount*25, _cardSize.Y);
		}

		public Point GetLocationOfExtraStack(int stack)
		{
			return new Point(_viewRect.Width - _cardSize.X - stack*25, _viewRect.Height - _cardSize.Y);
		}

		#endregion

		public void StartNewGame()
		{
			Reset();

			_board.StartNewGame();

			for (int i = 0; i < Board.StackCount; i++)
				FixupStackCardViewRects(_board.GetStack(i));
		}

		public void Reset()
		{
			_currentAnimations = new List<Animation>();

			_currentAction = CardAction.None;
			_currentStack = 0;
			_cardsInAction = null;
			_cardsSelectionAnimation = null;

			_completed = false;

			Errors.Clear();
		}

		public void Update()
		{
			for (int i = 0; i < _currentAnimations.Count;)
			{
				if (_currentAnimations[i].Update())
				{
					i++;
				}
				else
				{
					AnimationCompleteCallback onCompleteCallback = _currentAnimations[i].OnAnimationCompleted;
					Animation completeAnimation = _currentAnimations[i];
					_currentAnimations.RemoveAt(i);
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

			int cardWidth = rc.Width/10 - 4;
			int cardHeight = (int) (cardWidth*(3.5/2.5));
			int spacing = (rc.Width - cardWidth*Board.StackCount)/(Board.StackCount - 1);

			// Draw the game board
			for (int i = 0; i < Board.StackCount; i++)
			{
				int stackSize = _board.GetStack(i).Count;
				if (stackSize == 0)
				{
					var cardRect = new Rectangle(i*(cardWidth + spacing), 0, cardWidth, cardHeight);
					batch.Begin();
					batch.Draw(CardResources.PlaceholderTex, cardRect, Color.White);
					batch.End();
				}
				else
				{
					int stackCount = _board.GetStack(i).Count - (_currentAction == CardAction.Dragging && i == _currentStack ? _cardsInAction.Count : 0);
					for (int c = 0; c < stackSize; c++)
					{
						Card card = _board.GetStack(i).GetCard(c);
						if (!card.View.Animating && c < stackCount)
						{
							card.View.Render(batch);
						}
					}
				}
			}

			// Draw the current mover overlay
			if (_currentAction == CardAction.Moving)
			{
				CardStack stack = _board.GetStack(_currentStack);
				int stackSize = stack.Count;
				Point pt = _board.View.GetLocationOfCardInStack(_board.GetStack(_currentStack), stackSize - _cardsInAction.Count);
				Point pt2 = _board.View.GetLocationOfCardInStack(_board.GetStack(_currentStack), stackSize - 1);
				Point size = _board.View.GetCardSize();
				var overlayRect = new Rectangle(pt.X, pt.Y, size.X, pt2.Y - pt.Y + size.Y);
				overlayRect.Inflate(size.X/20, size.Y/20);

				var topRect = new Rectangle(overlayRect.Left, overlayRect.Top, overlayRect.Width, size.Y/4);
				var bottomRect = new Rectangle(overlayRect.Left, overlayRect.Bottom - topRect.Height, overlayRect.Width, topRect.Height);
				var centerRect = new Rectangle(overlayRect.Left, overlayRect.Top + topRect.Height, overlayRect.Width, overlayRect.Height - topRect.Height*2);

				batch.Begin();
				batch.Draw(CardResources.HighlightEndTex, topRect, Color.White);
				batch.Draw(CardResources.HighlightEndTex, bottomRect, null, Color.White, 0.0f, Vector2.Zero, SpriteEffects.FlipVertically, 0.0f);
				batch.Draw(CardResources.HightlightCenterTex, centerRect, Color.White);
				batch.End();
			}

			// Draw the extra stacks
			for (int i = 0; i < _board.CountOfExtraDealingsLeft(); i++)
			{
				var cardRect = new Rectangle(rc.Width - cardWidth - i*25, rc.Height - cardHeight, cardWidth, cardHeight);
				batch.Begin();
				batch.Draw(CardResources.CardBackTex, cardRect, Options.CardBackColor);
				batch.End();
			}

			// Draw the undo button
			Color undoColor = (_board.CanUndo() ? Color.White : Color.Multiply(Color.White, 0.4f));
			batch.Begin();
			batch.Draw(CardResources.UndoTex, _undoButtonRect, undoColor);
			batch.End();

			// Draw the completed stacks
			for (int i = 0; i < _board.CompletedCount(); i++)
			{
				Card completedCard = _board.GetCompletedStack(i);
				completedCard.View.Render(batch);
			}


			// Draw the cards being dragged
			if (_currentAction == CardAction.Dragging)
			{
				foreach (Card card in _cardsInAction)
					card.View.Render(batch);
			}

			if (_board.IsBoardClear())
			{
				var winTextSize = CardResources.WinFont.MeasureString("You Win!");
				var winPos = new Vector2(_viewRect.Width/2.0f - winTextSize.X/2, _viewRect.Height/2.0f - winTextSize.Y/2);

				batch.Begin();
				batch.DrawString(CardResources.WinFont, "You Win!", winPos, Color.Black);
				batch.End();

				if (_completed)
				{
					var newGameSize = CardResources.AgainFont.MeasureString("Tap to start a new game");
					var pos = new Vector2(_viewRect.Width/2.0f - newGameSize.X/2, _viewRect.Height/2.0f - newGameSize.Y/2 + winTextSize.Y);

					batch.Begin();
					batch.DrawString(CardResources.AgainFont, "Tap to start a new game", pos, Color.Black);
					batch.End();
				}
			}
			else if (Errors.Count > 0)
			{
			}


			foreach (Animation animation in _currentAnimations)
				animation.Render(batch);

			if (_currentAction == CardAction.Selecting)
			{
				batch.Begin();
				batch.Draw(CardResources.BlankTex, _board.View.GetViewArea(), new Color(0, 0, 0, 128));
				batch.End();

				foreach (CardAnimationView animation in _cardsSelectionAnimation)
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
			for (int i = 0; i < _board.CompletedCount(); i++)
			{
				Card completedCard = _board.GetCompletedStack(i);
				completedCard.View.Rect = new Rectangle(i*25 + _undoButtonRect.Width + 20, _viewRect.Height - cardSize.Y, cardSize.X,
					cardSize.Y);
			}
		}

		public void FixupCardHighlights()
		{
			if (_cardsInAction != null && _currentAction != CardAction.None)
			{
				for (int i = 0; i < Board.StackCount; i++)
				{
					CardStack stack = _board.GetStack(i);
					foreach (Card card in stack.GetCards())
					{
						card.View.Highlighted = false;
					}
					foreach (Card cardInAction in _cardsInAction)
					{
						if (_board.CanMoveCardToStack(cardInAction, stack))
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
					CardStack stack = _board.GetStack(i);
					foreach (Card card in stack.GetCards())
					{
						card.View.Highlighted = true;
					}
				}
			}

			// For now, ensure that all completed cards are highlighted
			for (int i = 0; i < _board.CompletedCount(); i++)
			{
				Card completedCard = _board.GetCompletedStack(i);
				completedCard.View.Highlighted = true;
			}
		}

		public void AddError(string error)
		{
			Errors.Add(error);
		}

		#region Events

		public void Touch(Point pt)
		{
			if (_completed)
			{
				StartNewGame();
				Deal();
			}
			else if (_currentAction == CardAction.Selecting)
			{
				_currentAction = CardAction.None;

				for (int i = 0; i < _cardsSelectionAnimation.Count; i++)
				{
					if (_cardsSelectionAnimation[i].CurrentRect.Contains(pt))
					{
						_currentAction = CardAction.Moving;
						_cardsInAction = new List<Card>();
						for (; i < _cardsSelectionAnimation.Count; i++)
							_cardsInAction.Add(_cardsSelectionAnimation[i].Card);
					}
				}
				CloseStack(_board.GetStack(_currentStack), _cardsSelectionAnimation);
				_cardsSelectionAnimation = null;
			}
			else if (_currentAction == CardAction.Dragging)
			{
				_currentAction = CardAction.None;

				CardStack destStack;
				Rectangle card0Rect = _cardsInAction[0].View.Rect;
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

				CardStack srcStack = _board.GetStack(_currentStack);
				bool movedRun = false;
				for (int t = 0; t < 2 && !movedRun; t++)
				{
					if (destStack != null && destStack.Index != _currentStack)
					{
						for (int i = 0; i < _cardsInAction.Count; i++)
						{
							if (_board.CanMoveCardToStack(_cardsInAction[i], destStack))
							{
								MoveCards(srcStack, srcStack.Count - _cardsInAction.Count + i, destStack);
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
				_cardsInAction = null;
			}
			else if (_currentAction == CardAction.Moving)
			{
				_currentAction = CardAction.None;

				CardStack stack = GetStackAtPoint(pt);
				if (stack != null)
				{
					if (stack.Index == _currentStack)
					{
						PickFromStack(stack, pt);
					}
					else
					{
						for (int i = 0; i < _cardsInAction.Count; i++)
						{
							if (_board.CanMoveCardToStack(_cardsInAction[i], stack))
							{
								MoveCards(_board.GetStack(_currentStack), _board.GetStack(_currentStack).Count - _cardsInAction.Count + i, stack);
								if (stack.ContainsCompleteRun())
									ClearRun(stack);
								_cardsInAction = null;
								break;
							}
						}

						if (_cardsInAction != null)
						{
							_cardsInAction = null;
							if (stack.Count > 0)
								SelectStack(stack);
						}
					}
				}
			}
			else if (GetExtraStackArea().Contains(pt))
			{
				if (_board.CountOfExtraDealingsLeft() > 0)
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
			else if (_undoButtonRect.Contains(pt) && _board.CanUndo())
			{
				UndoAction nextAction = _board.NextUndoAction();
				_board.Undo();
				FixupStackCardViewRects(nextAction.SourceStack);
				FixupStackCardViewRects(nextAction.DestStack);
			}

			_dragInfo = null;
			FixupCardHighlights();
		}

		public void StartDrag(Point pt)
		{
			CardStack cardStack = GetStackAtPoint(pt);
			if (cardStack != null && cardStack.Count > 0 &&
			    (_currentAction == CardAction.None || _currentAction == CardAction.Moving))
			{
				_dragInfo = new DragInfo();

				int top = cardStack.GetTopOfSequentialRun();
				_dragInfo.CardsToDrag = cardStack.GetCards();
				_dragInfo.CardsToDrag.RemoveRange(0, top);

				_dragInfo.Stack = cardStack.Index;

				Point cardOrigin = GetLocationOfCardInStack(cardStack, cardStack.Count - _dragInfo.CardsToDrag.Count);
				_dragInfo.StartPos = pt;
				_dragInfo.Offset = new Point(_dragInfo.StartPos.X - cardOrigin.X, _dragInfo.StartPos.Y - cardOrigin.Y);
			}
		}

		public void ContinueDrag(Point pt)
		{
			if (_dragInfo != null && DistanceSq(pt, _dragInfo.StartPos) > MiniumumDragDistanceSq &&
			    _currentAction != CardAction.Dragging)
			{
				_currentAction = CardAction.Dragging;
				_cardsInAction = _dragInfo.CardsToDrag;
				_currentStack = _dragInfo.Stack;

				FixupCardHighlights();
			}

			if (_currentAction == CardAction.Dragging)
			{
				_dragInfo.CurrentPos = pt;
				Point origin = new Point(_dragInfo.CurrentPos.X - _dragInfo.Offset.X, _dragInfo.CurrentPos.Y - _dragInfo.Offset.Y);
				int delta = GetVisibleCardDelta(_board.GetStack(_dragInfo.Stack));
				for (int i = 0; i < _cardsInAction.Count; i++)
				{
					_cardsInAction[i].View.Rect = new Rectangle(origin.X, origin.Y + i*delta, _cardSize.X, _cardSize.Y);
				}
			}
		}

		#endregion

		#region Animation Triggers

		public void Deal()
		{
			// Only 1 deal allowed at a time
			foreach (Animation animation in _currentAnimations)
			{
				if (animation is DealAnimation)
					return;
			}

			_board.ClearUndoStack();

			Animation dealAnimation = new DealAnimation(_board, _board.CardsInNextDeal())
			{
				OnAnimationCompleted = OnCompletedDealAnimation
			};
			_currentAnimations.Add(dealAnimation);
		}

		public void SelectStack(CardStack stack)
		{
			if (stack.Count > 0)
			{
				_currentAction = CardAction.Moving;
				int top = stack.GetTopOfSequentialRun();
				_cardsInAction = stack.GetCards();
				_cardsInAction.RemoveRange(0, top);

				_currentStack = stack.Index;
			}
		}

		public void PickFromStack(CardStack stack, Point ptExpand)
		{
			if (stack.Count > 0)
			{
				int top = stack.GetTopOfSequentialRun();
				if (top < stack.Count - 1)
				{
					Animation expandAnimation = new StackExpandAnimation(_board, stack, ptExpand)
					{
						OnAnimationCompleted = OnCompletedExpandAnimation
					};
					_currentAnimations.Add(expandAnimation);
				}
				else
				{
					SelectStack(stack);
				}
				_currentStack = stack.Index;
			}
		}

		public void CloseStack(CardStack stack, List<CardAnimationView> expandedCards)
		{
			Animation collapseAnimation = new StackCollapseAnimation(_board, stack.Index, expandedCards)
			{
				OnAnimationCompleted = OnCompletedCollapseAnimation
			};
			_currentAnimations.Add(collapseAnimation);
		}

		public void MoveCards(CardStack stackSrc, int posSrc, CardStack stackDest)
		{
			_board.MoveCards(stackSrc, posSrc, stackDest);
			FixupStackCardViewRects(stackSrc);
			FixupStackCardViewRects(stackDest);
		}

		public void ClearRun(CardStack stack)
		{
			// Need to account for any other run clearing in progress
			int clearAnimations = 0;
			foreach (Animation animation in _currentAnimations)
			{
				if (animation is ClearRunAnimation)
					clearAnimations++;
			}

			_board.ClearUndoStack();

			Point destPoint = new Point((_board.CompletedCount() + clearAnimations)*25 + _undoButtonRect.Width + 20,
				_viewRect.Height - _cardSize.Y);
			Animation clearAnimation = new ClearRunAnimation(_board, stack.Index, destPoint)
			{
				OnAnimationCompleted = OnCompletedClearRunAnimation
			};
			_currentAnimations.Add(clearAnimation);
		}

		public void FinishGame()
		{
			Animation winAnimation = new WinAnimation(_board) {OnAnimationCompleted = OnCompletedWinAnimation};
			_currentAnimations.Add(winAnimation);
		}

		#endregion

		#region Animation Callbacks

		private void OnCompletedDealAnimation(Animation animation)
		{
			_board.Deal();

			for (int i = 0; i < Board.StackCount; i++)
			{
				CardStack stack = _board.GetStack(i);
				FixupStackCardViewRects(stack);
			}

			for (int i = 0; i < Board.StackCount; i++)
			{
				if (_board.GetStack(i).ContainsCompleteRun())
					ClearRun(_board.GetStack(i));
			}
		}

		private void OnCompletedExpandAnimation(Animation animation)
		{
			_currentAction = CardAction.Selecting;
			_cardsSelectionAnimation = (animation as StackExpandAnimation).CardAnimations;
		}

		private void OnCompletedCollapseAnimation(Animation animation)
		{
		}

		private void OnCompletedClearRunAnimation(Animation animation)
		{
			CardStack stack = _board.GetStack((animation as ClearRunAnimation).Stack);
			_board.RemoveCompleteRun(stack, true, true);
			FixupStackCardViewRects(stack);
			FixupCompletedCardViewRects();

			if (_board.IsBoardClear())
			{
				FinishGame();
			}
		}

		private void OnCompletedWinAnimation(Animation animation)
		{
			_completed = true;
		}

		#endregion

		private int DistanceSq(Point a, Point b)
		{
			return ((a.X - b.X)*(a.X - b.X)) + ((a.Y - b.Y)*(a.Y - b.Y));
		}
	}
}