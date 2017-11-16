using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LibGit2Sharp;
using System.IO;
using System.Text.RegularExpressions;

namespace VSGitDiff
{
    class Git2Sharp : IGitProvider
    {
        /// <summary>
        /// Returns a unified diff between the working state and last commit of the provided file
        /// </summary>
        /// <param name="filePath">Path to a valid file.</param>
        /// <returns>Unified diff.</returns>
        public string Diff(string filePath)
        {
            string unifiedDiff = "";
            string repoPath = Repository.Discover(filePath);
            using (var repo = new Repository(repoPath))
            {
                var diff = repo.Diff.Compare<Patch>(new[] { filePath });
                
                unifiedDiff = diff.Content;
            }
            return unifiedDiff;
        }

        public string Diff(string filePath, string stringToCompare)
        {
            string unifiedDiff = "";
            
            string repoPath = Repository.Discover(filePath);
            string pathPrefix = "";
            Regex regex = new Regex(@"(.*)\.git\\$");
            Match match = regex.Match(repoPath);

            if (match.Success)
                pathPrefix = match.Groups[1].Value;
            else
                pathPrefix = repoPath;

            filePath = filePath.Replace(pathPrefix, "");

            if (match.Success)

            using (var repo = new Repository(repoPath))
            {
                var tip = repo.Head.Tip;
                var commit = repo.Lookup<Commit>(tip.ToString());
                
                var treeEntry = commit.Tree[filePath];
                var headBlob = (Blob)treeEntry.Target;

                var contentBytes = System.Text.Encoding.UTF8.GetBytes(stringToCompare);
                var ms = new MemoryStream(contentBytes);
                var newBlob = repo.ObjectDatabase.CreateBlob(ms);

                var diff = repo.Diff.Compare(headBlob, newBlob);

                unifiedDiff = diff.Patch;
            }
            return unifiedDiff;
        }
    }
}
