using System;
using System.Collections.Generic;
using System.Text;
using SQLite;

namespace TriviaDuelBot.DBModel
{
    [Table("Game")]
    public class Game
    {
        [PrimaryKey]
        public int Id { get; set; }

        public int Player1Id { get; set; }
        public int Player1Points { get; set; }

        public int Player2Id { get; set; }
        public int Player2Points { get; set; }

        public int WinnerId { get; set; }

        public int UntilRound { get; set; }

        public DateTime TimeStarted { get; set; }
        public DateTime TimeEnded { get; set; }

        public int Round1Category { get; set; }
        public int Round2Category { get; set; }
        public int Round3Category { get; set; }
        public int Round4Category { get; set; }
        public int Round5Category { get; set; }
        public int Round6Category { get; set; }
    }
}
