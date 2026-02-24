#if !NET9_0_OR_GREATER
#pragma warning disable IDE0130
namespace System.Diagnostics
#pragma warning restore IDE0130
{
  internal class UnreachableException : Exception
  {
  }
}
#endif //!NET9_0_OR_GREATER