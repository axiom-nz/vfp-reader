namespace VfpReader.Internal
{
    /// <summary>
    /// The DBF version bytes the reader knows, with the predicates that decide whether a version
    /// needs a memo file or carries a database backlink. Keeping the literals here stops them
    /// being re-tested in the header parser, the memo readers and the schema.
    /// </summary>
    internal static class VfpVersion
    {
        /// <summary>Visual FoxPro, no memo: dBASE/FoxPro-plus with the VFP header.</summary>
        internal const byte VisualFoxPro = 0x30;

        /// <summary>Visual FoxPro with autoincrement fields.</summary>
        internal const byte VisualFoxProAutoIncrement = 0x31;

        /// <summary>Visual FoxPro with <c>V</c> / <c>Q</c> varlength fields.</summary>
        internal const byte VisualFoxProVarLength = 0x32;

        /// <summary>dBASE III with memo (a <c>.dbt</c> in the dBASE III layout).</summary>
        internal const byte Dbase3WithMemo = 0x83;

        /// <summary>dBASE IV with memo (a <c>.dbt</c> in the dBASE IV layout).</summary>
        internal const byte Dbase4WithMemo = 0x8B;

        /// <summary>FoxPro 2.x with memo (an <c>.fpt</c>).</summary>
        internal const byte FoxProWithMemo = 0xF5;

        /// <summary>FoxBASE+ / dBASE IV with memo (an <c>.fpt</c>).</summary>
        internal const byte FoxBasePlusWithMemo = 0xFB;

        /// <summary>True for the Visual FoxPro versions, which carry a database backlink.</summary>
        internal static bool IsVisualFoxPro(byte version)
        {
            return version == VisualFoxPro
                || version == VisualFoxProAutoIncrement
                || version == VisualFoxProVarLength;
        }

        /// <summary>True when the version byte says a memo file is required.</summary>
        internal static bool HasMemo(byte version)
        {
            return version == Dbase3WithMemo
                || version == Dbase4WithMemo
                || version == FoxProWithMemo
                || version == FoxBasePlusWithMemo;
        }
    }
}
