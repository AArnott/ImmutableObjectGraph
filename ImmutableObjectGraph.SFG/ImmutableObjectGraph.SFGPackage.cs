namespace Microsoft.ImmutableObjectGraph_SFG
{
    using System;
    using System.Diagnostics;
    using System.Globalization;
    using System.Runtime.InteropServices;
    using System.ComponentModel.Design;
    using Microsoft.Win32;
    using Microsoft.VisualStudio;
    using Microsoft.VisualStudio.Shell.Interop;
    using Microsoft.VisualStudio.OLE.Interop;
    using Microsoft.VisualStudio.Shell;

    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    /// </summary>
    [PackageRegistration(UseManagedResourcesOnly = true)]
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)]
    [Guid(GuidList.guidImmutableObjectGraph_SFGPkgString)]
    public sealed class ImmutableObjectGraph_SFGPackage : Package
    {
        protected override void Initialize()
        {
            base.Initialize();

        }
    }
}
