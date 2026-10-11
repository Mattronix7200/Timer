using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace TimerSys.Core
{

    internal static class UpdateChecker
    {
        private const string TagsUrl = "https://api.github.com/repos/Mattronix7200/TimerSysApp/tags";

        private sealed class GitHubTag
        {
            [JsonProperty("name")] public string? Name { get; set; }
        }

        public static async Task<string?> LatestVersionAsync()
        {
            try
            {
                using var client = new System.Net.Http.HttpClient { Timeout = TimeSpan.FromSeconds(10) };
                client.DefaultRequestHeaders.Add("User-Agent", "TimerSys");

                var response = await client.GetAsync(TagsUrl).ConfigureAwait(false);
                string body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                var tags = JsonConvert.DeserializeObject<List<GitHubTag>>(body) ?? new List<GitHubTag>();

                var latest = tags
                    .Select(t => t.Name ?? "")
                    .Where(n => Regex.IsMatch(n, @"^release-\d+\.\d+\.\d+$"))
                    .Select(n => Version.TryParse(n.Replace("release-", ""), out var v) ? v : null)
                    .Where(v => v != null)
                    .OrderByDescending(v => v)
                    .FirstOrDefault();

                return latest?.ToString(3);
            }
            catch
            {

                return null;
            }
        }

        public static bool IsNewer(string? latest)
        {
            var current = Version.TryParse(AppEngine.AppVersion, out var cv) ? cv : new Version(0, 0, 0);
            return Version.TryParse(latest ?? "", out var lv) && lv > current;
        }
    }
}
