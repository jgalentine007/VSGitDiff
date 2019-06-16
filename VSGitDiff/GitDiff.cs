using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using Microsoft.TeamFoundation.Controls;
using Microsoft.TeamFoundation.Git.Controls.Extensibility;

namespace VSGitDiff
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal static class GitDiff
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
        /// ITeamExplorer Interface
        /// </summary>
        private static ITeamExplorer teamExplorer;

        public const int CmdID_SavedHeadSolution = 0x0100;
        public const int CmdID_WorkingHeadSolution = 0x0101;
        public const int CmdID_SavedHeadCodeWin = 0x0102;
        public const int CmdID_WorkingHeadCodeWin = 0x0103;
        public const int CmdID_SavedHeadSourceChanges = 0x0104;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("be3bf61c-3d52-4384-8f96-388ecbfe929e");

        /// <summary>
        /// Initializes static services of the command.
        /// Adds our command handlers for menu (commands must exist in the command table file).
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static async System.Threading.Tasks.Task InitializeAsync(AsyncPackage package)
        {
            await package.JoinableTaskFactory.SwitchToMainThreadAsync();

            scciProvider = await package.GetServiceAsync(typeof(IVsRegisterScciProvider)) as IVsGetScciProviderInterface;

            var commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (commandService != null)
            {
                AddMenuCommand(commandService, CmdID_SavedHeadSolution, SavedHeadSolutionCallback, SavedHeadSolutionQuery);
                AddMenuCommand(commandService, CmdID_WorkingHeadSolution, WorkingHeadSolutionCallback, WorkingHeadSolutionQuery);
                AddMenuCommand(commandService, CmdID_SavedHeadCodeWin, SavedHeadCodeWinCallback, SavedHeadCodeWinQuery);
                AddMenuCommand(commandService, CmdID_WorkingHeadCodeWin, WorkingHeadCodeWinCallback, WorkingHeadCodeWinQuery);
                AddMenuCommand(commandService, CmdID_SavedHeadSourceChanges, SavedHeadSolutionCallback, null);
            }

            teamExplorer = await package.GetServiceAsync(typeof(ITeamExplorer)) as ITeamExplorer;

            dte = Package.GetGlobalService(typeof(DTE)) as EnvDTE80.DTE2;
        }

        /// <summary>
        /// Adds a menu command for a given command ID, invocation handler, and status request handler.
        /// </summary>
        /// <param name="commandService">The service used to add handlers for menu commands.</param>
        /// <param name="commandID">The numeric command ID.</param>
        /// <param name="invokeHandler">The event handler called to execute the command.</param>
        /// <param name="beforeQueryStatusHandler">The event handler called when a client requests the command status.</param>
        private static void AddMenuCommand(OleMenuCommandService commandService, int commandID, EventHandler invokeHandler, EventHandler beforeQueryStatusHandler)
        {
            var menuCommandID = new CommandID(CommandSet, commandID);
            var menuItem = new OleMenuCommand(invokeHandler, null, beforeQueryStatusHandler, menuCommandID);
            commandService.AddCommand(menuItem);
        }

        /// <summary>
        /// Event handler called before saved head command is loaded / displayed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void SavedHeadSolutionQuery(object sender, EventArgs e)
        {
            var myCommand = (OleMenuCommand)sender;

            if (GitIsSCC())
                myCommand.Visible = true;
            else
                myCommand.Visible = false;
        }

        /// <summary>
        /// Event handler called before working head command is loaded / displayed 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void WorkingHeadSolutionQuery(object sender, EventArgs e)
        {
            var myCommand = (OleMenuCommand)sender;
            var selected = dte.SelectedItems.Item(1).ProjectItem;
            int selectedCount = dte.SelectedItems.Count;

            if (selectedCount == 1 && GitIsSCC() && selected.IsOpen && !selected.Saved)
                myCommand.Visible = true;
            else
                myCommand.Visible = false;
        }

        /// <summary>
        /// Event handler called before saved head command is loaded / displayed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void SavedHeadCodeWinQuery(object sender, EventArgs e)
        {
            var myCommand = (OleMenuCommand)sender;

            if (GitIsSCC())
            {
                bool underSCC = dte.SourceControl.IsItemUnderSCC(dte.ActiveDocument.Path);

                if (underSCC)
                    myCommand.Visible = true;
                else
                    myCommand.Visible = false;
            }
            else
                myCommand.Visible = false;
        }

        /// <summary>
        /// Event handler called before saved head command is loaded / displayed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void WorkingHeadCodeWinQuery(object sender, EventArgs e)
        {
            var myCommand = (OleMenuCommand)sender;

            if (GitIsSCC())
            {
                bool underSCC = dte.SourceControl.IsItemUnderSCC(dte.ActiveDocument.Path);
                bool saved = dte.ActiveDocument.Saved;

                if (underSCC && !saved)
                    myCommand.Visible = true;
                else
                    myCommand.Visible = false;
            }
            else
                myCommand.Visible = false; ;
        }

        /// <summary>
        /// Creates a unified diff comparing the selected saved file to itself in the HEAD commit.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        private static void SavedHeadSolutionCallback(object sender, EventArgs e)
        {
            try
            {
                var myCommand = (OleMenuCommand)sender;
                string unifiedDiff = "";
                var git = new Git2Sharp();

                // Get unified diff(s)
                List<string> paths = new List<string>();

                if (myCommand.CommandID.ID == CmdID_SavedHeadSourceChanges)
                    paths = SelectedSCCFilePaths();
                else if (myCommand.CommandID.ID == CmdID_SavedHeadSolution)
                    paths = SelectedItemFilePaths(dte);

                // todo fix this ugly mess below
                int index = 0;
                foreach (string path in paths)
                {
                    if (index > 0)
                        unifiedDiff += Environment.NewLine + Environment.NewLine;

                    if (dte.SourceControl.IsItemUnderSCC(path))
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

                NewVSDiffDocument(unifiedDiff);
            }
            catch (Exception)
            {

            }
        }

        /// <summary>
        /// Creates a unified diff comparing the selected unsaved modified file to itself in the HEAD commit. 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void WorkingHeadSolutionCallback(object sender, EventArgs e)
        {
            try
            {
                string unifiedDiff = "";
                var git = new Git2Sharp();

                var selected = dte.SelectedItems.Item(1).ProjectItem;
                var path = selected.Properties.Item("FullPath").Value.ToString();

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

                    NewVSDiffDocument(unifiedDiff);
                }
            }
            catch (Exception)
            {

            }
        }

        /// <summary>
        /// Creates a unified diff comparing the selected saved file to itself in the HEAD commit.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        private static void SavedHeadCodeWinCallback(object sender, EventArgs e)
        {
            try
            {
                string unifiedDiff = "";
                var git = new Git2Sharp();

                var path = dte.ActiveDocument.Path + dte.ActiveDocument.Name;

                string diff = git.Diff(path);
                if (diff != "")
                    unifiedDiff += diff;
                else
                    unifiedDiff += $"{path} - no changes found.";

                NewVSDiffDocument(unifiedDiff);
            }
            catch (Exception)
            {

            }
        }

        /// <summary>
        /// Creates a unified diff comparing the selected saved file to itself in the HEAD commit.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        private static void WorkingHeadCodeWinCallback(object sender, EventArgs e)
        {
            try
            {
                string unifiedDiff = "";
                var git = new Git2Sharp();

                var path = dte.ActiveDocument.Path + dte.ActiveDocument.Name;
               
                var txtDoc = (TextDocument)dte.ActiveDocument.Object("TextDocument");
                var editPnt = txtDoc.StartPoint.CreateEditPoint();
                string docText = editPnt.GetText(txtDoc.EndPoint);

                string diff = git.Diff(path, docText);
                if (diff != "")
                    unifiedDiff += diff;
                else
                    unifiedDiff += $"{path} - no changes found.";

                NewVSDiffDocument(unifiedDiff);
            }
            catch (Exception)
            {

            }
        }

        /// <summary>
        /// Creates a new text document in the visual studio documents window.
        /// </summary>
        /// <param name="content"></param>
        private static void NewVSDiffDocument(string content)
        {
            // Create a new Visual Studio document containing the unified diff(s)
            dte.ItemOperations.NewFile(@"General\Text File", "unified.diff");
            Document doc = dte.ActiveDocument;
            TextDocument textDoc = (TextDocument)doc.Object();
            var editPoint = textDoc.CreateEditPoint();
            editPoint.Insert(content);

            // Set the document as 'saved' even though it is not, to allow easy closure.
            doc.Saved = true;
        }

        /// <summary>
        /// Check if Microsoft Git Provider is the selected source code control.
        /// </summary>
        /// <returns></returns>
        private static bool GitIsSCC()
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

            if (pGuid == new Guid("{11b8e6d7-c08b-4385-b321-321078cdd1f8}"))
                return true;
            else
                return false;
        }

        /// <summary>
        /// Returns the file paths of selected items in the Visual Studio solution explorer.
        /// </summary>
        /// <returns>List of file paths.</returns>
        private static List<string> SelectedItemFilePaths(EnvDTE80.DTE2 dte)
        {
            List<string> paths = new List<string>();

            if (dte == null)
                return paths;

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

            return paths;
        }

        /// <summary>
        /// Returns the file paths of the selected files in the Team Explorer - Changes window.
        /// </summary>
        /// <returns></returns>
        private static List<string> SelectedSCCFilePaths()
        {
            List<string> paths = new List<string>();                                
            var teamExplorerPage = teamExplorer.CurrentPage;
            var changesExt = teamExplorerPage.GetExtensibilityService(typeof(IChangesExt3)) as IChangesExt3;
            var changes = changesExt.SelectedUnstagedChanges;

            foreach (IChangesPendingChangeItem change in changes)
            {
                paths.Add(change.SourceLocalItem);
            }
            
            return paths;
        }
    }
}
