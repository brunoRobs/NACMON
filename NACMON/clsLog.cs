using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Log
{
    class clsLog : ILog
    {
        int MaxLength;

        string LogFile;

        public void Init(string DefaultLogFile, int MaxLogLen)
        {
            LogFile = DefaultLogFile;

            MaxLength = MaxLogLen;

            if (!File.Exists(DefaultLogFile))
            {
                var file = new FileStream(DefaultLogFile, FileMode.Create);

                file.Close();
            }
        }
            
        public void Log(string fpMessage)
        {
            var log = new FileStream(LogFile, FileMode.Append);

            if (log.Length >= MaxLength)
            {
                log.Close();

                if(File.Exists(Path.GetFileNameWithoutExtension(log.Name) + "_old.log"))
                {
                    File.Delete(Path.GetFileNameWithoutExtension(log.Name) + "_old.log");
                }

                File.Move(log.Name, Path.GetFileNameWithoutExtension(log.Name) + "_old.log");

                log = new FileStream(LogFile, FileMode.Append);
            }

            byte[] message = Encoding.UTF8.GetBytes(fpMessage);

            log.Write(message, 0, message.Length);

            log.Close();
        }
    }
}