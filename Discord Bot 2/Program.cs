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
    static string turnOf = "0";

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
                    await _client.Connect("Mjc5MjkwNjgzMjY4MDA1ODg4.C4DXPg.31L5sU0R9yxYRWjDU0UQ8vE_1zQ", TokenType.Bot);
                    break;
                }
                catch
                {
                    await Task.Delay(3000);
                }
            }

        });




    }

    static void bot_MessageReceived(object sender, Discord.MessageEventArgs e) //Commands
    {


        char[] delimiterchars = { ' ' };
        string userRole;
        try
        {
            userRole = e.User.Roles.FirstOrDefault().ToString();
        }
        catch
        {
            userRole = "nothing";
        }

        if (e.Message.RawText.StartsWith("/ability")) //Ability Commands -personal
        {
            string[] command = e.Message.RawText.Split(delimiterchars);
            string charValueName = command[1];
            string charName;

            if (command.Length == 2) //uses nickname as charname
            {
                charName = e.User.Nickname;
            }
            else if (command.Length == 3 && userRole == "DM") //uses given charname as charname
            {
                charName = command[2];
            }
            else
            {
                string errorMessage = "U FUCKED UP, command error"; //error in command syntax
                e.User.SendMessage(e.User.Mention + errorMessage);
                return;
            }

            int? charValue = charAbilities(charName, charValueName); //get abilityscore
            if (charValue == null)
            {
                string errorMessage = "U FUCKED UP, parse error"; //error parsing the abilityname
                e.User.SendMessage(e.User.Mention + errorMessage);
                return;
            }

            e.Message.Delete(); //deleting command-message
            string message = String.Format("Your {0} score is {1}", charValueName, charValue);
            e.User.SendMessage(message); //sending user personal message with data
        }
        else if (e.Message.RawText.StartsWith("/hp")) //HP commands -personal
        {
            string[] command = e.Message.RawText.Split(delimiterchars);
            if (command.Length == 1) //the simple /hp command
            {
                int?[] healthPoints = new int?[2];
                healthPoints = charHp(e.User.Nickname);
                if (healthPoints[0] == null)
                {
                    string errorMessage = "U FUCKED UP, Nickname-parse error";
                    e.Message.Delete();
                    e.User.SendMessage(e.User.Mention + errorMessage);
                    return;
                }
                else
                {
                    string hpSender = string.Format("Your current hp is {0}/{1}", healthPoints[0], healthPoints[1]);
                    e.Message.Delete();
                    e.User.SendMessage(hpSender);
                }
            }
            else if (command.Length == 4 && userRole == "DM") //add or remove hp from player
            {
                string playerName = command[1];
                string modifier = command[2];
                string hpScore = command[3];
                int hpModifier;

                string succesmessage;

                if (modifier == "add")
                {
                    try
                    {
                        hpModifier = Int32.Parse(hpScore);
                        succesmessage = string.Format("{0} healt voor {1}", playerName, hpScore);
                    }
                    catch
                    {
                        string errorMessage = "U FUCKED UP, parse error"; //error parsing the abilityname
                        e.User.SendMessage(e.User.Mention + errorMessage);
                        return;
                    }

                }
                else if (modifier == "del" || modifier == "delete")
                {
                    hpScore = string.Format("-{0}", hpScore);
                    try
                    {
                        hpModifier = Int32.Parse(hpScore);
                        succesmessage = string.Format("{0} neemt {1} damage", playerName, hpScore);
                    }
                    catch
                    {
                        string errorMessage = "U FUCKED UP, parse error"; //error parsing the abilityname
                        e.User.SendMessage(e.User.Mention + errorMessage);
                        return;
                    }
                }

                else
                {
                    string errorMessage = "U FUCKED UP, command error"; //error parsing the command
                    e.User.SendMessage(e.User.Mention + errorMessage);
                    return;
                }

                bool succes;

                succes = editCharHp(playerName, hpModifier);

                if (succes == true)
                {
                    e.Channel.SendMessage(e.User.Mention + succesmessage);
                    return;
                }

                else
                {
                    string errorMessage = "U FUCKED UP, editHp error"; //error parsing the abilityname
                    e.User.SendMessage(errorMessage);
                    return;
                }

            }
        }
        else if (e.Message.RawText.StartsWith("/r")) //Roll Dem Dice 2.0
        {
            Random rnd = new Random();
            string[] command = e.Message.RawText.Split(delimiterchars);
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
        else if (userRole == "DM") //Admin commands
        {

            if (e.Message.RawText.StartsWith("/download")) //xml download
            {
                e.Message.Delete(); //deleting command-message
                e.User.SendFile(charSheetlocation);

            }
            else if (e.Message.RawText.StartsWith("/update")) //xml updater
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
                Console.WriteLine("Mentioner");
                char[] delChars2 = { '@' };
                char[] delChars = { '!', '>' };
                string[] split1 = e.Message.RawText.Split(delChars2);
                string[] split2 = split1[1].Split(delimiterchars);
                split2[0] = split2[0].Trim(delChars);
                Console.WriteLine("Mentioning {0}", split2[0]);
                ulong toMention = ulong.Parse(split2[0]);
                e.Server.GetUser(toMention).SendMessage("YOU HAVE BEEN SUMMONED BY THE DM");
                turnOf = e.Server.GetUser(toMention).Nickname;

                
            }
        }

    }

    static bool editCharHp(string charName, int hpModifier)
    {
        bool succes;
        int?[] currentHp = new int?[2];
        int newHpValue;
        string newHpValueString;

        //get current and max HP
        currentHp = charHp(charName);
        if (currentHp[0] == null)
        {
            succes = false;
            return succes;
        }

        //change it, and convert to string
        if (currentHp[0] + hpModifier > currentHp[1])
        {
            newHpValue = currentHp[1] ?? default(int);
        }
        else
        {
            newHpValue = hpModifier + currentHp[0] ?? default(int);
        }

        newHpValueString = newHpValue.ToString();

        //Laden van de XML-sheets
        XmlDocument charSheet = new XmlDocument();
        charSheet.Load(@charSheetlocation);
        string adress1 = string.Format("/csheets/{0}/hp/currenthp", charName);
        //XmlNode charCurrentHp = charSheet.DocumentElement.SelectSingleNode(adress1);

        charSheet.SelectSingleNode(adress1).InnerText = newHpValueString;

        //Change value in XML-sheet
        //charCurrentHp.Value = newHpValueString;
        charSheet.Save(@charSheetlocation);

        succes = true;
        return succes;

    }

    static int?[] charHp(string charName)
    {
        int?[] charHp = new int?[2];
        //Laden van de XML-sheets
        XmlDocument charSheet = new XmlDocument();

        charSheet.Load(@charSheetlocation);

        //Verkrijgen van info uit de XML
        string adress1 = String.Format(" / csheets/{0}/hp/currenthp", charName);
        string adress2 = String.Format("/csheets/{0}/hp/maxhp", charName);
        XmlNode charCurrentHp = charSheet.DocumentElement.SelectSingleNode(adress1);
        XmlNode charMaxHp = charSheet.DocumentElement.SelectSingleNode(adress2);
        try
        {
            charHp[0] = Int32.Parse(charCurrentHp.InnerText);
        }
        catch
        {
            charHp[0] = null;
            Console.WriteLine("charHp has been terminated, Parse error. {0} {1} ", charName, charHp);
            return charHp;
        }

        try
        {
            charHp[1] = Int32.Parse(charMaxHp.InnerText);
        }
        catch
        {
            charHp[1] = null;
            Console.WriteLine("charHp has been terminated, Parse error. {0} {1} ", charName, charHp);
            return charHp;
        }

        Console.WriteLine("charHp initialized for {0}, value of: {1}/{2}", charName, charHp[0], charHp[1]);

        return charHp;

    }

    static int? charAbilities(string charName, string charValueName) //Collects ability score from xml
    {
        //Laden van de XML-sheets
        XmlDocument charSheet = new XmlDocument();

        charSheet.Load(@charSheetlocation);

        //Verkrijgen van info uit de XML
        string adress = String.Format("/csheets/{0}/abilities/{1}", charName, charValueName);

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
            Console.WriteLine("charAbilities has been terminated, Parse error. {0} {1} ", charValueName, charValue);
            return errorCode;
        }

        Console.WriteLine("charAbilities has been succesfully executed with a {0} score of {1}", charValueName, charValue);

        //Return de value
        return charValue;
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
