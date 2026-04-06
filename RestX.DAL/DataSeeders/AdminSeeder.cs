using System.Text.RegularExpressions;
using RestX.DAL.Context;

namespace RestX.DAL.DataSeeders
{
    public class AdminSeeder : BaseUserSeeder
    {
        private const string DEFAULT_PASSWORD = "Admin@123";
        private const string DEFAULT_USERNAME = "TenantAdmin";
        private const string DEFAULT_ROLE = "Admin";
        private readonly string _sanitizedHostname;
        public AdminSeeder(TenantDbContext context, string tenantHostname) : base(context)
        {
            _sanitizedHostname = SanitizeHostname(tenantHostname);
        }
        public override int Order => 4;
        protected override string SeederName => "AdminSeeder";
        protected override string Email => $"admin@{_sanitizedHostname}";
        protected override string Username => DEFAULT_USERNAME;
        protected override string Password => DEFAULT_PASSWORD;
        protected override string RoleName => DEFAULT_ROLE;
        private static string SanitizeHostname(string hostname)
        {
            if (string.IsNullOrWhiteSpace(hostname))
                return "tenant";
            var normalized = RemoveVietnameseDiacritics(hostname);
            var parts = normalized.Split('.', StringSplitOptions.RemoveEmptyEntries);

            var sanitizedParts = parts
                .Select(part =>
                {
                    var clean = Regex.Replace(part, @"[^a-zA-Z0-9-]", "").ToLower();
                    return string.IsNullOrEmpty(clean) ? "tenant" : clean;
                });

            return string.Join(".", sanitizedParts);
        }
        private static string RemoveVietnameseDiacritics(string text)
        {
            var vietnameseChars = new Dictionary<char, char>
            {
                {'á', 'a'}, {'à', 'a'}, {'ả', 'a'}, {'ã', 'a'}, {'ạ', 'a'},
                {'ă', 'a'}, {'ắ', 'a'}, {'ằ', 'a'}, {'ẳ', 'a'}, {'ẵ', 'a'}, {'ặ', 'a'},
                {'â', 'a'}, {'ấ', 'a'}, {'ầ', 'a'}, {'ẩ', 'a'}, {'ẫ', 'a'}, {'ậ', 'a'},
                {'Á', 'A'}, {'À', 'A'}, {'Ả', 'A'}, {'Ã', 'A'}, {'Ạ', 'A'},
                {'Ă', 'A'}, {'Ắ', 'A'}, {'Ằ', 'A'}, {'Ẳ', 'A'}, {'Ẵ', 'A'}, {'Ặ', 'A'},
                {'Â', 'A'}, {'Ấ', 'A'}, {'Ầ', 'A'}, {'Ẩ', 'A'}, {'Ẫ', 'A'}, {'Ậ', 'A'},

                {'é', 'e'}, {'è', 'e'}, {'ẻ', 'e'}, {'ẽ', 'e'}, {'ẹ', 'e'},
                {'ê', 'e'}, {'ế', 'e'}, {'ề', 'e'}, {'ể', 'e'}, {'ễ', 'e'}, {'ệ', 'e'},
                {'É', 'E'}, {'È', 'E'}, {'Ẻ', 'E'}, {'Ẽ', 'E'}, {'Ẹ', 'E'},
                {'Ê', 'E'}, {'Ế', 'E'}, {'Ề', 'E'}, {'Ể', 'E'}, {'Ễ', 'E'}, {'Ệ', 'E'},

                {'í', 'i'}, {'ì', 'i'}, {'ỉ', 'i'}, {'ĩ', 'i'}, {'ị', 'i'},
                {'Í', 'I'}, {'Ì', 'I'}, {'Ỉ', 'I'}, {'Ĩ', 'I'}, {'Ị', 'I'},

                {'ó', 'o'}, {'ò', 'o'}, {'ỏ', 'o'}, {'õ', 'o'}, {'ọ', 'o'},
                {'ô', 'o'}, {'ố', 'o'}, {'ồ', 'o'}, {'ổ', 'o'}, {'ỗ', 'o'}, {'ộ', 'o'},
                {'ơ', 'o'}, {'ớ', 'o'}, {'ờ', 'o'}, {'ở', 'o'}, {'ỡ', 'o'}, {'ợ', 'o'},
                {'Ó', 'O'}, {'Ò', 'O'}, {'Ỏ', 'O'}, {'Õ', 'O'}, {'Ọ', 'O'},
                {'Ô', 'O'}, {'Ố', 'O'}, {'Ồ', 'O'}, {'Ổ', 'O'}, {'Ỗ', 'O'}, {'Ộ', 'O'},
                {'Ơ', 'O'}, {'Ớ', 'O'}, {'Ờ', 'O'}, {'Ở', 'O'}, {'Ỡ', 'O'}, {'Ợ', 'O'},

                {'ú', 'u'}, {'ù', 'u'}, {'ủ', 'u'}, {'ũ', 'u'}, {'ụ', 'u'},
                {'ư', 'u'}, {'ứ', 'u'}, {'ừ', 'u'}, {'ử', 'u'}, {'ữ', 'u'}, {'ự', 'u'},
                {'Ú', 'U'}, {'Ù', 'U'}, {'Ủ', 'U'}, {'Ũ', 'U'}, {'Ụ', 'U'},
                {'Ư', 'U'}, {'Ứ', 'U'}, {'Ừ', 'U'}, {'Ử', 'U'}, {'Ữ', 'U'}, {'Ự', 'U'},

                {'ý', 'y'}, {'ỳ', 'y'}, {'ỷ', 'y'}, {'ỹ', 'y'}, {'ỵ', 'y'},
                {'Ý', 'Y'}, {'Ỳ', 'Y'}, {'Ỷ', 'Y'}, {'Ỹ', 'Y'}, {'Ỵ', 'Y'},

                {'đ', 'd'}, {'Đ', 'D'}
            };

            var result = new char[text.Length];
            for (int i = 0; i < text.Length; i++)
            {
                result[i] = vietnameseChars.TryGetValue(text[i], out var replacement)
                    ? replacement
                    : text[i];
            }
            return new string(result);
        }
    }
}
