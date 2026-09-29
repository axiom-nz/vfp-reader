#if !NETFRAMEWORK
using System.Runtime.CompilerServices;
using System.Text;

namespace VfpReader.Tests
{
    /// <summary>
    /// Registers the code-page provider before any test decodes a header. The provider is only
    /// needed on .NET Core / .NET; .NET Framework resolves code pages natively, and the
    /// <see cref="ModuleInitializerAttribute"/> it relies on does not exist on .NET Framework.
    /// </summary>
    internal static class TestAssemblyInit
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }
    }
}
#endif
