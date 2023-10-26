# TriviaDuelBot
Source code of the [@TriviaDuelBot](https://t.me/TriviaDuelBot) on Telegram.

## Usage
The bot is running at [@TriviaDuelBot](https://t.me/TriviaDuelBot) on Telegram.<br>
If you just want to play it, go to the bot, start it and have fun!

## I need help
Come to the [ApeDev Support Group on Telegram](https://t.me/ApeGroup)! We will surely be able to answer your questions there :)

## I want to contribute
Pull requests are appreciated! If you plan to do something larger, maybe come to the [ApeDev Support Group on Telegram](https://t.me/ApeGroup) first,
to check if the pull request would be accepted. Thanks to every contributor!

## Installation
Being written in C# .NET 6, the bot should run on any OS where you can install the .NET 6 Runtime.<br> 
It's been tested under Windows 10 and Ubuntu 20.04.<br>
Before downloading and running the bot, you must have the .NET 6 Runtime installed!

First of all, create your Bot account with [@BotFather](https://t.me/BotFather) on Telegram.<br>
Bot Father will give you a token. Enter that token in the program (see further instructions) but **don't give the token to anybody else!**<br>

Go to your bot (for example through the link in Bot Father's message) and start your bot.<br>
It will not reply you yet, but it will message you later when it starts working :)

To have your own bot be a clone of [@TriviaDuelBot](https://t.me/TriviaDuelBot), you can simply download the latest Release from this GitHub repository.<br>
Unzip the release folder to any directory you like. Afterwards, ...

- Under Linux: Open a shell in the unzipped folder and type `dotnet TriviaDuelBot.dll`
- Under Windows: Open the unzipped folder and double-click `TriviaDuelBot.exe`

That's it! The program will start and guide you through the setup of the bot.<br>
It will ask you to enter three things:
 - Your Telegram ID. If you don't know it, start [@userinfobot](https://t.me/userinfobot) on Telegram and it will tell you.
 - A log chat ID. If you wish the bot to send errors to a group, channel or different user, enter its ID there. Enter 0 to have the bot send errors to yourself.
 - The bot token that you received from Bot Father when creating the bot account.
 
If everything goes right, your bot should now message you and the program should display a line with your bot's name, username and ID.<br>
Congratulations, you have the bot running now!

Alternatively, you can of course clone this repository, do changes to the code as you want and compile it.<br>
Constants.cs already gives you some nice settings to toy around with ;)

Have fun quizzing!
