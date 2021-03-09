using System;
using System.Collections.Generic;
using System.Text;
using SQLite;

namespace TriviaDuelBot.DBModel
{
    [Table("Player")]
    public class Player
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [Indexed]
        public long TelegramId { get; set; }

        public string Name { get; set; }

        [Collation("NOCASE")]
        public string Username { get; set; }

        [Collation("NOCASE")]
        public string QuizzerName { get; set; }

        public int Score { get; set; }

        public int PerfectGames { get; set; }
    }
}
