/*
 * Filename: Program.cs
 * Author: Marin Visscher
 * Edited by: Chiel Ton
 */

using System;
using System.IO;
using System.Xml;
using System.Linq;
using Discord;
using System.Threading;
using System.Threading.Tasks;
using DnDbot;


class Program
{
    static void Main(string[] args) => new Program().Start();

    //Variables and such
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

    //Message Handlers
    static void bot_MessageReceived(object sender, Discord.MessageEventArgs e) //Commands
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

    //Command Handlers
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
    static void pcCommand(object sender, Discord.MessageEventArgs e)
    {
        string[] command = commandPart(e.Message.RawText);
        if (e.Message.RawText.StartsWith("/ability"))
        {
            int? abilScore = charAbil(e.User.Nickname, command[1]);
            if (abilScore == null)
            {
                errorMessage(4, sender, e);
                return;
            }
            string message = string.Format("The {0} score of {1} = {2}", command[1], command[2], abilScore);
            personalMessage(message, sender, e);
            return;
        }
        else if (e.Message.RawText.StartsWith("/hp"))
        {
            simpleHP(sender, e);
            return;
        }
        else if (e.Message.RawText.StartsWith("/r"))                                    //Roll Dem Dice 2.0
        {
            Random rnd = new Random();
            string[] calculator = new string[30];
            string userName = e.User.Nickname;
            string skillName = " ";
            int skValue = 0;

            string output = " ";
            string diceOutput = " ";
            int addValue = 0;

            //let's split the calculator
            int[] i = { 0, 0, 0, 0, 1, 1 }; //indexer

            if (command[1].Contains("+") || command[1].Contains("-"))
            {
                while (i[4] != 0) //lelijkste code ooit, maar het werkt
                {

                    Console.WriteLine(i[5]);

                    i[1] = command[1].IndexOf("+", i[0]);
                    i[2] = command[1].IndexOf("-", i[0]);

                    i[1] = GetSmallestNonNegative(i[1], i[2]);

                    if (i[0] == 0)
                    {
                        calculator[0] = command[1].Substring(0, i[1]);
                        Console.WriteLine(calculator[0]);
                    }

                    i[3] = i[1] + 1;

                    i[0] = command[1].IndexOf("+", i[3]);
                    i[2] = command[1].IndexOf("-", i[3]);

                    i[0] = GetSmallestNonNegative(i[0], i[2]);

                    i[3] = i[0] + 1;

                    if (i[0] != 0)
                    {
                        calculator[i[4]] = command[1].Substring(i[1], (i[0] - i[1]));
                        Console.WriteLine(calculator[i[4]]);
                        i[4]++;
                    }
                    else
                    {
                        calculator[i[4]] = command[1].Substring(i[1], (command[1].Length - i[1]));
                        Console.WriteLine(calculator[i[4]]);
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
                Console.WriteLine("checking commandpart -{0}-", calculator[ii]);

                if (calculator[ii].Any(char.IsDigit) && calculator[ii].Contains("d")) //checks for dice
                {
                    Console.WriteLine("-{0}- is probably a dicer", calculator[ii]);
                    diceOutput = diceOutput + calculator[ii];
                    calculator[ii] = calculator[ii].TrimStart('+', '-');
                    //let's split the dicer
                    char dicerSplit = 'd';
                    int diceAmount = 0;
                    int diceValue = 0;
                    string[] dicer = calculator[ii].Split(dicerSplit);

                    Console.WriteLine("dicer.length={0} dicer[0]={1} dicer[1]={2}", dicer.Length, dicer[0], dicer[1]);

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

                if (int.TryParse(calculator[ii], out number)) //checks for integer value
                {

                    addValue = addValue + number;
                    output = output + calculator[ii];

                }

                if (calculator[ii].Any(char.IsDigit) == false) //probably a skill check 'n add
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

            string comment;

            try
            {
                comment = command[2];
            }
            catch
            {
                comment = "";
            }

            if (skValue != 0)
            {
                output = output + string.Format("+{0}", skValue);
            }

            //final changes to output
            output = string.Format("{0} = {1} = **{2}** `{3}` {4}", diceOutput, output, addValue, skillName, comment);

            e.Message.Delete(); //deleting command-message
            e.Channel.SendMessage(e.User.Mention + output);



        }

        return;
    }
    static string[] commandPart(string command)
    {
        char[] delimiterchars = { ' ' };
        string[] commandPart = command.Split(delimiterchars);
        return commandPart;
    }

    //xml Handlers
    static string xmlGet(string adress)
    {
        //laden XML-sheet
        XmlDocument charSheet = new XmlDocument();
        charSheet.Load(@charSheetlocation);
        string value;
        //verkrijgen info
        try
        {
            value = charSheet.DocumentElement.SelectSingleNode(adress).InnerText;
        }
        catch
        {
            value = "none";
        }
        return value;
    }
    static void xmlSet(string value, string adress)
    {
        XmlDocument charSheet = new XmlDocument();
        charSheet.Load(@charSheetlocation);
        charSheet.SelectSingleNode(adress).InnerText = value;
        charSheet.Save(@charSheetlocation);
        return;
    }

    //Send Messages
    static void errorMessage(int errorCode, object sender, Discord.MessageEventArgs e)
    {
        string message = string.Format("Command Failure, error code {0}", errorCode);
        e.User.SendMessage(message);
    }
    static void personalMessage(string message, object sender, Discord.MessageEventArgs e)
    {

        e.Message.Delete();
        e.User.SendMessage(message);
    }
    static void channelMessage(string message, object sender, Discord.MessageEventArgs e)
    {
        e.Message.Delete();
        e.Channel.SendMessage(message);
    }

    //Health Calculations
    static void simpleHP(object sender, Discord.MessageEventArgs e)
    {
        int? cHp = charHp(e.User.Nickname);
        int? cMaxHp = charMaxHp(e.User.Nickname);
        if (cHp == null)
        {
            errorMessage(1, sender, e);
        }
        else
        {
            string message = string.Format("Your current hp is {0}/{1}", cHp, cMaxHp);
            personalMessage(message, sender, e);
        }
    }
    static void complexHP(string[] command,object sender, Discord.MessageEventArgs e)
    {
        string playerName = command[1];
        string hpScore = command[2];
        int hpModifier;

        try
        {
            hpModifier = Int32.Parse(hpScore);
        }
        catch
        {
            errorMessage(2, sender, e);
            return;
        }

        editCharHp(playerName, hpModifier, sender, e);
    }
    static void editCharHp(string charName, int hpModifier, object sender, Discord.MessageEventArgs e)
    {
        //get current and max HP
        int? cHp = charHp(charName);
        int? cMaxHp = charMaxHp(charName);
        int cNewHp;
        string stringcNewHp;

        if (cHp == null)
        {
            errorMessage(3, sender, e);
            return;
        }
        //change HP, convert to string
        if (cHp + hpModifier > cMaxHp)
        {
            cNewHp = cMaxHp ?? default(int);
        }
        else
        {
            cNewHp = hpModifier + cHp ?? default(int);
        }

        stringcNewHp = cNewHp.ToString();

        string adress = string.Format("/csheets/{0}/hp/currenthp", charName);
        xmlSet(stringcNewHp, adress);

        string message = string.Format("De hp van {0} is nu {1}. *({2})*", charName, cNewHp, hpModifier);
        channelMessage(message, sender, e);
    }
    static int? charHp(string charName)
    {
        int? charHp;
        string adress = String.Format(" / csheets/{0}/hp/currenthp", charName);
        string charCurrentHp = xmlGet(adress);
        try
        {
            charHp = Int32.Parse(charCurrentHp);
        }
        catch
        {
            charHp = null;
        }

        return charHp;
    }
    static int? charMaxHp(string charName)
    {
        int? charHp;
        string adress = String.Format("/csheets/{0}/hp/maxhp", charName);
        string charMaxHp = xmlGet(adress);
        try
        {
            charHp = Int32.Parse(charMaxHp);
        }
        catch
        {
            charHp = null;
        }
        return charHp;
    }

    //Ability Calculations
    static int? charAbil(string charName, string charValueName)
    {
        int? charAbil;
        string adress = String.Format("/csheets/{0}/abilities/{1}", charName, charValueName);
        string value = xmlGet(adress);
        try
        {
            charAbil = Int32.Parse(value);
        }
        catch
        {
            charAbil = null;
        }
        return charAbil;
    }


    static int? charSkills(string charName, string charValueName) //collects skill score from xml
    {
        //Laden van de XML-sheets
        XmlDocument charSheet = new XmlDocument();

        charSheet.Load(@charSheetlocation);

        //Verkrijgen van info uit de XML
        string adress = String.Format("/csheets/{0}/skills/{1}", charName, charValueName);

        XmlNode charInfo = charSheet.DocumentElement.SelectSingleNode(adress);

        //Try to return the value, otherwise return errorcode
        int? charValue = null;
        try
        {
            charValue = Int32.Parse(charInfo.InnerText);
        }
        catch
        {
            int? errorCode = null;
            Console.WriteLine("charAbilities has been terminated, Parse error. {0} {1} {2} {3}", charValueName, charValue, charName, adress);
            return errorCode;
        }

        Console.WriteLine("charSkills has been succesfully executed with a {0} score of {1}", charValueName, charValue);

        //Return de value
        return charValue;
    }

    static string nameHandler(string namePart) //verandert afkorting naar volledige abilitynaam
    {
        string[] abilitiesList = { "Str", "Con", "Dex", "Int", "Wis", "Cha", "Acr", "Arc", "Ath", "Blu", "Dip", "Dun", "End", "Hea", "His", "Ins", "Itd", "Nat", "Per", "Rel", "Ste", "Stw", "Thi" };
        string[] abilitiesNames = { "strength", "constitution", "dexterity", "inteligence", "wisdom", "charisma", "acrobatics", "arcana", "athletics", "bluff", "diplomacy", "dungeoneering", "endurance", "heal", "history", "insight", "intimidate", "nature", "perception", "religion", "stealth", "streetwise", "thievery" };

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


        string fullname = abilitiesNames[i];
        return fullname;

    }

    static int[] typeChecker(string commandPart, string userName) //returns int array {value to be added, typecode}
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

    static int GetSmallestNonNegative(int a, int b)
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
