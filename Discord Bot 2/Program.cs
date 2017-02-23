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
    static string diceoutput = " ";         //output of dice displayed in message
    static string sumoutput = "";           //output of sum displayed in message
    static int valueoutput;                 //actual calculated value
    static string skillname = " ";          //name of skill used in throw
    
    /*
     * starts the program
     */
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
        //Roll Dem Dice 3.0
        else if (e.Message.RawText.StartsWith("/r"))                                    
        {
            dicereset();
            sumhandler(splitdicer(command[1]), sender, e);
            channelMessage(string.Format("{0} = {1} = **{2}** `{3}` {4}", diceoutput, sumoutput, valueoutput, skillname, commenthandler(command)), sender, e);
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
        e.Channel.SendMessage(e.User.Mention + message);
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
        //error check
        if (charHp(charName) == null)
        {
            errorMessage(3, sender, e);
            return;
        }
  
        int cNewHp = Math.Min(hpModifier + charHp(charName) ?? default(int), charMaxHp(charName) ?? default(int));

        xmlSet(cNewHp.ToString(), string.Format("/csheets/{0}/hp/currenthp", charName));
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
        string value = xmlGet(String.Format("/csheets/{0}/skills/{1}", charName, charValueName));
        try
        {
            return Int32.Parse(value);
        }
        catch
        {
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

    /**
     * following are the functions needed for the diceroller 
     */
     /*
      * original horror function
      * splits the dicer
      */
    static string[] splitdicer(string unsplitdicer)
    {
        string[] split = new string[20];                //parts of the original string
        int i = 0;                                      //idex
        while(unsplitdicer.IndexOf('+',1)!=-1 || unsplitdicer.IndexOf('-',1) != -1)
        {
            //stringpart till first + or -
            split[i] = unsplitdicer.Remove(getSmallestNonNegative(unsplitdicer.IndexOf('+', 1), unsplitdicer.IndexOf('-', 1))); ;
            //stringpart from first + or -       
            unsplitdicer = unsplitdicer.Remove(0, getSmallestNonNegative(unsplitdicer.IndexOf('+',1), unsplitdicer.IndexOf('-',1)));    
            i++;          
        }
        split[i] = unsplitdicer; //adding last part
        Array.Resize(ref split, i+1); //resize array from 20 to appropiate length
        return split;
    }

    /*
     * Handles all sums
     */
    static void sumhandler(string[] split, object sender, Discord.MessageEventArgs e)
    {
        int number;         //only for TryParse
        
        for(int i=0; i<=split.Length; i++)
        {
            //check for dice
            if (split[i].Any(char.IsDigit) && split[i].Contains("d"))
            {
                dicehandler(split[i], sender, e);
            }
            //checks for integer value
            if (int.TryParse(split[i], out number))
            {
                valueoutput += number;
                sumoutput += split[i];
            }
            //checks for skill
            if (!split[i].Any(char.IsDigit))
            {
                skillhandler(split[i], sender, e);
            }
        }

    }

    /*
     * handles all dicerolls
     */
    static void dicehandler(string dice, object sender, Discord.MessageEventArgs e)
    {
        diceoutput += dice;
        dice = dice.TrimStart('+', '-');        
        int diceAmount = 0;                     //amount of dice (part before d)
        int diceValue = 0;                      //value of dice (part after d)
        string[] dicer = dice.Split('d');       //string[] containing above parts
        if (dicer[0] == "")
        {
            dicer[0] = "1";
        }
        try
        {
            diceAmount = Int32.Parse(dicer[0]);
            diceValue = Int32.Parse(dicer[1]);
        }
        catch
        {
            errorMessage(5, sender, e);
            return;
        }

        if (diceAmount <= 101 && diceValue <= 1001) 
        {
            roller(diceAmount, diceValue);
        }
        else
        {
            errorMessage(6, sender, e);
        }

    }

    /*
     * Handles all skills
     */
    static void skillhandler(string skill,object sender,Discord.MessageEventArgs e)
    {
        skill = skill.TrimStart('+', '-');
        skillname += nameHandler(skill);
        int? skillValue = charSkills(e.Message.User.Nickname, skillname);
        if (skillValue == null)
        {
            errorMessage(6, sender, e);
            return;
        }
        valueoutput += skillValue ?? default(int);
        sumoutput += string.Format("+*{0}*", skillValue);
    }

    /*
     * Handles all comments
     */
    static string commenthandler(string[] command)
    {
        string comment = "";
        int i = 2;
        while (true)
        {
            try
            {
                comment += string.Format("{0} ", command[i]);
                i++;
            }
            catch
            {
                break;
            }
        }
        return comment;
    }

    /*
     * generates the random numbers for the dicerolls
     */
    static void roller(int diceamount, int dicevalue)
    {
        //seed is no problem unless called to often in a short period of time
        //the seed is automatically generated using the system clock
        //-> pseudo-random with a slightly elevated change of higher numbers
        Random rnd = new Random();
        int addValue = 0;

        for(int i = 0; i < diceamount; i++)
        {
            int dice = rnd.Next(1, (dicevalue + 1));
            addValue += dice;
            if (sumoutput == "")
            {
                sumoutput = string.Format("({0})", dice);
            }
            else
            {
                sumoutput += string.Format("+({0})", dice);
            }
        }
        valueoutput += addValue;
    }
    static void dicereset()
    {
        diceoutput = " ";         //output of dice displayed in message
        sumoutput = "";           //output of sum displayed in message
        valueoutput = 0;          //actual calculated value
        skillname = " ";          //name of skill used in throw
    }
}