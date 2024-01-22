using DotNetCore.CAP.Filter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CAP
{
    internal class CAPFilter: SubscribeFilter
    {
        public override Task OnSubscribeExecutedAsync(ExecutedContext context)
        {
            var headers = context.DeliverMessage.Headers;
            return base.OnSubscribeExecutedAsync(context);
        }
    }
}
