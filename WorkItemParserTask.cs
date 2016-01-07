using System;
using Microsoft.TeamFoundation.WorkItemTracking.Client;

namespace TfsCli
{
    class WorkItemParserTask : WorkItemParser
    {
        public WorkItemParserTask(WorkItem workItem)
            : base(workItem)
        {
            if (workItem.Type.Name != "Task")
            {
                throw new ArgumentException("Expected WorkItem of type \"Task\" but was given one of type " + workItem.Type.ToString());
            }

            this.Description = replaceSpecialCharacters(workItem.Fields["Description HTML"].Value != null && workItem.Fields["Description HTML"].Value.ToString().Length > 0 ? workItem.Fields["Description HTML"].Value.ToString() : "No Description is present");
            this.Description = stripOutHtmlTags(this.Description);

            this.AcceptanceCriteria = "No Acceptance Criteria is present";
            populateDetailedList(workItem);
        }

        protected void populateDetailedList(WorkItem workItem)
        {
            DetailedList.Clear();

            foreach (Field field in workItem.Fields)
            {
                if (field.Name != "Description HTML")
                {
                    Item item = new Item();
                    item.Name = field.Name;
                    item.Value = (field.Value == null ? string.Empty : field.Value.ToString());
                    DetailedList.Add(item);
                }
            }
        }
    }
}
