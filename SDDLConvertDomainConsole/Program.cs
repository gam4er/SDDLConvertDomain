using CommandLine;

using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text.RegularExpressions;

namespace SDDLConvertDomainConsole
{
    public class Program
    {
        public static bool Verbose = false;
        public class Options
        {
            [Option('s', "SDDL", HelpText = "Input SDDL to be processed.")]
            public string SDDL { get; set; } = "";

            // Omitting long name, defaults to name of property, ie "--verbose"
            [Option(
              Default = false,
              HelpText = "Prints all messages to standard output.")]
            public bool Verbose { get; set; }

            [Option("stdin",
              Default = false,
              HelpText = "Read from stdin")]
            public bool stdin { get; set; }

        }

        static void Main(string [] args)
        {
            //GUIDLookup("05c74c5e-4deb-43b4-bd9f-86664c2a7fd5");
            //AllLookup.Lookup("c975c901-6cea-4b6f-8319-d67f45449506"); 
            //AllLookup.Lookup("0296c120-40da-11d1-a9c0-0000f80367c1");
            //RPCLookup.Lookup("05c74c5e-4deb-43b4-bd9f-86664c2a7fd5");

            CommandLine.Parser.Default.ParseArguments<Options>(args)
              .WithParsed(RunOptions)
              .WithNotParsed(HandleParseError);

        }

        static void RunOptions(Options opts)
        {
            string SDDL = "";
            if (opts.SDDL != "")
                SDDL = opts.SDDL;
            else
            {
                if (opts.stdin)
                {
                    SDDL = AnsiConsole.Ask<string>("[bold red]Input SDDL string[/]");
                    //SDDL = Console.ReadLine();
                }
            }

            Verbose = opts.Verbose;
            

            string pattern_all = @"^
            (?:
              O:(?<OwnerSID>S-\d+(-\d+)+|[A-Z]{2})
            )?
            (?:
              G:(?<GroupSID>S-\d+(-\d+)+|[A-Z]{2})
            )?
            (?:
              D:
              (?<DACLFlags>[PARI]{0,5})
              (?<DACLACEs>(?:\([^\)]+\))+)
            )?
            (?:
              S:
              (?<SACLFlags>[PARI]{0,5})
              (?<SACLACEs>(?:\([^\)]+\))+)
            )?
            $";

            RegexOptions options = RegexOptions.IgnorePatternWhitespace | RegexOptions.Singleline | RegexOptions.IgnoreCase;
            Regex regex_all = new Regex(pattern_all, options);
            Match match_all = regex_all.Match(SDDL);

            Dictionary<string, Dictionary<string, string>> ACEs = new Dictionary<string, Dictionary<string, string>>();

