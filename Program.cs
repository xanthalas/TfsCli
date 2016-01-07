using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommandLine;
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
        static int maxOutputLength = 400;
        static ConsoleColor summaryColour = ConsoleColor.DarkRed;
        static ConsoleColor descriptionColour = ConsoleColor.DarkRed;
        static ConsoleColor acceptanceColour = ConsoleColor.DarkRed;
        static ConsoleColor tasksColour = ConsoleColor.DarkRed;
        static ConsoleColor linksColour = ConsoleColor.DarkRed;

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

            connectToTfs();

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
                        break;

                    case "Bug":
                        writeOutDescriptionAndAcceptanceCriteria(new WorkItemParserBug(wi));
                        if (options.ShowTasks)
                        {
                            getTasks(wi);
                        }
                        break;

                    case "Task":
                        writeOutTaskDetails(new WorkItemParserTask(wi));
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
                maxOutputLength = System.Convert.ToInt32(getLine(sw));
                
                summaryColour = (ConsoleColor)Enum.Parse(typeof(ConsoleColor), getLine(sw));
                descriptionColour = (ConsoleColor)Enum.Parse(typeof(ConsoleColor), getLine(sw));
                acceptanceColour = (ConsoleColor)Enum.Parse(typeof(ConsoleColor), getLine(sw));
                tasksColour = (ConsoleColor)Enum.Parse(typeof(ConsoleColor), getLine(sw));
                linksColour = (ConsoleColor)Enum.Parse(typeof(ConsoleColor), getLine(sw));
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
