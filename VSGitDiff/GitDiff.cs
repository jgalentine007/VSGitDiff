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
using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell.Settings;

namespace VSGitDiff
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class GitDiff
    {
        /// <summary>
        /// Source Code Provider Interface
        /// </summary>
        private static IVsGetScciProviderInterface scciProvider;

        /// <summary>
        /// Static DTE object.
        /// </summary>
        private static EnvDTE80.DTE2 dte;        

        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CmdID_SavedHead = 0x0100;
        public const int CmdID_WorkingHead = 0x0101;

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
                var menuCommandID = new CommandID(CommandSet, CmdID_SavedHead);
                var menuItem = new OleMenuCommand(this.SavedHeadCallback, menuCommandID);
                menuItem.BeforeQueryStatus += new EventHandler(MenuItemCallbackBeforeQuery);
                commandService.AddCommand(menuItem);

                menuCommandID = new CommandID(CommandSet, CmdID_WorkingHead);
                menuItem = new OleMenuCommand(this.WorkingHeadCallback, menuCommandID);
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
        public static void Initialize(Package package, ref IVsGetScciProviderInterface _scciProvider)
        {
            scciProvider = _scciProvider;
            Instance = new GitDiff(package);
        }


        /// <summary>
        /// Event handler called before command is loaded / displayed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void MenuItemCallbackBeforeQuery(object sender, EventArgs e)
        {            
            // Check if current source code provider matches MS Git provider guid
            Guid pGuid;

            try
            {
                scciProvider.GetSourceControlProviderID(out pGuid);
            }
            catch
            {
                // fatal error occured retrieving provider ID, set guid to all zeros
                pGuid = new Guid();
            }

            OleMenuCommand myCommand = (OleMenuCommand)sender;

            if (pGuid == new Guid("{11b8e6d7-c08b-4385-b321-321078cdd1f8}"))
                myCommand.Visible = true;
            else
                myCommand.Visible = false;
        }

        /// <summary>
        /// This function is the callback used to execute the command when the menu item is clicked.
        /// See the constructor to see how the menu item is associated with this function using
        /// OleMenuCommandService service and MenuCommand class.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        private void SavedHeadCallback(object sender, EventArgs e)
        {
            string unifiedDiff = "";
            var git = new Git2Sharp();

            // Get unified diff(s)
            var paths = SelectedItemFilePaths(dte);
            // todo fix this ugly mess below
            int index = 0;
            foreach (string path in paths)
            {
                bool underSCC = dte.SourceControl.IsItemUnderSCC(path);

                if (index > 0)
                    unifiedDiff += Environment.NewLine + Environment.NewLine;

                if (underSCC)
                {
                    string diff = git.Diff(path);
                    if (diff != "")
                        unifiedDiff += diff;
                    else
                        unifiedDiff += $"{path} - no changes found.";
                }
                else
                    unifiedDiff += $"{path} - file is not under source control.";
                index++;
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

        private void WorkingHeadCallback(object sender, EventArgs e)
        {
            string unifiedDiff = "";
            var git = new Git2Sharp();

            var selected = dte.SelectedItems.Item(1).ProjectItem;

            // Get unified diff(s)
            var path = selected.Properties.Item("FullPath").Value.ToString();
            // todo fix this ugly mess below
            
            if (selected.IsOpen)
            {
                var txtDoc = (TextDocument)selected.Document.Object("TextDocument");
                var editPnt = txtDoc.StartPoint.CreateEditPoint();
                string docText = editPnt.GetText(txtDoc.EndPoint);

                if (dte.SourceControl.IsItemUnderSCC(path))
                {
                    string diff = git.Diff(path, docText);
                    if (diff != "")
                        unifiedDiff += diff;
                    else
                        unifiedDiff += $"{path} - no changes found.";
                }
                else
                    unifiedDiff += $"{path} - file is not under source control.";

                // Create a new Visual Studio document containing the unified diff(s)
                dte.ItemOperations.NewFile(@"General\Text File", "unified.diff");
                Document doc = dte.ActiveDocument;
                TextDocument textDoc = (TextDocument)doc.Object();
                var editPoint = textDoc.CreateEditPoint();
                editPoint.Insert(unifiedDiff);

                // Set the document as 'saved' even though it is not, to allow easy closure.
                doc.Saved = true;
            }
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
