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

### Captain Definition
You can also use caprover to deploy it.
* Create caprover app
* Enter environment variable
* Use deployment methods : https://caprover.com/docs/deployment-methods.html
* * Method 2: Tarball => Download this repo, convert zip to tar. And upload it.
* * Method 3: Deploy from Github/Bitbucket/Gitlab => Fork repository. Enter git crediantials, and repo address. 
* * Method 6: Deploy via ImageName => type ```farukcan/rss-bot:latest``` to the area and deploy. (recommended)

## Environment Variables

BOT_TOKEN= ...from @botfather...

DELAY= ...in seconds...

CHAT_ID=- ...use telegram web to get chat id...

