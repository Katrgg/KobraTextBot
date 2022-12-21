using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InputFiles;
using File = System.IO.File;
using Image = System.Drawing.Image;
using TDict = System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, uint>>;
using WDict = System.Collections.Generic.Dictionary<string, uint>;

namespace KobraTextBot
{
    class SettingsKobra
    {
        [Range(0, 85)] public byte chanseAnswer = 50;
        public bool mustAnswer = true;
        public bool mustLearn = true;
        public LastWord sLastWord;
        public bool memorizeLastWord = true; // remembers the last message, if the next message is written within 60 seconds and from the same user, it will be added to the dictionary
    }

    class GroupData
    {
        public SettingsKobra settings = new SettingsKobra();
        public TDict DictMain = new TDict();
        public List<Sticker> stickerList = new List<Sticker>();
        public List<string> photosDem = new List<string>();
        public long chatID = 0;
        public DateTime lastDem;
    }

    struct Sticker
    {
        public string FileId;
        public string UnicId;
    }

    struct LastWord
    {
        public string lastWord;
        public long id;
        public DateTime date;
    }

    internal class Program
    {
        static readonly Random r = new Random();
        static Dictionary<long, GroupData> groupDatas = new Dictionary<long, GroupData>();

        static void Main(string[] args)
        {
            var client = new TelegramBotClient("bot token");
            
            client.StartReceiving(Update, Error);
            Console.ReadLine();
        }

