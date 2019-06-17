# VSGitDiff
Visual Studio extension that displays a Git unified diff for the selected file.

## Requirements ##
Visual Studio 2017 or later  
Microsoft Git Source Control Provider

## Usage ##
A new 'GitDiff' command will be added to the solution explorer context menu. You may highlight one or more files in the solution explorer window. The 'GitDiff' command will display a unified diff (Unix style) for each file in a new Visual Studio document. The unified diff consists of the changes between a file and the last commit. The Microsoft Git Source Control Provider must be selected as the current provider, and the file(s) to be diffed must be under source control.

New Features in v1.3:

* Add support for Visual Studio 2019
* Migrate to AsyncPackage to support asynchronous autoloading

New Features in v1.2:

* Add VSGitDiff buttons to team explorer changes window items
* Drop support for Visual Studio 2015

New features in v1.1:

* Allow diffing of unsaved 'working' changes to HEAD when selecting a single file
* Add VSGitDiff buttons to code window source control context menu
* Couple of bug fixes and code improvements

Upcoming features for v1.4:

* Refactoring and additional error logging (there is none!)

## Notes ##
When performing a diff on unsaved 'working' changes it is assumed that your Git installation & repository is using auto.crlf = true, which is the default when using Visual Studio and Git on windows. In this case unsaved changes from your current document have their line endings converted to unix style so that they can be compared properly to the document in the Git repository database. Also, it is assumed that your .git folder is located inside your repository directory (which is the default.)