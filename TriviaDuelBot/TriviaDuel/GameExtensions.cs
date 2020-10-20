using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TriviaDuelBot.DBModel;

namespace TriviaDuelBot.TriviaDuel
{
    public static class GameExtensions
    {
        /// <summary>
        /// Get a list of all category IDs that were already played in the game <paramref name="g"/>.
        /// </summary>
        /// <param name="g">The game</param>
        /// <returns>A list of all category IDs that were already played in the game <paramref name="g"/></returns>
        public static List<int> GetPlayedCategories(this RunningGame g)
        {
            return new List<int>
            {
                g.R1Category,
                g.R2Category,
                g.R3Category,
                g.R4Category,
                g.R5Category,
                g.R6Category
            }.Where(x => x != 0).ToList();
        }

        /// <summary>
        /// Convert a flags enum value to a list of unique flags that this value has
        /// </summary>
        /// <typeparam name="T">The enum type</typeparam>
        /// <param name="flags">The enum value that contains multiple flags</param>
        /// <returns>A list of the unique flags that the <paramref name="flags"/> have.</returns>
        public static List<T> GetUniqueFlags<T>(this T flags) where T : Enum
        {
            List<T> uniqueFlags = new List<T>();
            foreach (var v in typeof(T).GetEnumValues().Cast<T>())
            {
                if (v.ToString() == "None") continue;
                if (flags.HasFlag(v)) uniqueFlags.Add(v);
            }
            return uniqueFlags;
        }

        /// <summary>
        /// Sets the category of the current round to the given <paramref name="categoryId"/>.
        /// NOTE: It gets the category of the current <see cref="RunningGame.Round"/>, as this is called
        /// when the new round is played
        /// </summary>
        /// <param name="game">The game to be modified</param>
        /// <param name="categoryId">The category ID to be set for the current round</param>
        public static void SetCurrentCategory(this RunningGame game, int categoryId)
        {
            switch (game.Round)
            {
                case 1:
                    game.R1Category = categoryId;
                    break;

                case 2:
                    game.R2Category = categoryId;
                    break;

                case 3:
                    game.R3Category = categoryId;
                    break;

                case 4:
                    game.R4Category = categoryId;
                    break;

                case 5:
                    game.R5Category = categoryId;
                    break;

                case 6:
                    game.R6Category = categoryId;
                    break;
            }
        }

        /// <summary>
        /// Gets the category of the current round.
        /// NOTE: It gets the category of the PREVIOUS <see cref="RunningGame.Round"/>, as this is called
        /// when the pending round is played
        /// </summary>
        /// <returns></returns>
        public static int GetCurrentCategory(this RunningGame game)
        {
            return game.Round switch // I have never seen this kind of expression before
            {                        // Thanks, Visual Studio, I keep learning new stuff :D
                2 => game.R1Category,
                3 => game.R2Category,
                4 => game.R3Category,
                5 => game.R4Category,
                6 => game.R5Category,
                7 => game.R6Category,
                _ => -1,
            };
        }

        public static void SetCurrentQuestions(this RunningGame game, List<TriviaQuestion> questions)
        {
            var q1 = questions[0];
            var q2 = questions[1];
            var q3 = questions[2];

            game.CurrentQ1 = q1.Question;
            game.CurrentQ1Right = q1.CorrectAnswer;
            game.CurrentQ1Wrong1 = q1.IncorrectAnswers[0];
            game.CurrentQ1Wrong2 = q1.IncorrectAnswers[1];
            game.CurrentQ1Wrong3 = q1.IncorrectAnswers[2];

            game.CurrentQ2 = q2.Question;
            game.CurrentQ2Right = q2.CorrectAnswer;
            game.CurrentQ2Wrong1 = q2.IncorrectAnswers[0];
            game.CurrentQ2Wrong2 = q2.IncorrectAnswers[1];
            game.CurrentQ2Wrong3 = q2.IncorrectAnswers[2];

            game.CurrentQ3 = q3.Question;
            game.CurrentQ3Right = q3.CorrectAnswer;
            game.CurrentQ3Wrong1 = q3.IncorrectAnswers[0];
            game.CurrentQ3Wrong2 = q3.IncorrectAnswers[1];
            game.CurrentQ3Wrong3 = q3.IncorrectAnswers[2];
        }

        public static (string Question, string CorrectAnswer, List<string> IncorrectAnswers) GetCurrentQuestion(this RunningGame game, int question)
        {
            return question switch
            {
                1 => (game.CurrentQ1, game.CurrentQ1Right, new List<string> { game.CurrentQ1Wrong1, game.CurrentQ1Wrong2, game.CurrentQ1Wrong3 }),
                2 => (game.CurrentQ2, game.CurrentQ2Right, new List<string> { game.CurrentQ2Wrong1, game.CurrentQ2Wrong2, game.CurrentQ2Wrong3 }),
                3 => (game.CurrentQ3, game.CurrentQ3Right, new List<string> { game.CurrentQ3Wrong1, game.CurrentQ3Wrong2, game.CurrentQ3Wrong3 }),
                _ => (null, null, null),
            };
        }

