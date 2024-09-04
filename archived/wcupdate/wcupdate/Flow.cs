namespace wcupdate
{
    internal static class Flow
    {
        public static void Status(string statusMessage, string group = "", string details = "")
        {
            var s = "- ";

            if (!string.IsNullOrWhiteSpace(group))
            {
                s += $"[{group}] ";
            }

            s += statusMessage;

            if (!string.IsNullOrWhiteSpace(details))
            {
                s += $" (\"{details}\")";
            }

            Console.WriteLine(s);
        }

        public static void Exit(string errorMessage, int exitCode, Exception? e = null)
        {
            if (e != null)
            {
                Console.WriteLine();
                Console.WriteLine("An exception occurred:");
                Console.WriteLine($"- [Exception] Type -> {e.GetType()}");
                Console.WriteLine($"- [Exception] Message -> {e.Message}");
            }

            Console.WriteLine();
            Console.WriteLine($"Error: {errorMessage}");
            Console.WriteLine();
            Console.WriteLine(App.Help);

            Environment.Exit(exitCode);
        }
    }
}
