using System;
using System.Collections.Generic;
using System.Text;
using SQLite;

namespace TriviaDuelBot.DBModel
{
    [Table("Settings")]
    public class Settings
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public long BotOwner { get; set; }

        public long LogChat { get; set; }

        public string BotToken { get; set; }
    }
}
