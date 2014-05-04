namespace JiraAggregator
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading;

    using AnotherJiraRestClient;

    using CommandLine;

    /// <summary>
    /// The program.
    /// </summary>
    internal class Program
    {
        #region Methods

        /// <summary>
        /// Mains the specified arguments.
        /// </summary>
        /// <param name="args">
        /// The arguments.
        /// </param>
        internal static void Main(string[] args)
        {
            Options options = new Options();
            if (Parser.Default.ParseArguments(args, options))
            {
                JiraAccount jiraAccount = new JiraAccount
                                          {
                                              ServerUrl = options.ServerUrl, 
                                              User = options.User, 
                                              Password = options.Password
                                          };

                AggregateReports(jiraAccount);
            }
        }

        /// <summary>
        /// Aggregates the reports.
        /// </summary>
        /// <param name="jiraAccount">
        /// The jira account.
        /// </param>
        private static void AggregateReports(JiraAccount jiraAccount)
        {
            StringBuilder sb = new StringBuilder();
            List<IssueReport> issueReports = new List<IssueReport>();

            DateTime endDate = DateTime.Now;
            DateTime startDate = new DateTime(endDate.Year, endDate.Month, 1);

            JiraClient jiraClient = new JiraClient(jiraAccount);

            while (true)
            {
                List<IssueReport> buffer = new List<IssueReport>();

                while (startDate.DayOfWeek == DayOfWeek.Saturday || startDate.DayOfWeek == DayOfWeek.Sunday)
                {
                    startDate = startDate.AddDays(1);
                }

                if (startDate > endDate)
                {
                    break;
                }

                Console.WriteLine(startDate.ToString("M/d/yyyy dddd"));

                // Issues that I completed.
                List<Issue> issues = GetIssuesByJql(jiraClient, string.Format("assignee was currentUser() on \"{0}\" AND status was in (Resolved) on \"{0}\" by currentUser()", startDate.ToString("yyyy/MM/dd")), new[] { Issue.FIELD_SUMMARY });

                foreach (Issue issue in issues)
                {
                    IssueReport issueReport = new IssueReport
                                              {
                                                  Key = issue.key, 
                                                  Summary = issue.fields.summary.Trim(), 
                                                  Status = IssueReportStatus.Completed, 
                                                  Comments = new List<string>(), 
                                                  Date = startDate
                                              };

                    if (buffer.All(ir => ir.Key != issueReport.Key))
                    {
                        buffer.Add(issueReport);
                    }
                }

                // Issues that I worked.
                issues = GetIssuesByJql(jiraClient, string.Format("assignee was currentUser() on \"{0}\" AND status was in (\"In Progress\") on \"{0}\" by currentUser()", startDate.ToString("yyyy/MM/dd")), new[] { Issue.FIELD_SUMMARY });

                foreach (Issue issue in issues)
                {
                    IssueReport issueReport = new IssueReport
                                              {
                                                  Key = issue.key, 
                                                  Summary = issue.fields.summary.Trim(), 
                                                  Status = IssueReportStatus.Worked, 
                                                  Comments = new List<string>(), 
                                                  Date = startDate
                                              };
                    if (buffer.All(ir => ir.Key != issueReport.Key))
                    {
                        buffer.Add(issueReport);
                    }
                }

                // Issues that I code reviewed.
                issues = GetIssuesByJql(jiraClient, string.Format("assignee was currentUser() on \"{0}\" AND status was in (\"In Progress\") on \"{0}\"", startDate.ToString("yyyy/MM/dd")), new[] { Issue.FIELD_SUMMARY, Issue.FIELD_COMMENT });

                foreach (Issue issue in issues)
                {
                    List<Comment> comments = issue
                        .fields
                        .comment
                        .comments
                        .Where(c => c.author.name == jiraAccount.User
                                    && (DateTime.Parse(c.created) - startDate) < TimeSpan.FromDays(1))
                        .ToList();

                    if (!comments.Any())
                    {
                        continue;
                    }

                    bool codeReviewed = comments.Any(c => c.body.ToLower()
                                                              .Contains("look "));

                    IssueReport issueReport = new IssueReport
                                              {
                                                  Key = issue.key, 
                                                  Summary = issue.fields.summary.Trim(), 
                                                  Status =
                                                      codeReviewed
                                                          ? IssueReportStatus.CodeReview
                                                          : IssueReportStatus.Investigated, 
                                                  Comments = new List<string>(), 
                                                  Date = startDate
                                              };

                    if (buffer.All(ir => ir.Key != issueReport.Key))
                    {
                        buffer.Add(issueReport);
                    }
                }

                issueReports.AddRange(buffer);
                startDate = startDate.AddDays(1);
            }

            foreach (IGrouping<DateTime, IssueReport> issueReportGroup in issueReports.GroupBy(ir => ir.Date)
                .OrderBy(g => g.Key))
            {
                sb.AppendLine(string.Format("IntApp.Wilco. Communication\t2\tCommunications with the team, daily scrum, processed emails.\t{0}\t{0}", issueReportGroup.Key.ToString("M/d/yyyy")));
                foreach (IssueReport issueReport in issueReportGroup.OrderBy(x => x.Key))
                {
                    string status;
                    string effort = string.Empty;
                    switch (issueReport.Status)
                    {
                        case IssueReportStatus.None:
                            status = string.Empty;
                            break;
                        case IssueReportStatus.CodeReview:
                            status = "Code Review.";
                            effort = "0.5";
                            break;
                        case IssueReportStatus.Completed:
                            status = "Completed.";
                            break;
                        case IssueReportStatus.Worked:
                            status = "Worked.";
                            break;
                        case IssueReportStatus.Investigated:
                            status = "Investigated.";
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                    string summary = issueReport.Summary.Trim();
                    if (!summary.EndsWith("."))
                    {
                        summary += ".";
                    }

                    string description = string.Format("{0} {1} {2}", issueReport.Key, summary, status);

                    string projectTask = "IntApp.Wilco. Development";
                    if (issueReport.Status == IssueReportStatus.Investigated)
                    {
                        projectTask = "IntApp.Wilco. Investigation";
                    }

                    sb.AppendLine(
                        string.Format(
                            "{0}\t{1}\t{2}\t{3}\t{3}", 
                            projectTask, 
                            effort, 
                            description, 
                            issueReportGroup.Key.ToString("M/d/yyyy")));
                }
            }

            File.WriteAllText("report.txt", sb.ToString());
        }

        /// <summary>
        /// Gets the issues by JQL.
        /// </summary>
        /// <param name="jiraClient">
        /// The jira client.
        /// </param>
        /// <param name="jql">
        /// The JQL search string.
        /// </param>
        /// <param name="fields">
        /// The fields.
        /// </param>
        /// <returns>
        /// A list of issues.
        /// </returns>
        private static List<Issue> GetIssuesByJql(JiraClient jiraClient, string jql, string[] fields = null)
        {
            while (true)
            {
                try
                {
                    return jiraClient.GetIssuesByJql(
                        jql, 
                        0, 
                        int.MaxValue, 
                        fields)
                        .issues;
                }
                catch
                {
                    Thread.Sleep(5000);
                }
            }
        }

        #endregion
    }
}