        /// <summary>
        /// Bot command handler
        /// </summary>
        /// <param name="botClient">Bot client</param>
        /// <param name="update">Update</param>
        /// <param name="command">Command to process</param>
        /// <param name="groupData">Data of the group from which the call came</param>
        /// <returns></returns>
        public async static Task CommandHandler(ITelegramBotClient botClient,Update update, string[] command, GroupData groupData)
        {
            var message = update.Message;
            command[0] = command[0].Replace("@KobraText_Bot", "");
            try
            {
                switch (command[0])
                {
                    case "/kgd": // generate demotivator
                        if (groupData.photosDem.Count <= 0)
                        {
                            await botClient.SendTextMessageAsync(message.Chat.Id, "Not enough saved images to generate.");
                            return;
                        }
                        if(groupData.lastDem.AddMinutes(1) > DateTime.Now)
                        {
                            await botClient.SendTextMessageAsync(message.Chat.Id, "Wait before using the command again.");
                            return;
                        }    
                        GenerateDemotivator(groupData.photosDem[r.Next(0, groupData.photosDem.Count - 1)], MarkovChains.BuildString(groupData.DictMain, true, r), MarkovChains.BuildString(groupData.DictMain, true, r), message.Chat.Id);
                        groupData.lastDem = DateTime.Now;
                        using (var stream = File.OpenRead($@"D:\KobraData\{message.Chat.Id}\dem.jpg"))
                        {
                            InputOnlineFile inputOnlineFile = new InputOnlineFile(stream);
                            await botClient.SendPhotoAsync(chatId: message.Chat.Id, inputOnlineFile, replyToMessageId: message.MessageId);
                        }
                        break;
                    case "/kc": // change chanse answer
                        if (byte.TryParse(command[1], out groupData.settings.chanseAnswer))
                            await botClient.SendTextMessageAsync(message.Chat.Id, $"The response chance has been successfully changed to {groupData.settings.chanseAnswer}");
                        else
                            await botClient.SendTextMessageAsync(message.Chat.Id, "The operation failed.");
                        break;
                    case "/kcd": // clearing the dictionary of duplicate pairs 
                        await botClient.SendTextMessageAsync(message.Chat.Id, "Processing...");
                        List<KeyValuePair<string, WDict>> tempDictionary = new List<KeyValuePair<string, WDict>>();
                        tempDictionary = groupData.DictMain.ToList();
                        int iterat = 0;
                        foreach (var item in tempDictionary)
                        {
                            foreach (var wdictValue in item.Value)
                            {
                                if (item.Key == wdictValue.Key)
                                {
                                    groupData.DictMain.Remove(wdictValue.Key);
                                    iterat++;
                                }
                            }
                        }
                        if (iterat == 0)
                            await botClient.SendTextMessageAsync(message.Chat.Id, $"No identical pairs found.");
                        else
                            await botClient.SendTextMessageAsync(message.Chat.Id, $"The dictionary has been successfully cleared. Removed pairs: {iterat}");
                        break;
                    case "/help": // command list
                        await botClient.SendTextMessageAsync(message.Chat.Id, "/kc <0-100> - Changes the chance of a response to a message." +
                            "\r\n/help - List of commands." +
                            "\r\n/kg - Generate message." +
                            "\r\n/kg <0-150> - Generate a message with a specified length" +
                            "\r\n/kma <true or false> - Does the bot need to respond to messages." +
                            "\r\n/kml <true or false> - Does the bot need to learn." +
                            "\r\n/kcs - Current settings." +
                            "\r\n/kgs - Send sticker." +
                            "\r\n/kgd - Generate demotivator." +
                            "\r\n/kcdd - Clear data about saved demotivators." +
                            "\r\n/kcsd - Clear data about saved stickers." +
                            "\r\n/kcsr - Clear saved rows." +
                            "\r\n/kmlw <true or false> - Remember the last message." +
                            "\r\ntrue(Yes), false(No)");
                        break;
                    case "/kg": // generate message, or generate message with a specified length
                        string answer = "";
                        if (command.Length > 1)
                        {
                            int length = 0;

                            if (Int32.TryParse(command[1], out length) == true)
                            {
                                if (length < 150)
                                    answer = MarkovChains.BuildString(groupData.DictMain, length, true, r);
                            }
                        }
                        else
                        {
                            answer = MarkovChains.BuildString(groupData.DictMain, true, r);
                        }
                        await botClient.SendTextMessageAsync(message.Chat.Id, answer);
                        Console.WriteLine($"{message.From.FirstName}   |   {message.Text}   |   {answer}");
                        break;
                    case "/kgs": // send random sticker
                        await botClient.SendStickerAsync(message.Chat.Id, new InputOnlineFile(groupData.stickerList[r.Next(0, groupData.stickerList.Count - 1)].FileId));
                        Console.WriteLine($"{message.From.FirstName}   |   {message.Text}   |   Sticker");
                        break;
                    case "/kma": // change parametr must answer
                        if (bool.TryParse(command[1], out groupData.settings.mustAnswer))
                            await botClient.SendTextMessageAsync(message.Chat.Id, $"Successfully changed to {groupData.settings.mustAnswer}");
                        else
                            await botClient.SendTextMessageAsync(message.Chat.Id, "The operation failed.");
                        break;
                    case "/kml": // change value must learn
                        if (bool.TryParse(command[1], out groupData.settings.mustLearn))
                            await botClient.SendTextMessageAsync(message.Chat.Id, $"Successfully changed to {groupData.settings.mustLearn}");
                        else
                            await botClient.SendTextMessageAsync(message.Chat.Id, "The operation failed.");
                        break;
                    case "/kcs": // send message with current settings
                        await botClient.SendTextMessageAsync(message.Chat.Id, $"Chance of an answer - *{groupData.settings.chanseAnswer}*\r\n" +
                            $"Should a bot learn - *{groupData.settings.mustLearn}*\r\n" +
                            $"Should the bot respond - *{groupData.settings.mustAnswer}*\r\n" +
                            $"Saved rows - *{groupData.DictMain.Keys.Count}*\r\n" +
                            $"Chat *ID* - *\"{groupData.chatID}\"*\r\n" +
                            $"Saved stickers - *{groupData.stickerList.Count}*\r\n" +
                            $"Saved photos for demotivator - *{groupData.photosDem.Count}*\r\n" +
                            $"Remember the last message - *{groupData.settings.memorizeLastWord}*\r\n" +
                            $"*True(Yes)*, *False(No)*", parseMode: ParseMode.Markdown);
                        break;
                    case "/kcsd": // clear sticker data
                        iterat = groupData.stickerList.Count;
                        groupData.stickerList.Clear();
                        await botClient.SendTextMessageAsync(message.Chat.Id, $"The data has been successfully cleared. Stickers removed {iterat}");
                        break;
                    case "/kcsr": // clear dictionary
                        groupData.DictMain = new TDict();
                        await botClient.SendTextMessageAsync(message.Chat.Id, "Cleared.");
                        break;
                    case "/kcdd": // clear demotivators data
                        if (groupData.lastDem.AddMinutes(1) > DateTime.Now)
                        {
                            await botClient.SendTextMessageAsync(message.Chat.Id, "Wait before using the command.");
                            return;
                        }
                        groupData.photosDem.Clear();
                        Directory.Delete($@"D:\KobraData\{groupData.chatID}", true);
                        await botClient.SendTextMessageAsync(message.Chat.Id, "Cleared.");
                        break;
                    case "/kmlw":
                        if (bool.TryParse(command[1], out groupData.settings.memorizeLastWord))
                            await botClient.SendTextMessageAsync(message.Chat.Id, $"Successfully changed to {groupData.settings.memorizeLastWord}");
                        else
                            await botClient.SendTextMessageAsync(message.Chat.Id, "The operation failed.");
                        break;
                }
            }
            catch (Exception ex)
            {
                await botClient.SendTextMessageAsync(message.Chat.Id, "The operation failed.");
                Console.WriteLine(ex.ToString());
            }
        }


