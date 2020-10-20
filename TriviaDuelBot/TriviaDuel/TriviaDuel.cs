using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot.Types.ReplyMarkups;
using TriviaDuelBot.DBModel;

namespace TriviaDuelBot.TriviaDuel
{
    public class TriviaDuel
    {
        private static readonly Random R = new Random();

        public Player DBPlayer1;
        public Player DBPlayer2;
        public RunningGame DBGame;

        private int Choice = -1;
        public int WaitingFor { get; private set; } = 0;

        public void Choose(int choice)
        {
            if (WaitingFor != 0)
            {
                WaitingFor = 0;
                Choice = choice;
            }
        }

        public async Task Start()
        {
            await SendGameMessage(DBPlayer2, DBPlayer1, true, false);
        }

        public async Task Play()
        {
            try
            {
                var currentPlayer = DBGame.Round % 2 == 0 ? DBPlayer1 : DBPlayer2;
                var opponent = currentPlayer == DBPlayer2 ? DBPlayer1 : DBPlayer2;

                if (!DBGame.PlayedPending) // IF: Pending questions, ELSE: New round questions
                {
                    var category = (await QuestionAPI.GetCategories()).FirstOrDefault(x => x.Id == DBGame.GetCurrentCategory());
                    var cName = category?.Name ?? $"Unknown category ({category.Id})";

                    var m = await Bot.SendMessage($"Round {DBGame.Round - 1} Category: {cName}", currentPlayer.TelegramId);
                    // TODO cancel the game if m == null? 

                    for (int qNr = 1; qNr <= 3; qNr++)
                    {
                        var q = DBGame.GetCurrentQuestion(qNr);
                        var question = $"Round {DBGame.Round - 1}, Question {qNr}: " + q.Question;
                        var options = new List<string> { q.CorrectAnswer };
                        options.AddRange(q.IncorrectAnswers.Take(3));
                        options = options.OrderBy(x => R.NextDouble()).ToList();
                        var correctId = options.IndexOf(q.CorrectAnswer);

                        Choice = -1;
                        WaitingFor = currentPlayer.TelegramId;

                        m = await Bot.SendQuiz(currentPlayer.TelegramId, question, options, correctId);
                        if (m != null)
                            Program.PlayingDuels.Add(m.Poll.Id, this);
                        // TODO cancel the game otherwise?

                        for (int i = 0; i < Constants.AnswerTime + 3; i++)
                        {
                            await Task.Delay(1000);
                            if (Choice != -1) break;
                        }
                        WaitingFor = 0;
                        Program.PlayingDuels.Remove(m.Poll.Id);

                        if (Choice == correctId)
                        {
                            var yet = currentPlayer == DBPlayer1 ? DBGame.Player1Correct : DBGame.Player2Correct;
                            var flags = (Question)yet;
                            flags |= GetQuestionFlag(DBGame.Round - 1, qNr);
                            if (currentPlayer == DBPlayer1) DBGame.Player1Correct = (int)flags;
                            else DBGame.Player2Correct = (int)flags;
                        }
                        Choice = -1;
                    }
                    DBGame.PlayedPending = true;

                    if (DBGame.Round == 7)
                    {
                        var finished = DBGame.ToFinishedGame();
                        var winner = finished.WinnerId;

                        finished.InsertDB();
                        DBGame.DeleteDB();

                        if (winner == DBPlayer1.Id)
                        {
                            DBPlayer1.Score += 3;
                            DBPlayer2.Score = Math.Max(0, DBPlayer2.Score - 1);
                        }
                        else if (winner == DBPlayer2.Id)
                        {
                            DBPlayer1.Score = Math.Max(0, DBPlayer1.Score - 1);
                            DBPlayer2.Score += 3;
                        }
                        else
                        {
                            DBPlayer1.Score += 1;
                            DBPlayer2.Score += 1;
                        }

                        if (((Question)DBGame.Player1Correct).GetUniqueFlags().Count == 18)
                            DBPlayer1.PerfectGames++;

                        if (((Question)DBGame.Player2Correct).GetUniqueFlags().Count == 18)
                            DBPlayer2.PerfectGames++;

                        DBPlayer1.UpdateScoreDB();
                        DBPlayer2.UpdateScoreDB();

                        await SendGameEndMessages();
                    }
                    else
                    {
                        DBGame.SaveDB();
                        await SendGameMessage(currentPlayer, opponent, true, false);
                    }
                }
                else
                {
                    var categories = await GetCategoryOptions();
                    var question = $"Round {DBGame.Round} - Which category would you like to play?";

                    Choice = -1;
                    WaitingFor = currentPlayer.TelegramId;
                    var m = await Bot.SendPoll(currentPlayer.TelegramId, question, categories.Select(x => x.Name));

                    if (m != null)
                        Program.PlayingDuels.Add(m.Poll.Id, this);
                    // TODO cancel the game otherwise?

                    for (int i = 0; i < Constants.AnswerTime + 3; i++)
                    {
                        await Task.Delay(1000);
                        if (Choice != -1) break;
                    }
                    WaitingFor = 0;
                    Program.PlayingDuels.Remove(m.Poll.Id);

                    TriviaCategory category;
                    if (Choice == -1)
                    {
                        category = categories[R.Next(Constants.CategoryChoiceOptions)];
                        await Bot.SendMessage("Time's up! I chose this category: " + category.Name, currentPlayer.TelegramId);
                    }
                    else category = categories[Choice];

                    Choice = -1;

                    var questions = await QuestionAPI.GetQuestions(category.Id);

                    DBGame.SetCurrentCategory(category.Id);
                    DBGame.SetCurrentQuestions(questions);
                    DBGame.SaveDB();

                    for (int qNr = 1; qNr <= 3; qNr++)
                    {
                        var q = questions[qNr - 1];
                        question = $"Round {DBGame.Round}, Question {qNr}: " + q.Question;
                        var options = new List<string> { q.CorrectAnswer };
                        options.AddRange(q.IncorrectAnswers.Take(3));
                        options = options.OrderBy(x => R.NextDouble()).ToList();
                        var correctId = options.IndexOf(q.CorrectAnswer);

                        Choice = -1;
                        WaitingFor = currentPlayer.TelegramId;

                        m = await Bot.SendQuiz(currentPlayer.TelegramId, question, options, correctId);
                        if (m != null)
                            Program.PlayingDuels.Add(m.Poll.Id, this);
                        else
                        {
                            var text = "I could not send out a quiz to one of this game's players! This game sadly has to be cancelled.";
                            await Bot.SendMessage(text, currentPlayer.TelegramId);
                            await Bot.SendMessage(text, opponent.TelegramId);

                            DBGame.ToFinishedGame().InsertDB();
                            DBGame.DeleteDB();
                            return;
                        }

                        for (int i = 0; i < Constants.AnswerTime + 3; i++)
                        {
                            await Task.Delay(1000);
                            if (Choice != -1) break;
                        }
                        WaitingFor = 0;
                        Program.PlayingDuels.Remove(m.Poll.Id);

                        if (Choice == correctId)
                        {
                            var yet = currentPlayer == DBPlayer1 ? DBGame.Player1Correct : DBGame.Player2Correct;
                            var flags = (Question)yet;
                            flags |= GetQuestionFlag(DBGame.Round, qNr);
                            if (currentPlayer == DBPlayer1) DBGame.Player1Correct = (int)flags;
                            else DBGame.Player2Correct = (int)flags;
                        }
                        Choice = -1;
                    }
                    DBGame.Round++;
                    DBGame.PlayedPending = false;
                    DBGame.SaveDB();

                    await SendGameMessage(currentPlayer, opponent, true, true);
                    await SendGameMessage(opponent, currentPlayer, false, false);
                }
            }
            catch (Exception e)
            {
                await Bot.LogError(e);
            }
        }


