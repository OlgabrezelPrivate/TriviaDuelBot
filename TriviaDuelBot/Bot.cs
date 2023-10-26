using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Polling;
using System.Threading;
using TriviaDuelBot.TriviaDuel;

namespace TriviaDuelBot
{
    public static class Bot
    {
        private static TelegramBotClient Api;

        public static User Me;

        #region Init
        public static async Task Init()
        {
            Api = new TelegramBotClient(Constants.BotToken);
            Me = await Api.GetMeAsync();
            await SendMessage("Started up!", Constants.BotOwner);

            Api.StartReceiving((sender, update, token) => { OnUpdate(sender, update, token); },
                               (sender, exception, token) => { });
        }
        #endregion

        #region Receiving Updates        
        private static void OnUpdate(object sender, Update u, CancellationToken token)
        {
            switch (u.Type)
            {
                case UpdateType.Message:
                    Task.Run(async () => await HandleMessage(u.Message), token);
                    break;

                case UpdateType.CallbackQuery:
                    Task.Run(async () => await HandleCallbackQuery(u.CallbackQuery), token);
                    break;

                case UpdateType.PollAnswer:
                    Task.Run(async () => await HandlePollAnswer(u.PollAnswer), token);
                    break;
            }
        }

        private static async Task HandleMessage(Message msg)
        {
            try
            {
                if (msg.From.Id == 777000 || msg.From.Id == 1087968824 ||
                    (DateTime.UtcNow - msg.Date).TotalSeconds > 20 || msg.Type != MessageType.Text ||
                    !msg.Text.StartsWith('/')) return;

                var u = msg.From;
                u.SaveDB();

                var p = u.GetPlayer();

                var command = msg.Text.Split(' ')[0];
                if (command.ToLower().EndsWith("@" + Me.Username.ToLower()))
                    command = command.Remove(command.Length - Me.Username.Length - 1);

                var args = msg.Text.Contains(' ')
                    ? msg.Text.Substring(msg.Text.IndexOf(' ') + 1)
                    : null;

                switch (command)
                {
                    case "/start":
                    case "/help":
                        await Commands.Start(msg);
                        break;

                    case "/name":
                        await Commands.Name(msg, args, p);
                        break;

                    case "/play":
                        await Commands.Play(msg, args, p);
                        break;

                    case "/maint":
                    case "/maintenance":
                        await Commands.Maintenance(msg);
                        break;

                    default:
                        await SendMessage("I don't know this command, sorry!", msg.Chat.Id);
                        break;
                }
            }
            catch (Exception e)
            {
                await LogError(e);
            }
        }

        private static async Task HandleCallbackQuery(CallbackQuery call)
        {
            try
            {
                var u = call.From;
                u.SaveDB();

                var p = u.GetPlayer();

                var args = call.Data.Split('|');

                switch (args[0])
                {
                    case "removemarkup":
                        await EditInlineKeyboard(call.Message.Chat.Id, call.Message.MessageId, null);
                        break;

                    case "play":
                        await Commands.PlayCallback(call, args, p);
                        break;

                    case "game":
                        await Commands.GameCallback(call, args, p);
                        break;
                }
            }
            catch (Exception e)
            {
                await LogError(e);
            }
        }

        private static async Task HandlePollAnswer(PollAnswer ans)
        {
            try
            {
                if (Program.PlayingDuels.TryGetValue(ans.PollId, out var g) && g.WaitingFor == ans.User.Id)
                {
                    g.Choose(ans.OptionIds[0]);
                }
            }
            catch (Exception e)
            {
                await LogError(e);
            }
        }
        #endregion

        #region Making calls to Bot API
        public static async Task<Message> SendMessage(string text, long chatId, IReplyMarkup keyboard = null, ParseMode pm = ParseMode.Html, bool disableWebPagePreview = true)
        {
            try
            {
                return await Api.SendTextMessageAsync(
                    chatId: chatId, 
                    text: text, 
                    parseMode: pm, 
                    disableWebPagePreview: disableWebPagePreview, 
                    replyMarkup: keyboard);
            }
            catch (Exception e)
            {
                await LogError(e);
                return null;
            }
        }

        public static async Task<Message> EditMessage(string text, long chatId, int messageId, InlineKeyboardMarkup keyboard = null, ParseMode pm = ParseMode.Html, bool disableWebPagePreview = true)
        {
            try
            {
                return await Api.EditMessageTextAsync(
                    chatId: chatId, 
                    messageId: messageId, 
                    text: text, 
                    parseMode: pm, 
                    disableWebPagePreview: disableWebPagePreview, 
                    replyMarkup: keyboard);
            }
            catch (Exception e)
            {
                await LogError(e);
                return null;
            }
        }

        public static async Task<Message> SendPoll(long chatId, string question, IEnumerable<string> options)
        {
            try
            {
                return await Api.SendPollAsync(
                    chatId: chatId, 
                    question: question, 
                    options: options,
                    type: PollType.Regular,
                    isAnonymous: false, 
                    openPeriod: Constants.AnswerTime);
            }
            catch (Exception e)
            {
                await LogError(e);
                return null;
            }
        }

        public static async Task<Message> SendQuiz(long chatId, string question, IEnumerable<string> options, int correctAnswerId)
        {
            try
            {
                return await Api.SendPollAsync(
                    chatId: chatId, 
                    question: question, 
                    options: options, 
                    type: PollType.Quiz, 
                    correctOptionId: correctAnswerId, 
                    isAnonymous: false, 
                    openPeriod: Constants.AnswerTime);
            }
            catch (Exception e)
            {
                await LogError(e);
                return null;
            }
        }

        public static async Task<Poll> ClosePoll(long chatId, int messageId, InlineKeyboardMarkup keyboard = null)
        {
            try
            {
                return await Api.StopPollAsync(
                    chatId: chatId, 
                    messageId: messageId, 
                    replyMarkup: keyboard);
            }
            catch (Exception e)
            {
                await LogError(e);
                return null;
            }
        }

        public static async Task EditInlineKeyboard(long chatId, int messageId, InlineKeyboardMarkup newMarkup = null)
        {
            try
            {
                await Api.EditMessageReplyMarkupAsync(
                    chatId: chatId,
                    messageId: messageId,
                    replyMarkup: newMarkup);
            }
            catch (Exception e)
            {
                await LogError(e);
            }
        }

        public static async Task AnswerCallback(string callbackId, string text = null, bool alert = false)
        {
            try
            {
                await Api.AnswerCallbackQueryAsync(
                    callbackQueryId: callbackId, 
                    text: text, 
                    showAlert: alert);
            }
            catch (Exception e)
            {
                await LogError(e);
            }
        }
        #endregion


        #region Error Handling
        public static async Task LogError(Exception e)
        {
            var trace = e.StackTrace;
            var message = e.Message;

            do
            {
                e = e.InnerException;
                if (e == null) break;
                message += "\n" + e.Message;
            }
            while (true);

            await SendMessage((message + "\n\n" + trace).FormatHTML(), Constants.LogChat);
        }
        #endregion
    }
}
