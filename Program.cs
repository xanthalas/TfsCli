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
 *
 *  Code to retrieve the history is based on the following article by Tarun Arora: 
 *    http://geekswithblogs.net/TarunArora/archive/2011/08/21/tfs-sdk-work-item-history-visualizer-using-tfs-api.aspx
 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Data;
using CommandLine.Text;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.WorkItemTracking.Client;

namespace TfsCli
{
    class Program
    {
        private const string INI_FILE = "TfsCli.ini";

        static Options options = new Options();
        static WorkItemStore workItemStore;

        static string tfsUrl = string.Empty;
        static string tfsProject = string.Empty;
        static string separator = "=================================================================================================================";
        static string descriptionSeparator = "-----  Description  ---------------------------------------------------------------------------------------------";
        static string AcceptanceSeparator = "-----  Acceptance Criteria  -------------------------------------------------------------------------------------";
        static string tasksSeparator = "-----  Tasks  ---------------------------------------------------------------------------------------------------";
        static string linksSeparator = "-----  Links  ---------------------------------------------------------------------------------------------------";
        static string historySeparator = "-----  History  -------------------------------------------------------------------------------------------------";
        static int maxOutputLength = 400;
        static ConsoleColor summaryColour = ConsoleColor.DarkRed;
        static ConsoleColor descriptionColour = ConsoleColor.DarkRed;
        static ConsoleColor acceptanceColour = ConsoleColor.DarkRed;
        static ConsoleColor tasksColour = ConsoleColor.DarkRed;
        static ConsoleColor linksColour = ConsoleColor.DarkRed;
        static ConsoleColor historyColour = ConsoleColor.DarkRed;

        static ConsoleColor defaultColour = Console.ForegroundColor;

        static void Main(string[] args)
        {

            if (!CommandLine.Parser.Default.ParseArguments(args, options))
            {
                Environment.Exit(CommandLine.Parser.DefaultExitCodeFail);
            }

            if (options.Help)
            {
                showHelp();
                return;
            }

            try
            {
                loadIniFile();
            }
            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("The ini file is invalid or cannot connect to TFS. Error {0}", e.Message);
                Console.ForegroundColor = defaultColour;
                Console.WriteLine("");
                Environment.Exit(2);
            }

            var tfsIds = extractTfsIds(args);

            if (tfsIds.Count == 0)
            {
               tfsIds = retrievePreviousTfsIds();
            }

            connectToTfs();

            saveCurrentTfsIds(String.Join(" ", tfsIds));

            writeOutput(tfsIds);

            Console.ForegroundColor = defaultColour;
            Console.WriteLine("");
            //Console.ReadLine();
        }

