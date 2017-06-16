//------------------------------------------------------------------------------
// <copyright file="GitDiff.cs" company="Company">
//     Copyright (c) Company.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.ComponentModel.Design;
using System.Globalization;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using EnvDTE;
using System.Collections.Generic;

namespace VSGitDiff
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class GitDiff
    {
        /// <summary>
        /// Static DTE object.
        /// </summary>
        private static EnvDTE80.DTE2 dte;        

        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 0x0100;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("be3bf61c-3d52-4384-8f96-388ecbfe929e");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly Package package;

        /// <summary>
        /// Initializes a new instance of the <see cref="GitDiff"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        private GitDiff(Package package)
        {           
            if (package == null)
            {
                throw new ArgumentNullException("package");
            }

            this.package = package;

            OleMenuCommandService commandService = this.ServiceProvider.GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (commandService != null)
            {
                var menuCommandID = new CommandID(CommandSet, CommandId);
                var menuItem = new OleMenuCommand(this.MenuItemCallback, menuCommandID);
                menuItem.BeforeQueryStatus += new EventHandler(MenuItemCallbackBeforeQuery);
                commandService.AddCommand(menuItem);
            }

            dte = GetDTE2();            
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static GitDiff Instance
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        private IServiceProvider ServiceProvider
        {
            get
            {
                return this.package;
            }
        }

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static void Initialize(Package package)
        {
            Instance = new GitDiff(package);
        }

        void MenuItemCallbackBeforeQuery(object sender, EventArgs e)
        {
            OleMenuCommand myCommand = (OleMenuCommand)sender;
            myCommand.Visible = true;
        }

        /// <summary>
        /// This function is the callback used to execute the command when the menu item is clicked.
        /// See the constructor to see how the menu item is associated with this function using
        /// OleMenuCommandService service and MenuCommand class.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        private void MenuItemCallback(object sender, EventArgs e)
        {
            string unifiedDiff = "";
            var git = new Git2Sharp();

            // Get unified diff(s)
            var paths = SelectedItemFilePaths(dte);
            foreach (string path in paths)
            {                
                if(paths.Count > 1)
                    unifiedDiff += Environment.NewLine + Environment.NewLine;
                unifiedDiff += git.Diff(path);
            }            
            
            // Create a new Visual Studio document containing the unified diff(s)
            dte.ItemOperations.NewFile(@"General\Text File", "unified.diff");
            Document doc = dte.ActiveDocument;
            TextDocument textDoc = (TextDocument)doc.Object();
            var editPoint = textDoc.CreateEditPoint();
            editPoint.Insert(unifiedDiff);

            // Set the document as 'saved' even though it is not, to allow easy closure.
            doc.Saved = true;
        }

        /// <summary>
        /// Returns a static DTE2 debugging object.
        /// </summary>
        /// <returns>DTE2 debugging object.</returns>
        private static EnvDTE80.DTE2 GetDTE2()
        {
            return Package.GetGlobalService(typeof(DTE)) as EnvDTE80.DTE2;
        }

        /// <summary>
        /// Returns the file paths of selected items in the Visual Studio solution explorer.
        /// </summary>
        /// <returns>List of file paths.</returns>
        private List<string> SelectedItemFilePaths(EnvDTE80.DTE2 dte)
        {
            List<string> paths = new List<string>();            
            
            if (dte != null)
            {
                UIHierarchy solution = dte.ToolWindows.SolutionExplorer;
                Array selectedItems = (Array)solution.SelectedItems;

                if (selectedItems != null)
                {
                    foreach (UIHierarchyItem item in selectedItems)
                    {
                        ProjectItem projectItem = item.Object as ProjectItem;
                        paths.Add(projectItem.Properties.Item("FullPath").Value.ToString());
                    }
                }
            }

            return paths;
        }
    }
}
