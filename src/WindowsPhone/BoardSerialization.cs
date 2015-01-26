using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace Spider
{
    [XmlRootAttribute("SpiderGame")]
    public class GameSerialization
    {
        [XmlAttribute]
        public int Suits;
        [XmlAttribute]
        public int MoveCount;
        [XmlAttribute]
        public int Score;

        public BoardSerialization Board = new BoardSerialization();
        public UndoStackSerialization UndoStack = new UndoStackSerialization();
    }

    public class BoardSerialization
    {
        public List<CardStackSerialization> Stacks = new List<CardStackSerialization>();
        public List<CardStackSerialization> Extras = new List<CardStackSerialization>();
        public List<CardStackSerialization> Completed = new List<CardStackSerialization>();
    }

    public class CardStackSerialization
    {
        public List<CardSerialization> Cards = new List<CardSerialization>();
    }

    public class CardSerialization
    {
        [XmlAttribute]
        public int Value;
        [XmlAttribute]
        public int Suit;
        [XmlAttribute]
        public bool Visible;
    }

    // TODO: Undo stack serialization is pretty hacky right now - but it should work
    public class UndoStackSerialization
    {
        public List<UndoActionSerialization> Actions = new List<UndoActionSerialization>();
    }

    public class UndoActionSerialization
    {
        [XmlAttribute]
        public int CardCount;
        [XmlAttribute]
        public int SourceStack;
        [XmlAttribute]
        public int DestStack;
        [XmlAttribute]
        public bool RevealedCard;
    }
}