        /// <summary>
        /// Write out the data to the console
        /// </summary>
        /// <param name="tfsIds"></param>
        private static void writeOutput(List<int> tfsIds)
        {

            foreach (var id in tfsIds)
            {
                WorkItem wi;
                try
                {
                    wi = workItemStore.GetWorkItem(id);
                }
                catch (DeniedOrNotExistException)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("WorkItem {0} does not exist or you don't have sufficient access rights to view it", id);
                    continue;
                }
                Console.ForegroundColor = summaryColour;

                Console.WriteLine(separator);

                Console.WriteLine("ID        :" + wi.Id + "   (" + wi.Type.Name + ")");
                Console.WriteLine("Title     :" + wi.Title);
                Console.WriteLine("State     :" + wi.State);
                Console.WriteLine(string.Empty);
                Console.WriteLine("Area      :" + wi.AreaPath);
                Console.WriteLine("Iteration :" + wi.IterationPath);
                Console.WriteLine("Owner     :" + wi.Fields["Assigned To"].Value);

                switch (wi.Type.Name)
                {
                    case "Story":
                        writeOutDescriptionAndAcceptanceCriteria(new WorkItemParserStory(wi));
                        if (options.ShowTasks)
                        {
                            getTasks(wi);
                        }
                        if (options.ShowHistory)
                        {
                            generateHistory(wi);
                        }
                        break;

                    case "Bug":
                        writeOutDescriptionAndAcceptanceCriteria(new WorkItemParserBug(wi));
                        if (options.ShowTasks)
                        {
                            getTasks(wi);
                        }
                        if (options.ShowHistory)
                        {
                            generateHistory(wi);
                        }
                        break;

                    case "Task":
                        writeOutTaskDetails(new WorkItemParserTask(wi));

                        if (options.ShowHistory)
                        {
                            generateHistory(wi);
                        }
                        break;
                }

                if (options.ShowLinks)
                {
                    getLinks(wi);
                }

                Console.ForegroundColor = defaultColour;
                Console.WriteLine(separator);
            }
        }

        /// <summary>
        /// Extract all the TFS ids passed on the command line
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        private static List<int> extractTfsIds(string[] args)
        {
            List<int> numbers = new List<int>();

            foreach (var arg in args)
            {
                if (!arg.StartsWith("-"))
                {
                    try
                    {
                        var number = System.Convert.ToInt32(arg);

                        numbers.Add(number);

                    }
                    catch (Exception)
                    {
                        //Do nothing, we'll use -1 to indicate that the argument was not valid
                    } 
                }
            }

            return numbers;
        }

        private static void saveCurrentTfsIds(string ids)
        {
            var outputFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TfsCli.data");

            using (StreamWriter sw = new StreamWriter(outputFile))
            {
                sw.WriteLine(ids);
                sw.Close();
            }
        }

        private static List<int> retrievePreviousTfsIds()
        {
            var inputFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TfsCli.data");

            using (StreamReader reader = new StreamReader(inputFile))
            {
                try
                {
                    string line;

                    line = reader.ReadLine();           //Line 1 (Control name)

                    if (line != null)
                    {
                        string[] values = line.Split((string[]) null, StringSplitOptions.RemoveEmptyEntries);
                        return extractTfsIds(values);
                    }
                }
                catch (Exception)
                {
                    //Don't do anything if the read fails
                }
            }
            return new List<int>();
        }

        private static void connectToTfs()
        {
            try
            {
                // Connect to the server and the store, and get the WorkItemType object
                // for user stories from the team project where the user story will be created. 
                Uri collectionUri = new Uri(tfsUrl);

                TfsTeamProjectCollection tpc = new TfsTeamProjectCollection(collectionUri);
                workItemStore = tpc.GetService<WorkItemStore>();
                Project teamProject = workItemStore.Projects[tfsProject];
            }
            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("A problem occurred while connecting to TFS:");
                Console.WriteLine(e);
                Console.ForegroundColor = defaultColour;
                Console.WriteLine("");
                Environment.Exit(3);
            }

        }

        private static void writeOutDescriptionAndAcceptanceCriteria(WorkItemParser wip)
        {
            if (options.ShowSummary)
            {
                return;             //Drop out if the user only wants to see the summary
            }

            Console.ForegroundColor = descriptionColour;

            string desc = wip.Description;

            var maxLen = (desc.Length - 1 > maxOutputLength ? maxOutputLength : desc.Length);

            if (options.ShowAllText)
            {
                maxLen = desc.Length;
            }

            if (maxLen > 0)
            {
                Console.WriteLine(descriptionSeparator);
                Console.WriteLine(desc.Substring(0, maxLen));
            }
            else
            {
                Console.WriteLine("Description   :<empty>");
            }


            Console.ForegroundColor = acceptanceColour;

            string acceptance = wip.AcceptanceCriteria;

            maxLen = (acceptance.Length - 1 > maxOutputLength ? maxOutputLength : acceptance.Length);

            if (options.ShowAllText)
            {
                maxLen = acceptance.Length;
            }

            if (maxLen > 0)
            {
                Console.WriteLine(AcceptanceSeparator);
                Console.WriteLine(acceptance.Substring(0, maxLen));
            }
            else
            {
                Console.WriteLine("Accp Criteria :<empty>");
            }
        }

        private static void writeOutTaskDetails(WorkItemParser wip)
        {
            Console.WriteLine("Activity      :" + wip.WorkItem.Fields["Activity"].Value);
            Console.WriteLine("Remaining Work:" + wip.WorkItem.Fields["Remaining Work"].Value);
        }

        private static void getTasks(WorkItem parent)
        {
            Console.ForegroundColor = tasksColour;

            Console.WriteLine(tasksSeparator);

            var tasksCount = 0;

            for (int i = 0; i < parent.Links.Count; i++)
            {
                var child = parent.Links[i];

                if (child.BaseType != BaseLinkType.RelatedLink)
                {
                    continue;
                }

                var childAsRelatedLink = (RelatedLink)child;

                WorkItem childWorkItem;
                try
                {
                    childWorkItem = workItemStore.GetWorkItem(childAsRelatedLink.RelatedWorkItemId);
                }
                catch (DeniedOrNotExistException)
                {
                    //Console.WriteLine("WorkItem {0} does not exist or you don't have sufficient access rights to view it", child.Id);
                    continue;
                }

                if (childWorkItem.Type.Name == "Task")
                {
                    string assigned = childWorkItem.Fields["Assigned To"].Value.ToString();
                    assigned = assigned.PadRight(25);
                    string state = childWorkItem.State.PadRight(8);
                    Console.WriteLine("Task: {0}  [{1}]  [{2}]  {3}", childWorkItem.Id, state, assigned, childWorkItem.Title);
                    tasksCount++;
                }
            }

            if (tasksCount == 0)
            {
                Console.WriteLine("There are no tasks belonging to this work item.");
            }
        }

        private static void getLinks(WorkItem parent)
        {
            Console.ForegroundColor = linksColour;

            Console.WriteLine(linksSeparator);

            var linksCount = 0;

            for (int i = 0; i < parent.Links.Count; i++)
            {
                var child = parent.Links[i];

                if (child.BaseType != BaseLinkType.RelatedLink)
                {
                    continue;
                }

                var childAsRelatedLink = (RelatedLink)child;

                WorkItem childWorkItem;
                try
                {
                    childWorkItem = workItemStore.GetWorkItem(childAsRelatedLink.RelatedWorkItemId);
                }
                catch (DeniedOrNotExistException)
                {
                    //Console.WriteLine("WorkItem {0} does not exist or you don't have sufficient access rights to view it", child.Id);
                    continue;
                }

                if (childWorkItem.Type.Name != "Task")
                {
                    string state = childWorkItem.State.PadRight(8);
                    string itemType = childWorkItem.Type.Name.PadRight(6);
                    Console.WriteLine("{0}: {1}  [{2}] {3}",itemType , childWorkItem.Id, state, childWorkItem.Title);
                    linksCount++;
                }
            }

            if (linksCount == 0)
            {
                Console.WriteLine("There is nothing linked to this work item.");
            }
        }

        private static void loadIniFile()
        {
            using (StreamReader sw = new StreamReader(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, INI_FILE)))
            {
                tfsUrl = getLine(sw);
                tfsProject = getLine(sw);
                separator = getLine(sw);
                descriptionSeparator = getLine(sw);
                AcceptanceSeparator = getLine(sw);
                tasksSeparator = getLine(sw);
                linksSeparator = getLine(sw);
                historySeparator = getLine(sw);
                maxOutputLength = System.Convert.ToInt32(getLine(sw));
                
                summaryColour = (ConsoleColor)Enum.Parse(typeof(ConsoleColor), getLine(sw));
                descriptionColour = (ConsoleColor)Enum.Parse(typeof(ConsoleColor), getLine(sw));
                acceptanceColour = (ConsoleColor)Enum.Parse(typeof(ConsoleColor), getLine(sw));
                tasksColour = (ConsoleColor)Enum.Parse(typeof(ConsoleColor), getLine(sw));
                linksColour = (ConsoleColor)Enum.Parse(typeof(ConsoleColor), getLine(sw));
                historyColour = (ConsoleColor)Enum.Parse(typeof(ConsoleColor), getLine(sw));
            }
        }

        private static string getLine(StreamReader sw)
        {
            string line = sw.ReadLine();

            if (line == null)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Invalid ini file.");
                Console.ForegroundColor = defaultColour;
                Console.WriteLine("");
                Environment.Exit(1);
            }

            return line;
        }

        private static void generateHistory(WorkItem wi)
        {
            Console.ForegroundColor = historyColour;
            Console.WriteLine(historySeparator);

            var dataTable = new DataTable();

            foreach (Field field in wi.Fields)
            {
                dataTable.Columns.Add(field.Name);
            }

            // Loop through the work item revisions
            foreach (Revision revision in wi.Revisions)
            {
                // Get values for the work item fields for each revision
                var row = dataTable.NewRow();
                foreach (Field field in wi.Fields)
                {
                    row[field.Name] = revision.Fields[field.Name].Value;
                }
                dataTable.Rows.Add(row);
            }

            // List of fields to ignore in comparison
            var visualize = new List<string>() { "Title", "State", "Rev", "Reason", "Iteration Path", "Assigned To", "Effort", "Area Path" };


            //Debug.Write(String.Format("Work Item: {0}{1}", wi.Id, Environment.NewLine));

            List<string> output = new List<string>();

            // Compare Two Work Item Revisions 
            for (int i = 0; i < dataTable.Rows.Count; i++)
            {
                var currentRow = dataTable.Rows[i];

                if (i + 1 < dataTable.Rows.Count)
                {
                    var currentRowPlus1 = dataTable.Rows[i + 1];

                    //Debug.Write(String.Format("Comparing Revision {0} to {1} {2}", i, i + 1, Environment.NewLine));

                    bool title = false;

                    for (int j = 0; j < dataTable.Columns.Count; j++)
                    {
                        if (!title)
                        {
                            var outString = String.Format(String.Format("Changed By '{0}' On '{1}", currentRow["Changed By"].ToString(), currentRow["Changed Date"].ToString()));
                            flushPreviousToConsole(output);
                            output.Clear();
                            output.Add(outString);
                            title = true;
                        }

                        if (visualize.Contains(dataTable.Columns[j].ColumnName))
                        {
                            if (currentRow[j].ToString() != currentRowPlus1[j].ToString())
                            {
                                var outString = String.Format("[{0}]: '{1}' => '{2}'", dataTable.Columns[j].ColumnName, currentRow[j], currentRowPlus1[j]);

                                if (!outString.StartsWith("[Rev]"))
                                {
                                    output.Add(outString);
                                }
                            }
                        }

                    }
                }
            }
        }

        private static void flushPreviousToConsole(List<string> output)
        {
            if (output.Count < 2)               //If all we have is a "Changed By" then this is a revision only change so don't print anything
            {
                return;
            }

            foreach (var line in output)
            {
                Console.WriteLine(line);
            }
            if (output.Count > 0)
            {
                Console.WriteLine(" ");
            }

        }
        private static void showHelp()
        {
            HelpText ht = new HelpText("TfsCli version 0.1");
            ht.AddOptions(options);
            Console.WriteLine(ht.ToString());
            Console.WriteLine("");
            Console.WriteLine("Usage: tfscli 12345 <options>");
            Console.WriteLine("");
            Console.WriteLine("  where 12345 is the id of the TFS workitem you want to view");
        }
    }
}
