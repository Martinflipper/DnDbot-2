/*
 * Filename: Program.cs
 * Author: Marin Visscher
 * Edited by: Chiel Ton
 */

 /*
  * Packages
  */
using System;
using System.IO;
using System.Xml;
using System.Linq;
using Discord;
using System.Threading;
using System.Threading.Tasks;
using DnDbot;

/*
 * Main code
 */
class Program
{
    /*
     * Main function replacement
     */
    static void Main(string[] args) => new Program().Start();

    /*
     * Variables and such
     */
    private DiscordClient _client;
    static string charSheetlocation = string.Format("{0}CharSheet.xml", Path.GetTempPath());
    public void Start()
    {
        XmlDocument charSheet = new XmlDocument();
        charSheet.LoadXml(DnDbot.Properties.Resources.CharSheet);
        charSheet.Save(charSheetlocation);

        _client = new DiscordClient();
        _client.MessageReceived += bot_MessageReceived;

        _client.ExecuteAndWait(async () =>
        {
            while (true)
            {
                try
                {
                    await _client.Connect("Mjg0MDEzODI0OTI1ODkyNjA5.C49b_w.ZCP3wwhWAvjcHi2onTpPkMD9jZk", TokenType.Bot);
                    break;
                }
                catch
                {
                    await Task.Delay(3000);
                }
            }

        });
    }

    /*
     * message received from a bot
     */
    static void bot_MessageReceived(object sender, Discord.MessageEventArgs e)
    {
        string userRole;
        try
        {
            userRole = e.User.Roles.FirstOrDefault().ToString();
        }
        catch
        {
            userRole = "nothing";
        }

        if (userRole == "DM")
        {
            dmCommand(sender, e);
        }
        else
        {
            pcCommand(sender, e);
        }
    }

    /*
     * command from dm
     */
    static void dmCommand(object sender, Discord.MessageEventArgs e)
    {
        string[] command = commandPart(e.Message.RawText);
        if (e.Message.RawText.StartsWith("/ability") && command.Length == 3)            // DM ability-lookup (/ability playername)
        {
            int? abilScore = charAbil(command[2], command[1]);
            if (abilScore == null)
            {
                errorMessage(4, sender, e);
                return;
            }
            string message = string.Format("The {0} score of {1} = {2}", command[1], command[2], abilScore);
            personalMessage(message, sender, e);
            return;
        }
        else if (e.Message.RawText.StartsWith("/hp") && command.Length == 3)            // DM HP-edit (/hp playername {+/-}value)
        {
            complexHP(command, sender, e);
        }
        else if (e.Message.RawText.StartsWith("/download"))                             //xml download
        {
            e.Message.Delete(); //deleting command-message
            e.User.SendFile(charSheetlocation);

        }
        else if (e.Message.RawText.StartsWith("/update"))                               //xml updater
        {
            e.Message.Delete(); //deleting command-message
            e.User.SendMessage("Old XML file:");
            e.User.SendFile(charSheetlocation);

            string xmlUrl = e.Message.Attachments[0].Url;
            XmlDocument charSheet = new XmlDocument();
            Console.WriteLine(xmlUrl);

            charSheet.Load(xmlUrl);
            charSheet.Save(charSheetlocation);
            e.User.SendMessage("XML file updated");
        }
        else if (e.Message.RawText.Contains("@") && e.Channel.Name == "general")
        {
            char[] delChars = { '!', '>' };
            string[] split1 = e.Message.RawText.Split('@');
            string[] split2 = split1[1].Split(' ');
            ulong toMention = ulong.Parse(split2[0].Trim(delChars));
            e.Server.GetUser(toMention).SendMessage("YOU HAVE BEEN SUMMONED BY THE DM");

        }
        else pcCommand(sender, e);
    }

