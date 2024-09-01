namespace wcupdate
{
    internal static class Error
    {
        public const string Step01 = "Given argument is not a valid command.";
        public const string Step02 = "This application was started with insufficient (non-administrative) rights.";
        public const string Step03 = "Could not determine update file location.";
        public const string Step04 = "Update file not exists.";
        public const string Step05 = "Could not determine target file location.";
        public const string Step06 = "Target file not exists.";
        public const string Step07 = "Could not found running target process.";
        public const string Step08 = "Could not kill running target process.";
        public const string Step09 = "Could not replace target file.";
        public const string Step10 = "Could not start target app.";
    }
}
