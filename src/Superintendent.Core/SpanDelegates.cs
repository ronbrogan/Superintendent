using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Superintendent.Core
{
    public delegate void ReadOnlySpanAction<T>(ReadOnlySpan<T> span);
    public delegate void SpanAction<T>(Span<T> span);

}
