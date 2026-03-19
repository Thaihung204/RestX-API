using Microsoft.AspNetCore.Identity;
using Newtonsoft.Json;
using PayOS.Exceptions;
using RestX.BLL.DataTranferObjects.Share;
using RestX.Models.Tenants;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Dynamic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

namespace RestX.BLL.Helpers
{
    public static class UtilitiesHelper
    {
        public const string AdminRoles = "";
        public const string RestXEmailDomain = "@restx.food";

        // Default Settings
        public const string DefaultFromEmailAddress = "admin@restx.food";
        public const string DefaultFromName = "Admin";

        // Common Cache Key


        private static readonly HashSet<char> base64Chars = new HashSet<char>(
            "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/=".ToCharArray()
        );

        public static string Base64Decode(string base64EncodedData)
        {
            if (string.IsNullOrWhiteSpace(base64EncodedData))
            {
                return null;
            }

            base64EncodedData = base64EncodedData.Trim();

            // Fast manual check for valid chars
            foreach (char c in base64EncodedData)
            {
                if (!base64Chars.Contains(c))
                {
                    return null;
                }
            }

            if (base64EncodedData.Length % 4 != 0)
            {
                return null;
            }

            try
            {
                var bytes = Convert.FromBase64String(base64EncodedData);
                return Encoding.UTF8.GetString(bytes);
            }
            catch
            {
                return null;
            }
        }