        private InlineKeyboardMarkup PlayMarkup => new InlineKeyboardMarkup(
            new InlineKeyboardButton[]
            {
                InlineKeyboardButton.WithCallbackData("Play", $"game|{DBGame.Id}|play"),
                //InlineKeyboardButton.WithCallbackData("Give up", $"game|{DBGame.Id}|giveup"), // TODO
            }
        );

        private async Task SendGameMessage(Player player, Player opponent, bool playedPending, bool playedNextRound)
        {
            try
            {
                var playerCorrectFlags = player == DBPlayer1 ? DBGame.Player1Correct : DBGame.Player2Correct;
                var opponentCorrectFlags = opponent == DBPlayer1 ? DBGame.Player1Correct : DBGame.Player2Correct;

                var playerCorrect = ((Question)playerCorrectFlags).GetUniqueFlags();
                var opponentCorrect = ((Question)opponentCorrectFlags).GetUniqueFlags();

                var opponentName = player == DBPlayer1 ? DBGame.Player2QuizzerName : DBGame.Player1QuizzerName;
                int opponentCount = 0;

                string message = $"Your game against <b>{opponentName}</b>:\n\n" +
                    $"<code>You: {playerCorrect.Count}           Opponent: {{0}}\n";


                for (int i = 1; i <= DBGame.Round - (playedPending && !playedNextRound ? 0 : 1); i++)
                {
                    bool ownUnknown = (playedPending && !playedNextRound && i == DBGame.Round) || (!playedPending && i + 1 == DBGame.Round);
                    bool opponentUnknown = playedPending && (playedNextRound ? i + 1 : i) == DBGame.Round;
                    bool opponentHidden = !playedPending && i + 1 == DBGame.Round;

                    message += "\n" + GenerateRoundRow(playerCorrect, opponentCorrect, i, ownUnknown, opponentUnknown, opponentHidden, ref opponentCount);
                }
                message += "</code>\n\n";
                message = string.Format(message, opponentCount);

                if (playedNextRound)
                {
                    message += $"It is <b>{opponentName}</b>'s turn now. They have " +
                        $"<b>{Constants.PlayTime}</b> hours to do their turn, starting now.";
                    await Bot.SendMessage(message, player.TelegramId);
                }
                else
                {
                    message += $"It is your turn to play round <b>{DBGame.Round - (playedPending ? 0 : 1)}</b> now! " +
                      $"Press the button when you are ready to play! You have <b>{Constants.PlayTime}</b> hours!";
                    await Bot.SendMessage(message, player.TelegramId, PlayMarkup);
                }
            }
            catch (Exception e)
            {
                await Bot.LogError(e);
            }
        }

