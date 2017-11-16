using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VSGitDiff
{
    interface IGitProvider
    {
        string Diff(string filePath);
        string Diff(string relativeFilePath, string stringToCompare);
    }
}
