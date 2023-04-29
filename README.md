# RssBot

Open source Telegram RSS bot. 

Techs: C#, .NET Core, Docker (Alpine image), Telegram Bot. 

Highly portable, optimized and crossplatform (linux to windows)

## Features

```/add <name> <url>``` : add a RSS feed 

```/remove <name>``` : remove a RSS feed 

```/list ``` : list all feeds

```/help``` : show this message 

Chat Id : To post feeds to the group or channel. (Example: -98453564)

Delay : New feed checking interval in second. (Example: 30)

/Data/database.json : Your feeds will be saved in this file. You can use this file to backup your feeds.

## Usage

### Docker without persistent storage:

```bash
docker run -e "BOT_TOKEN=sometoken" \
-e "CHAT_ID=somechatid" \
-e "DELAY=refresh-interval-in-seconds" \
-d farukcan/rss-bot
```

### Docker with persistent storage:

```bash
docker create \
  --name=rss.bot \
  -e DELAY=60 \
  -e BOT_TOKEN=InsertToken \
  -e CHAT_ID=InsertChatID \
  -v /path/to/host/data:/Data \
  --restart unless-stopped \
  farukcan/rss-bot
```

### Local

* Create .env file that contains environment values (BOT_TOKEN,DELAY,CHAT_ID)
* Run command : ```npm run dev``` or ```dotnet watch run``` (dotnet installation required)

## Environment Variables

BOT_TOKEN= ...from @botfather...

DELAY= ...in seconds...

CHAT_ID=- ...use telegram web to get chat id...

