using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using TriviaDuelBot.DBModel;
using TriviaDuelBot.TriviaDuel;

namespace TriviaDuelBot
{
    public static class Commands
    {
        public static async Task Start(Message msg)
        {
            try
            {
                await Bot.SendMessage($"Hello! I am @{Bot.Me.Username} and I moderate games of Trivia Duel!\n\n" +
                    $"To start playing, you need to set yourself a Quizzer Name with the /name command " +
                    $"that will be displayed to your opponents. They will NOT receive information " +
                    $"about your Telegram account, unless you explicitly allow them to.\n\n" +
                    $"My source code is <a href=\"https://github.com/Olgabrezel/TriviaDuelBot\">publicly " +
                    $"available on GitHub</a>!\n\nThis bot is powered by questions from " +
                    $"<a href=\"https://opentdb.com\">Open Trivia DB</a>, licensed under the " +
                    $"<a href=\"https://creativecommons.org/licenses/by-sa/4.0\">CC BY-SA 4.0</a> license.", msg.Chat.Id);
            }
            catch (Exception e)
            {
                await Bot.LogError(e);
            }
        }

        // 5-20 latin letters, underscores, dashes; must start with a letter
        static readonly Regex QuizzerNameRegex = new Regex(@"^[A-Za-z][A-Za-z0-9\-_]{4,19}$");

        // no 2 consecutive dashes/underscores
        static readonly Regex QuizzerNameBlacklist = new Regex(@"[\-_]{2}");

        public static async Task Name(Message msg, string args, Player p)
        {
            try
            {
                if (args == null)
                {
                    await Bot.SendMessage("You need to attach your desired Quizzer Name to this command! " +
                        "It must have between 5 and 20 latin letters and digits, dashes and underscores, " +
                        "start with a letter and not be in use by someone else already! " +
                        "Using multiple consecutive dashes/underscores is forbidden.", msg.Chat.Id);
                    return;
                }

                if (!QuizzerNameRegex.IsMatch(args) || QuizzerNameBlacklist.IsMatch(args))
                {
                    await Bot.SendMessage("Your Quizzer Name must have between 5 and 20 latin letters and digits, " +
                        "dashes and underscores, start with a letter and not be in use by someone else already! " +
                        "Using multiple consecutive dashes/underscores is forbidden.",
                        msg.Chat.Id);
                    return;
                }

                var already = Database.Player_GetByQuizzerName(args);
                if (already != null)
                {
                    await Bot.SendMessage("Sorry, this Quizzer Name is already taken. Please choose a different one.",
                        msg.Chat.Id);
                    return;
                }

                p.QuizzerName = args;
                p.UpdateDB();
                await Bot.SendMessage($"Success! Your QuizzerName is now <b>{args}</b>", msg.Chat.Id);
            }
            catch (Exception e)
            {
                await Bot.LogError(e);
            }
        }

        static readonly InlineKeyboardMarkup PlayMarkup = new InlineKeyboardMarkup(
            new InlineKeyboardButton[][]
            {
                new InlineKeyboardButton[]
                {
                    InlineKeyboardButton.WithCallbackData("Play against strangers", "play|stranger")
                },
                new InlineKeyboardButton[]
                {
                    InlineKeyboardButton.WithCallbackData("Cancel", "removemarkup"),
                }
            }
        );

        public static async Task Play(Message msg, string args, Player p)
        {
            try
            {
                if (Program.Maintenance)
                {
                    await Bot.SendMessage("You can't play now, the bot is in maintenance mode! Please try again later.", msg.Chat.Id);
                    return;
                }

                if (p.QuizzerName == null)
                {
                    await Bot.SendMessage("You need to set yourself a Quizzer Name before you can " +
                        "start playing! Use <code>/name &lt;Your Quizzer Name&gt;</code> to set your Quizzer Name!",
                        msg.Chat.Id);
                    return;
                }

                if (args == p.QuizzerName)
                {
                    await Bot.SendMessage("You cannot play a duel against yourself!", msg.Chat.Id);
                    return;
                }

                if (args != null)
                {
                    TriviaDuel.TriviaDuel.PlayerQueue.Add((p, args));
                    await Bot.SendMessage($"You are challenging <b>{args}</b> to a game! Please wait, I'm assigning you a game...", msg.Chat.Id);
                    return;
                }

                await Bot.SendMessage("If you would like to play against strangers, use the button below!\n" +
                    "If you would like to challenge someone by their QuizzerName, use " +
                    "<code>/play &lt;Quizzer Name of your opponent here&gt;</code>", msg.Chat.Id, PlayMarkup);
            }
            catch (Exception e)
            {
                await Bot.LogError(e);
            }
        }

