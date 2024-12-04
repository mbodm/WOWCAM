using System.Text.Json;

namespace WOWCAM.Helper
{
    public sealed class CurseHelper
    {
        // See StackOverflow -> https://stackoverflow.com/questions/7134837/how-do-i-decode-a-base64-encoded-string
        public static string FetchJsonScript =>
            "btoa(unescape(encodeURIComponent(document.querySelector('script#__NEXT_DATA__')?.innerHTML ?? '')))";

        public static bool IsAddonPageUrl(string url)
        {
            // Example -> https://www.curseforge.com/wow/addons/deadly-boss-mods
            url = GuardAndNormalize(url);
            return url.StartsWith("https://www.curseforge.com/wow/addons/") && !url.EndsWith("/addons");
        }

        public static bool IsInitialDownloadUrl(string url)
        {
            // Example -> https://www.curseforge.com/api/v1/mods/3358/files/4485146/download
            url = GuardAndNormalize(url);
            return url.StartsWith("https://www.curseforge.com/api/v1/mods/") && url.Contains("/files/") && url.EndsWith("/download");
        }

        public static bool IsRealDownloadUrl(string url)
        {
            // Example -> https://mediafilez.forgecdn.net/files/4485/146/DBM-10.0.35.zip
            url = GuardAndNormalize(url);
            return url.StartsWith("https://mediafilez.forgecdn.net/files/") && url.EndsWith(".zip");
        }

        public static string GetAddonSlugNameFromAddonPageUrl(string url)
        {
            // Example -> https://www.curseforge.com/wow/addons/deadly-boss-mods
            url = GuardAndNormalize(url);
            return IsAddonPageUrl(url) ? url.Split("https://www.curseforge.com/wow/addons/").Last().ToLower() : string.Empty;
        }

        public static CurseAddonPageJson SerializeAddonPageJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                throw new ArgumentException($"'{nameof(json)}' cannot be null or whitespace.", nameof(json));
            }

            // Curse addon page JSON format:
            // props
            //   pageProps
            //     project
            //       id             --> Short number for download URL       Example --> 3358
            //       mainFile
            //         fileName     --> The name of the zip file            Example --> "DBM-10.0.35.zip"
            //         fileSize     --> The size of the zip file            Example --> 123456789
            //         id           --> Long number for download URL        Example --> 4485146
            //       name           --> Useful name of the addon            Example --> "Deadly Boss Mods (DBM)"
            //       slug           --> Slug name of the addon              Example --> "deadly-boss-mods"

            using var doc = JsonDocument.Parse(json);

            var project = doc.RootElement.GetProperty("props").GetProperty("pageProps").GetProperty("project");
            var projectId = project.GetProperty("id").GetUInt64();
            var projectName = project.GetProperty("name").GetString() ?? throw new InvalidOperationException("The 'name' entry was null, in fetched addon page JSON.");
            var projectSlug = project.GetProperty("slug").GetString() ?? throw new InvalidOperationException("The 'slug' entry was null, in fetched addon page JSON.");
            var mainFile = project.GetProperty("mainFile");
            var fileId = mainFile.GetProperty("id").GetUInt64();
            var fileName = mainFile.GetProperty("fileName").GetString() ?? throw new InvalidOperationException("The 'fileName' entry was null, in fetched addon page JSON.");
            var fileSize = mainFile.GetProperty("fileLength").GetUInt64();

            return new CurseAddonPageJson(projectId, projectName, projectSlug, fileId, fileName, fileSize);
        }

        // Example -> https://www.curseforge.com/api/v1/mods/3358/files/4485146/download
        public static string BuildInitialDownloadUrl(ulong projectId, ulong fileId) =>
            $"https://www.curseforge.com/api/v1/mods/{projectId}/files/{fileId}/download";

        private static string GuardAndNormalize(string url) =>
            url?.Trim().TrimEnd('/') ?? string.Empty;
    }
}