            if (match_all.Success)
            {
                //if (opts.Verbose)
                {
                    Console.WriteLine("SDDL строка валидна.");
                    Console.WriteLine($"SID владельца:\t\t{match_all.Groups ["OwnerSID"].Value}");
                    Console.WriteLine($"SID группы:\t\t{match_all.Groups ["GroupSID"].Value}");
                    Console.WriteLine($"Флаги DACL:\t\t{match_all.Groups ["DACLFlags"].Value}");
                    //Console.WriteLine($"ACE DACL: {match_all.Groups ["DACLACEs"].Value}");
                    Console.WriteLine($"Флаги SACL:\t\t{match_all.Groups ["SACLFlags"].Value}");
                    //Console.WriteLine($"ACE SACL: {match_all.Groups ["SACLACEs"].Value}");
                    Console.WriteLine("====================================");
                }
                string pattern_acl = @"\(([A-Z]{1,2});([A-Z]{0,18});([^;]*?);([a-f0-9\-]{36})?;([a-f0-9\-]{36})?;(S-\d+(-\d+)+|[A-Z]{2})\)";
                Regex regex_acl = new Regex(pattern_acl, options);
                Match match_acls = regex_acl.Match(match_all.Groups ["DACLACEs"].Value);

                while (match_acls.Success)
                {
                    string ace_type = match_acls.Groups [1].Value;
                    string ace_flags = match_acls.Groups [2].Value;
                    string ace_rights = match_acls.Groups [3].Value;
                    string ace_guid = match_acls.Groups [4].Value;
                    string ace_inherited_guid = match_acls.Groups [5].Value;
                    string ace_sid = match_acls.Groups [6].Value;
                    string currect_ACE = match_acls.Value;

                    if (opts.Verbose)
                    {
                        Console.WriteLine($"ACE Type:\t\t{ace_type}");
                        Console.WriteLine($"ACE Flags:\t\t{ace_flags}");
                        Console.WriteLine($"ACE Rights:\t\t{ace_rights}");
                        Console.WriteLine($"ACE GUID:\t\t{ace_guid}");
                        Console.WriteLine($"ACE Inherited GUID:\t{ace_inherited_guid}");
                        Console.WriteLine($"ACE SID:\t\t{ace_sid}");
                        Console.WriteLine("====================================");
                    }

                    bool user_result = false;
                    bool rights_result = false;

                    if (ace_sid == "DG" ||              // DOMAIN_GUESTS
                        ace_sid == "DU" ||              // DOMAIN_USERS
                        ace_sid == "DC" ||              // DOMAIN_COMPUTERS
                        ace_sid == "BG" ||              // BUILTIN_GUESTS
                        ace_sid == "LG" ||              // GUEST
                        ace_sid == "AU" ||              // AUTHENTICATED_USERS
                        ace_sid == "WD" ||              // EVERYONE
                        ace_sid == "AN" ||              // ANONYMOUS
                        ace_sid == "S-1-1-0" ||
                        ace_sid == "S-1-5-7" ||
                        ace_sid == "S-1-5-11" ||
                        ace_sid == "S-1-5-32-545" ||
                        ace_sid == "S-1-5-32-546" ||
                        ace_sid == "S-1-5-32-560" ||
                        ace_sid == "S-1-5-32-562" ||
                        ace_sid == "S-1-5-32-571" ||
                        ace_sid == "S-1-5-32-581" ||
                        ace_sid.EndsWith("-501") ||
                        ace_sid.EndsWith("-513") ||
                        ace_sid.EndsWith("-514") ||
                        ace_sid.EndsWith("-515")
                       )
                    {
                        user_result = true;
                    }

                    var chunks = Enumerable.Range(0, ace_rights.Length / 2).Select(i => ace_rights.Substring(i * 2, 2));

                    rights_result = chunks.Any(x => x == "GW" || // Generic Write  0x40000000
                                                    x == "GA" || // Generic All    0x10000000
                                                    x == "WO" || // Write Owner    0x00080000
                                                    x == "WD" || // Write DAC      0x00040000
                                                    x == "CR" || // Control Access 0x00000100
                                                    x == "WP" || // Write Property 0x00000020
                                                    x == "CC"    // Create Child   0x00000001
                                                   );

                    string convertes_ace_rights = Rights.DecodeAccessRights(ace_rights);
                    string converted_GUID = "";
                    if (ace_guid != "")
                        converted_GUID = AllLookup.Lookup(ace_guid);
                    else
                        converted_GUID = "whole object";

                    if (user_result && rights_result)
                    {
                        if (!ACEs.ContainsKey(ace_sid))
                            ACEs.Add(ace_sid, new Dictionary<string, string>());
                        ACEs [ace_sid].Add(currect_ACE, convertes_ace_rights + " on " + converted_GUID);
                    }
                    match_acls = match_acls.NextMatch();
                }

                var t = new Table()
                    .Border(TableBorder.DoubleEdge)
                    .Title("[yellow]Suspicious Rights[/]")
                    .AddColumn(new TableColumn("[u]User[/]"))
                    .AddColumn(new TableColumn("[u]ACE[/]"))                    
                    //.AddColumn(new TableColumn(new Panel("[u]Explain[/]").BorderColor(Color.Blue)))
                    //.AddRow(new Text("Hello").Centered(), new Markup("[red]World![/]"), Text.Empty)
                    //.AddRow(second, new Text("Whaaat"), new Text("Lol"))
                    //.AddRow(new Markup("[blue]Hej[/]").Centered(), new Markup("[yellow]Världen![/]"), Text.Empty);
                    ;
                t.Expand();
                t.ShowRowSeparators = true;

                // Добавление данных в основную таблицу
                foreach (var user in ACEs)
                {
                    // Создаем вложенную таблицу для пары "ключ-значение"

                    var Explain = new Table()
                        .AddColumn(new TableColumn("[u]ACE[/]"))
                        .AddColumn(new TableColumn("[u]Explain[/]"));

                    Explain.ShowRowSeparators = true;                    
                        //.AddRow("Hello", "[red]World![/]", "")
                        //.AddRow(simple, new Text("Whaaat"), new Text("Lolz"))
                        //.AddRow("[blue]Hej[/]", "[yellow]Världen![/]", "");

                    // Добавляем строки в вложенную таблицу
                    foreach (var kvp in user.Value)
                    {
                        Explain.AddRow($"[green]{kvp.Key}[/]", $"[blue]{kvp.Value}[/]");
                    }

                    Explain.Expand().HideHeaders().NoBorder();
                    //Explain.Border = TableBorder.Minimal;
                    //Explain.ShowRowSeparators = true;

                    // Добавляем пользователя и его вложенную таблицу в основную таблицу
                    if (SddlSidStrings.SIDs.TryGetValue(user.Key, out string username))
                        t.AddRow(new Markup($"[yellow]{username}[/]"), Explain);
                    else
                        t.AddRow(new Markup($"[yellow]{user.Key}[/]"), Explain);
                }

                t.Expand();
                AnsiConsole.Write(t);
            }
            else
            {
                Console.WriteLine("SDDL строка невалидна.");
            }
        }
        static void HandleParseError(IEnumerable<Error> errs)
        {
            //handle errors
        }
    }
}
