/*
 * CommandHandler.cs
 * 
 * Interface between the discord bot and the Country system
 * 
 * 14/02/2022; Seperated from Bot.cs into new file
 *             Threaded Command creation
 * 
 * 15/02/2022; Added commands to make govt, name and religion editable
 *             Added list command
 *             Added the log channel and beautified command responses
 *             Simplified the declare-war command
 *             Added Colors to the log output
 *  
 * 16/02/2022; Added some really verbose comments for Nick (its like 00:30 lmao)
 */


using Discord;
using Discord.WebSocket;
using Newtonsoft.Json;

namespace DiscordBot
{
    internal class CommandHandler
    {
        // Channel which the log output is put into
        private ISocketMessageChannel? logChannel = null;

        // used to track the current index of the request to be sent
        private int requestNumber = 0;

        // list of past ally requests in form (requester id, requestee id)
        private readonly List<Tuple<ulong, ulong>> requests = new();

        private readonly DiscordSocketClient client = Bot.Instance.Client;

        // called when a button is pressed (hooked in Bot.cs
        internal async Task ButtonHandler(SocketMessageComponent arg)
        {
            Console.WriteLine(requests.Count);
            switch (arg.Data.CustomId)
            {
                // case for accept button
                case "ally":
                    {
                        int requestNo = int.Parse(arg.Message.Embeds.First().Description.Split('×')[1]);
                        var ids = requests.ElementAt(requestNo);
                        if (ids.Item2 != arg.User.Id)
                        {
                            await arg.RespondAsync("You are not the person this request is being sent to!!", ephemeral: true);
                            break;
                        }
                        Country my = Country.GetByOwnerId(ids.Item1)!;
                        Country other = Country.GetByOwnerId(ids.Item2)!;
                        Console.WriteLine(my.Name + " allies " + other.Name);
                        my.Ally(other);
                        await arg.RespondAsync("Yay!! you're allies now!!", ephemeral: true);
                        await arg.Message.DeleteAsync();
                        await LogToChannel("Alliance", $"A Valiant alliance has been forged in love and friendship! Let us hope that *{my.Name}* and *{other.Name}* shall remain in peace for all the ages.", raw: $"<@{ids.Item1}>,<@{ids.Item2}>", color: 0x22BB22);
                        break;
                    }
                // case for ignore button
                case "ignore":
                    {
                        int requestNo = int.Parse(arg.Message.Content.Split('×')[1]);
                        var ids = requests.ElementAt(requestNo);
                        if (ids.Item2 != arg.User.Id)
                        {
                            await arg.RespondAsync("You are not the person this request is being sent to!!", ephemeral: true);
                            break;
                        }
                        await arg.RespondAsync($"Allyship Request from <@{ids.Item1}> denied", ephemeral: true);
                        await arg.Message.DeleteAsync();
                        break;
                    }
            }
        }

