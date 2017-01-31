using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuickShare.FileSendReceive
{
    class FileReceiver
    {
        //Singleton class
        static FileReceiver _instance = null;
        public static FileReceiver Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new FileReceiver();

                return _instance;
            }
        }

        private FileReceiver() { }
    }
}
