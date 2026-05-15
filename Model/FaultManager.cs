using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MHC.Model
{
    public class FaultManager
    {
        private FaultLogger _faultLogger;
        private Dictionary<string, bool> _faultStatuses;

        public FaultManager(string logDirectory = "Logs")
        {
            _faultLogger = new FaultLogger(logDirectory);
            _faultStatuses = new Dictionary<string, bool>();
        }

        public void RecordFault(string location, string type, string value = "", Dictionary<string, double> signalValues = null, Dictionary<string, string> faultSignals = null)
        {
            string faultKey = $"{location}_{type}";

            if (!_faultStatuses.TryGetValue(faultKey, out bool isFault) || !isFault)
            {
                var signalValuesCopy = signalValues != null 
                    ? new Dictionary<string, double>(signalValues) 
                    : new Dictionary<string, double>();
                
                var faultSignalsCopy = faultSignals != null 
                    ? new Dictionary<string, string>(faultSignals) 
                    : new Dictionary<string, string>();
                
                var fault = new FaultModel(DateTime.Now, location, type, value, signalValuesCopy, faultSignalsCopy);
                Task.Run(() => _faultLogger.LogFault(fault));
                _faultStatuses[faultKey] = true;
            }
        }

        public void RecordFaultWithDisplayNames(List<string> faultSignalKeys, FaultLogEntry logEntry)
        {
            bool hasNewFault = false;
            
            foreach (var signalKey in faultSignalKeys)
            {
                if (!_faultStatuses.TryGetValue(signalKey, out bool isFault) || !isFault)
                {
                    hasNewFault = true;
                    _faultStatuses[signalKey] = true;
                }
            }

            if (hasNewFault)
            {
                Task.Run(() => _faultLogger.LogFaultWithDisplayNames(logEntry));
            }
        }

        public void ClearFaultStatus(string signalKey)
        {
            if (_faultStatuses.ContainsKey(signalKey))
            {
                _faultStatuses[signalKey] = false;
            }
        }

        public void RecordFaults(IEnumerable<(string location, string type, string value)> faults, Dictionary<string, double> signalValues = null, Dictionary<string, string> faultSignals = null)
        {
            foreach (var (location, type, value) in faults)
            {
                RecordFault(location, type, value, signalValues, faultSignals);
            }
        }
    }
}