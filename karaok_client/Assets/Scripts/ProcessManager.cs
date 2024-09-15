using System.Collections.Generic;

namespace DefaultNamespace
{
    public class ProcessManager
    {
        private static ProcessManager _instance;

        public static ProcessManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new ProcessManager();
                }

                return _instance;
            }
        }

        private Queue<ProcessItemData> _processQueue;
    }
}