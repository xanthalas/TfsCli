using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommandLine;
using CommandLine.Text;

namespace TfsCli
{
    public class Options
    {
        [Option('h', "help", Required = false, HelpText = "Show this help.")]
        public bool Help { get; set; }

        [Option('t', "tasks", DefaultValue = false, HelpText = "Show tasks for Stories and Bugs.")]
        public bool ShowTasks { get; set; }

        [Option('l', "links", DefaultValue = false, HelpText = "Show linked items")]
        public bool ShowLinks { get; set; }

        [Option('a', "all", DefaultValue = false, HelpText = "Show all text")]
        public bool ShowAllText { get; set; }

        [Option('s', "summary", DefaultValue = false, HelpText = "Show summary only")]
        public bool ShowSummary { get; set; }

        //[HelpOption]
        //public string GetUsage()
        //{
        //    return HelpText.AutoBuild(this,
        //      (HelpText current) => HelpText.DefaultParsingErrorsHandler(this, current));
        //}
    }
}
