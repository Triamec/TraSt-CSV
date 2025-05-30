using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TraSt_CSV {
    internal class CsvParser {

        public string Profile { get; private set; }
        public double SamplingTime { get; private set; }
        public List<string> columnsName { get; private set; } = new List<string>();

        public void ParseHeader(string filePath) {
            foreach (var line in File.ReadLines(filePath)) {
                string trimmedLine = line.Trim();

                // Wenn die Zeile nicht mit "//" beginnt, dann ist der Header zu Ende und wir beenden die Schleife
                if (!trimmedLine.StartsWith("//")) {
                    break;
                }

                // Profile auslesen
                if (trimmedLine.StartsWith("// Profile:")) {
                    Profile = trimmedLine.Substring("// Profile:".Length).Trim();
                }
                // Sampling Time auslesen
                else if (trimmedLine.StartsWith("// SamplingTime:")) {
                    string samplingStr = trimmedLine.Substring("// SamplingTime:".Length).Trim();
                    if (samplingStr.EndsWith("s")) {
                        samplingStr = samplingStr.Substring(0, samplingStr.Length - 1);
                    }
                } 
                // Spaltennamen auslesen
                else if (trimmedLine.StartsWith("// Column")) {
                    int colonIndex = trimmedLine.IndexOf(':');
                    if(colonIndex != -1 && (colonIndex + 1 < trimmedLine.Length)) {
                        string columnName = trimmedLine.Substring(colonIndex + 1).Trim();
                        columnsName.Add(columnName);
                    }
                }

            }
        }
    }
}

