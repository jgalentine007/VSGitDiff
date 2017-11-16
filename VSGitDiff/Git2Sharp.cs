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
        /// Returns a unified diff between the saved state in the index and last commit of the provided absolute file path.
        /// </summary>
        /// <param name="filePath">Path to a valid file.</param>
        /// <returns>Unified diff.</returns>
        public string Diff(string filePath)
        {
            string repoPath = Repository.Discover(filePath);

            using (var repo = new Repository(repoPath))
            {
                var diff = repo.Diff.Compare<Patch>(new[] { filePath });
                return diff.Content;
            }
        }

        /// <summary>
        /// Returns a unified diff between the provided string and last commit of the provided absolute file path.
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="stringToCompare"></param>
        /// <returns></returns>
        public string Diff(string filePath, string stringToCompare)
        {
            // convert crlf to lf
            stringToCompare = stringToCompare.Replace("\r\n", "\n");

            // get file path relative to repository path
            string repoPath = RepoBasePathFromFilePath(filePath);
            filePath = filePath.Replace(repoPath, "");

            using (var repo = new Repository(repoPath))
            {
                // extract file content from repo and convert to blob to normalize encodings etc.
                string content = FileContentFromRepoHead(filePath, repo);
                Blob dbBlob = GitBlobFromString(content);
                Blob newBlob = GitBlobFromString(stringToCompare);

                var diff = repo.Diff.Compare(dbBlob, newBlob);
                return diff.Patch;
            }
        }

        /// <summary>
        /// Return path to repository sans .git directory.
        /// </summary>
        /// <param name="repoPath">Repository path</param>
        /// <returns></returns>
        private string RepoBasePathFromFilePath(string filePath)
        {
            string repoPath = Repository.Discover(filePath);
            Regex regex = new Regex(@"(.*)\.git\\$");
            Match match = regex.Match(repoPath);

            string pathPrefix;

            if (match.Success)
                pathPrefix = match.Groups[1].Value;
            else
                pathPrefix = repoPath;

            return pathPrefix;
        }

        /// <summary>
        /// Get the most recent file content from provided repository and file path.
        /// </summary>
        /// <param name="filePath">File path (relative to repository.)</param>
        /// <param name="repo">Git repository.</param>
        /// <returns></returns>
        private string FileContentFromRepoHead(string filePath, Repository repo)
        {
            var tip = repo.Head.Tip;
            var treeEntry = tip.Tree[filePath];
            var headBlob = (Blob)treeEntry.Target;
            var contentStream = headBlob.GetContentStream();

            using (var sr = new StreamReader(contentStream, Encoding.UTF8))
            {
                string content = sr.ReadToEnd();
                return content;
            }
        }

        /// <summary>
        /// Creates a UTF8 encoded Git database object blob from a content string.
        /// </summary>
        /// <param name="content">Content to encode.</param>
        /// <returns></returns>
        private Blob GitBlobFromString(string content)
        {
            using (var repo = new Repository())
            {
                var contentBytes = System.Text.Encoding.UTF8.GetBytes(content);
                var memStream = new MemoryStream(contentBytes);
                var blob = repo.ObjectDatabase.CreateBlob(memStream);
                return blob;
            }
        }
    }
}
