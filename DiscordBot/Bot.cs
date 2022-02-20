/*
 * Nick's Discord Bot
 * 
 * Jared Healy, All Rights Reserved (c) 2022
 * 
 * A program for Nick's Nation RPG
 * 
 * 09/02/2022; Added Government type to new counrty command
 *             Commented through code
 *
 * 14/02/2022; Split file into this and CommandHandler.cs
 * 
 * 20/02/2022; Uploaded to github and finally started using Git!!
 */

using Discord;
using Discord.WebSocket;
namespace DiscordBot;
public class Bot
{
    private static readonly Bot instance = new();

// HACK: making these values nullable is more trouble than it's worth, so i just bit the bullet and disabled the compiler error
#pragma warning disable CS8618
    private CommandHandler handler;
    private static readonly string token = "";
    public DiscordSocketClient Client { get; private set; }
#pragma warning restore CS8618

    public static Task Main() => instance.MainAsync();

    public static Bot Instance { get => instance; }

    public async Task MainAsync()
    {
        Client = new DiscordSocketClient();
        // this here is my client secret, a string which i am absolutely NOT allowed to share
        // its the way that discord verifies that i am the bot owner
        await Client.LoginAsync(TokenType.Bot, token);
        await Client.StartAsync();
        // setting up all of the events
        handler = new CommandHandler();
        Client.MessageReceived += handler.MessageHandler;
        Client.SlashCommandExecuted += handler.SlashCommandHandler;
        Client.Connected += Start;
        Client.Log += LogAsync;
        Client.ButtonExecuted += handler.ButtonHandler;
        await Task.Delay(-1);
        //ASDFLHASFGLHASDLKSA

    }



    private Task Start()
    {
        Console.WriteLine("Bot Started");
        Country.LoadAllFromFile();
        return Task.CompletedTask;
    }


    internal Task LogAsync(LogMessage message)
    {
        Console.WriteLine($"[General/{message.Severity}] {message}");
        return Task.CompletedTask;
    }
}
