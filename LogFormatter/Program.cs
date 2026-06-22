using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;

namespace LogFormatter
{
    internal class Program
    {
        static void Main(string[] args)
        {
            // настройки
            string outputFilename = AppContext.BaseDirectory + "\\output.txt";
            string problemsFilename = AppContext.BaseDirectory + "\\problems.txt";

            const string format1Pattern = @"^\d{2}\.\d{2}\.\d{4}\s\d{2}:\d{2}:\d{2}\.\d*\s" + @"(INFORMATION|WARNING|ERROR|DEBUG)\s" +
                @"Версия программы:\s.*$";
            const string format1DateAndTimeFormat = "dd.MM.yyyy HH:mm:ss.fff";
            Dictionary<string, string> format1LevelToOutputLevel = new Dictionary<string, string>
            {
                { "INFORMATION", "INFO" },
                { "WARNING", "WARN" },
                { "DEBUG", "DEBUG" },
                { "ERROR", "ERROR" }
            };

            const string format2Pattern = @"^\d{4}-\d{2}-\d{2} \d*:\d{2}:\d{2}\.\d*\| " + @"(INFO|WARN|ERROR|DEBUG)\|" +
                @"\w*\|" + @"[a-zA-Z0-9\.]*\|" + @" Код устройства: '.*'$";

            const string outputDateAndTimeFormat = "yyyy-MM-dd HH:mm:ss.ffff";
            string filePath;
            Regex format1Regex = new Regex(format1Pattern, RegexOptions.Compiled);
            Regex format2Regex = new Regex(format2Pattern, RegexOptions.Compiled);

            // проверяем переданный путь к файлу
            if (args.Length == 0)
            {
                Console.WriteLine("Лог-файл не был передан. Завершение работы...");
                return;
            }
            else
                filePath = args[0];
            if (!File.Exists(filePath))
                throw new FileNotFoundException("Лог-файл не существует в указанном расположении");

            // создаем выходные файлы
            if (!File.Exists(outputFilename))
                File.Create(outputFilename);
            if (!File.Exists(problemsFilename))
                File.Create(problemsFilename);


            // обрабатываем файл
            using (StreamReader reader  = new StreamReader(filePath))
            using (StreamWriter writer = new StreamWriter(outputFilename))
            using (StreamWriter problemsWriter = new StreamWriter(problemsFilename))
            {
                string? currentLine;
                while ((currentLine = reader.ReadLine())  != null)
                {
                    StringBuilder outputLine = new StringBuilder();
                    // строка в формате 1
                    if (format1Regex.IsMatch(currentLine))
                    {
                        string[] splittedLine = currentLine.Split();
                        // 10.03.2025 15:14:49.523 INFORMATION Версия программы: '3.4.0.48729'
                        // 0          1            2           3      4          5
                        string dateAndTime = splittedLine[0] + ' ' + splittedLine[1];
                        string level = splittedLine[2];

                        // дата и время
                        DateTime parsedDateAndTime;
                        if (DateTime.TryParseExact(dateAndTime, format1DateAndTimeFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out parsedDateAndTime))
                            outputLine.Append(parsedDateAndTime.ToString(outputDateAndTimeFormat) + '\t');
                        else // не парсится
                        {
                            problemsWriter.WriteLine(currentLine);
                            continue;
                        }

                        // уровень логирования
                        if (format1LevelToOutputLevel.ContainsKey(level))
                            outputLine.Append(format1LevelToOutputLevel[level] + '\t');
                        else // не бывает такого уровня
                        {
                            problemsWriter.WriteLine(currentLine);
                            continue;
                        }

                        // метода и кода устройства в этом формате нет
                        outputLine.Append("DEFAULT\tКодустройства: ''");

                        writer.WriteLine(outputLine);
                    }
                    // строка в формате 2
                    else if(format2Regex.IsMatch(currentLine))
                    {
                        string[] splittedLine = currentLine.Split('|', StringSplitOptions.TrimEntries);
                        // 2025-03-10 15:14:51.5882| INFO|11|MobileComputer.GetDeviceId| Код устройства: '@MINDEO-M40-D-410244015546'
                        // 0                         1    2  3                           4
                        string dateAndTime = splittedLine[0];
                        string level = splittedLine[1];
                        string method = splittedLine[3];
                        string code = splittedLine[4];

                        // проверим дату-время, у нее такой же формат как у выходного
                        if (!DateTime.TryParseExact(dateAndTime, outputDateAndTimeFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
                        {
                            problemsWriter.WriteLine(currentLine);
                            continue;
                        }

                        // проверим уровень, они есть в словаре для формата 1
                        if(!(format1LevelToOutputLevel.ContainsValue(level)))
                        {
                            problemsWriter.WriteLine(currentLine);
                            continue;
                        }

                        // во втором формате конвертировать ничего не нужно
                        outputLine.Append(dateAndTime + '\t' + level + '\t' + method + '\t' + code);

                        writer.WriteLine(outputLine);
                    }
                    // некорректная строка
                    else
                    {
                        problemsWriter.WriteLine(currentLine);
                    }
                }
            }
        }
    }
}