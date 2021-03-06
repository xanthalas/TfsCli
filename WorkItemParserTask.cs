﻿/* Copyright (c) 2016 xanthalas.co.uk
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

            this.Description = replaceSpecialCharacters(workItem.Fields["Description"].Value != null && workItem.Fields["Description"].Value.ToString().Length > 0 ? workItem.Fields["Description"].Value.ToString() : "No Description is present");
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