        private async Task SendGameEndMessages()
        {
            try
            {
                var p1CorrectFlags = ((Question)DBGame.Player1Correct).GetUniqueFlags();
                var p2CorrectFlags = ((Question)DBGame.Player2Correct).GetUniqueFlags();

                string p1Message = $"Your game against <b>{DBGame.Player2QuizzerName}</b>:\n\n" +
                    $"<code>You: {p1CorrectFlags.Count}           Opponent: {p2CorrectFlags.Count}\n";
                string p2Message = $"Your game against <b>{DBGame.Player1QuizzerName}</b>:\n\n" +
                    $"<code>You: {p2CorrectFlags.Count}           Opponent: {p1CorrectFlags.Count}\n";

                int dummy = 0;

                for (int i = 1; i <= 6; i++)
                {
                    p1Message += "\n" + GenerateRoundRow(p1CorrectFlags, p2CorrectFlags, i, false, false, false, ref dummy);
                    p2Message += "\n" + GenerateRoundRow(p2CorrectFlags, p1CorrectFlags, i, false, false, false, ref dummy);
                }
                p1Message += "</code>\n\nThis game is over now.\n";
                p2Message += "</code>\n\nThis game is over now.\n";

                if (p1CorrectFlags.Count > p2CorrectFlags.Count)
                {
                    p1Message += "Congratulations, you won this game! Would you like to play again?";
                    p2Message += "Sadly, you lost this game. But don't give up, try again!";
                }
                else if (p2CorrectFlags.Count > p1CorrectFlags.Count)
                {
                    p2Message += "Congratulations, you won this game! Would you like to play again?";
                    p1Message += "Sadly, you lost this game. But don't give up, try again!";
                }
                else
                {
                    p1Message += "The game ended with a draw - Nothing won, nothing lost. Play again?";
                    p2Message += "The game ended with a draw - Nothing won, nothing lost. Play again?";
                }

                var p1Markup = new InlineKeyboardMarkup(InlineKeyboardButton.WithCallbackData("Play again!", $"play|player|{DBGame.Player2QuizzerName}"));
                var p2Markup = new InlineKeyboardMarkup(InlineKeyboardButton.WithCallbackData("Play again!", $"play|player|{DBGame.Player1QuizzerName}"));

                await Bot.SendMessage(p1Message, DBPlayer1.TelegramId, p1Markup);
                await Bot.SendMessage(p2Message, DBPlayer2.TelegramId, p2Markup);
            }
            catch (Exception e)
            {
                await Bot.LogError(e);
            }
        }

