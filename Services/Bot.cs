using System.ComponentModel;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Serilog;
using Newtonsoft.Json;
using RssBot.Models;
using System.Net;
using System.Xml.Linq;
using System.Text.RegularExpressions;

namespace RssBot.Services 
{
    public class Bot : IHostedService
    {
        public static Database? Database;
        public static Bot? Instance;
        string? BotToken => Environment.GetEnvironmentVariable("BOT_TOKEN");
        string? ChatId => Environment.GetEnvironmentVariable("CHAT_ID");
        int delayInSeconds => int.Parse(Environment.GetEnvironmentVariable("DELAY") ?? "60");
        private TelegramBotClient client;
        public Bot(){
            // create Telegram.Bot client
            if(BotToken is null)
                throw new ArgumentNullException("BOT_TOKEN is null");
            client = new TelegramBotClient(BotToken);
            Log.Information("Bot is instantiated");
            Instance = this;
        }
        public async Task StartAsync(CancellationToken cancellationToken){
            Log.Information("Bot is running");
            ReceiverOptions receiverOptions = new()
            {
                AllowedUpdates = Array.Empty<UpdateType>() // receive all update types
            };
            client.StartReceiving(
                updateHandler: HandleUpdateAsync,
                pollingErrorHandler: HandlePollingErrorAsync,
                receiverOptions: receiverOptions,
                cancellationToken: cancellationToken
            );
            var me = await client.GetMeAsync(cancellationToken);
            Log.Information("Start receiving updates for {BotName}", me.Username ?? "No username");
            BackgroundWorker backgroundWorker = new();
            backgroundWorker.DoWork += Loop;
            backgroundWorker.RunWorkerAsync();
        }

        private async void Loop(object? sender, DoWorkEventArgs e)
        {
            await LoadDatabase();
            if(Database is null) return;
            while(true){
                bool anyChange = false;
                // foreach feed
                foreach(var feed in Database.Feeds){
                    // create httpclient and send get request to url
                    try{
                        var httpClient = new HttpClient();
                        var response = await httpClient.GetAsync(feed.Url);
                        // if response is not ok, log error and continue
                        if(!response.IsSuccessStatusCode){
                            Log.Error("Error getting feed from {Url}", feed.Url);
                            continue;
                        }
                        // read response content
                        var content = await response.Content.ReadAsStringAsync();
                        // parse xml
                        var xml = XDocument.Parse(content);
                        // check pubdate of all items
                        var lastUpdated = feed.LastUpdated;
                            foreach(var item in xml.Root.Element("channel").Elements("item")){
                                // if pubdate is newer than lastupdated
                                var pubDate = DateTime.Parse(item.Element("pubDate").Value);
                                if(pubDate > lastUpdated){
                                    // send message to all users
                                    await client.SendTextMessageAsync(
                                        chatId: ChatId,
                                        text: 
                                            "<b>" + StripHTML(item.Element("title").Value)+ "</b>"
                                            + "\n" + StripHTML(item.Element("description").Value)
                                            + "\n" + StripHTML(item.Element("link").Value),
                                        parseMode: ParseMode.Html
                                    );
                                    // update lastupdated
                                    if(pubDate > feed.LastUpdated){
                                        feed.LastUpdated = pubDate;
                                        anyChange = true;
                                    }
                                }
                            }
                    }catch(Exception exception){
                        Log.Error(exception.ToString());
                    }
                }
                if(anyChange){
                    await WriteDatabase();
                }
                await Task.Delay(delayInSeconds*1000);
            }
        }

        public static string StripHTML(string? input)
        {
            if (input==null)
            {
                return string.Empty;
            }
            return Regex.Replace(input, "<.*?>", String.Empty);

        }

        private async Task LoadDatabase()
        {
            // read database.json file
            string? data = null;
            try{
                data = await System.IO.File.ReadAllTextAsync("Data/database.json");
            }catch(Exception e){
                Log.Error(e, "Error reading Data/database.json");
            }
            if(string.IsNullOrEmpty(data)){
                // create new Database object
                Database = new Database();
                await WriteDatabase();
            }else{
                // convert it to Database object
                Database = JsonConvert.DeserializeObject<Database>(data);
            }
        }

