using System.Linq;
using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.Text;

namespace SDDLConvertDomainConsole
{

    public static class AllLookup
    {
        public static string GuidToLdapFilter(byte [] guidBytes)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("\\");
            foreach (byte b in guidBytes)
            {
                sb.AppendFormat("{0:X2}\\", b);
            }
            // Remove the trailing backslash
            sb.Length--;
            return sb.ToString();
        }


        public static string Lookup(string guidString)
        {
            // Список для хранения найденных имен
            List<string> foundNames = new List<string>();

            // Поиск в контейнере Extended Rights по rightsGuid
            foundNames.AddRange(SearchInExtendedRightsContainer(guidString));

            // Поиск в контексте Schema по schemaIDGUID
            foundNames.AddRange(SearchInSchemaContext(guidString));

            // Поиск в контексте домена по objectGUID
            foundNames.AddRange(SearchInDomainContext(guidString));

            if (Program.Verbose)
            {
                // Output the found names
                if (foundNames.Count > 0)
                {
                    Console.WriteLine("Found entries:");
                    foreach (var name in foundNames)
                    {
                        Console.WriteLine($"- {name}");
                    }
                }
                else
                {
                    Console.WriteLine("No entries found with the specified GUID.");
                }
            }
            return foundNames?.FirstOrDefault() ?? guidString;
        }

        static List<string> SearchInExtendedRightsContainer(string guidString)
        {
            List<string> names = new List<string>();

            try
            {
                // Получаем configurationNamingContext из RootDSE
                DirectoryEntry rootDSE = new DirectoryEntry("LDAP://RootDSE");
                string configurationNamingContext = rootDSE.Properties ["configurationNamingContext"].Value.ToString();

                // Устанавливаем базу поиска на контейнер CN=Extended-Rights
                string searchBase = $"LDAP://CN=Extended-Rights,{configurationNamingContext}";
                DirectoryEntry extendedRightsEntry = new DirectoryEntry(searchBase);

                // Создаем LDAP-фильтр (rightsGuid является строкой)
                string ldapFilter = $"(rightsGuid={guidString})";

                // Создаем DirectorySearcher
                DirectorySearcher searcher = new DirectorySearcher(extendedRightsEntry)
                {
                    Filter = ldapFilter,
                    SearchScope = SearchScope.OneLevel // Ограничиваем поиск только на уровне контейнера
                };

                // Указываем свойства для загрузки
                searcher.PropertiesToLoad.Add("name");

                // Выполняем поиск
                SearchResultCollection results = searcher.FindAll();

                foreach (SearchResult result in results)
                {
                    if (result.Properties ["name"].Count > 0)
                    {
                        names.Add(result.Properties ["name"] [0].ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ошибка при поиске в контейнере Extended Rights: " + ex.Message);
            }

            return names;
        }

        static List<string> SearchInSchemaContext(string guidString)
        {
            List<string> names = new List<string>();

            try
            {
                // Преобразуем GUID в формат для LDAP-фильтра (schemaIDGUID является бинарным)
                byte [] guidBytes = new Guid(guidString).ToByteArray();
                string ldapGuid = GuidToLdapFilter(guidBytes);

                // Получаем schemaNamingContext из RootDSE
                DirectoryEntry rootDSE = new DirectoryEntry("LDAP://RootDSE");
                string schemaNamingContext = rootDSE.Properties ["schemaNamingContext"].Value.ToString();
                DirectoryEntry schemaEntry = new DirectoryEntry("LDAP://" + schemaNamingContext);

                // Создаем LDAP-фильтр
                string ldapFilter = "(schemaIDGUID=" + ldapGuid + ")";

                // Создаем DirectorySearcher
                DirectorySearcher searcher = new DirectorySearcher(schemaEntry)
                {
                    Filter = ldapFilter,
                    SearchScope = SearchScope.Subtree
                };

                // Указываем свойства для загрузки
                searcher.PropertiesToLoad.Add("name");

                // Выполняем поиск
                SearchResultCollection results = searcher.FindAll();

                foreach (SearchResult result in results)
                {
                    if (result.Properties ["name"].Count > 0)
                    {
                        names.Add(result.Properties ["name"] [0].ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ошибка при поиске в контексте Schema: " + ex.Message);
            }

            return names;
        }

        static List<string> SearchInDomainContext(string guidString)
        {
            List<string> names = new List<string>();

            try
            {
                // Преобразуем GUID в формат для LDAP-фильтра (objectGUID является бинарным)
                byte [] guidBytes = new Guid(guidString).ToByteArray();
                string ldapGuid = GuidToLdapFilter(guidBytes);

                // Получаем defaultNamingContext из RootDSE
                DirectoryEntry rootDSE = new DirectoryEntry("LDAP://RootDSE");
                string defaultNamingContext = rootDSE.Properties ["defaultNamingContext"].Value.ToString();
                DirectoryEntry domainEntry = new DirectoryEntry("LDAP://" + defaultNamingContext);

                // Создаем LDAP-фильтр
                string ldapFilter = "(objectGUID=" + ldapGuid + ")";

                // Создаем DirectorySearcher
                DirectorySearcher searcher = new DirectorySearcher(domainEntry)
                {
                    Filter = ldapFilter,
                    SearchScope = SearchScope.Subtree
                };

                // Указываем свойства для загрузки
                searcher.PropertiesToLoad.Add("name");

                // Выполняем поиск
                SearchResultCollection results = searcher.FindAll();

                foreach (SearchResult result in results)
                {
                    if (result.Properties ["name"].Count > 0)
                    {
                        names.Add(result.Properties ["name"] [0].ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ошибка при поиске в контексте домена: " + ex.Message);
            }

            return names;
        }

    }

}
