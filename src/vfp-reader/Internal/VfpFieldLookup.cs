using System;

namespace VfpReader.Internal
{
    /// <summary>
    /// The one case-insensitive field-name scan, shared by the internal <see cref="VfpHeader"/>
    /// and the public <see cref="VfpSchema"/> so both can never disagree.
    /// </summary>
    internal static class VfpFieldLookup
    {
        /// <summary>The index of a field by name, or -1. Case-insensitive.</summary>
        internal static int IndexOf(VfpField[] fields, string name)
        {
            return Array.FindIndex(
                fields,
                f => string.Equals(f.Name, name, StringComparison.OrdinalIgnoreCase));
        }
    }
}