        private async Task WriteDatabase()
        {
            // convert Database object to json
            var data = JsonConvert.SerializeObject(Database);
            // write it to Data/database.json file
            await System.IO.File.WriteAllTextAsync("Data/database.json", data);
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await client.CloseAsync();
        }

        private async Task HandlePollingErrorAsync(ITelegramBotClient client, Exception exception, CancellationToken cts)
        {
            throw exception;
        }

        // Commands :
        // /add <name> <url>
        // /remove <name>
        // /list
        // /help
        // /start
        // /stop
        private async Task HandleUpdateAsync(ITelegramBotClient client, Update update, CancellationToken cts)
        {
            Log.Information(update.Type.ToString() + " type update");
            if (update.Type == UpdateType.Message && update.Message is not null)
            {
                var username = update.Message.From?.Username ?? "no-username";
                Log.Information($"Message from {username} received");
                if(update.Message.Text is null) return;
                // update.Message
                if(update.Message.Text.StartsWith("/add")){
                    var args = update.Message.Text.Split(" ");
                    if(args.Length != 3 && args.Length != 2){
                        await client.SendTextMessageAsync(
                            chatId: update.Message.Chat.Id,
                            text: "Invalid arguments"
                        );
                        return;
                    }
                    Feed feed;
                    if(args.Length == 3){
                        feed = new Feed(args[2]);
                        feed.Name = args[1];
                    }else{
                        feed = new Feed(args[1]);
                    }
                    if(Database is null) return;
                    Database.Feeds.Add(feed);
                    await WriteDatabase();
                    await client.SendTextMessageAsync(
                        chatId: update.Message.Chat.Id,
                        text: $"Feed added : {feed.Name} \n\r {feed.Url}"
                    );
                }else if(update.Message.Text.StartsWith("/remove")){
                    var args = update.Message.Text.Split(" ");
                    if(args.Length != 2){
                        await client.SendTextMessageAsync(
                            chatId: update.Message.Chat.Id,
                            text: "Invalid arguments"
                        );
                        return;
                    }
                    if(Database is null) return;
                    var feed = Database.Feeds.FirstOrDefault(f => f.Name == args[1]);
                    if(feed is null){
                        await client.SendTextMessageAsync(
                            chatId: update.Message.Chat.Id,
                            text: "Feed not found"
                        );
                        return;
                    }
                    Database.Feeds.Remove(feed);
                    await WriteDatabase();
                    await client.SendTextMessageAsync(
                        chatId: update.Message.Chat.Id,
                        text: $"Feed removed : {feed.Name} \n\r {feed.Url}"
                    );
                }else if(update.Message.Text.StartsWith("/list")){
                    if(Database is null) return;
                    var text = "Feeds : \n\r";
                    foreach(var feed in Database.Feeds){
                        text += $"{feed.Name} : {feed.Url} \n\r";
                    }
                    await client.SendTextMessageAsync(
                        chatId: update.Message.Chat.Id,
                        text: text
                    );
                }else if(   update.Message.Text.StartsWith("/help")
                            || update.Message.Text.StartsWith("/start")
                            || update.Message.Text.StartsWith("/stop")
                        )
                    {
                    var text = "/add <name> <url> : add a feed \n\r";
                    text += "/remove <name> : remove a feed \n\r";
                    text += "/list : list all feeds \n\r";
                    text += "/help : show this message \n\r";
                    text += $"Chat Id : {ChatId} \n\r";
                    text += $"Delay : {delayInSeconds} \n\r";
                    await client.SendTextMessageAsync(
                        chatId: update.Message.Chat.Id,
                        text: text
                    );
                    }else{
                        await client.SendTextMessageAsync(
                            chatId: update.Message.Chat.Id,
                            text: "Invalid command, type /help"
                        );
                    }
            }
        }
    }
}