using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace TriviaDuelBot
{
    class Program
    {
        /// <summary>
        /// This dict contains the duels that are being played RIGHT NOW.
        /// Only those where a player is playing at this moment, not those that are waiting
        /// for the opponent's turn.
        /// </summary>
        public static Dictionary<string, TriviaDuel.TriviaDuel> PlayingDuels =
            new Dictionary<string, TriviaDuel.TriviaDuel>();

        /// <summary>
        /// Maintenance mode. If this is true, nobody can take any game actions, ensuring the bot
        /// is safe to be taken down.
        /// </summary>
        public static bool Maintenance = false;

        static int Main()
        {
            var s = StartupRoutine();
            if (s != 0) return s;

            TriviaDuel.TriviaDuel.GameAssigner.Start();
            Database.RunningGameCleaner.Start();
            Thread.Sleep(-1);

            return 0;
        }

        #region Startup
        static int StartupRoutine()
        {
            Console.Title = "Trivia Duel Bot - Starting up...";
            Console.WriteLine("Trivia Duel Bot - Starting up...");

            if (string.IsNullOrEmpty(Constants.AppDataFolder))
            {
                PrintError("The base path is empty! Cannot find the folder to store the files!");
                return 1;
            }
            if (!Directory.Exists(Constants.AppDataFolder))
            {
                PrintError("The base path doesn't exist! Cannot find the folder to store the files!", false);
                PrintError("Base path: " + Constants.AppDataFolder);
                return 1;
            }

            var appFolder = Path.Combine(Constants.AppDataFolder, Constants.AppFolder);
            if (!Directory.Exists(appFolder))
            {
                PrintInfo("App folder does not exist yet. It is being created.");
                try
                {
                    Directory.CreateDirectory(appFolder);
                }
                catch (Exception e)
                {
                    PrintError("Could not create the app folder! Error message below:", false);
                    PrintError(e.GetType().Name + ": " + e.Message);
                    return 1;
                }
            }

            PrintInfo("Initializing database...");
            Database.Init();
            if (!Database.ReadSettings()) // Read the settings from the settings table
            {
                PrintInfo("The database does not exist yet. It is being created.");
                PrintInfo("Database path: " + Path.Combine(appFolder, Constants.DatabaseName));
                Console.WriteLine();

                PrintInfo("First things first: Please enter your Telegram ID.");
                PrintInfo("You can obtain it from @userinfobot on Telegram, for example.");
                Constants.BotOwner = AskInputInt("Your Telegram ID");
                Console.WriteLine();

                PrintInfo("Next, enter the ID for your log chat. All errors by the bot will be sent there.");
                PrintInfo("If you want the bot to send the errors to yourself, you can enter 0");
                Constants.LogChat = AskInputLong("Log Chat ID", true);
                if (Constants.LogChat == 0) Constants.LogChat = Constants.BotOwner;
                Console.WriteLine();

                PrintInfo("Finally, enter your bot token by @BotFather on telegram.");
                Constants.BotToken = AskInput("Bot Token").Trim('"');
                Console.WriteLine();

                PrintInfo("Setup complete!");
                Database.WriteSettings();
            }
            PrintInfo("Database startup successful!");

            PrintInfo("Starting bot...");
            while (true)
            {
                try
                {
                    Bot.Init().Wait();
                    break;
                }
                catch (Exception e)
                {
                    PrintError("Could not initialize the bot! Error message below:", false);
                    PrintError(e.GetType().Name + ": " + e.Message, false);
                    Console.WriteLine();
                    PrintInfo($"Your current bot token is \"{Constants.BotToken}\".");
                    PrintInfo($"Enter \"exit\" to leave the program, or enter a different bot token to save.");
                    var i = AskInput("New Bot Token").Trim('"');
                    if (i.ToLower() == "exit") return 1;
                    Constants.BotToken = i;
                    Database.WriteSettings();
                }
            }

            PrintInfo("Startup complete!");
            Console.WriteLine();
            Console.Title = "Trivia Duel Bot - Running";
            Console.WriteLine($"Connected to {Bot.Me.FirstName} (@{Bot.Me.Username}, {Bot.Me.Id})");
            return 0;
        }

        static void PrintError(string error, bool exiting = true)
        {
            var fc = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine("[ERROR] " + error);
            Console.ForegroundColor = fc;
            if (exiting)
            {
                Console.WriteLine("Press any key to exit the program...");
                Console.ReadKey(true);
            }
        }

        static void PrintInfo(string info)
        {
            var fc = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("[INFO] " + info);
            Console.ForegroundColor = fc;
        }

        static string AskInput(string question)
        {
            while (true)
            {
                var fc = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.Write("[INPUT] " + question + ": ");
                Console.ForegroundColor = ConsoleColor.Green;
                var input = Console.ReadLine();
                Console.ForegroundColor = fc;
                if (!string.IsNullOrEmpty(input)) return input;
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine();
                Console.WriteLine("[INPUT] You need to enter a valid string! Please try again!");
                Console.ForegroundColor = fc;
            }
        }

        static int AskInputInt(string question, bool canBeZero = false)
        {
            while (true)
            {
                if (int.TryParse(AskInput(question), out int result) && (canBeZero || result != 0)) return result;
                var fc = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine();
                Console.WriteLine("[INPUT] You need to enter a valid integer! Please try again!");
                Console.ForegroundColor = fc;
            }
        }

        static long AskInputLong(string question, bool canBeZero = false)
        {
            while (true)
            {
                if (long.TryParse(AskInput(question), out long result) && (canBeZero || result != 0)) return result;
                var fc = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine();
                Console.WriteLine("[INPUT] You need to enter a valid integer! Please try again!");
                Console.ForegroundColor = fc;
            }
        }
        #endregion
    }
}
