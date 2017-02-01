using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuickShare.FileSendReceive
{
    class FileSender
    {
        //Singleton class
        static FileSender _instance = null;
        public static FileSender Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new FileSender();

                return _instance;
            }
        }

        private FileSender() { }
    }
}
