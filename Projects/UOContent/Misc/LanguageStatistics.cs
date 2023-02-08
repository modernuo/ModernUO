using System.Collections.Generic;
using System.IO;
using Server.Accounting;

namespace Server.Misc
{
    /**
   * This file requires to be saved in a Unicode
   * compatible format.
   *
   * Warning: if you change String.Format methods,
   * please note that the following character
   * is suggested before any left-to-right text
   * in order to prevent undesired formatting
   * resulting from mixing LR and RL text: ‎
   *
   * Use this one if you need to force RL: ‏
   *
   * If you do not see the above chars, please
   * enable showing of unicode control chars
   **/
    public static class LanguageStatistics
    {
        private static readonly InternationalCode[] InternationalCodes =
        {
            new("ARA", "Arabic", "Saudi Arabia", "العربية", "السعودية"),
            new("ARI", "Arabic", "Iraq", "العربية", "العراق"),
            new("ARE", "Arabic", "Egypt", "العربية", "مصر"),
            new("ARL", "Arabic", "Libya", "العربية", "ليبيا"),
            new("ARG", "Arabic", "Algeria", "العربية", "الجزائر"),
            new("ARM", "Arabic", "Morocco", "العربية", "المغرب"),
            new("ART", "Arabic", "Tunisia", "العربية", "تونس"),
            new("ARO", "Arabic", "Oman", "العربية", "عمان"),
            new("ARY", "Arabic", "Yemen", "العربية", "اليمن"),
            new("ARS", "Arabic", "Syria", "العربية", "سورية"),
            new("ARJ", "Arabic", "Jordan", "العربية", "الأردن"),
            new("ARB", "Arabic", "Lebanon", "العربية", "لبنان"),
            new("ARK", "Arabic", "Kuwait", "العربية", "الكويت"),
            new("ARU", "Arabic", "U.A.E.", "العربية", "الامارات"),
            new("ARH", "Arabic", "Bahrain", "العربية", "البحرين"),
            new("ARQ", "Arabic", "Qatar", "العربية", "قطر"),
            new("BGR", "Bulgarian", "Bulgaria", "Български", "България"),
            new("CAT", "Catalan", "Spain", "Català", "Espanya"),
            new("CHT", "Chinese", "Taiwan", "台語", "臺灣"),
            new("CHS", "Chinese", "PRC", "中文", "中国"),
            new("ZHH", "Chinese", "Hong Kong", "中文", "香港"),
            new("ZHI", "Chinese", "Singapore", "中文", "新加坡"),
            new("ZHM", "Chinese", "Macau", "中文", "澳門"),
            new("CSY", "Czech", "Czech Republic", "Čeština", "Česká republika"),
            new("DAN", "Danish", "Denmark", "Dansk", "Danmark"),
            new("DEU", "German", "Germany", "Deutsch", "Deutschland"),
            new("DES", "German", "Switzerland", "Deutsch", "der Schweiz"),
            new("DEA", "German", "Austria", "Deutsch", "Österreich"),
            new("DEL", "German", "Luxembourg", "Deutsch", "Luxembourg"),
            new("DEC", "German", "Liechtenstein", "Deutsch", "Liechtenstein"),
            new("ELL", "Greek", "Greece", "Ελληνικά", "Ελλάδα"),
            new("ENU", "English", "United States"),
            new("ENG", "English", "United Kingdom"),
            new("ENA", "English", "Australia"),
            new("ENC", "English", "Canada"),
            new("ENZ", "English", "New Zealand"),
            new("ENI", "English", "Ireland"),
            new("ENS", "English", "South Africa"),
            new("ENJ", "English", "Jamaica"),
            new("ENB", "English", "Caribbean"),
            new("ENL", "English", "Belize"),
            new("ENT", "English", "Trinidad"),
            new("ENW", "English", "Zimbabwe"),
            new("ENP", "English", "Philippines"),
            new("ESP", "Spanish", "Spain (Traditional Sort)", "Español", "España (tipo tradicional)"),
            new("ESM", "Spanish", "Mexico", "Español", "México"),
            new("ESN", "Spanish", "Spain (International Sort)", "Español", "España (tipo internacional)"),
            new("ESG", "Spanish", "Guatemala", "Español", "Guatemala"),
            new("ESC", "Spanish", "Costa Rica", "Español", "Costa Rica"),
            new("ESA", "Spanish", "Panama", "Español", "Panama"),
            new("ESD", "Spanish", "Dominican Republic", "Español", "Republica Dominicana"),
            new("ESV", "Spanish", "Venezuela", "Español", "Venezuela"),
            new("ESO", "Spanish", "Colombia", "Español", "Colombia"),
            new("ESR", "Spanish", "Peru", "Español", "Peru"),
            new("ESS", "Spanish", "Argentina", "Español", "Argentina"),
            new("ESF", "Spanish", "Ecuador", "Español", "Ecuador"),
            new("ESL", "Spanish", "Chile", "Español", "Chile"),
            new("ESY", "Spanish", "Uruguay", "Español", "Uruguay"),
            new("ESZ", "Spanish", "Paraguay", "Español", "Paraguay"),
            new("ESB", "Spanish", "Bolivia", "Español", "Bolivia"),
            new("ESE", "Spanish", "El Salvador", "Español", "El Salvador"),
            new("ESH", "Spanish", "Honduras", "Español", "Honduras"),
            new("ESI", "Spanish", "Nicaragua", "Español", "Nicaragua"),
            new("ESU", "Spanish", "Puerto Rico", "Español", "Puerto Rico"),
            new("FIN", "Finnish", "Finland", "Suomi", "Suomi"),
            new("FRA", "French", "France", "Français", "France"),
            new("FRB", "French", "Belgium", "Français", "Belgique"),
            new("FRC", "French", "Canada", "Français", "Canada"),
            new("FRS", "French", "Switzerland", "Français", "Suisse"),
            new("FRL", "French", "Luxembourg", "Français", "Luxembourg"),
            new("FRM", "French", "Monaco", "Français", "Monaco"),
            new("HEB", "Hebrew", "Israel", "עִבְרִית", "ישׂראל"),
            new("HUN", "Hungarian", "Hungary", "Magyar", "Magyarország"),
            new("ISL", "Icelandic", "Iceland", "Íslenska", "Ísland"),
            new("ITA", "Italian", "Italy", "Italiano", "Italia"),
            new("ITS", "Italian", "Switzerland", "Italiano", "Svizzera"),
            new("JPN", "Japanese", "Japan", "日本語", "日本"),
            new("KOR", "Korean (Extended Wansung)", "Korea", "한국어", "한국"),
            new("NLD", "Dutch", "Netherlands", "Nederlands", "Nederland"),
            new("NLB", "Dutch", "Belgium", "Nederlands", "België"),
            new("NOR", "Norwegian", "Norway (Bokmål)", "Norsk", "Norge (Bokmål)"),
            new("NON", "Norwegian", "Norway (Nynorsk)", "Norsk", "Norge (Nynorsk)"),
            new("PLK", "Polish", "Poland", "Polski", "Polska"),
            new("PTB", "Portuguese", "Brazil", "Português", "Brasil"),
            new("PTG", "Portuguese", "Portugal", "Português", "Brasil"),
            new("ROM", "Romanian", "Romania", "Limba Română", "România"),
            new("RUS", "Russian", "Russia", "Русский", "Россия"),
            new("HRV", "Croatian", "Croatia", "Hrvatski", "Hrvatska"),
            new("SRL", "Serbian", "Serbia (Latin)", "Srpski", "Srbija i Crna Gora"),
            new("SRB", "Serbian", "Serbia (Cyrillic)", "Српски", "Србија и Црна Гора"),
            new("SKY", "Slovak", "Slovakia", "Slovenčina", "Slovensko"),
            new("SQI", "Albanian", "Albania", "Shqip", "Shqipëria"),
            new("SVE", "Swedish", "Sweden", "Svenska", "Sverige"),
            new("SVF", "Swedish", "Finland", "Svenska", "Finland"),
            new("THA", "Thai", "Thailand", "ภาษาไทย", "ประเทศไทย"),
            new("TRK", "Turkish", "Turkey", "Türkçe", "Türkiye"),
            new("URP", "Urdu", "Pakistan", "اردو", "پاکستان"),
            new("IND", "Indonesian", "Indonesia", "Bahasa Indonesia", "Indonesia"),
            new("UKR", "Ukrainian", "Ukraine", "Українська", "Украина"),
            new("BEL", "Belarusian", "Belarus", "Беларускі", "Беларусь"),
            new("SLV", "Slovene", "Slovenia", "Slovenščina", "Slovenija"),
            new("ETI", "Estonian", "Estonia", "Eesti", "Eesti"),
            new("LVI", "Latvian", "Latvia", "Latviešu", "Latvija"),
            new("LTH", "Lithuanian", "Lithuania", "Lietuvių", "Lietuva"),
            new("LTC", "Classic Lithuanian", "Lithuania", "Lietuviškai", "Lietuva"),
            new("FAR", "Farsi", "Iran", "فارسى", "ايران"),
            new("VIT", "Vietnamese", "Viet Nam", "tiếng Việt", "Việt Nam"),
            new("HYE", "Armenian", "Armenia", "Հայերէն", "Հայաստան"),
            new("AZE", "Azeri", "Azerbaijan (Latin)", "Azərbaycanca", "Azərbaycan"),
            new("AZE", "Azeri", "Azerbaijan (Cyrillic)", "Азәрбајҹанҹа", "Азәрбајҹан"),
            new("EUQ", "Basque", "Spain", "Euskera", "Espainia"),
            new("MKI", "Macedonian", "Macedonia", "Македонски", "Македонија"),
            new("AFK", "Afrikaans", "South Africa", "Afrikaans", "Republiek van Suid-Afrika"),
            new("KAT", "Georgian", "Georgia", "ქართული", "საკარტველო"),
            new("FOS", "Faeroese", "Faeroe Islands", "Føroyska", "Føroya"),
            new("HIN", "Hindi", "India", "हिन्दी", "भारत"),
            new("MSL", "Malay", "Malaysia", "Bahasa melayu", "Malaysia"),
            new("MSB", "Malay", "Brunei Darussalam", "Bahasa melayu", "Negara Brunei Darussalam"),
            new("KAZ", "Kazak", "Kazakstan", "Қазақ", "Қазақстан"),
            new("SWK", "Swahili", "Kenya", "Kiswahili", "Kenya"),
            new("UZB", "Uzbek", "Uzbekistan (Latin)", "O'zbek", "O'zbekiston"),
            new("UZB", "Uzbek", "Uzbekistan (Cyrillic)", "Ўзбек", "Ўзбекистон"),
            new("TAT", "Tatar", "Tatarstan", "Татарча", "Татарстан"),
            new("BEN", "Bengali", "India", "বাংলা", "ভারত"),
            new("PAN", "Punjabi", "India", "ਪੰਜਾਬੀ", "ਭਾਰਤ"),
            new("GUJ", "Gujarati", "India", "ગુજરાતી", "ભારત"),
            new("ORI", "Oriya", "India", "ଓଡ଼ିଆ", "ଭାରତ"),
            new("TAM", "Tamil", "India", "தமிழ்", "இந்தியா"),
            new("TEL", "Telugu", "India", "తెలుగు", "భారత"),
            new("KAN", "Kannada", "India", "ಕನ್ನಡ", "ಭಾರತ"),
            new("MAL", "Malayalam", "India", "മലയാളം", "ഭാരത"),
            new("ASM", "Assamese", "India", "অসমিয়া", "Bhārat"), // missing correct country name
            new("MAR", "Marathi", "India", "मराठी", "भारत"),
            new("SAN", "Sanskrit", "India", "संस्कृत", "भारतम्"),
            new("KOK", "Konkani", "India", "कोंकणी", "भारत")
        };

