/* Copyright (c) 2016 xanthalas.co.uk
 * 
 * Author: Xanthalas
 * Date  : January 2016
 * 
 *  This file is part of TfsCli.
 *
 *  TfsCli is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *
 *  TfsCli is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License
 *  along with TfsCli.  If not, see <http://www.gnu.org/licenses/>.
 */
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
