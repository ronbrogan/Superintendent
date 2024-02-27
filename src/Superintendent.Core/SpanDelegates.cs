using System;

namespace Superintendent.Core
{
    public delegate void ReadOnlySpanAction<T>(ReadOnlySpan<T> span);
    public delegate void SpanAction<T>(Span<T> span);

}