    /*
     * command from pc
     */
    static void pcCommand(object sender, Discord.MessageEventArgs e)
    {
        string[] command = commandPart(e.Message.RawText);          //the different command sections

        //ability command
        if (e.Message.RawText.StartsWith("/ability"))
        {
            if (charAbil(e.User.Nickname, command[1]) == null)
            {
                errorMessage(4, sender, e);
                return;
            }
            string message = string.Format("The {0} score of {1} = {2}", 
                command[1], command[2], charAbil(e.User.Nickname, command[1]));
            personalMessage(message, sender, e);
            return;
        }
        //HP command
        else if (e.Message.RawText.StartsWith("/hp"))
        {
            simpleHP(sender, e);
            return;
        }
        //Roll Dem Dice 2.0
        //warning dangerous code ahead
        else if (e.Message.RawText.StartsWith("/r"))                                    
        {
            Random rnd = new Random();                      //random seed?
            string[] calculator = new string[30];           //parts of the command, or something
            string userName = e.User.Nickname;              //username
            string skillName = " ";                         //name of the used skill
            string output = " ";                            //output string
            string diceOutput = " ";                        //dice output string
            string comment;                                 //contains the optional comment
            int skValue = 0;                                //value of the used skill
            int addValue = 0;                               //summing value
            int[] i = { 0, 0, 0, 0, 1, 1 };                 //indexers

            if (command[1].Contains("+") || command[1].Contains("-"))
            {
                while (i[4] != 0)
                {
                    i[1] = command[1].IndexOf("+", i[0]);
                    i[2] = command[1].IndexOf("-", i[0]);

                    i[1] = getSmallestNonNegative(i[1], i[2]);

                    if (i[0] == 0)
                    {
                        calculator[0] = command[1].Substring(0, i[1]);
                    }

                    i[3] = i[1] + 1;

                    i[0] = command[1].IndexOf("+", i[3]);
                    i[2] = command[1].IndexOf("-", i[3]);

                    i[0] = getSmallestNonNegative(i[0], i[2]);

                    i[3] = i[0] + 1;

                    if (i[0] != 0)
                    {
                        calculator[i[4]] = command[1].Substring(i[1], (i[0] - i[1]));
                        i[4]++;
                    }
                    else
                    {
                        calculator[i[4]] = command[1].Substring(i[1], (command[1].Length - i[1]));
                        i[4] = 0;

                    }
                    i[5]++;
                }
            }
            else
            {
                calculator[0] = command[1];
            }

            int ii = 0; //indexer2
            int number = 0;

            while (i[5] != ii)
            {
                //check for dices
                if (calculator[ii].Any(char.IsDigit) && calculator[ii].Contains("d"))
                {
                    diceOutput += calculator[ii];
                    calculator[ii] = calculator[ii].TrimStart('+', '-');
                    //let's split the dicer
                    char dicerSplit = 'd';
                    int diceAmount = 0;
                    int diceValue = 0;
                    string[] dicer = calculator[ii].Split(dicerSplit);

                    if (dicer[0] == "")
                    {
                        try
                        {
                            diceAmount = 1;
                            diceValue = Int32.Parse(dicer[1]);
                        }
                        catch
                        {
                            string errorMessage = "U FUCKED UP, parse error";
                            e.User.SendMessage(errorMessage);
                            return;
                        }
                    }
                    else
                    {
                        try
                        {
                            diceAmount = Int32.Parse(dicer[0]);
                            diceValue = Int32.Parse(dicer[1]);
                        }
                        catch
                        {
                            string errorMessage = "U FUCKED UP, parse error";
                            e.User.SendMessage(errorMessage);
                            return;
                        }
                    }
                    while (diceAmount != 0)
                    {
                        int dice = rnd.Next(1, (diceValue + 1));
                        addValue = addValue + dice;
                        if (output == " ")
                        {
                            output = string.Format("({0})", dice);
                            diceAmount = diceAmount - 1;
                        }
                        else
                        {
                            output = output + string.Format("+({0})", dice);
                            diceAmount = diceAmount - 1;
                        }
                    }
                }
                //checks for integer value
                if (int.TryParse(calculator[ii], out number))
                {

                    addValue += number;
                    output += calculator[ii];
                }
                //checks for skill
                if (calculator[ii].Any(char.IsDigit) == false) 
                {
                    calculator[ii] = calculator[ii].TrimStart('+', '-');
                    Console.WriteLine("skill after trim {0}", calculator[ii]);
                    skillName = nameHandler(calculator[ii]);
                    int? skillValue = charSkills(userName, skillName);
                    if (skillValue == null)
                    {
                        string errorMessage = "U FUCKED UP, command error";
                        e.User.SendMessage(e.User.Mention + errorMessage);
                        return;
                    }
                    skValue = skillValue ?? default(int);
                    addValue = addValue + skValue;
                }
                ii++;
            }
            //check for comment if it exists add it
            try
            {
                comment = command[2];
            }
            catch
            {
                comment = "";
            }
            //add skvalue to the output
            if (skValue != 0)
            {
                output += string.Format("+{0}", skValue);
            }
            //final changes to output
            output = string.Format("{0} = {1} = **{2}** `{3}` {4}", diceOutput, output, addValue, skillName, comment);
            //deleting command-message
            e.Message.Delete();
            e.Channel.SendMessage(e.User.Mention + output);
        }
    }