        private static readonly bool DefaultLocalNames = false;
        private static readonly bool ShowAlternatives = true;
        private static readonly bool CountAccounts = true; // will consider only first character's valid language

        private static string GetFormattedInfo(string code)
        {
            if (code?.Length == 3)
            {
                for (var i = 0; i < InternationalCodes.Length; i++)
                {
                    if (code == InternationalCodes[i].Code)
                    {
                        return InternationalCodes[i].GetName();
                    }
                }
            }

            return $"Unknown code {code}";
        }

        public static void Initialize()
        {
            CommandSystem.Register("LanguageStatistics", AccessLevel.Administrator, LanguageStatistics_OnCommand);
        }

        [Usage("LanguageStatistics"), Description("Generate a file containing the list of languages for each PlayerMobile.")]
        public static void LanguageStatistics_OnCommand(CommandEventArgs e)
        {
            var ht = new Dictionary<string, InternationalCodeCounter>();

            using var writer = new StreamWriter("languages.txt");
            if (CountAccounts)
            {
                foreach (Account acc in Accounts.GetAccounts())
                {
                    for (var i = 0; i < acc.Length; i++)
                    {
                        var mob = acc[i];

                        var lang = mob?.Language;

                        if (lang == null)
                        {
                            continue;
                        }

                        lang = lang.ToUpper();

                        if (ht.TryGetValue(lang, out var codes))
                        {
                            codes.Increase();
                        }
                        else
                        {
                            ht[lang] = new InternationalCodeCounter(lang);
                        }

                        break;
                    }
                }
            }
            else
            {
                foreach (var mob in World.Mobiles.Values)
                {
                    if (mob.Player)
                    {
                        var lang = mob.Language;

                        if (lang == null)
                        {
                            continue;
                        }

                        lang = lang.ToUpper();

                        if (ht.TryGetValue(lang, out var codes))
                        {
                            codes.Increase();
                        }
                        else
                        {
                            ht[lang] = new InternationalCodeCounter(lang);
                        }
                    }
                }
            }

            writer.WriteLine(
                $"Language statistics. Numbers show how many {(CountAccounts ? "accounts" : "playermobile")} use the specified language.");
            writer.WriteLine(
                "====================================================================================================");
            writer.WriteLine();

            // sort the list
            var list = new List<InternationalCodeCounter>(ht.Values);
            list.Sort(InternationalCodeComparer.Instance);

            foreach (var c in list)
            {
                writer.WriteLine($"{GetFormattedInfo(c.Code)}‎ : {c.Count}");
            }

            e.Mobile.SendMessage("Languages list generated.");
        }

