namespace WOWCAM.Update
{
    internal static class Messages
    {
        // Status
        public const string Status1 = "Fetched update folder from WOWCAM config";
        public const string Status2 = "Update file exists";
        public const string Status3 = "Target file exists";
        public const string Status4 = "Update file version is higher than target file version";
        public const string Status5 = "Target process running";
        public const string Status6 = "Target process killed";
        public const string Status7 = "Target file replaced";
        public const string Status8 = "Target application started";
        // Error
        public const string Error1 = "Could not determine update folder";
        public const string Error2 = "Update file not exists";
        public const string Error3 = "Target file not exists";
        public const string Error4 = "Update file is not newer than target file";
        public const string Error5 = "Could not found running target process";
        public const string Error6 = "Could not kill running target process";
        public const string Error7 = "Could not replace target file";
        public const string Error8 = "Could not start target application";
    }
}