    /*
     * split the command into command sections
     */
    static string[] commandPart(string command)
    {
        return command.Split(' ');
    }

    /*
     * XML-handler: Get
     */
    static string xmlGet(string adress)
    {
        //load XML-sheet
        XmlDocument charSheet = new XmlDocument();
        charSheet.Load(@charSheetlocation);
        //obtain info
        try
        {
            return charSheet.DocumentElement.SelectSingleNode(adress).InnerText;
        }
        catch
        {
            return "none";
        }
    }

    /*
     * XML-handler: Set
     */
    static void xmlSet(string value, string adress)
    {
        XmlDocument charSheet = new XmlDocument();
        charSheet.Load(@charSheetlocation);
        charSheet.SelectSingleNode(adress).InnerText = value;
        charSheet.Save(@charSheetlocation);
    }

    /*
     * sends an error message
     */
    static void errorMessage(int errorCode, object sender, Discord.MessageEventArgs e)
    {
        string message = string.Format("Command Failure, error code {0}", errorCode);
        e.User.SendMessage(message);
    }

    /*
     * sends a personal message
     */
    static void personalMessage(string message, object sender, Discord.MessageEventArgs e)
    {
        e.Message.Delete();
        e.User.SendMessage(message);
    }

    /*
     * sends a channel message
     */
    static void channelMessage(string message, object sender, Discord.MessageEventArgs e)
    {
        e.Message.Delete();
        e.Channel.SendMessage(message);
    }

    /*
     * sets the HP value
     */
    static void simpleHP(object sender, Discord.MessageEventArgs e)
    {
        if (charHp(e.User.Nickname) == null)
        {
            errorMessage(1, sender, e);
        }
        else
        {
            personalMessage(string.Format("Your current hp is {0}/{1}",
                charHp(e.User.Nickname), charMaxHp(e.User.Nickname)), sender, e);
        }
    }

    /*
     * changes the HP value (DM only)
     */
    static void complexHP(string[] command,object sender, Discord.MessageEventArgs e)
    {
        try
        {
            editCharHp(command[1], Int32.Parse(command[2]), sender, e);
        }
        catch
        {
            errorMessage(2, sender, e);
        }
    }

    /*
     * called by compPlexHP and DM command handler
     * does checks sets new HP and edits everything in XML
     */
    static void editCharHp(string charName, int hpModifier, object sender, Discord.MessageEventArgs e)
    {
        int cNewHp;                     //new HP

        //error check
        if (charHp(charName) == null)
        {
            errorMessage(3, sender, e);
            return;
        }
        //change HP, convert to string
        if (charHp(charName) + hpModifier > charMaxHp(charName))
        {
            cNewHp = charMaxHp(charName) ?? default(int);
        }
        else
        {
            cNewHp = hpModifier + charHp(charName) ?? default(int);
        }
        string adress = string.Format("/csheets/{0}/hp/currenthp", charName);
        xmlSet(cNewHp.ToString(), adress);
        string message = string.Format("De hp van {0} is nu {1}. *({2})*", charName, cNewHp, hpModifier);
        channelMessage(message, sender, e);
    }

    /*
     * Displays charHp
     */
    static int? charHp(string charName)
    {
        try
        {
            return Int32.Parse(xmlGet(String.Format(" / csheets/{0}/hp/currenthp", charName)));
        }
        catch
        {
            return null;
        }
    }

    /*
     * Displays charMax Hp
     */
    static int? charMaxHp(string charName)
    {
        try
        {
            return Int32.Parse(xmlGet(String.Format("/csheets/{0}/hp/maxhp", charName)));
        }
        catch
        {
            return null;
        }
    }

