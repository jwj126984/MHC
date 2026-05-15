using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NPOI.XSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.HSSF.UserModel;

namespace MHC.Model
{
    public class FaultLogger : IDisposable
    {
        private string _logDirectory;
        private const string LogFileNameFormat = "{0}.xlsx";
        private bool _disposed = false;

        private const string TimeHeader = "故障发生时间";

        public FaultLogger(string logDirectory = null)
        {
            if (string.IsNullOrEmpty(logDirectory))
            {
                string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                _logDirectory = Path.Combine(documentsPath, "CANMonitorLogs");
            }
            else
            {
                _logDirectory = logDirectory;
            }
            if (!Directory.Exists(_logDirectory))
            {
                Directory.CreateDirectory(_logDirectory);
            }
        }

        private static readonly object _lock = new object();

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;
            _disposed = true;
        }

        ~FaultLogger()
        {
            Dispose(false);
        }

        public void LogFault(FaultModel fault)
        {
            lock (_lock)
            {
                try
                {
                    if (fault.SignalValues == null || fault.SignalValues.Count == 0)
                    {
                        Console.WriteLine("故障日志数据为空，跳过写入");
                        return;
                    }

                    string fileName = string.Format(LogFileNameFormat, DateTime.Now.ToString("yyyy-MM-dd"));
                    string filePath = Path.Combine(_logDirectory, fileName);

                    IWorkbook workbook;
                    ISheet worksheet;
                    Dictionary<string, int> headerIndexMap;

                    if (File.Exists(filePath))
                    {
                        using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                        {
                            workbook = new XSSFWorkbook(fileStream);
                        }
                        worksheet = workbook.GetSheetAt(0);
                        headerIndexMap = ReadHeaderIndexMap(worksheet);
                        
                        bool needUpdate = false;
                        foreach (var signalKey in fault.SignalValues.Keys)
                        {
                            if (!headerIndexMap.ContainsKey(signalKey))
                            {
                                headerIndexMap[signalKey] = headerIndexMap.Count;
                                needUpdate = true;
                            }
                        }
                        
                        if (needUpdate)
                        {
                            UpdateHeaderRow(worksheet, headerIndexMap);
                        }
                    }
                    else
                    {
                        workbook = new XSSFWorkbook();
                        worksheet = workbook.CreateSheet("故障日志");
                        
                        headerIndexMap = new Dictionary<string, int>();
                        headerIndexMap[TimeHeader] = 0;
                        
                        int index = 1;
                        foreach (var signalKey in fault.SignalValues.Keys.OrderBy(k => k))
                        {
                            headerIndexMap[signalKey] = index++;
                        }
                        
                        UpdateHeaderRow(worksheet, headerIndexMap);
                    }

                    int rowNum = worksheet.LastRowNum + 1;
                    var row = worksheet.CreateRow(rowNum);
                    
                    var redStyle = workbook.CreateCellStyle();
                    var redFont = workbook.CreateFont();
                    redFont.Color = IndexedColors.Red.Index;
                    redStyle.SetFont(redFont);
                    
                    row.CreateCell(headerIndexMap[TimeHeader]).SetCellValue(fault.Time.ToString("yyyy-MM-dd HH:mm:ss"));
                    
                    foreach (var kvp in fault.SignalValues)
                    {
                        if (headerIndexMap.TryGetValue(kvp.Key, out int colIndex))
                        {
                            var cell = row.CreateCell(colIndex);
                            
                            if (fault.FaultSignals != null && fault.FaultSignals.ContainsKey(kvp.Key))
                            {
                                cell.SetCellValue(fault.FaultSignals[kvp.Key]);
                                cell.CellStyle = redStyle;
                            }
                            else
                            {
                                cell.SetCellValue(kvp.Value);
                            }
                        }
                    }

                    for (int i = 0; i < headerIndexMap.Count; i++)
                    {
                        worksheet.AutoSizeColumn(i);
                    }

                    using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                    {
                        workbook.Write(fileStream);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"生成故障日志时出错: {ex.Message}");
                }
            }
        }

