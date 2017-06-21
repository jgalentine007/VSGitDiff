# VSGitDiff
Visual Studio extension that displays a Git unified diff for the selected file.

## Requirements ##
Visual Studio 2015 or later  
Microsoft Git Source Control Provider

## Usage ##
A new 'GitDiff' command will be added to the solution explorer context menu. You may highlight one or more files in the solution explorer window. The 'GitDiff' command will display a unified diff (Unix style) for each file in a new Visual Studio document. The unified diff consists of the changes between a saved file and the last commit. The Microsoft Git Source Control Provider must be selected as the current provider, and the file(s) to be diffed must be under source control.