        // called when message is sent (used to define the log channel and also for setup)
        internal async Task MessageHandler(SocketMessage msg)
        {
            if (msg is not SocketUserMessage || msg.Author.IsBot) return;
            if (msg.Channel is SocketGuildChannel channel)
            {
                // if author is Jared and message is !init
                if (msg.Author.Id == 346180804168122369 && msg.Content == "!init")
                {
                    // start a new thread (this basically stops the command creation from halting the whole program, because the command creation is real slow)
                    new Thread(async () =>
                    {

                        try
                        {
                            // get the server the message is sent on (channel is defined in the smart cast in the first if statement)
                            SocketGuild currentGuild = channel.Guild;
                            // delete the original message
                            await msg.DeleteAsync();
                            SlashCommandBuilder slashBuilder = new();

                            slashBuilder = new SlashCommandBuilder();

                            //
                            // BEGINNING OF COMMAND DEFENITIONS
                            //

                            // new-country name:str, religion:str, species:str

                            slashBuilder.WithName("new-country").WithDescription("creates a new Country")
                                .AddOption("name", ApplicationCommandOptionType.String, "The Name of the Country", isRequired: true)
                                .AddOption("religion", ApplicationCommandOptionType.String, "The Religion the Country will follow", isRequired: true)
                                .AddOption("species", ApplicationCommandOptionType.String, "The Species the Country will be controlled by", isRequired: true)
                                .AddOption("government", ApplicationCommandOptionType.String, "The Government type the country will have", isRequired: false);
                            await client.Rest.CreateGuildCommand(slashBuilder.Build(), currentGuild.Id);



                            // ally name:user, [message:str]

                            slashBuilder = new SlashCommandBuilder().WithName("ally").WithDescription("creates a new Alliance with another Country")
                                .AddOption("user", ApplicationCommandOptionType.User, "The other Member of this alliance", isRequired: true)
                                .AddOption("message", ApplicationCommandOptionType.String, "The Message to request said alliance");
                            await client.Rest.CreateGuildCommand(slashBuilder.Build(), currentGuild.Id);


                            // declare-war name:user, [message:str]

                            slashBuilder = new SlashCommandBuilder().WithName("declare-war").WithDescription("opens a conflict with another country")
                                .AddOption("user", ApplicationCommandOptionType.User, "The Country to battle", isRequired: true)
                                .AddOption("message", ApplicationCommandOptionType.String, "The Warcry of this fight");
                            await client.Rest.CreateGuildCommand(slashBuilder.Build(), currentGuild.Id);



                            // change-govt government:str

                            slashBuilder = new SlashCommandBuilder().WithName("change-govt").WithDescription("Changes your country's government type")
                                .AddOption("government", ApplicationCommandOptionType.String, "The new government type", isRequired: true);
                            await client.Rest.CreateGuildCommand(slashBuilder.Build(), currentGuild.Id);


                            // change-name name:str

                            slashBuilder = new SlashCommandBuilder().WithName("change-name").WithDescription("Changes your country's name")
                                .AddOption("name", ApplicationCommandOptionType.String, "The new name", isRequired: true);
                            await client.Rest.CreateGuildCommand(slashBuilder.Build(), currentGuild.Id);


                            // change-religion religion:str

                            slashBuilder = new SlashCommandBuilder().WithName("change-religion").WithDescription("Changes your country's religion")
                                .AddOption("religion", ApplicationCommandOptionType.String, "The new religion", isRequired: true);
                            await client.Rest.CreateGuildCommand(slashBuilder.Build(), currentGuild.Id);



                            // print-country [owner: user]

                            slashBuilder = new SlashCommandBuilder().WithName("print-country").WithDescription("Gets the stats of a player's Country")
                                .AddOption("user", ApplicationCommandOptionType.User, "Country Owner's stats to look up", isRequired: false);
                            await client.Rest.CreateGuildCommand(slashBuilder.Build(), currentGuild.Id);


                            // buy-farm  (no args)

                            slashBuilder = new SlashCommandBuilder().WithName("buy-farm").WithDescription("Creates a new farm for your country ");
                            await client.Rest.CreateGuildCommand(slashBuilder.Build(), currentGuild.Id);


                            // list [to-list: str]

                            slashBuilder = new SlashCommandBuilder().WithName("list").WithDescription("Lists details of countries").AddOption("to-list", ApplicationCommandOptionType.String, "The information to list", isRequired: false);
                            await client.Rest.CreateGuildCommand(slashBuilder.Build(), currentGuild.Id);


                            // disolve-country
                            slashBuilder = new SlashCommandBuilder().WithName("disolve-country").WithDescription("Removes your country");
                            await client.Rest.CreateGuildCommand(slashBuilder.Build(), currentGuild.Id);

                            // next-day
                            slashBuilder = new SlashCommandBuilder().WithName("next-day").WithDescription("Progresses the game to the next day");
                            await client.Rest.CreateGuildCommand(slashBuilder.Build(), currentGuild.Id);

                            // save-all
                            slashBuilder = new SlashCommandBuilder().WithName("save-all").WithDescription("Saves All Countries to Jared's disk");
                            await client.Rest.CreateGuildCommand(slashBuilder.Build(), currentGuild.Id);


                            Console.WriteLine("Successfully Initialised Commands");
                        }
                        catch (Exception ex)
                        {
                            string json = JsonConvert.SerializeObject(ex, Formatting.Indented);
                            Console.WriteLine(json);
                        }
                    }).Start();
                }
                else if ((msg.Author.Id == 346180804168122369 || msg.Author.Id == 379830061584482305) && msg.Content == "!log")
                {
                    // set the log channel to the current channel
                    logChannel = msg.Channel;
                    await msg.DeleteAsync();
                }
            }
            else
            {
                // Prints out if you receive a DM (can easily be removed, is not relevant to the program at all)
                Console.WriteLine($"From {msg.Author.Username}: {msg.CleanContent}");
            }
        }

