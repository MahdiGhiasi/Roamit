using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuickShare.HelperClasses
{
    public static class PersistentDisplay
    {
        private static Windows.System.Display.DisplayRequest _displayRequest;
        private static bool isAlreadyRequested = false;

        public static void ActivatePersistentDisplay()
        {
            try
            {
                //create the request instance if needed
                if (_displayRequest == null)
                    _displayRequest = new Windows.System.Display.DisplayRequest();
                else if (isAlreadyRequested)
                    return;

                //make request to put in active state
                _displayRequest.RequestActive();
                isAlreadyRequested = true;
            }
            catch { }
        }

        public static void ReleasePersistentDisplay()
        {
            try
            {
                //must be same instance, so quit if it doesn't exist
                if (_displayRequest == null)
                    return;

                //undo the request
                _displayRequest.RequestRelease();
                isAlreadyRequested = false;
            }
            catch { }
        }
    }
}