        public static async Task Maintenance(Message msg)
        {
            try
            {
                Program.Maintenance = !Program.Maintenance;
                if (Program.Maintenance)
                {
                    var waitTime = Constants.AnswerTime * 4 + 20;
                    await Bot.SendMessage($"Maintenance mode is enabled. You should wait at least <b>{waitTime}</b> " +
                        $"seconds before taking down the bot, so all ongoing game turns can be finished.\n\n" +
                        $"I will notify you when that time has passed if maintenance is still on :)", msg.Chat.Id);

                    await Task.Delay(1000 * waitTime);

                    if (Program.Maintenance) // Dont notify if maintenance has been disabled since
                        await Bot.SendMessage($"<b>{waitTime}</b> seconds have passed, you can take the bot down now :)", msg.Chat.Id);
                }
                else
                {
                    await Bot.SendMessage($"Maintenance mode is disabled again!", msg.Chat.Id);
                }
            }
            catch (Exception e)
            {
                await Bot.LogError(e);
            }
        }

        public static async Task PlayCallback(CallbackQuery call, string[] args, Player p)
        {
            try
            {
                if (Program.Maintenance)
                {
                    await Bot.AnswerCallback(call.Id, "You can't play now, the bot is in maintenance mode! Please try again later.", true);
                    return;
                }

                switch (args[1])
                {
                    case "stranger":
                        TriviaDuel.TriviaDuel.RandomPlayerQueue.Enqueue(p);
                        await Bot.EditMessage("You want to play against a stranger. I'm assigning you a game...", call.Message.Chat.Id, call.Message.MessageId);
                        break;

                    case "player":
                        TriviaDuel.TriviaDuel.PlayerQueue.Add((p, args[2]));
                        await Bot.EditInlineKeyboard(call.Message.Chat.Id, call.Message.MessageId);
                        await Bot.SendMessage($"You are challenging <b>{args[2]}</b> to a game! Please wait, I'm assigning you a game...", call.Message.Chat.Id);
                        break;

                    case "accept":
                        await Bot.EditMessage($"You accepted this incoming game request!", call.Message.Chat.Id, call.Message.MessageId);
                        var gameId = int.Parse(args[2]);
                        var g = Database.RunningGame_Get(gameId);
                        g.Round = 1;
                        g.SaveDB();
                        var duel = new TriviaDuel.TriviaDuel
                        {
                            DBPlayer1 = Database.Player_Get(g.Player1Id),
                            DBPlayer2 = p,
                            DBGame = g
                        };
                        await Bot.SendMessage($"<b>{g.Player2QuizzerName}</b> accepted your challenge! They have up to <b>{Constants.PlayTime}</b> hours " +
                                $"to play it now!", duel.DBPlayer1.TelegramId);
                        await duel.Start();
                        break;

                    case "reject":
                        await Bot.EditMessage($"You rejected this incoming game request!", call.Message.Chat.Id, call.Message.MessageId);
                        break;
                }
            }
            catch (Exception e)
            {
                await Bot.LogError(e);
            }
        }

        public static async Task GameCallback(CallbackQuery call, string[] args, Player p)
        {
            try
            {
                if (Program.Maintenance)
                {
                    await Bot.AnswerCallback(call.Id, "You can't play now, the bot is in maintenance mode! Please try again later.", true);
                    return;
                }

                var g = Database.RunningGame_Get(int.Parse(args[1]));
                if (g == null)
                {
                    await Bot.AnswerCallback(call.Id, "This game has already expired!", true);
                    await Bot.EditInlineKeyboard(call.Message.Chat.Id, call.Message.MessageId);
                    return;
                }

                switch (args[2])
                {
                    case "play":
                        await Bot.EditInlineKeyboard(call.Message.Chat.Id, call.Message.MessageId);
                        var game = new TriviaDuel.TriviaDuel
                        {
                            DBGame = g,
                            DBPlayer1 = g.Player1Id == p.Id ? p : Database.Player_Get(g.Player1Id),
                            DBPlayer2 = g.Player2Id == p.Id ? p : Database.Player_Get(g.Player2Id),
                        };
                        g.LastUpdate = DateTime.UtcNow;
                        g.LastWarning = 0;
                        g.SaveDB();
                        await game.Play();
                        break;

                    case "giveup":
                        await Bot.AnswerCallback(call.Id, "Not yet implemented!", true);
                        break;
                }
            }
            catch (Exception e)
            {
                await Bot.LogError(e);
            }
        }
    }
}