        // A Simple wrapper for the SendMessage function to the log channel, basically takes a title and a message and writes it in a way that is pretty
        // this can be treated as a black box, so no modification is needed
        internal async Task LogToChannel(string title, string msg, MessageComponent? components = null, string? raw = null, uint? color = null)
        {
            if (logChannel == null) return;

            EmbedBuilder builder = new();
            builder.WithTitle(title).WithDescription(msg).WithCurrentTimestamp();
            if (color != null)
            {
                builder.WithColor(new Color(color.Value));
            }
            await logChannel.SendMessageAsync(embed: builder.Build(), components: components, text: raw);
        }

        // Called when a slash command is used
        // Could probably be split into mutltiple commands, but I'm lazy and think that this is a better way of doing things
        // that said, I will mark it with a

        // TODO: Refactor me into multiple functions for the sake of future reader's sanity
        internal async Task SlashCommandHandler(SocketSlashCommand command)
        {
            Console.WriteLine($"command {command.CommandName} called by {command.User.Username}");
            // options is a list of the arguments passed to the slash command
            // it also lacks relevant type info bc welcome to strong typed languages
            List<SocketSlashCommandDataOption> options = command.Data.Options.ToList();
            switch (command.CommandName)
            {
                case "new-country":
                    {
                        // get the id of the command sender
                        ulong ownerId = command.User.Id;
                        // check if the user already has a country
                        if (Country.GetByOwnerId(ownerId) != null)
                        {
                            await command.RespondAsync("You already have a country!", ephemeral: true);
                            break;
                        }
                        string name = (string)command.Data.Options.First().Value;
                        string religion = (string)command.Data.Options.ElementAt(1).Value;
                        string species = (string)command.Data.Options.ElementAt(2).Value;

                        // note that govtType is optional, so get or default with a Null-coalescing (idk thats what the wiki calls the '??' thing) operation
                        string govtType = command.Data.Options.ElementAtOrDefault(3)?.Value as string ?? "Democratic";


                        // try catch block probably isnt neccessary, I think this is from legacy code?
                        // TODO: remove try catch
                        try
                        {
                            Country.NewCountryAndSaveAsync(name, owner: command.User, religion: religion, species: species, govtType: govtType);
                        }
                        catch (ArgumentException ex)
                        {
                            await command.RespondAsync(ex.Message, ephemeral: true);
                            break;
                        }
                        await LogToChannel("New Country", $"The Country of *{name}* has been created by <@{ownerId}>", color: 0xb00b69);
                        // Really important that this line is here, for some reason, discord decided that every slash command *MUST* have a response?
                        // anyway you'll notice that every case ends with one of these
                        await command.RespondAsync($"The country of **{name}** has been sucessfully created", ephemeral: true);
                        break;
                    }
                case "ally":
                    {
                        // get the "id" of both users
                        ulong myId = command.User.Id;
                        ulong otherId = ((SocketUser)options.First().Value).Id;

                        // ensure both players have a country
                        if (Country.GetByOwnerId(myId) == null)
                        {
                            await command.RespondAsync("You don't currently have a country!  Create one using `/new-country`", ephemeral: true);
                            break;
                        }
                        else if (Country.GetByOwnerId(otherId) == null)
                        {
                            await command.RespondAsync("Your friend doesn't have a country!!", ephemeral: true);
                            break;
                        }


                        string message = command.Data.Options.ElementAtOrDefault(1)?.Value as string ?? "Would you like to be allies?";
                        // make them pwetty bwuttons (ꈍ ᴗ ꈍ✿)
                        ComponentBuilder builder = new();
                        builder.WithButton("Accept", "ally").WithButton("Decline", "ignore");

                        // add the request into the array of all requests
                        requests.Add(new Tuple<ulong, ulong>(myId, otherId));

                        // note the way that the requestNumber is kinda sneakily hidden into the message so that we can find it again once the response button is pressed
                        // specifically its hidden between 2 ALT+0215 (×) characters to make it easy to access
                        await LogToChannel("Alliance Request", $"||×{requestNumber++}×||\n Alliance Request From {Country.GetByOwnerId(myId)!.Name} To {Country.GetByOwnerId(otherId)!.Name} ```{message.Replace("×", string.Empty)}```", components: builder.Build(), raw: $"<@{myId}>,<@{otherId}>", color: 0xDD00DD);
                        await command.RespondAsync("Requesting allyship", ephemeral: true);
                        break;
                    }

                // comments are going to get less verbose from here bc the same patterns are continuosly applied

                case "disolve-country":
                    {
                        ulong myId = command.User.Id;

                        if (Country.GetByOwnerId(myId) == null)
                        {
                            await command.RespondAsync("You don't currently have a country!  Create one using `/new-country`", ephemeral: true);
                            break;
                        }
                        Country c = Country.GetByOwnerId(myId)!;
                        Country.RemoveByOwnerId(myId);
                        await command.RespondAsync("Country deleted!", ephemeral: true);
                        await LogToChannel("Country Dissolved", $"The Country of *{c.Name}* has undergone a tragic civil war, which lead to the implosion of the government.", color: 0xCC0000);
                        break;
                    }
                case "declare-war":
                    {
                        ulong myId = command.User.Id;
                        ulong otherId = ((SocketUser)options.First().Value).Id;
                        if (Country.GetByOwnerId(myId) == null)
                        {
                            await command.RespondAsync("You don't currently have a country!  Create one using `/new-country`", ephemeral: true);
                            break;
                        }
                        else if (Country.GetByOwnerId(otherId) == null)
                        {
                            await command.RespondAsync("Your enemy doesn't have a country!!", ephemeral: true);
                            break;
                        }

                        Country myCountry = Country.GetByOwnerId(myId)!;
                        Country other = Country.GetByOwnerId(otherId)!;
                        string message = command.Data.Options.ElementAtOrDefault(1)?.Value as string ?? $"I formally declare war upon {other.Name}";
                        myCountry.War(other);
                        await LogToChannel("War declaration", $"\nA bloody and brutal conflict has broken out between *{myCountry.Name}* and *{other.Name}*. The conflict was sparked by *{myCountry.Name}*, who said ```{message}```", raw: $"<@{myId}> <@{otherId}>", color: 0xFF0000);
                        await command.RespondAsync("Declaring War now", ephemeral: true);
                        break;
                    }
                case "change-govt":
                    {
                        ulong myId = command.User.Id;
                        string govt = (string)options.First().Value;
                        if (Country.GetByOwnerId(myId) == null)
                        {
                            await command.RespondAsync("You don't currently have a country!  Create one using `/new-country`", ephemeral: true);
                            break;
                        }
                        Country my = Country.GetByOwnerId(myId)!;
                        my.GovtType = govt;
                        // forgot this first time around, but it's important to save the changes that we make
                        my.WriteToFile();
                        await command.RespondAsync("Government successfully changed", ephemeral: true);
                        break;
                    }
                case "change-name":
                    {
                        ulong myId = command.User.Id;
                        string name = (string)options.First().Value;
                        if (Country.GetByOwnerId(myId) == null)
                        {
                            await command.RespondAsync("You don't currently have a country!  Create one using `/new-country`", ephemeral: true);
                            break;
                        }
                        Country my = Country.GetByOwnerId(myId)!;
                        my.Name = name;
                        my.WriteToFile();
                        await command.RespondAsync("Name successfully changed", ephemeral: true);
                        break;
                    }
                case "change-religion":
                    {
                        ulong myId = command.User.Id;
                        string religion = (string)options.First().Value;
                        if (Country.GetByOwnerId(myId) == null)
                        {
                            await command.RespondAsync("You don't currently have a country!  Create one using `/new-country`", ephemeral: true);
                            break;
                        }
                        Country my = Country.GetByOwnerId(myId)!;
                        my.Religion = religion;
                        my.WriteToFile();
                        await command.RespondAsync("Religion successfully changed", ephemeral: true);
                        break;
                    }
                case "print-country":
                    {
                        IUser user = options.FirstOrDefault()?.Value as IUser ?? command.User;
                        ulong ownerId = user.Id;
                        Country? c = Country.GetByOwnerId(ownerId);
                        if (c == null)
                        {
                            await command.RespondAsync("You don't currently have a country!  Create one using `/new-country`", ephemeral: true);
                            break;
                        }

                        await command.RespondAsync(embed: c.GetEmbed(), ephemeral: true);
                        break;

                    }
                // TODO: Make the range of acceptable inputs more clear
                case "list":
                    {
                        List<string> toPrint = new();
                        string toList = options.FirstOrDefault()?.Value as string ?? "country";
                        toList = toList.ToLower();
                        switch (toList)
                        {
                            case "country":
                                {
                                    Country.ForEach(c => toPrint.Add(c.Name));
                                    break;
                                }
                            case "religion":
                                {
                                    Country.ForEach(c => toPrint.Add($"{c.Name}: {c.Religion}"));
                                    break;
                                }
                            case "species":
                                {
                                    Country.ForEach(c => toPrint.Add($"{c.Name}: {c.Species}"));
                                    break;
                                }
                            case "government":
                                {
                                    Country.ForEach(c => toPrint.Add($"{c.Name}: {c.GovtType}"));
                                    break;
                                }
                            default:
                                {
                                    toPrint.Add("That is not a valid field!!");
                                    break;
                                }
                        }

                        /* oh god i have to explain this :/  ok so from left to right
                         * 2 underscores (__): make the title Underlined
                         * 2 asterisks (**)  : make the title Bold
                         * grave (`)         : make the title appear in an inline codeblock
                         * the title         : hopefully self explanatory
                         *
                         * Then everything in reverse ensures that nothing affects the next line of output
                         * 
                         * Now to format the actual list (yikes, I know)
                         * open with three graves (```): opens a code block
                         * then yaml, which says that the code block is going to be yaml code
                         * (it isn't, but yaml gives us pretty highlighting when colons are involved)
                         * then the list generated above
                         * then close the code block
                         * 
                         * fuck we got there in the end
                         */
                        await command.RespondAsync($"__**`{Country.CaseEachWord(toList)}`**__" + "```yaml\n" + string.Join('\n', toPrint) + "```", ephemeral: true);
                        break;
                    }
                case "buy-farm":
                    {
                        ulong ownerId = command.User.Id;
                        Country? c = Country.GetByOwnerId(ownerId);
                        if (c == null)
                        {
                            await command.RespondAsync("You don't currently have a country!  Create one using `/new-country`", ephemeral: true);
                            break;
                        }
                        try
                        {
                            c.BuyFarm();
                        }
                        catch (Exception ex)
                        {
                            await command.RespondAsync(ex.Message, ephemeral: true);
                            break;
                        }
                        await command.RespondAsync("Successfully bought A Farm!", ephemeral: true);
                        break;
                    }
                case "next-day":
                    {
                        // checks if sender == Nick or Jared
                        if (!(command.User.Id == 346180804168122369 || command.User.Id == 379830061584482305))
                        {
                            await command.RespondAsync("Insufficient Permisions!!", ephemeral: true);
                        }
                        Country.ForEach(a => a.NextDay());
                        Country random = Country.GetRandom();
                        await command.RespondAsync("Day progressed", ephemeral: true);
                        await LogToChannel("Next day", $"The sun begins to set, marking the beckoning of a new day! Today's blessing goes to the nation of *{random.Name}*", color: 0xeeff22, raw: $"<@{random.OwnerId}>");
                        break;
                    }

                case "save-all":
                    {
                        // checks if user is jared
                        if (command.User.Id != 346180804168122369)
                        {
                            await command.RespondAsync("Insufficient Permisions!!", ephemeral: true);
                        }
                        Country.ForEach(a => a.WriteToFile());
                        await command.RespondAsync("Saved!", ephemeral: true);
                        break;
                    }
            }
        }
    }
}
