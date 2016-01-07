using System.Text;
using System.Collections.ObjectModel;
using Microsoft.TeamFoundation.WorkItemTracking.Client;
using System.Text.RegularExpressions;

namespace TfsCli
{
    class WorkItemParser
    {
        public string WorkItemType { get; set; }

        public string Title { get; set; }

        public string State { get; set; }

        public string Mscw { get; set; }

        public string AssignedTo { get; set; }

        public string Description { get; set; }

        public string AcceptanceCriteria { get; set; }

        public ObservableCollection<Item> DetailedList { get; private set; }

        public WorkItemParser(WorkItem workItem)
        {
            this.WorkItem = workItem;

            DetailedList = new ObservableCollection<Item>();

            loadCommonData(workItem);
        }

        protected void loadCommonData(WorkItem workItem)
        {
            this.WorkItemType = workItem.Type.Name;
            this.Title = workItem.Title;
            this.State = workItem.State;
            this.AssignedTo = workItem.Fields["Assigned To"].Value.ToString();
        }

        protected string replaceSpecialCharacters(string input)
        {
            StringBuilder newString = new StringBuilder(input);
            newString.Replace("‘", "'");
            newString.Replace("’", "'");
            newString.Replace("”", "\"");
            return newString.ToString();
        }

        protected string stripOutHtmlTags(string inputString)
        {
            string tags_re = @"(?></?\w+)(?>(?:[^>'""]+|'[^']*'|""[^""]*"")*)>";

            var outputString = Regex.Replace(inputString, tags_re, "");
            outputString = outputString.Replace("&nbsp;", " ");
            outputString = outputString.Replace("&amp;", "&");
            outputString = outputString.Replace("&quot;", "\"");
            outputString = outputString.Replace("&apos;", "'");

            return outputString;
        }

        public WorkItem WorkItem
        {
            get; set;
        }
    }
}