        public static string FormatDataSize(double len)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.00} {sizes[order]}";
        }

        public static decimal GetMegabyteSizeOfString(string data)
        {
            try
            {
                if (string.IsNullOrEmpty(data))
                    return 0;
                return ((decimal)Encoding.Unicode.GetByteCount(data) / 1048576);
            }
            catch
            {
                return 0;
            }
        }

        public static string SanitiseStringForUrl(string value)
        {
            var sanitisedValue = "";
            if (!string.IsNullOrEmpty(value))
            {
                sanitisedValue = value.Replace(" ", "-").Replace("/", "-").Replace("&", "and").ToLower();
                var reg = new Regex("[^A-Za-z0-9-]");
                sanitisedValue = reg.Replace(sanitisedValue, string.Empty);
                sanitisedValue = Regex.Replace(sanitisedValue, "-{2,}", "-");
            }
            return sanitisedValue;
        }

        /// <summary>
        ///     Generates a Random Password
        ///     respecting the given strength requirements.
        /// </summary>
        /// <param name="opts">
        ///     A valid PasswordOptions object
        ///     containing the password strength requirements.
        /// </param>
        /// <returns>A random password</returns>
        public static string GenerateRandomPassword(PasswordOptions opts = null)
        {
            if (opts == null)
                opts = new PasswordOptions()
                {
                    RequiredLength = 8,
                    RequiredUniqueChars = 4,
                    RequireDigit = true,
                    RequireLowercase = true,
                    RequireNonAlphanumeric = true,
                    RequireUppercase = true
                };

            string[] randomChars = new[]
            {
                "ABCDEFGHJKLMNOPQRSTUVWXYZ", // uppercase 
                "abcdefghijkmnopqrstuvwxyz", // lowercase
                "0123456789", // digits
                "!@$?_-" // non-alphanumeric
            };
            Random rand = new Random(Environment.TickCount);
            List<char> chars = new List<char>();

            if (opts.RequireUppercase)
                chars.Insert(rand.Next(0, chars.Count),
                    randomChars[0][rand.Next(0, randomChars[0].Length)]);

            if (opts.RequireLowercase)
                chars.Insert(rand.Next(0, chars.Count),
                    randomChars[1][rand.Next(0, randomChars[1].Length)]);

            if (opts.RequireDigit)
                chars.Insert(rand.Next(0, chars.Count),
                    randomChars[2][rand.Next(0, randomChars[2].Length)]);

            if (opts.RequireNonAlphanumeric)
                chars.Insert(rand.Next(0, chars.Count),
                    randomChars[3][rand.Next(0, randomChars[3].Length)]);

            for (int i = chars.Count;
                i < opts.RequiredLength
                || chars.Distinct().Count() < opts.RequiredUniqueChars;
                i++)
            {
                string rcs = randomChars[rand.Next(0, randomChars.Length)];
                chars.Insert(rand.Next(0, chars.Count),
                    rcs[rand.Next(0, rcs.Length)]);
            }

            return new string(chars.ToArray());
        }

        public static List<SelectOption> ConvertEnumToList(Type type)
        {
            var options = new List<SelectOption>();
            foreach (Enum item in Enum.GetValues(type))
            {
                options.Add(new SelectOption
                { Id = ((int)(Enum.Parse(type, item.ToString()))).ToString(), Name = StringValueOfEnum(item) });
            }

            return options;
        }

        public static string StringValueOfEnum(Enum value)
        {
            FieldInfo fi = value.GetType().GetField(value.ToString());
            DescriptionAttribute[] attributes = (DescriptionAttribute[])fi.GetCustomAttributes(typeof(DescriptionAttribute), false);
            if (attributes.Length > 0)
            {
                return attributes[0].Description;
            }
            else
            {
                return value.ToString();
            }
        }

        public static ExpandoObject ConvertToExpandoObject(object obj)
        {
            if (obj == null)
            {
                return null;
            }

            var expando = new ExpandoObject();
            var dictionary = (IDictionary<string, object>)expando;

            foreach (var property in obj.GetType().GetProperties())
                dictionary.Add(property.Name, property.GetValue(obj));

            return expando;
        }

        public static string FormatTimeSpan(TimeSpan timeSpan, bool includeDays = true)
        {
            Func<Tuple<int, string>, string> tupleFormatter = t => $"{t.Item1} {t.Item2}{(t.Item1 == 1 ? string.Empty : "s")}";
            var components = includeDays
                ? new List<Tuple<int, string>>
                {
                    Tuple.Create((int)timeSpan.TotalDays, "day"),
                    Tuple.Create(timeSpan.Hours, "hour"),
                    Tuple.Create(timeSpan.Minutes, "minute"),
                    Tuple.Create(timeSpan.Seconds, "second"),
                    Tuple.Create(timeSpan.Milliseconds, "millisecond")
                }
                : new List<Tuple<int, string>>
                {
                    Tuple.Create((int)timeSpan.TotalHours, "hour"),
                    Tuple.Create(timeSpan.Minutes, "minute"),
                    Tuple.Create(timeSpan.Seconds, "second"),
                    Tuple.Create(timeSpan.Milliseconds, "millisecond")
                };

            components.RemoveAll(i => i.Item1 == 0);

            string extra = "";

            if (components.Count > 1)
            {
                var finalComponent = components[components.Count - 1];
                components.RemoveAt(components.Count - 1);
                extra = $" and {tupleFormatter(finalComponent)}";
            }

            var formattedTime = $"{string.Join(", ", components.Select(tupleFormatter))}{extra}";
            return string.IsNullOrEmpty(formattedTime) ? $"Less than a millisecond ({timeSpan.Ticks} ticks)" : formattedTime;
        }

        public static string HtmlToPlainText(string html)
        {
            if (html == null) return null;

            html = Regex.Replace(html, "<img .*?alt=[\"']?([^\"']*)[\"']?.*?/?>", "$1"); /* Use image alt text. */
            html = Regex.Replace(html, "<li>(.*?)</li>", "- $1\n"); /* Convert links to something useful */
            html = Regex.Replace(html, "<a .*?href=[\"']?([^\"']*)[\"']?.*?>(.*)</a>",
                "$2"); /* Convert links to something useful */
            html = Regex.Replace(html, "<(/p|/div|/ul|/h\\d|br)\\w?/?>", "\n"); /* Let's try to keep vertical whitespace intact. */
            html = Regex.Replace(html, "<[A-Za-z/][^<>]*>", ""); /* Remove the rest of the tags. */
            html = Regex.Replace(html, "[ \n]{3,}", "\n\n");
            html = html.Replace("&lt;", "<").Replace("&gt;", ">");

            return html;
        }

        public static string PlainTextToHtml(string text)
        {
            if (text == null) return null;
            text = HttpUtility.HtmlEncode(text);
            text = text.Replace("&lt;", "<");
            text = text.Replace("&gt;", ">");
            text = text.Replace("\r\n", "\r");
            text = text.Replace("\n", "\r");
            text = text.Replace("\r", "<br>\r\n");
            text = text.Replace("  ", " &nbsp;");
            return text;
        }

        public static int CalculateAge(DateTime dateOfBirth, DateTime pointInTime)
        {
            if (pointInTime < dateOfBirth)
                throw new ArgumentException("Point in time cannot be before the date of birth");

            // Calculate the age.
            var age = pointInTime.Year - dateOfBirth.Year;

            // Go back to the year in which the person was born in case of a leap year
            if (dateOfBirth.Date > pointInTime.AddYears(-age))
                age--;

            return age;
        }

        public static bool IsCurrentUserAnAdmin(ClaimsPrincipal user)
        {
            foreach (var role in AdminRoles.Split(","))
            {
                if (user.IsInRole(role))
                {
                    return true;
                }
            }

            return false;
        }

        private const string PassPhrase = "E546C8DF278CD5931069B522E695D4F2";
        static readonly char[] padding = { '=' };
        public static string Encrypt(string plainText)
        {
            try
            {
                byte[] clearBytes = Encoding.Unicode.GetBytes(plainText);
                using (Aes encryptor = Aes.Create())
                {
                    Rfc2898DeriveBytes pdb = new Rfc2898DeriveBytes(PassPhrase, new byte[] { 0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d, 0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76 });
                    encryptor.Key = pdb.GetBytes(32);
                    encryptor.IV = pdb.GetBytes(16);
                    using (MemoryStream ms = new MemoryStream())
                    {
                        using (CryptoStream cs = new CryptoStream(ms, encryptor.CreateEncryptor(), CryptoStreamMode.Write))
                        {
                            cs.Write(clearBytes, 0, clearBytes.Length);
                            cs.Close();
                        }
                        plainText = Convert.ToBase64String(ms.ToArray()).TrimEnd(padding).Replace('+', '-').Replace('/', '_');
                    }
                }
                return plainText;
            }
            catch
            {

            }
            return "";
        }

        public static string Decrypt(string cipherText)
        {
            string incoming = cipherText.Replace('_', '/').Replace('-', '+');
            switch (cipherText.Length % 4)
            {
                case 2: incoming += "=="; break;
                case 3: incoming += "="; break;
            }
            try
            {
                byte[] cipherBytes = Convert.FromBase64String(incoming);
                using (Aes encryptor = Aes.Create())
                {
                    Rfc2898DeriveBytes pdb = new Rfc2898DeriveBytes(PassPhrase, new byte[] { 0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d, 0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76 });
                    encryptor.Key = pdb.GetBytes(32);
                    encryptor.IV = pdb.GetBytes(16);
                    using (MemoryStream ms = new MemoryStream())
                    {
                        using (CryptoStream cs = new CryptoStream(ms, encryptor.CreateDecryptor(), CryptoStreamMode.Write))
                        {
                            cs.Write(cipherBytes, 0, cipherBytes.Length);
                            cs.Close();
                        }
                        cipherText = Encoding.Unicode.GetString(ms.ToArray());
                    }
                }
                return cipherText;
            }
            catch
            {

            }
            return "";
        }

        public static bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            try
            {
                // Normalize the domain
                email = Regex.Replace(email, @"(@)(.+)$", DomainMapper,
                    RegexOptions.None, TimeSpan.FromMilliseconds(200));

                // Examines the domain part of the email and normalizes it.
                string DomainMapper(Match match)
                {
                    // Use IdnMapping class to convert Unicode domain names.
                    var idn = new IdnMapping();

                    // Pull out and process domain name (throws ArgumentException on invalid)
                    string domainName = idn.GetAscii(match.Groups[2].Value);

                    return match.Groups[1].Value + domainName;
                }
            }
            catch (RegexMatchTimeoutException e)
            {
                return false;
            }
            catch (ArgumentException e)
            {
                return false;
            }

            try
            {
                return Regex.IsMatch(email,
                    @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
                    RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(250));
            }
            catch (RegexMatchTimeoutException)
            {
                return false;
            }
        }

        private static readonly Regex RegexHelper = new Regex("(.)", RegexOptions.Compiled);
        private static readonly Dictionary<string, string> Characters = new Dictionary<string, string>()
            {
                {"À", "A"},
                {"Á", "A"},
                {"Â", "A"},
                {"Ã", "A"},
                {"Ä", "A"},
                {"Å", "A"},
                {"Æ", "AE"},
                {"Ç", "C"},
                {"È", "E"},
                {"É", "E"},
                {"Ê", "E"},
                {"Ë", "E"},
                {"Ì", "I"},
                {"Í", "I"},
                {"Î", "I"},
                {"Ï", "I"},
                {"Ð", "ETH"},
                {"Ñ", "N"},
                {"Ò", "O"},
                {"Ó", "O"},
                {"Ô", "O"},
                {"Õ", "O"},
                {"Ö", "O"},
                {"Ø", "O"},
                {"Ù", "U"},
                {"Ú", "U"},
                {"Û", "U"},
                {"Ü", "U"},
                {"Ý", "Y"},
                {"Þ", "THORN"},
                {"ß", "s"},
                {"à", "a"},
                {"á", "a"},
                {"â", "a"},
                {"ã", "a"},
                {"ä", "a"},
                {"å", "a"},
                {"æ", "ae"},
                {"ç", "c"},
                {"è", "e"},
                {"é", "e"},
                {"ê", "e"},
                {"ë", "e"},
                {"ì", "i"},
                {"í", "i"},
                {"î", "i"},
                {"ï", "i"},
                {"ð", "eth"},
                {"ñ", "n"},
                {"ò", "o"},
                {"ó", "o"},
                {"ô", "o"},
                {"õ", "o"},
                {"ö", "o"},
                {"ø", "o"},
                {"ù", "u"},
                {"ú", "u"},
                {"û", "u"},
                {"ü", "u"},
                {"ý", "y"},
                {"þ", "thorn"},
                {"ÿ", "y"},
                {"Ā", "A"},
                {"ā", "a"},
                {"Ă", "A"},
                {"ă", "a"},
                {"Ą", "A"},
                {"ą", "a"},
                {"Ć", "C"},
                {"ć", "c"},
                {"Ĉ", "C"},
                {"ĉ", "c"},
                {"Ċ", "C"},
                {"ċ", "c"},
                {"Č", "C"},
                {"č", "c"},
                {"Ď", "D"},
                {"ď", "d"},
                {"Đ", "D"},
                {"đ", "d"},
                {"Ē", "E"},
                {"ē", "e"},
                {"Ĕ", "E"},
                {"ĕ", "e"},
                {"Ė", "E"},
                {"ė", "e"},
                {"Ę", "E"},
                {"ę", "e"},
                {"Ě", "E"},
                {"ě", "e"},
                {"Ĝ", "G"},
                {"ĝ", "g"},
                {"Ğ", "G"},
                {"ğ", "g"},
                {"Ġ", "G"},
                {"ġ", "g"},
                {"Ģ", "G"},
                {"ģ", "g"},
                {"Ĥ", "H"},
                {"ĥ", "h"},
                {"Ħ", "H"},
                {"ħ", "h"},
                {"Ĩ", "I"},
                {"ĩ", "i"},
                {"Ī", "I"},
                {"ī", "i"},
                {"Ĭ", "I"},
                {"ĭ", "i"},
                {"Į", "I"},
                {"į", "i"},
                {"İ", "I"},
                {"ı", "i"},
                {"Ĵ", "J"},
                {"ĵ", "j"},
                {"Ķ", "K"},
                {"ķ", "k"},
                {"ĸ", "kra"},
                {"Ĺ", "L"},
                {"ĺ", "l"},
                {"Ļ", "L"},
                {"ļ", "l"},
                {"Ľ", "L"},
                {"ľ", "l"},
                {"Ŀ", "L"},
                {"ŀ", "l"},
                {"Ł", "L"},
                {"ł", "l"},
                {"Ń", "N"},
                {"ń", "n"},
                {"Ņ", "N"},
                {"ņ", "n"},
                {"Ň", "N"},
                {"ň", "n"},
                {"ŉ", "n"},
                {"Ŋ", "ENG"},
                {"ŋ", "eng"},
                {"Ō", "O"},
                {"ō", "o"},
                {"Ŏ", "O"},
                {"ŏ", "o"},
                {"Ő", "O"},
                {"ő", "o"},
                {"Ŕ", "R"},
                {"ŕ", "r"},
                {"Ŗ", "R"},
                {"ŗ", "r"},
                {"Ř", "R"},
                {"ř", "r"},
                {"Ś", "S"},
                {"ś", "s"},
                {"Ŝ", "S"},
                {"ŝ", "s"},
                {"Ş", "S"},
                {"ş", "s"},
                {"Š", "S"},
                {"š", "s"},
                {"Ţ", "T"},
                {"ţ", "t"},
                {"Ť", "T"},
                {"ť", "t"},
                {"Ŧ", "T"},
                {"ŧ", "t"},
                {"Ũ", "U"},
                {"ũ", "u"},
                {"Ū", "U"},
                {"ū", "u"},
                {"Ŭ", "U"},
                {"ŭ", "u"},
                {"Ů", "U"},
                {"ů", "u"},
                {"Ű", "U"},
                {"ű", "u"},
                {"Ų", "U"},
                {"ų", "u"},
                {"Ŵ", "W"},
                {"ŵ", "w"},
                {"Ŷ", "Y"},
                {"ŷ", "y"},
                {"Ÿ", "Y"},
                {"Ź", "Z"},
                {"ź", "z"},
                {"Ż", "Z"},
                {"ż", "z"},
                {"Ž", "Z"},
                {"ž", "z"},
                {"ſ", "s"},
                {"ƀ", "b"},
                {"Ɓ", "B"},
                {"Ƃ", "B"},
                {"ƃ", "b"},
                {"Ƅ", "SIX"},
                {"ƅ", "six"},
                {"Ɔ", "O"},
                {"Ƈ", "C"},
                {"ƈ", "c"},
                {"Ɖ", "D"},
                {"Ɗ", "D"},
                {"Ƌ", "D"},
                {"ƌ", "d"},
                {"ƍ", "delta"},
                {"Ǝ", "E"},
                {"Ə", "SCHWA"},
                {"Ɛ", "E"},
                {"Ƒ", "F"},
                {"ƒ", "f"},
                {"Ɠ", "G"},
                {"Ɣ", "GAMMA"},
                {"ƕ", "hv"},
                {"Ɩ", "IOTA"},
                {"Ɨ", "I"},
                {"Ƙ", "K"},
                {"ƙ", "k"},
                {"ƚ", "l"},
                {"ƛ", "lambda"},
                {"Ɯ", "M"},
                {"Ɲ", "N"},
                {"ƞ", "n"},
                {"Ɵ", "O"},
                {"Ơ", "O"},
                {"ơ", "o"},
                {"Ƣ", "OI"},
                {"ƣ", "oi"},
                {"Ƥ", "P"},
                {"ƥ", "p"},
                {"Ƨ", "TWO"},
                {"ƨ", "two"},
                {"Ʃ", "ESH"},
                {"ƫ", "t"},
                {"Ƭ", "T"},
                {"ƭ", "t"},
                {"Ʈ", "T"},
                {"Ư", "U"},
                {"ư", "u"},
                {"Ʊ", "UPSILON"},
                {"Ʋ", "V"},
                {"Ƴ", "Y"},
                {"ƴ", "y"},
                {"Ƶ", "Z"},
                {"ƶ", "z"},
                {"Ʒ", "EZH"},
                {"Ƹ", "EZH"},
                {"ƹ", "ezh"},
                {"ƺ", "ezh"},
                {"Ƽ", "FIVE"},
                {"ƽ", "five"},
                {"Ǆ", "DZ"},
                {"ǅ", "D"},
                {"ǆ", "dz"},
                {"Ǉ", "LJ"},
                {"ǈ", "L"},
                {"ǉ", "lj"},
                {"Ǌ", "NJ"},
                {"ǋ", "N"},
                {"ǌ", "nj"},
                {"Ǎ", "A"},
                {"ǎ", "a"},
                {"Ǐ", "I"},
                {"ǐ", "i"},
                {"Ǒ", "O"},
                {"ǒ", "o"},
                {"Ǔ", "U"},
                {"ǔ", "u"},
                {"Ǖ", "U"},
                {"ǖ", "u"},
                {"Ǘ", "U"},
                {"ǘ", "u"},
                {"Ǚ", "U"},
                {"ǚ", "u"},
                {"Ǜ", "U"},
                {"ǜ", "u"},
                {"ǝ", "e"},
                {"Ǟ", "A"},
                {"ǟ", "a"},
                {"Ǡ", "A"},
                {"ǡ", "a"},
                {"Ǣ", "AE"},
                {"ǣ", "ae"},
                {"Ǥ", "G"},
                {"ǥ", "g"},
                {"Ǧ", "G"},
                {"ǧ", "g"},
                {"Ǩ", "K"},
                {"ǩ", "k"},
                {"Ǫ", "O"},
                {"ǫ", "o"},
                {"Ǭ", "O"},
                {"ǭ", "o"},
                {"Ǯ", "EZH"},
                {"ǯ", "ezh"},
                {"ǰ", "j"},
                {"Ǳ", "DZ"},
                {"ǲ", "D"},
                {"ǳ", "dz"},
                {"Ǵ", "G"},
                {"ǵ", "g"},
                {"Ƕ", "HWAIR"},
                {"Ƿ", "WYNN"},
                {"Ǹ", "N"},
                {"ǹ", "n"},
                {"Ǻ", "A"},
                {"ǻ", "a"},
                {"Ǽ", "AE"},
                {"ǽ", "ae"},
                {"Ǿ", "O"},
                {"ǿ", "o"},
                {"Ȁ", "A"},
                {"ȁ", "a"},
                {"Ȃ", "A"},
                {"ȃ", "a"},
                {"Ȅ", "E"},
                {"ȅ", "e"},
                {"Ȇ", "E"},
                {"ȇ", "e"},
                {"Ȉ", "I"},
                {"ȉ", "i"},
                {"Ȋ", "I"},
                {"ȋ", "i"},
                {"Ȍ", "O"},
                {"ȍ", "o"},
                {"Ȏ", "O"},
                {"ȏ", "o"},
                {"Ȑ", "R"},
                {"ȑ", "r"},
                {"Ȓ", "R"},
                {"ȓ", "r"},
                {"Ȕ", "U"},
                {"ȕ", "u"},
                {"Ȗ", "U"},
                {"ȗ", "u"},
                {"Ș", "S"},
                {"ș", "s"},
                {"Ț", "T"},
                {"ț", "t"},
                {"Ȝ", "YOGH"},
                {"ȝ", "yogh"},
                {"Ȟ", "H"},
                {"ȟ", "h"},
                {"Ƞ", "N"},
                {"ȡ", "d"},
                {"Ȣ", "OU"},
                {"ȣ", "ou"},
                {"Ȥ", "Z"},
                {"ȥ", "z"},
                {"Ȧ", "A"},
                {"ȧ", "a"},
                {"Ȩ", "E"},
                {"ȩ", "e"},
                {"Ȫ", "O"},
                {"ȫ", "o"},
                {"Ȭ", "O"},
                {"ȭ", "o"},
                {"Ȯ", "O"},
                {"ȯ", "o"},
                {"Ȱ", "O"},
                {"ȱ", "o"},
                {"Ȳ", "Y"},
                {"ȳ", "y"},
                {"ȴ", "l"},
                {"ȵ", "n"},
                {"ȶ", "t"},
                {"ȷ", "j"},
                {"ȸ", "db"},
                {"ȹ", "qp"},
                {"Ⱥ", "A"},
                {"Ȼ", "C"},
                {"ȼ", "c"},
                {"Ƚ", "L"},
                {"Ⱦ", "T"},
                {"ȿ", "s"},
                {"ɀ", "z"},
                {"Ɂ", "STOP"},
                {"ɂ", "stop"},
                {"Ƀ", "B"},
                {"Ʉ", "U"},
                {"Ʌ", "V"},
                {"Ɇ", "E"},
                {"ɇ", "e"},
                {"Ɉ", "J"},
                {"ɉ", "j"},
                {"Ɋ", "Q"},
                {"ɋ", "q"},
                {"Ɍ", "R"},
                {"ɍ", "r"},
                {"Ɏ", "Y"},
                {"ɏ", "y"},
                {"ɐ", "a"},
                {"ɑ", "alpha"},
                {"ɒ", "alpha"},
                {"ɓ", "b"},
                {"ɔ", "o"},
                {"ɕ", "c"},
                {"ɖ", "d"},
                {"ɗ", "d"},
                {"ɘ", "e"},
                {"ə", "schwa"},
                {"ɚ", "schwa"},
                {"ɛ", "e"},
                {"ɜ", "e"},
                {"ɝ", "e"},
                {"ɞ", "e"},
                {"ɟ", "j"},
                {"ɠ", "g"},
                {"ɡ", "script"},
                {"ɣ", "gamma"},
                {"ɤ", "rams"},
                {"ɥ", "h"},
                {"ɦ", "h"},
                {"ɧ", "heng"},
                {"ɨ", "i"},
                {"ɩ", "iota"},
                {"ɫ", "l"},
                {"ɬ", "l"},
                {"ɭ", "l"},
                {"ɮ", "lezh"},
                {"ɯ", "m"},
                {"ɰ", "m"},
                {"ɱ", "m"},
                {"ɲ", "n"},
                {"ɳ", "n"},
                {"ɵ", "barred"},
                {"ɷ", "omega"},
                {"ɸ", "phi"},
                {"ɹ", "r"},
                {"ɺ", "r"},
                {"ɻ", "r"},
                {"ɼ", "r"},
                {"ɽ", "r"},
                {"ɾ", "r"},
                {"ɿ", "r"},
                {"ʂ", "s"},
                {"ʃ", "esh"},
                {"ʄ", "j"},
                {"ʅ", "squat"},
                {"ʆ", "esh"},
                {"ʇ", "t"},
                {"ʈ", "t"},
                {"ʉ", "u"},
                {"ʊ", "upsilon"},
                {"ʋ", "v"},
                {"ʌ", "v"},
                {"ʍ", "w"},
                {"ʎ", "y"},
                {"ʐ", "z"},
                {"ʑ", "z"},
                {"ʒ", "ezh"},
                {"ʓ", "ezh"},
                {"ʚ", "e"},
                {"ʞ", "k"},
                {"ʠ", "q"},
                {"ʣ", "dz"},
                {"ʤ", "dezh"},
                {"ʥ", "dz"},
                {"ʦ", "ts"},
                {"ʧ", "tesh"},
                {"ʨ", "tc"},
                {"ʩ", "feng"},
                {"ʪ", "ls"},
                {"ʫ", "lz"},
                {"ʮ", "h"},
                {"ʯ", "h"}
            };


        public static string ReplaceSpecialCharactersWithTheirEquivalents(string originalText)
        {
            var words = string.Join("|", Characters.Keys);
            return Regex.Replace(originalText, $"({words})", m => Characters[m.Value]);
        }

        // Used when needing to check if an int in an expression tree where out variables and discard's are not allowed.
        public static bool IsInt(string stringToCheck)
        {
            return int.TryParse(stringToCheck, out _);
        }

        public static ulong DatabaseVersionToLong(byte[] bigEndianBinary)
        {
            return ((ulong)bigEndianBinary[0] << 56) |
                   ((ulong)bigEndianBinary[1] << 48) |
                   ((ulong)bigEndianBinary[2] << 40) |
                   ((ulong)bigEndianBinary[3] << 32) |
                   ((ulong)bigEndianBinary[4] << 24) |
                   ((ulong)bigEndianBinary[5] << 16) |
                   ((ulong)bigEndianBinary[6] << 8) |
                           bigEndianBinary[7];
        }

        /// <summary>
        /// Replace URLS in a string with HTML Links.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static string ConvertUrlsToHtmlLinks(string input)
        {
            // Regular expression to match URLs not already inside anchor tags
            string pattern = @"(?<!href=['""])(?<!<[^>]*)\b(https?://\S+|www\.\S+)\b(?![^<]*?>)";

            // Replace URLs with HTML anchor tags using "https://" if needed
            string result = Regex.Replace(input, pattern, match =>
            {
                string url = match.Value;
                if (!url.StartsWith("http://") && !url.StartsWith("https://"))
                {
                    url = "https://" + url; // Add https:// prefix for "www" URLs
                }
                return $"<a href='{url}' target='_blank'>{url}</a>";
            });

            return result;
        }

        /// <summary>
        /// Sanitise a string for use in a CSV
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
		public static string SanitiseForCSV(string input)
        {
            // If the string contains double quotes, escape them
            input = input.Replace("\"", "\"\"");

            // If the string contains commas, enclose the string in double quotes
            if (input.Contains(","))
            {
                input = "\"" + input + "\"";
            }

            // Remove or replace newlines with a space
            input = Regex.Replace(input, @"\r\n?|\n", " "); // This will replace newlines with a space

            return input;
        }

        public static List<string> SplitColonList(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return new List<string>();
            }

            return input.Split(new[] { " : ", ":" }, StringSplitOptions.None)
                .Select(s => s?.Trim())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Distinct()
                .ToList();
        }

        public static string FirstUrl(string input)
        {
            return SplitColonList(input).FirstOrDefault() ?? string.Empty;
        }
        public static string NormalizePhone(string rawPhone)
        {
            var cleaned = rawPhone.Trim().Replace(" ", "").Replace("-", "");

            if (cleaned.StartsWith("07")) // UK
            {
                cleaned = "+44" + cleaned.Substring(1);
            }
            else if (cleaned.StartsWith("09")) // VN
            {
                cleaned = "+84" + cleaned.Substring(1);
            }
            else if (cleaned.StartsWith("08")) // Ireland
            {
                cleaned = "+353" + cleaned.Substring(1);
            }

            return cleaned;
        }

        public static string DenormalizePhone(string normalizedPhone)
        {
            if (string.IsNullOrWhiteSpace(normalizedPhone))
                return normalizedPhone;

            var cleaned = normalizedPhone.Trim().Replace(" ", "").Replace("-", "");

            if (cleaned.StartsWith("+44")) // UK
            {
                return "0" + cleaned.Substring(3);
            }
            else if (cleaned.StartsWith("+84")) // VN
            {
                return "0" + cleaned.Substring(3);
            }
            else if (cleaned.StartsWith("+353")) // Ireland
            {
                return "0" + cleaned.Substring(4);
            }

            return cleaned;
        }


        // Adds https:// to a URL if it doesn't already have http:// or https://
        public static string NormalizeUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return url;

            url = url.Trim();

            if (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                return url;

            return "https://" + url.TrimStart('/');
        }



        public static byte[] ComputePayloadHash(object payload)
        {
            using var sha = SHA256.Create();
            var json = JsonConvert.SerializeObject(payload);
            return sha.ComputeHash(Encoding.UTF8.GetBytes(json));
        }

        public static DateTime? CombineDateAndTime(DateTime? date, string time)
        {
            if (!date.HasValue || string.IsNullOrEmpty(time))
                return date;

            if (DateTime.TryParse($"{date.Value:yyyy-MM-dd} {time}", out var combinedDate))
            {
                return combinedDate;
            }

            return date;
        }
    }
}
