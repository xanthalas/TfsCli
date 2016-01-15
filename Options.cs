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
using CommandLine;

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

        [Option('y', "history", DefaultValue = false, HelpText = "Show history")]
        public bool ShowHistory { get; set; }

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
