namespace JiraAggregator
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// The issue report.
    /// </summary>
    public class IssueReport
    {
        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="IssueReport"/> class.
        /// </summary>
        public IssueReport()
        {
            this.Comments = new List<string>();
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets or sets the comments.
        /// </summary>
        public List<string> Comments { get; set; }

        /// <summary>
        /// Gets or sets the date.
        /// </summary>
        public DateTime Date { get; set; }

        /// <summary>
        /// Gets or sets the key.
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// Gets or sets the status.
        /// </summary>
        public IssueReportStatus Status { get; set; }

        /// <summary>
        /// Gets or sets the summary.
        /// </summary>
        public string Summary { get; set; }

        #endregion
    }
}