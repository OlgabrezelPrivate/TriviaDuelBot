using System;
using System.Collections.Generic;
using System.Text;

namespace TriviaDuelBot
{
    public static class Constants
    {
        #region Can be modified in the code
        /// <summary>
        /// The place where the bot's folder for saving data will be created. 
        /// Defaults to AppData/Local
        /// </summary>
        public static readonly string AppDataFolder =
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        /// <summary>
        /// The name of the folder that contains the database. 
        /// This folder will be created inside the <see cref="AppDataFolder"/>
        /// </summary>
        public const string AppFolder = "TriviaDuelBot";

        /// <summary>
        /// The name of the database file, which is located in the <see cref="AppFolder"/>
        /// </summary>
        public const string DatabaseName = "TriviaDuel.sqlite";

        /// <summary>
        /// Time in minutes that the list of categories from the question API is cached.
        /// Once the questions are retrieved from the API, the program will not retrieve them again for the next x minutes.
        /// </summary>
        public const int CategoryCacheTime = 300;

        /// <summary>
        /// Number of categories that a player can choose from at round start.
        /// </summary>
        public const int CategoryChoiceOptions = 3;

        /// <summary>
        /// Time in seconds that players have to answer each question or choose a category
        /// </summary>
        public const int AnswerTime = 15;

        /// <summary>
        /// Time in hours that each player has to play before their time runs out
        /// </summary>
        public const int PlayTime = 48;

        /// <summary>
        /// Time in hours after which the current player is warned for the first time about their pending game
        /// </summary>
        public const int FirstTimeWarning = 24;

        /// <summary>
        /// Time in hours after which the current player is warned for the second time about their pending game
        /// </summary>
        public const int SecondTimeWarning = 47;

        /// <summary>
        /// Emoji that is used to indicate a player has played this question but his answer is not yet visible to you
        /// </summary>
        public const string AnswerUnknown = "🌫";

        /// <summary>
        /// Emoji that is used to indicate a player hasn't played this question yet
        /// </summary>
        public const string NotPlayed = "❔";

        /// <summary>
        /// Emoji that is used to indicate a player's correct answer
        /// </summary>
        public const string AnswerRight = "✅";

        /// <summary>
        /// Emoji that is used to indicate a player's wrong answer
        /// </summary>
        public const string AnswerWrong = "❌";
        #endregion

        #region Loaded in from the database
        /// <summary>
        /// The user who owns the bot
        /// </summary>
        public static long BotOwner;

        /// <summary>
        /// The place where the bot sends all error messages. 
        /// Can be a user, a group or a channel
        /// </summary>
        public static long LogChat;

        /// <summary>
        /// The bot's API Token by @BotFather on Telegram
        /// </summary>
        public static string BotToken;
        #endregion
    }
}
