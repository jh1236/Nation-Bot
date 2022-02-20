/*
 * Country.cs
 * 
 * A simple Country structure
 * 
 * 09/02/2022; added Government Type
 * 
 * 14/02/2022; added Country dissolution and optimised key value
 * 
 * 15/02/2022; fixed Country dissolution and fixed key values
 *             added ships
 *             fixed forEach
 *             
 * 16/02/2022; reorganised code so that static members were at the top (why this wasn't done from the start is beyond me)
 *             restructured storage method of all countries so that it is done completely based on owner id 
 *                  HACK: This means that owning multiple countries will require a code refactor
 *             commented through the code
 */

using Discord;
using Discord.WebSocket;
using Newtonsoft.Json;

namespace DiscordBot
{
    internal class Country
    {

        internal static List<string> fileNames = new();
        internal static Dictionary<ulong, Country> CountryList = new();
        private static readonly DiscordSocketClient client = Bot.Instance.Client;

        // runs a function for each country
        internal static void ForEach(Action<Country> action)
        {
            foreach (ulong key in CountryList.Keys)
            {
                Country country = CountryList[key];
                action(country);
            }
        }

        // gets a country based off of it's owner
        internal static Country? GetByOwnerId(ulong id)
        {
            return CountryList[id];
        }

        // deletes a country based of it's owner's ID
        // TODO: refactor this out of a static method (which is where it really obviously belongs?)
        internal static void RemoveByOwnerId(ulong ID)
        {
            Country toRemove = CountryList[ID];
            fileNames.Remove(toRemove.Name);
            File.Delete($"{filePath}/{toRemove.Name}.json");
            File.WriteAllText($"{filePath}/fileNames.txt", String.Join('\n', fileNames));
            CountryList.Remove(ID);
        }

        // pretty self explanatory, but hey it's late and I need to be doing something
        // returns a random country
        internal static Country GetRandom()
        {
            List<Country> countries = CountryList.Values.ToList();
            Random random = new();
            int index = random.Next(countries.Count);
            return countries[index];
        }

        // exactly as it says
        internal static Country NewCountryAndSaveAsync(string name, IUser owner, string religion, string species, string govtType)
        {
            var ret = new Country(name, owner, religion, species, govtType);
            ret.WriteToFile();
            return ret;
        }

        // probably doesn't belong here, but it makes the first letter of every word capitalised
        // TODO: find me a home
        internal static string CaseEachWord(String inputString)
        {
            string[] split = inputString.Split(' ');
            for (int i = 0; i < split.Length; i++)
            {
                string c = split[i][0].ToString().ToUpper();
                string post = split[i][1..];
                split[i] = c + post;
            }
            return string.Join(' ', split);
        }

        // loads all the countries from disk
        internal static void LoadAllFromFile()
        {
            fileNames.Clear();
            string[] allCountries = File.ReadAllText("G:/Programming/c#/DiscordBot/data/fileNames.txt").Split('\n');
            fileNames.AddRange(allCountries);
            foreach (string countryName in allCountries)
            {
                if (string.IsNullOrWhiteSpace(countryName)) continue;
                string a = File.ReadAllText($"G:/Programming/c#/DiscordBot/data/{countryName}.json");
                Country ret = JsonConvert.DeserializeObject<Country>(a)!;
                CountryList[ret.OwnerId] = ret;
            }
        }



        // FIXME: This is a VERY system dependent line, program WILL break on any other pc but mine
        private const string filePath = "G:/Programming/c#/DiscordBot/data";

        // Aesthetic details
        internal string Name;
        internal string Religion;
        internal readonly string Species;
        internal string GovtType;
        internal List<int> Friends = new();
        internal List<int> Enemies = new();


        // Values used in calcs
        internal int MovesToPlay;
        internal int Farms;
        internal int Food;
        internal int Money;
        internal int ArmySize;
        internal int Ships;
        internal int CountryId { get; set; }
        private IUser? owner;
        internal ulong OwnerId;


        // HACK: Warning disabled so the compiler shuts up about the fact that we don't assign values for Name, Religion and GovtType,
        // Which will be automatically replaced by the JsonConvert.DeserializeObject function, which is the ONLY PLACE THAT THIS
        // CONSTRUCTOR SHOULD BE CALLED FROM
#pragma warning disable CS8618
        /**
         * <summary>DO NOT USE</summary>
         */
        [JsonConstructor]
        private Country(string species)
        {
            Species = species;
            Money = 100;
            ArmySize = 0;
            Farms = 0;
            MovesToPlay = 10;
            Food = 0;
            Ships = 0;
        }
#pragma warning restore CS8618

