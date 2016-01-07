using System;
using Microsoft.TeamFoundation.WorkItemTracking.Client;

namespace TfsCli
{
    class WorkItemParserBug : WorkItemParser
    {
        public WorkItemParserBug(WorkItem workItem)
            : base(workItem)
        {
            if (workItem.Type.Name != "Bug")
            {
                throw new ArgumentException("Expected WorkItem of type \"Bug\" but was given one of type " + workItem.Type.ToString());
            }

            this.Description = replaceSpecialCharacters(workItem.Fields["Repro Steps"].Value != null && workItem.Fields["Repro Steps"].Value.ToString().Length > 0 ? workItem.Fields["Repro Steps"].Value.ToString() : "No Description is present");
            this.Description = stripOutHtmlTags(this.Description);

            this.AcceptanceCriteria = replaceSpecialCharacters(workItem.Fields["Acceptance Criteria"].Value != null && workItem.Fields["Acceptance Criteria"].Value.ToString().Length > 0 ? workItem.Fields["Acceptance Criteria"].Value.ToString() : "No Acceptance Criteria is present");
            this.AcceptanceCriteria = stripOutHtmlTags(this.AcceptanceCriteria);

            populateDetailedList(workItem);
        }

        protected void populateDetailedList(WorkItem workItem)
        {
            DetailedList.Clear();

            foreach (Field field in workItem.Fields)
            {
                if (field.Name != "Repro Steps" && field.Name != "Acceptance Criteria")
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
