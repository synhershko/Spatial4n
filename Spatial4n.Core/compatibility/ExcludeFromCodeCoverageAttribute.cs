#if !FEATURE_EXCLUDEFROMCODECOVERAGEATTRIBUTE

namespace System.Diagnostics.CodeAnalysis
{
    /// <summary>
    /// Stub attribute, since .NET Framework 3.5 doesn't support it.
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Constructor | AttributeTargets.Event | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
    internal class ExcludeFromCodeCoverageAttribute : Attribute
    {
    }
}
#endif