    /*
     * return ability value
     */
    static int? charAbil(string charName, string charValueName)
    {
        try
        {
            return Int32.Parse(xmlGet(String.Format("/csheets/{0}/abilities/{1}", charName, charValueName)));
        }
        catch
        {
            return null;
        }
    }

    /*
     * collects skill score from xml
     */
    static int? charSkills(string charName, string charValueName)
    {
        //Laden van de XML-sheets
        XmlDocument charSheet = new XmlDocument();
        charSheet.Load(@charSheetlocation);

        //Verkrijgen van info uit de XML
        XmlNode charInfo = charSheet.DocumentElement.SelectSingleNode(String.Format("/csheets/{0}/skills/{1}", charName, charValueName));

        //Try to return the value, otherwise return errorcode
        int? charValue = null;
        try
        {
            Console.WriteLine("charSkills has been succesfully executed with a {0} score of {1}", charValueName, charValue);
            return Int32.Parse(charInfo.InnerText);
        }
        catch
        {
            Console.WriteLine("charAbilities has been terminated, Parse error. {0} {1} {2} {3}", 
                charValueName, Int32.Parse(charInfo.InnerText), charName, String.Format("/csheets/{0}/skills/{1}", charName, charValueName));
            return null;
        }
    }

    /*
     * changes shortcuts to full names
     */
    static string nameHandler(string namePart) //verandert afkorting naar volledige abilitynaam
    {
        string[] abilitiesList = { "Str", "Con", "Dex", "Int", "Wis", "Cha", "Acr", "Arc", "Ath",
            "Blu", "Dip", "Dun", "End", "Hea", "His", "Ins", "Itd", "Nat", "Per", "Rel", "Ste", "Stw", "Thi" };
        string[] abilitiesNames = { "strength", "constitution", "dexterity", "inteligence", "wisdom",
            "charisma", "acrobatics", "arcana", "athletics", "bluff", "diplomacy", "dungeoneering",
            "endurance", "heal", "history", "insight", "intimidate", "nature", "perception", "religion",
            "stealth", "streetwise", "thievery" };

        int i = 0; //indexer

        if (namePart.Length == 3)
        {
            while (abilitiesList[i].Equals(namePart, StringComparison.OrdinalIgnoreCase) == false)
            {
                i = i + 1;
                if (i > (abilitiesList.Length - 1))
                {
                    return ("false");
                }
            }
        }
        else if (namePart.Length > 3)
        {
            while (abilitiesNames[i].Equals(namePart, StringComparison.OrdinalIgnoreCase) == false)
            {
                i = i + 1;
                if (i > (abilitiesList.Length - 1))
                {
                    return ("false");
                }
            }
        }
        return abilitiesNames[i];
    }

    /*
     * returns int array {value to be added, typecode}
     * Checks command parts to determine function
     */
    static int[] typeChecker(string commandPart, string userName)
    {
        int functionValue;
        int[] typeChecker = new int[2];

        if (int.TryParse(commandPart, out functionValue)) //checks for integer value
        {
            typeChecker[0] = functionValue;

            Console.WriteLine("Typechecker exec:{0}{1}", typeChecker[0], typeChecker[1]);

            return typeChecker;
        }
        else if (commandPart.StartsWith("#")) //checks for comment
        {
            typeChecker[1] = 1;

            Console.WriteLine("Typechecker exec:{0}{1}", typeChecker[0], typeChecker[1]);

            return typeChecker;
        }
        else //checks for ability check
        {
            string fullName = nameHandler(commandPart);
            if (fullName == "false")
            {
                typeChecker[1] = 2;
                return typeChecker;
            }

            int? abilScore = charSkills(userName, fullName);

            typeChecker[0] = abilScore ?? default(int);
            typeChecker[1] = 3;

            Console.WriteLine("Typechecker exec:{0}{1}", typeChecker[0], typeChecker[1]);

            return typeChecker;
        }
    }

    /*
     * returns the smalles nog negative of two numbers
     */
    static int getSmallestNonNegative(int a, int b)
    {
        if (a >= 0 && b >= 0)
            return Math.Min(a, b);
        else if (a >= 0 && b < 0)
            return a;
        else if (a < 0 && b >= 0)
            return b;
        else
            return 0;
    }
}