        public static async Task Update(ITelegramBotClient botClient, Update update, CancellationToken token)
        {
            var message = update.Message;
            if (message == null) { return; }
            long chatId = message.Chat.Id;
            if (message.LeftChatMember != null)
            {
                if(message.LeftChatMember.Username == "KobraText_Bot")
                    DataIO.DeleteData(chatId);
            }

            GroupData groupData;

            if (!groupDatas.ContainsKey(chatId))
            {
                groupData = DataIO.LoadData(chatId);
                await Task.Delay(500);
                groupDatas.Add(chatId, groupData);
            }
            else
                groupData = groupDatas[chatId];

            try
            {
                if (message.Type == MessageType.Photo && message.Caption == "/kgd") // if the user requested to make a demotivator
                {
                    if (groupData.lastDem.AddMinutes(1) > DateTime.Now)
                    {
                        await botClient.SendTextMessageAsync(message.Chat.Id, "Wait before using the command again.");
                        return;
                    }
                    var photo = botClient.GetFileAsync(message.Photo[message.Photo.Count() - 1].FileId).Result.FilePath; // Getting the path to the photo
                    string output = $@"D:\KobraData\{message.Chat.Id}\dem.jpg";
                    GenerateDemotivator(photo,
                                MarkovChains.BuildString(groupData.DictMain, true, r),
                                MarkovChains.BuildString(groupData.DictMain, true, r), message.Chat.Id);
                    groupData.lastDem = DateTime.Now;
                    using (var stream = File.OpenRead(output))
                    {
                        InputOnlineFile inputOnlineFile = new InputOnlineFile(stream);
                        await botClient.SendPhotoAsync(chatId: chatId, inputOnlineFile, replyToMessageId:update.Message.MessageId);
                        foreach (var item in groupData.photosDem)
                        {
                            if (item == photo)
                            {
                                DataIO.SaveData(groupData, chatId);
                                groupDatas[chatId] = groupData;
                                return;
                            }
                        }
                        groupData.photosDem.Add(photo);
                    }
                    DataIO.SaveData(groupData, chatId);
                    groupDatas[chatId] = groupData;
                    return;
                }
                if (message.Type == MessageType.Sticker)
                {
                    if (!Learning.CheckExsitsSticker(groupData.stickerList, message.Sticker.FileUniqueId))
                    {
                        groupData.stickerList.Add(new Sticker()
                        {
                            FileId = message.Sticker.FileId,
                            UnicId = message.Sticker.FileUniqueId
                        });
                    }
                    
                }
                if (message.Type == MessageType.Text)
                {
                    string text = message.Text;
                    text = Regex.Replace(text, @"\s+", " ").TrimEnd(' ');

                    if (text[0] == '/')
                    {
                        CommandHandler(botClient, update, text.Split(' '), groupData);
                        DataIO.SaveData(groupData, chatId);
                        groupDatas[chatId] = groupData;
                        return;
                    }

                    if (groupData.settings.mustLearn)
                    {
                        groupData = Learning.learnOnMessage(text, message.From.Id, groupData, r);
                    };
                }
                if (groupData.settings.chanseAnswer > r.Next(0, 100) && groupData.settings.mustAnswer && groupData.DictMain.Count > 0)
                {
                    if (r.Next(0, 100) > 85 && groupData.stickerList.Count > 1)
                    {
                        if (r.Next(0, 100) > 50 && groupData.photosDem.Count >= 1)
                        {
                            GenerateDemotivator(groupData.photosDem[r.Next(groupData.photosDem.Count-1)],
                                MarkovChains.BuildString(groupData.DictMain, true, r),
                                MarkovChains.BuildString(groupData.DictMain, true, r), message.Chat.Id);
                            using (var stream = File.OpenRead($@"D:\KobraData\{message.Chat.Id}\dem.jpg"))
                            {
                                InputOnlineFile inputOnlineFile = new InputOnlineFile(stream);
                                await botClient.SendPhotoAsync(chatId: message.Chat.Id, inputOnlineFile, replyToMessageId: message.MessageId);
                            }
                        }
                        await botClient.SendStickerAsync(chatId, new InputOnlineFile(groupData.stickerList[r.Next(0, groupData.stickerList.Count - 1)].FileId));
                        Console.WriteLine($"{message.From.FirstName}   |   {message.Text}   |   Sticker");
                        return;
                    }
                    string answer = MarkovChains.BuildString(groupData.DictMain, true, r);
                    await botClient.SendTextMessageAsync(message.Chat.Id, answer);
                    Console.WriteLine($"{message.From.FirstName}   |   {message.Text}   |   {answer}");
                }
            }
            catch (Exception ex)
            {
                await botClient.SendTextMessageAsync(chatId, "Operation failed. More info in console.");
                Console.WriteLine(ex.ToString());
            }
            
            DataIO.SaveData(groupData, chatId);
            groupDatas[chatId] = groupData;
        }

