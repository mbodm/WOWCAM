namespace WOWCAM.WebView
{
    public static class LogHelper
    {
        public static IEnumerable<string> CreateLines(string name, object? sender, object? e, IEnumerable<string> details)
        {
            var lines = new string[]
            {
                $"{name} (event)",
                $"{nameof(sender)} = {sender}",
                $"{nameof(e)} = {e}"
            };

            return lines.Concat(details);
        }
    }
}
