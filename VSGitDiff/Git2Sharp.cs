using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LibGit2Sharp;
using System.IO;
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
    }
}
