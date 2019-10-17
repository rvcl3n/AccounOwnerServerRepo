using Contracts;
using System;
using System.Collections.Generic;
using System.Text;

namespace UserService
{
    public class AppSettings: IAppSettings
    {
        public string Secret { get; set; }
    }
}
