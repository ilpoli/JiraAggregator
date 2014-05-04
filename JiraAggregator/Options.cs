namespace JiraAggregator
{
    using CommandLine;
    using CommandLine.Text;

    /// <summary>
    /// The options.
    /// </summary>
    public class Options
    {
        #region Public Properties

        /// <summary>
        /// Gets or sets the password.
        /// </summary>
        [Option('p', "password", Required = true, HelpText = "Jira server password.")]
        public string Password { get; set; }

        /// <summary>
        /// Gets or sets the server URL.
        /// </summary>
        [Option('s', "serverurl", Required = true, HelpText = "Jira server url, for example https://example.atalassian.net. Please note that the protocol needs to be https.")]
        public string ServerUrl { get; set; }

        /// <summary>
        /// Gets or sets the user.
        /// </summary>
        [Option('u', "user", Required = true, HelpText = "Jira server user name.")]
        public string User { get; set; }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Gets the usage.
        /// </summary>
        /// <returns>
        /// A string.
        /// </returns>
        [HelpOption]
        public string GetUsage()
        {
            return HelpText.AutoBuild(this, current => HelpText.DefaultParsingErrorsHandler(this, current));
        }

        #endregion
    }
}