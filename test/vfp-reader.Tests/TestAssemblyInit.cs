using System.Runtime.CompilerServices;
using System.Text;

namespace VfpReader.Tests
{
    /// <summary>Registers the code-page provider before any test decodes a header.</summary>
    internal static class TestAssemblyInit
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }
    }
}
