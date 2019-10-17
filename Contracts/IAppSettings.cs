using System;
using System.Collections.Generic;
using System.Text;

namespace Contracts
{
    public interface IAppSettings
    {
        string Secret { get; set; }
    }
}
