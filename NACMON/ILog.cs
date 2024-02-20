using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Log
{
    public interface ILog
    {
        void Init(string DefaultLogFile, int MaxLogLen);

        void Log(string fpMessage);
    }
}