        public static async Task SendExpiryMessages(this RunningGame expired)
        {
            var currentPlayerId = expired.Round % 2 == 0 ? expired.Player1Id : expired.Player2Id;
            var opponentId = currentPlayerId == expired.Player1Id ? expired.Player2Id : expired.Player1Id;
            Player currentPlayer = null, opponent = null;
            string curQuizzerName = currentPlayerId == expired.Player1Id ? expired.Player1QuizzerName : expired.Player2QuizzerName;
            string opQuizzerName = currentPlayerId == expired.Player1Id ? expired.Player2QuizzerName : expired.Player1QuizzerName;

            if (currentPlayerId != 0)
                currentPlayer = Database.Player_Get(currentPlayerId);
            if (opponentId != 0)
                opponent = Database.Player_Get(opponentId);

            if (currentPlayer != null && opponent != null)
            {
                if (expired.Round == 0)
                {
                    await Bot.SendMessage($"<b>{opQuizzerName}</b> did not accept your game request within " +
                        $"<b>{Constants.PlayTime}</b> hours :(\n Your game request has been cancelled.", currentPlayer.TelegramId);
                }
                else
                {
                    await Bot.SendMessage($"Your playing time of <b>{Constants.PlayTime}</b> hours expired, therefore you lost " +
                        $"your game against <b>{opQuizzerName}</b>!", currentPlayer.TelegramId);
                    await Bot.SendMessage($"<b>{curQuizzerName}</b>'s time of <b>{Constants.PlayTime}</b> hours expired, therefore " +
                        $"you won the game!", opponent.TelegramId);
                }
            }
            else if (currentPlayer != null)
            {
                await Bot.SendMessage($"Nobody joined your game within <b>{Constants.PlayTime}</b> hours :(\n" +
                    $"Your game request has been cancelled.", currentPlayer.TelegramId);
            }
            else if (opponent != null) // Not sure how this should happen, but why not
            {
                await Bot.SendMessage($"Nobody joined your game within <b>{Constants.PlayTime}</b> hours :(\n" +
                    $"Your game request has been cancelled.", opponent.TelegramId);
            }
        }

        public static async Task SendFirstTimeWarning(this RunningGame game)
        {
            var currentPlayerId = game.Round % 2 == 0 ? game.Player1Id : game.Player2Id;
            var opQuizzerName = currentPlayerId == game.Player1Id ? game.Player2QuizzerName : game.Player1QuizzerName;
            var currentPlayer = Database.Player_Get(currentPlayerId);

            await Bot.SendMessage($"Your game against <b>{opQuizzerName}</b> expires in <b>{Constants.PlayTime - Constants.FirstTimeWarning}</b> hours! Remember to play it!", currentPlayer.TelegramId);
        }

        public static async Task SendSecondTimeWarning(this RunningGame game)
        {
            var currentPlayerId = game.Round % 2 == 0 ? game.Player1Id : game.Player2Id;
            var opQuizzerName = currentPlayerId == game.Player1Id ? game.Player2QuizzerName : game.Player1QuizzerName;
            var currentPlayer = Database.Player_Get(currentPlayerId);

            await Bot.SendMessage($"Your game against <b>{opQuizzerName}</b> expires in <b>{Constants.PlayTime - Constants.SecondTimeWarning}</b> hours! <b>Hurry! Remember to play it!</b>", currentPlayer.TelegramId);
        }

        public static Game ToFinishedGame(this RunningGame running)
        {
            var p1Points = ((Question)running.Player1Correct).GetUniqueFlags().Count;
            var p2Points = ((Question)running.Player2Correct).GetUniqueFlags().Count;
            var untilRound = running.Round - 1;
            var winner = untilRound == 6 ? p1Points > p2Points ? running.Player1Id : p2Points > p1Points ? running.Player2Id : 0 : 0;


            return new Game
            {
                Id = running.Id,
                Player1Id = running.Player1Id,
                Player2Id = running.Player2Id,
                Player1Points = p1Points,
                Player2Points = p2Points,
                Round1Category = running.R1Category,
                Round2Category = running.R2Category,
                Round3Category = running.R3Category,
                Round4Category = running.R4Category,
                Round5Category = running.R5Category,
                Round6Category = running.R6Category,
                TimeStarted = running.TimeStarted,
                TimeEnded = DateTime.UtcNow,
                UntilRound = untilRound,
                WinnerId = winner,
            };
        }

        public static string FormatHTML(this string str)
        {
            return str.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");
        }
    }
}