        #region Helpers
        private static string GenerateRoundRow(List<Question> playerCorrect, List<Question> opponentCorrect, int round, bool ownUnknown, bool opponentUnknown, bool opponentHidden, ref int opponentCount)
        {
            string row = "";
            var (q1, q2, q3) = GetQuestionFlags(round);

            if (ownUnknown) row += Constants.NotPlayed + Constants.NotPlayed + Constants.NotPlayed;
            else
            {
                row += playerCorrect.Contains(q1) ? Constants.AnswerRight : Constants.AnswerWrong;
                row += playerCorrect.Contains(q2) ? Constants.AnswerRight : Constants.AnswerWrong;
                row += playerCorrect.Contains(q3) ? Constants.AnswerRight : Constants.AnswerWrong;
            }

            row += $"   Round {round}   ";

            if (opponentUnknown) row += Constants.NotPlayed + Constants.NotPlayed + Constants.NotPlayed;
            else if (opponentHidden) row += Constants.AnswerUnknown + Constants.AnswerUnknown + Constants.AnswerUnknown;
            else
            {
                if (opponentCorrect.Contains(q1))
                {
                    row += Constants.AnswerRight;
                    opponentCount++;
                }
                else row += Constants.AnswerWrong;

                if (opponentCorrect.Contains(q2))
                {
                    row += Constants.AnswerRight;
                    opponentCount++;
                }
                else row += Constants.AnswerWrong;

                if (opponentCorrect.Contains(q3))
                {
                    row += Constants.AnswerRight;
                    opponentCount++;
                }
                else row += Constants.AnswerWrong;
            }
            return row;
        }

        private static (Question q1, Question q2, Question q3) GetQuestionFlags(int round)
        {
            Question q1, q2, q3;

            switch (round)
            {
                case 1:
                    q1 = Question.R1Q1;
                    q2 = Question.R1Q2;
                    q3 = Question.R1Q3;
                    break;

                case 2:
                    q1 = Question.R2Q1;
                    q2 = Question.R2Q2;
                    q3 = Question.R2Q3;
                    break;

                case 3:
                    q1 = Question.R3Q1;
                    q2 = Question.R3Q2;
                    q3 = Question.R3Q3;
                    break;

                case 4:
                    q1 = Question.R4Q1;
                    q2 = Question.R4Q2;
                    q3 = Question.R4Q3;
                    break;

                case 5:
                    q1 = Question.R5Q1;
                    q2 = Question.R5Q2;
                    q3 = Question.R5Q3;
                    break;

                case 6:
                    q1 = Question.R6Q1;
                    q2 = Question.R6Q2;
                    q3 = Question.R6Q3;
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(round), round, "round must be between 1 and 6");
            }

            return (q1, q2, q3);
        }

        public static Question GetQuestionFlag(int round, int question)
        {
            switch (round)
            {
                case 1:
                    switch (question)
                    {
                        case 1: return Question.R1Q1;
                        case 2: return Question.R1Q2;
                        case 3: return Question.R1Q3;
                    }
                    break;

                case 2:
                    switch (question)
                    {
                        case 1: return Question.R2Q1;
                        case 2: return Question.R2Q2;
                        case 3: return Question.R2Q3;
                    }
                    break;

                case 3:
                    switch (question)
                    {
                        case 1: return Question.R3Q1;
                        case 2: return Question.R3Q2;
                        case 3: return Question.R3Q3;
                    }
                    break;

                case 4:
                    switch (question)
                    {
                        case 1: return Question.R4Q1;
                        case 2: return Question.R4Q2;
                        case 3: return Question.R4Q3;
                    }
                    break;

                case 5:
                    switch (question)
                    {
                        case 1: return Question.R5Q1;
                        case 2: return Question.R5Q2;
                        case 3: return Question.R5Q3;
                    }
                    break;

                case 6:
                    switch (question)
                    {
                        case 1: return Question.R6Q1;
                        case 2: return Question.R6Q2;
                        case 3: return Question.R6Q3;
                    }
                    break;
            }
            return Question.None;
        }

