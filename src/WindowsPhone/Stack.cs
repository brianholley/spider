using System;
using System.Collections.Generic;

namespace Spider
{
	internal class CardStack
	{
		private readonly List<Card> _cards = new List<Card>();

		public int Index { get; private set; }

		public int Count
		{
			get { return _cards.Count; }
		}

		public CardStack(int index)
		{
			Index = index;
		}

		public void Add(Card card)
		{
			_cards.Add(card);
		}

		public void RemoveRange(int index, int count)
		{
			_cards.RemoveRange(index, count);
		}

		public void Clear()
		{
			_cards.Clear();
		}

		public Card GetCard(int pos)
		{
			return _cards[pos];
		}

		public List<Card> GetCards()
		{
			return new List<Card>(_cards);
		}

		public Card GetLastCard()
		{
			if (_cards.Count > 0)
				return _cards[_cards.Count - 1];
			return null;
		}

		public int GetCountOfHiddenCards()
		{
			int c = 0;
			foreach (Card card in _cards)
			{
				if (card.Visible)
					break;
				c++;
			}
			return c;
		}

		public bool CanPickupRun(int pos)
		{
			int topOfRun = GetTopOfSequentialRun();
			return (pos >= topOfRun && pos < _cards.Count);
		}

		public int GetTopOfSequentialRun()
		{
			if (_cards.Count == 0)
				return -1;

			int i = _cards.Count - 1;
			Card current = GetLastCard();
			for (; i > 0; i--)
			{
				Card next = _cards[i - 1];
				if (!next.Visible || next.Value != current.Value + 1 || next.Suit != current.Suit)
					break;
				current = next;
			}
			return i;
		}

		public bool ContainsCompleteRun()
		{
			int top = GetTopOfSequentialRun();
			if (_cards.Count - top == 13)
			{
				Card topCard = _cards[top];
				Card bottomCard = GetLastCard();
				if (topCard.Value == Value.King && bottomCard.Value == Value.Ace)
					return true;
			}
			return false;
		}

		public List<Card> RemoveCompleteRun(bool revealCard)
		{
			if (!ContainsCompleteRun())
			{
				Console.WriteLine("Asked to remove run from stack when run is not complete - bug?");
				return null;
			}

			var completed = new List<Card>(_cards.GetRange(_cards.Count - 13, 13));
			_cards.RemoveRange(_cards.Count - 13, 13);

			if (_cards.Count > 0 && revealCard)
			{
				Card card = GetLastCard();
				if (!card.Visible)
					card.Reveal();
			}
			return completed;
		}
	}
}