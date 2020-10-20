using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SQLite;
using TriviaDuelBot.DBModel;
using TriviaDuelBot.TriviaDuel;

namespace TriviaDuelBot
{
    public static class Database
    {
        private static SQLiteConnection DB;

        #region Init / Settings
        /// <summary>
        /// Initiate the database connection and create the tables if they don't exist yet
        /// </summary>
        public static void Init()
        {
            DB = new SQLiteConnection(Path.Combine(Constants.BasePath, Constants.AppFolder, Constants.DatabaseName));
            DB.CreateTable<Player>();
            DB.CreateTable<Settings>();
            DB.CreateTable<Game>();
            DB.CreateTable<RunningGame>();
        }

        /// <summary>
        /// Read the settings from the database
        /// </summary>
        /// <returns><see cref="true"/> if the settings are present and could be read, <see cref="false"/> otherwise.</returns>
        public static bool ReadSettings()
        {
            var s = DB.Table<Settings>().FirstOrDefault();
            if (s == null || s.BotOwner == 0 || s.LogChat == 0 || string.IsNullOrEmpty(s.BotToken))
                return false;

            Constants.BotOwner = s.BotOwner;
            Constants.LogChat = s.LogChat;
            Constants.BotToken = s.BotToken;
            return true;
        }

        /// <summary>
        /// Write the settings to the database
        /// </summary>
        public static void WriteSettings()
        {
            var s = DB.Table<Settings>().FirstOrDefault();
            if (s == null)
            {
                s = new Settings
                {
                    BotOwner = Constants.BotOwner,
                    LogChat = Constants.LogChat,
                    BotToken = Constants.BotToken,
                };
                DB.Insert(s);
            }
            else
            {
                s.BotOwner = Constants.BotOwner;
                s.LogChat = Constants.LogChat;
                s.BotToken = Constants.BotToken;
                DB.Update(s);
            }
        }
        #endregion

        #region Player
        public static void Player_Save(int TelegramId, string Name, string Username)
        {
            var p = DB.Table<Player>().FirstOrDefault(x => x.TelegramId == TelegramId);
            if (p == null)
            {
                p = new Player
                {
                    TelegramId = TelegramId,
                    Name = Name,
                    Username = Username,
                    QuizzerName = null,
                    PerfectGames = 0,
                    Score = 0
                };
                DB.Insert(p);
            }
            else
            {
                p.Name = Name;
                p.Username = Username;
                DB.Update(p);
            }
        }

        public static void Player_Update(Player p)
        {
            DB.Update(p);
        }

        public static void Player_UpdateScore(int Id, int Score, int PerfectGames)
        {
            var p = DB.Table<Player>().FirstOrDefault(x => x.Id == Id);
            if (p == null) return;
            p.Score = Score;
            p.PerfectGames = PerfectGames;
            DB.Update(p);
        }

        public static Player Player_Get(int Id)
        {
            return DB.Table<Player>().FirstOrDefault(x => x.Id == Id);
        }

        public static Player Player_GetByTelegramId(int TelegramId)
        {
            return DB.Table<Player>().FirstOrDefault(x => x.TelegramId == TelegramId);
        }

        public static Player Player_GetByQuizzerName(string QuizzerName)
        {
            return DB.Table<Player>().FirstOrDefault(x => x.QuizzerName == QuizzerName);
        }
        #endregion

        #region RunningGame
        public static Thread RunningGameCleaner = new Thread(async () => await RunningGameCleaning());

        private static async Task RunningGameCleaning()
        {
            while (true)
            {
                var timeout = DateTime.UtcNow.AddHours(-Constants.PlayTime);
                var oldGames = DB.Table<RunningGame>().Where(x => timeout > x.LastUpdate).ToList();
                for (int i = oldGames.Count - 1; i >= 0; i--)
                {
                    var g = oldGames[i];
                    await g.SendExpiryMessages();
                    g.DeleteDB();
                }

                timeout = DateTime.UtcNow.AddHours(-Constants.SecondTimeWarning);
                oldGames = DB.Table<RunningGame>().Where(x => timeout > x.LastUpdate && x.LastWarning < 2).ToList();
                foreach (var g in oldGames)
                {
                    await g.SendSecondTimeWarning();
                    g.LastWarning = 2;
                    g.SaveDB();
                }

                timeout = DateTime.UtcNow.AddHours(-Constants.FirstTimeWarning);
                oldGames = DB.Table<RunningGame>().Where(x => timeout > x.LastUpdate && x.LastWarning < 1).ToList();
                foreach (var g in oldGames)
                {
                    await g.SendFirstTimeWarning();
                    g.LastWarning = 1;
                    g.SaveDB();
                }
                await Task.Delay(120000);
            }
        }

        public static void RunningGame_Save(RunningGame game)
        {
            var g = DB.Table<RunningGame>().FirstOrDefault(x => x.Id == game.Id);
            if (g == null)
            {
                DB.Insert(game);
            }
            else
            {
                DB.Update(game);
            }
        }

        public static RunningGame RunningGame_Get(int Id)
        {
            return DB.Table<RunningGame>().FirstOrDefault(x => x.Id == Id);
        }

        public static List<RunningGame> RunningGames_GetByPlayer(int PlayerId)
        {
            return DB.Table<RunningGame>().Where(x => x.Player1Id == PlayerId || x.Player2Id == PlayerId).ToList();
        }

        public static RunningGame RunningGame_GetByBothPlayers(int PlayerId1, int PlayerId2)
        {
            return DB.Table<RunningGame>().FirstOrDefault(x => (x.Player1Id == PlayerId1 && x.Player2Id == PlayerId2) || (x.Player1Id == PlayerId2 && x.Player2Id == PlayerId1));
        }

        public static RunningGame RunningGame_GetOpenStranger(int StarterId)
        {
            // IDs of all people that the player has open games against INCLUDING the player themselves
            var existingOpponents = DB.Table<RunningGame>().Where(x => x.Player1Id == StarterId || x.Player2Id == StarterId).SelectMany(x => new[] { x.Player1Id, x.Player2Id }).Distinct().ToList();

            var g = DB.Table<RunningGame>().FirstOrDefault(x => x.Round == 0 && x.Player2Id == 0 && !existingOpponents.Contains(x.Player1Id));
            if (g != null)
            {
                g.Round = 1; // Immediately set round to 1 so it's impossible 2 players will get the same game simultaneously
                DB.Update(g);
            }
            return g;
        }

        public static void RunningGame_Delete(RunningGame game)
        {
            DB.Delete(game);
        }
        #endregion

        #region Game
        public static void Game_Insert(Game g)
        {
            DB.Insert(g);
        }
        #endregion
    }

    public static class DatabaseExtensions
    {
        public static void UpdateDB(this Player p)
        {
            Database.Player_Update(p);
        }

        public static void UpdateScoreDB(this Player p)
        {
            Database.Player_UpdateScore(p.Id, p.Score, p.PerfectGames);
        }

        public static void SaveDB(this RunningGame g)
        {
            Database.RunningGame_Save(g);
        }

        public static void DeleteDB(this RunningGame g)
        {
            Database.RunningGame_Delete(g);
        }

        public static void SaveDB(this Telegram.Bot.Types.User u)
        {
            Database.Player_Save(u.Id,
                string.IsNullOrEmpty(u.LastName) ? u.FirstName : $"{u.FirstName} {u.LastName}",
                u.Username);
        }

        public static void InsertDB(this Game g)
        {
            Database.Game_Insert(g);
        }

        public static Player GetPlayer(this Telegram.Bot.Types.User u)
        {
            return Database.Player_GetByTelegramId(u.Id);
        }
    }
}