        private async Task<TriviaCategory[]> GetCategoryOptions()
        {
            try
            {
                var categories = await QuestionAPI.GetCategories();
                var playedCategories = DBGame.GetPlayedCategories();
                var allOptions = categories.Where(x => !playedCategories.Contains(x.Id)).ToList();

                var options = new TriviaCategory[Constants.CategoryChoiceOptions];
                for (int i = 0; i < Constants.CategoryChoiceOptions; i++)
                {
                    var o = allOptions[R.Next(allOptions.Count)];
                    options[i] = o;
                    allOptions.Remove(o);
                }
                return options;
            }
            catch (Exception e)
            {
                await Bot.LogError(e);
                return null;
            }
        }
        #endregion

        #region Game Assigner
        public static readonly Thread GameAssigner = new Thread(async () => await GameAssigning());
        public static readonly Queue<Player> RandomPlayerQueue = new Queue<Player>();
        public static readonly List<(Player p, string opQuizzerName)> PlayerQueue = new List<(Player p, string opQuizzerName)>();

        private static async Task GameAssigning()
        {
            while (true)
            {
                try
                {
                    await Task.Delay(5000);
                    while (RandomPlayerQueue.Any())
                    {
                        var p = RandomPlayerQueue.Dequeue();
                        var game = Database.RunningGame_GetOpenStranger(p.Id);
                        if (game != null) // Pending challenge by someone else
                        {
                            game.Round = 1;
                            game.Player2Id = p.Id;
                            game.Player2QuizzerName = p.QuizzerName;
                            game.TimeStarted = DateTime.UtcNow;
                            game.LastUpdate = DateTime.UtcNow;
                            game.SaveDB();

                            var duel = new TriviaDuel
                            {
                                DBGame = game,
                                DBPlayer1 = Database.Player_Get(game.Player1Id),
                                DBPlayer2 = p,
                            };
                            await Bot.SendMessage($"You are now playing against <b>{duel.DBGame.Player1QuizzerName}</b>!", p.TelegramId);
                            await duel.Start();
                        }
                        else
                        {
                            game = new RunningGame
                            {
                                Round = 0,
                                Player1Id = p.Id,
                                Player1QuizzerName = p.QuizzerName,
                                PlayedPending = true,
                                LastUpdate = DateTime.UtcNow,
                                LastWarning = 0,
                            };
                            game.SaveDB();
                            await Bot.SendMessage("Please wait, your opponent is playing the first round...", p.TelegramId);
                        }
                    }

                    while (PlayerQueue.Any())
                    {
                        var (p, opQuizzerName) = PlayerQueue.First();
                        PlayerQueue.Remove((p, opQuizzerName));

                        var cnt = Database.RunningGames_GetCountByPlayer(p.Id);
                        if (cnt >= 10)
                        {
                            await Bot.SendMessage("You can't have over 10 duels running at a time! Finish other duels first before starting new ones!", p.TelegramId);
                            continue;
                        }

                        var opponent = Database.Player_GetByQuizzerName(opQuizzerName);
                        if (opponent == null)
                        {
                            await Bot.SendMessage($"I can't find anyone by the Quizzer Name <b>{opQuizzerName}</b>!\n\n" +
                                $"Perhaps they changed it? Sorry, can't start a game.", p.TelegramId);
                            continue;
                        }
                        
                        if (opponent.Id == p.Id)
                        {
                            await Bot.SendMessage("You cannot play a duel against yourself!", p.TelegramId);
                            continue;
                        }

                        cnt = Database.RunningGames_GetCountByPlayer(opponent.Id);
                        if (cnt >= 10)
                        {
                            await Bot.SendMessage($"You can't challenge <b>{opQuizzerName}</b> to a duel, as " +
                                $"they already have 10 duels running! Try again later ;)", p.TelegramId);
                            continue;
                        }

                        var existing = Database.RunningGame_GetByBothPlayers(p.Id, opponent.Id);
                        if (existing != null)
                        {
                            await Bot.SendMessage($"You are already playing against <b>{opQuizzerName}</b>!\n" +
                                $"You can't start multiple games against the same player simultaneously!", p.TelegramId);
                            continue;
                        }

                        var opChallenge = PlayerQueue.FirstOrDefault(x => x.p.Id == opponent.Id && x.opQuizzerName == p.QuizzerName);
                        if (opChallenge != default)
                        {
                            PlayerQueue.Remove(opChallenge);
                            var game = new RunningGame
                            {
                                Round = 1,
                                Player1Id = p.Id,
                                Player1QuizzerName = p.QuizzerName,
                                Player2Id = opponent.Id,
                                Player2QuizzerName = opponent.QuizzerName,
                                PlayedPending = true,
                                LastUpdate = DateTime.UtcNow,
                                LastWarning = 0
                            };
                            game.SaveDB();
                            await Bot.SendMessage($"<b>{opQuizzerName}</b> accepted your challenge! They have up to <b>{Constants.PlayTime}</b> hours " +
                                $"to play it now!", p.TelegramId);
                            var duel = new TriviaDuel
                            {
                                DBPlayer1 = p,
                                DBPlayer2 = opponent,
                                DBGame = game
                            };
                            await duel.Start();
                        }
                        else
                        {
                            var g = new RunningGame
                            {
                                Round = 0,
                                Player1Id = p.Id,
                                Player1QuizzerName = p.QuizzerName,
                                Player2Id = opponent.Id,
                                Player2QuizzerName = opponent.QuizzerName,
                                PlayedPending = true,
                                LastUpdate = DateTime.UtcNow,
                                LastWarning = 0
                            };
                            g.SaveDB();
                            await Bot.SendMessage($"<b>{p.QuizzerName}</b> is challenging you to a game! Do you " +
                                $"want to play against them? You can accept this game within the next <b>{Constants.PlayTime}</b> hours.",
                                opponent.TelegramId, GetAcceptRejectButtons(g.Id));
                            await Bot.SendMessage($"You challenged <b>{opQuizzerName}</b> to a game! They have up to <b>{Constants.PlayTime}</b> hours " +
                                $"to accept it (and then another <b>{Constants.PlayTime}</b> hours to play it)!", p.TelegramId);
                        }
                    }
                }
                catch (Exception e)
                {
                    await Bot.LogError(e);
                    await Task.Delay(10000);
                }
            }
        }

