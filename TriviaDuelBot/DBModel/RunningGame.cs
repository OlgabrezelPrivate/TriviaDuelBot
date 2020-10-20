using System;
using System.Collections.Generic;
using System.Text;
using SQLite;

namespace TriviaDuelBot.DBModel
{
    [Table("RunningGame")]
    public class RunningGame
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public int Player1Id { get; set; }
        public int Player2Id { get; set; }

        public string Player1QuizzerName { get; set; }
        public string Player2QuizzerName { get; set; }

        /// <summary>
        /// This is a bit tricky. If the value is 0, the second player has not accepted the
        /// duel yet. If the value is 1, Player 2 is supposed to play the first round. If the value
        /// is 2, Player 1 is supposed to play the first round and the second round. And so on. The final value is 7,
        /// when Player 1 is supposed to play the sixth (and final) round.
        /// </summary>
        public int Round { get; set; }

        public bool PlayedPending { get; set; }

        public DateTime TimeStarted { get; set; }
        public DateTime LastUpdate { get; set; }
        public int LastWarning { get; set; }

        public string CurrentQ1 { get; set; }
        public string CurrentQ2 { get; set; }
        public string CurrentQ3 { get; set; }

        public string CurrentQ1Right { get; set; }
        public string CurrentQ1Wrong1 { get; set; }
        public string CurrentQ1Wrong2 { get; set; }
        public string CurrentQ1Wrong3 { get; set; }

        public string CurrentQ2Right { get; set; }
        public string CurrentQ2Wrong1 { get; set; }
        public string CurrentQ2Wrong2 { get; set; }
        public string CurrentQ2Wrong3 { get; set; }

        public string CurrentQ3Right { get; set; }
        public string CurrentQ3Wrong1 { get; set; }
        public string CurrentQ3Wrong2 { get; set; }
        public string CurrentQ3Wrong3 { get; set; }
        
        public int R1Category { get; set; }
        public int R2Category { get; set; }
        public int R3Category { get; set; }
        public int R4Category { get; set; }
        public int R5Category { get; set; }
        public int R6Category { get; set; }

        public int Player1Correct { get; set; }
        public int Player2Correct { get; set; }
    }
}