        private struct InternationalCode
        {
            private readonly bool m_HasLocalInfo;

            public string Code { get; }

            public string Language { get; }

            public string Country { get; }

            public string Language_LocalName { get; }

            public string Country_LocalName { get; }

            public InternationalCode(string code, string language, string country) : this(code, language, country, null,
                null) =>
                m_HasLocalInfo = false;

            public InternationalCode(string code, string language, string country, string language_localname,
                string country_localname)
            {
                Code = code;
                Language = language;
                Country = country;
                Language_LocalName = language_localname;
                Country_LocalName = country_localname;
                m_HasLocalInfo = true;
            }

            public string GetName()
            {
                string s;

                if (m_HasLocalInfo)
                {
                    s =
                        $"{(DefaultLocalNames ? Language_LocalName : Language)}‎ - {(DefaultLocalNames ? Country_LocalName : Country)}";

                    if (ShowAlternatives)
                    {
                        s +=
                            $"‎ 【{(DefaultLocalNames ? Language : Language_LocalName)}‎ - {(DefaultLocalNames ? Country : Country_LocalName)}‎】";
                    }
                }
                else
                {
                    s = $"{Language}‎ - {Country}";
                }

                return s;
            }
        }

        private class InternationalCodeCounter
        {
            public InternationalCodeCounter(string code)
            {
                Code = code;
                Count = 1;
            }

            public string Code { get; }

            public int Count { get; private set; }

            public void Increase()
            {
                Count++;
            }
        }

        private class InternationalCodeComparer : IComparer<InternationalCodeCounter>
        {
            public static readonly InternationalCodeComparer Instance = new();

            public int Compare(InternationalCodeCounter x, InternationalCodeCounter y)
            {
                var ca = x?.Count ?? 0;
                var cb = y?.Count ?? 0;

                if (ca > cb)
                {
                    return -1;
                }

                if (ca < cb)
                {
                    return 1;
                }

                return string.CompareOrdinal(x?.Code, y?.Code);
            }
        }
    }
}
