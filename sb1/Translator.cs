namespace sbx
{
    internal static class Translator
    {
        /// <summary>
        /// Get a localized string by key
        /// </summary>
        /// <param name="key">Key to look for. Can be any valid string.</param>
        /// <returns></returns>
        internal static string Translate(string key)
        {
            return App.Current?.FindResource(key) as string ?? "NULL";
        }

        /// <summary>
        /// Get a localized, formatted string by key. Uses <see cref="string.Format(string, object?[])"/> to format the result.
        /// </summary>
        /// <param name="key">Key to look for. Can be any valid string.</param>
        /// <param name="args">Formatting arguments</param>
        /// <returns></returns>
        internal static string Translate(string key, params object[] args)
        {
            return string.Format(Translate(key), args);
        }
    }
}