        public void LogFaultWithDisplayNames(FaultLogEntry logEntry)
        {
            lock (_lock)
            {
                try
                {
                    if (logEntry.DisplayValues == null || logEntry.DisplayValues.Count == 0)
                    {
                        Console.WriteLine("故障日志条目为空，跳过写入");
                        return;
                    }

                    string fileName = string.Format(LogFileNameFormat, DateTime.Now.ToString("yyyy-MM-dd"));
                    string filePath = Path.Combine(_logDirectory, fileName);

                    IWorkbook workbook;
                    ISheet worksheet;
                    Dictionary<string, int> headerIndexMap;

                    if (File.Exists(filePath))
                    {
                        using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                        {
                            workbook = new XSSFWorkbook(fileStream);
                        }
                        worksheet = workbook.GetSheetAt(0);
                        headerIndexMap = ReadHeaderIndexMap(worksheet);
                        
                        bool needUpdate = false;
                        foreach (var displayName in logEntry.DisplayValues.Keys)
                        {
                            if (!headerIndexMap.ContainsKey(displayName))
                            {
                                headerIndexMap[displayName] = headerIndexMap.Count;
                                needUpdate = true;
                            }
                        }
                        
                        if (needUpdate)
                        {
                            UpdateHeaderRow(worksheet, headerIndexMap);
                        }
                    }
                    else
                    {
                        workbook = new XSSFWorkbook();
                        worksheet = workbook.CreateSheet("故障日志");
                        
                        headerIndexMap = new Dictionary<string, int>();
                        headerIndexMap[TimeHeader] = 0;
                        
                        int index = 1;
                        foreach (var displayName in logEntry.DisplayValues.Keys.OrderBy(k => k))
                        {
                            headerIndexMap[displayName] = index++;
                        }
                        
                        UpdateHeaderRow(worksheet, headerIndexMap);
                    }

                    int rowNum = worksheet.LastRowNum + 1;
                    var row = worksheet.CreateRow(rowNum);
                    
                    var redStyle = workbook.CreateCellStyle();
                    var redFont = workbook.CreateFont();
                    redFont.Color = IndexedColors.Red.Index;
                    redStyle.SetFont(redFont);
                    
                    row.CreateCell(headerIndexMap[TimeHeader]).SetCellValue(logEntry.Time.ToString("yyyy-MM-dd HH:mm:ss"));
                    
                    foreach (var kvp in logEntry.DisplayValues)
                    {
                        if (headerIndexMap.TryGetValue(kvp.Key, out int colIndex))
                        {
                            var cell = row.CreateCell(colIndex);
                            
                            bool isFault = logEntry.IsFaultColumn.ContainsKey(kvp.Key) && logEntry.IsFaultColumn[kvp.Key];
                            if (isFault)
                            {
                                cell.SetCellValue(kvp.Value);
                                cell.CellStyle = redStyle;
                            }
                            else
                            {
                                cell.SetCellValue(kvp.Value);
                            }
                        }
                    }

                    for (int i = 0; i < headerIndexMap.Count; i++)
                    {
                        worksheet.AutoSizeColumn(i);
                    }

                    using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                    {
                        workbook.Write(fileStream);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"生成故障日志时出错: {ex.Message}");
                }
            }
        }

        private Dictionary<string, int> ReadHeaderIndexMap(ISheet worksheet)
        {
            var headerIndexMap = new Dictionary<string, int>();
            var headerRow = worksheet.GetRow(0);
            
            if (headerRow != null)
            {
                for (int i = 0; i < headerRow.LastCellNum; i++)
                {
                    var cell = headerRow.GetCell(i);
                    if (cell != null && !string.IsNullOrEmpty(cell.ToString()))
                    {
                        headerIndexMap[cell.ToString()] = i;
                    }
                }
            }
            
            return headerIndexMap;
        }

        private void UpdateHeaderRow(ISheet worksheet, Dictionary<string, int> headerIndexMap)
        {
            var headerRow = worksheet.GetRow(0) ?? worksheet.CreateRow(0);
            
            foreach (var kvp in headerIndexMap)
            {
                headerRow.CreateCell(kvp.Value).SetCellValue(kvp.Key);
            }
        }

        public void LogFaults(IEnumerable<FaultModel> faults)
        {
            foreach (var fault in faults)
            {
                LogFault(fault);
            }
        }
    }
}