        /// <summary>
        /// Generates demotivators
        /// </summary>
        /// <param name="photoId">Photo path</param>
        /// <param name="header">Main text</param>
        /// <param name="text">Second text</param>
        /// <param name="chatID">Chat Id</param>
        public static void GenerateDemotivator(string photoId, string header, string text, long chatID)
        {
            var download_url = @"https://api.telegram.org/file/bot5623081399:AAEjS7HzEoXuZEzjIebPgH-Qzt3Jz6J3DtE/" + photoId;
            string downloadPhoto = $@"D:\KobraData\{chatID}\PhotoForDem.jpg";
            using (WebClient client = new WebClient())
            {
                if (!Directory.Exists($@"D:\KobraData\{chatID}"))
                    Directory.CreateDirectory($@"D:\KobraData\{chatID}");
                client.DownloadFile(new Uri(download_url), downloadPhoto);
            }
            string imageFilePath = @"C:\Users\MainPC\Documents\sampleDem.jpg";
            var bitmap = Image.FromFile(imageFilePath);
            var a = Image.FromFile(downloadPhoto);
            header = DeleteWordInLength(header, 35);
            text = DeleteWordInLength(text, 86);

            using (Graphics graphics = Graphics.FromImage(bitmap))
            {
                graphics.DrawImage(a, 80f, 50f, 600, 565);
                using (Font arialFont = new Font("Georgia", 30))
                {
                    Rectangle rect1 = new Rectangle(1, 630, 758, 200);
                    StringFormat stringFormat = new StringFormat();
                    stringFormat.Alignment = StringAlignment.Center;
                    graphics.DrawString(header, arialFont, Brushes.White, rect1, stringFormat);
                }
                using (Font arialFont = new Font("Georgia", 25))
                {
                    Rectangle rect1 = new Rectangle(1, 680, 758, 200);
                    StringFormat stringFormat = new StringFormat();
                    stringFormat.Alignment = StringAlignment.Center;
                    graphics.DrawString(text, arialFont, Brushes.White, rect1, stringFormat);
                }
                bitmap.Save($@"D:\KobraData\{chatID}\dem.jpg");
            }
        }

        /// <summary>
        /// Reduces the text to the desired length
        /// </summary>
        /// <param name="text">Text to be processed</param>
        /// <param name="length">Length</param>
        /// <returns>Abbreviated text</returns>
        public static string DeleteWordInLength(string text, int length)
        {
            if (text.Length > length)
            {
                List<string> sp = text.Split(' ').ToList();

                while (true)
                {
                    string len = "";
                    sp.RemoveAt(sp.Count - 1);
                    foreach (string item in sp)
                    {
                        len += " ";
                        len += item;
                    }
                    if (len.Length <= length)
                    {
                        text = len;
                        return text;
                    }
                }
            }
            return text;
        }

        public static async Task Error(ITelegramBotClient client, Exception exception, CancellationToken token)
        {
            Console.WriteLine(exception.Message);
        }
    }
}