        static InlineKeyboardMarkup GetAcceptRejectButtons(int gameId)
        {
            return new InlineKeyboardMarkup(
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("Accept", $"play|accept|{gameId}"),
                    InlineKeyboardButton.WithCallbackData("Ignore", $"play|ignore"),
                }
            );
        }
        #endregion
    }

    #region Question Flags
    [Flags]
    public enum Question
    {
        None = 0,

        R1Q1 = 1,
        R1Q2 = 2,
        R1Q3 = 4,
        R2Q1 = 8,
        R2Q2 = 16,
        R2Q3 = 32,
        R3Q1 = 64,
        R3Q2 = 128,
        R3Q3 = 256,
        R4Q1 = 512,
        R4Q2 = 1024,
        R4Q3 = 2048,
        R5Q1 = 4096,
        R5Q2 = 8192,
        R5Q3 = 16384,
        R6Q1 = 32768,
        R6Q2 = 65536,
        R6Q3 = 131072,

        All = R1Q1 | R1Q2 | R1Q3 |
              R2Q1 | R2Q2 | R2Q3 |
              R3Q1 | R3Q2 | R3Q3 |
              R4Q1 | R4Q2 | R4Q3 |
              R5Q1 | R5Q2 | R5Q3 |
              R6Q1 | R6Q2 | R6Q3,
    }
    #endregion
}
