using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReadieFur.SourceAnalyzer.VSIX
{
    public static class ModuleInitializer
    {
        public static void Initialize()
        {
            Debugger.Break();
        }
    }
}
