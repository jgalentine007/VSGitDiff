using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace VSGitDiff
{
    [PackageRegistration(UseManagedResourcesOnly = true)]
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)] // Info on this package for Help/About
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [Guid(GitDiffPackage.PackageGuidString)]
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "pkgdef, VS and vsixmanifest are valid VS terms")]
    [ProvideAutoLoad(UIContextGuids80.SolutionExists)]
    public sealed class GitDiffPackage : Package
    {
        
        /// <summary>
        /// GitDiffPackage GUID string.
        /// </summary>
        public const string PackageGuidString = "6c7e4e31-deed-4c9f-bed3-1a8b9360b855";

        /// <summary>
        /// Initializes a new instance of the <see cref="GitDiff"/> class.
        /// </summary>
        public GitDiffPackage()
        {
            // Inside this method you can place any initialization code that does not require
            // any Visual Studio service because at this point the package object is created but
            // not sited yet inside Visual Studio environment. The place to do all the other
            // initialization is the Initialize method.
        }

        #region Package Members

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        protected override void Initialize()
        {
            // pass in SCC provider service
            try
            {
                var scciProvider = GetService(typeof(IVsRegisterScciProvider)) as IVsGetScciProviderInterface;
                GitDiff.Initialize(this, ref scciProvider);
                base.Initialize();
            }
            catch (Exception ex)
            {
                //todo error notification / logging
            }
        }
        
        #endregion        
    }
}

