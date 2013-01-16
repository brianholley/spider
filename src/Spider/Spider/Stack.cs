using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Spider
{
    class CardStack
    {
        private List<Card> cards = new List<Card>();

        public int Index { get; private set; }
        public int Count { get { return cards.Count; } }

        public CardStack(int index)
        {
            Index = index;
        }

        public void Add(Card card)
        {
            cards.Add(card);
        }

        public void RemoveRange(int index, int count)
        {
            cards.RemoveRange(index, count);
        }

        public void Clear()
        {
            cards.Clear();
        }

        public Card GetCard(int pos)
        {
            return cards[pos];
        }

        public List<Card> GetCards()
        {
            return new List<Card>(cards);
        }

        public Card GetLastCard()
        {
            if (cards.Count > 0)
                return cards[cards.Count - 1];
            return null;
        }

        public bool CanPickupRun(int pos)
        {
            int topOfRun = GetTopOfSequentialRun();
            return (pos >= topOfRun && pos < cards.Count);
        }

        public int GetTopOfSequentialRun()
        {
            if (cards.Count == 0)
                return -1;

            int i = cards.Count - 1;
            Card current = GetLastCard();
            for (; i > 0; i--)
            {
                Card next = cards[i - 1];
                if (!next.Visible || next.Value != current.Value + 1 || next.Suit != current.Suit)
                    break;
                current = next;
            }
            return i;
        }

        public bool ContainsCompleteRun()
        {
            int top = GetTopOfSequentialRun();
            if (cards.Count - top == 13)
            {
                Card topCard = cards[top];
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

            List<Card> completed = new List<Card>(cards.GetRange(cards.Count - 13, 13));
            cards.RemoveRange(cards.Count - 13, 13);

            if (cards.Count > 0 && revealCard)
            {
                Card card = GetLastCard();
                if (!card.Visible)
                    card.Reveal();
            }
            return completed;
        }
    }
}