        // This is the public, safe to use constructor for creating a Country, although it is probably preferable to use NewCountryAndSaveAsync
        internal Country(string name, IUser owner, string religion, string species, string govtType)
        {
            if (name.StartsWith("@") || name.Contains('\n'))
            {
                throw new ArgumentException("Name must not start with an @ symbol and cannot Contain a newline!!");
            }
            GovtType = CaseEachWord(govtType);
            Species = species;
            Religion = CaseEachWord(religion);
            Name = CaseEachWord(name);
            owner = (SocketGuildUser)owner;
            if (owner != null)
            {
                OwnerId = owner.Id;
            }
            Money = 100;
            ArmySize = 0;
            Farms = 0;
            MovesToPlay = 10;
            Food = 0;
            Ships = 0;
            CountryList[OwnerId] = this;
        }

        internal bool IsAlliesWith(Country c)
        {
            return Friends.Contains(c.CountryId);
        }

        internal void Ally(Country other)
        {
            if (!Friends.Contains(other.CountryId))
            {
                Enemies.Remove(other.CountryId);
                Friends.Add(other.CountryId);
                other.Ally(this);
            }
        }

        internal void War(Country other)
        {
            if (!Enemies.Contains(other.CountryId))
            {
                Friends.Remove(other.CountryId);
                Enemies.Add(other.CountryId);
                other.War(this);
            }

        }

        //HACK: Used to get around the saving of the User, by instead saving their Id and only filling the owner field when it is called, instead of at launch
        //      Mind you, this could be a 'feature' which helps us avoid ratelimiting
        internal async Task<IUser> GetOwner()
        {
            if (owner == null)
            {
                owner = await client.GetUserAsync(OwnerId);
            }
            return owner;
        }

        internal void NextDay()
        {
            Money += 10;
            MovesToPlay = 5;
            for (int i = 0; i < Farms; i++)
            {
                Food += 5;
            }
            WriteToFile();
        }

        internal void BuyFarm()
        {
            if (Money < 10)
            {
                throw new InsufficientFundException($"A Farm Costs 10 million Dollars, you only have {Money} million!");
            }
            if (MovesToPlay < 1)
            {
                throw new OutOfMovesException("No Moves Remaining!!");
            }
            Money -= 10;
            Farms++;
            MovesToPlay--;
        }

        // Saves the file to the desk
        internal bool WriteToFile()
        {
            try
            {
                string writeString = JsonConvert.SerializeObject(this);
                File.WriteAllText($"{filePath}/{Name}.json", writeString);
                if (!fileNames.Contains(Name))
                {
                    fileNames.Add(Name);
                    File.WriteAllText($"{filePath}/fileNames.txt", String.Join('\n', fileNames));
                }
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }

        }

        // used to make printing pretty
        public override string ToString()
        {
            return Name;
        }

        // gets the beautified details of the country, used in /print-country
        internal Embed GetEmbed()
        {
            EmbedBuilder embed = new();
            embed.WithTitle(this.Name).WithDescription($"Owned by <@{OwnerId}>")
                .AddField("Religion", $"*{Name}* is currently practicing **{Religion}**")
                .AddField("Race", $"*{Name}* has the ethnicity of **{Species}**")
                .AddField("Money", $"*{Name}* has $**{Money}** million in it's national bank account")
                .AddField("Army Size", $"*{Name}* has an army of **{ArmySize}** Troops")
                .AddField("Navy Size", $"*{Name}* has a navy of **{Ships}**")
                .AddField("Farms", $"*{Name}* has a countryside of **{Farms}** Ranches")
                .AddField("Food", $"*{Name}* has a federal food reserve of **{Food}** Thousand Potatoes")
                .AddField("Government", $"*{Name}* has a **{GovtType}**")
                .AddField("Actions", $"*{Name}* has **{MovesToPlay}** moves left today");
            List<Country> friendsAsCountry = new();
            foreach (ulong id in Friends)
            {
                friendsAsCountry.Add(GetByOwnerId(id)!);
            }
            if (friendsAsCountry.Any())
            {
                embed.AddField("Allies", $"*{Name}* is currently friends with **{string.Join("**, **", friendsAsCountry)}**");
            }
            List<Country> enemiesAsCountry = new();
            foreach (ulong id in Enemies)
            {
                enemiesAsCountry.Add(GetByOwnerId(id)!);
            }
            if (enemiesAsCountry.Any())
            {
                embed.AddField("Conflicts", $"*{Name}* is currently at war with **{string.Join("**, **", enemiesAsCountry)}**");
            }
            return embed.Build();
        }

    }
}