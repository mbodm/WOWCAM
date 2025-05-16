namespace WOWCAM.Core.Parts.Config
{
    [Serializable]
    public class ConfigValidationException : Exception
    {
        public ConfigValidationException() { }
        public ConfigValidationException(string message) : base(message) { }
        public ConfigValidationException(string message, Exception inner) : base(message, inner) { }
    }
}
