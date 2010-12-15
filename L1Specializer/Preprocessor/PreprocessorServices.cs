using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace L1Specializer.Preprocessor
{
    internal static class PreprocessorServices
    {

        #region Static fields

        internal static string LineCommentPreffix = "*";
        internal static string SubLineCommentPreffix = "***";

        #endregion

        #region Static Methods

        internal static string GetText(byte[] buffer)
        {


            return "";
        }


        internal static Stream DeleteComments(string fname)//, Stream inputStream)
        {
            //byte[] buffer = new byte[inputStream.Length];
            //inputStream.Read(buffer, 0, (int)inputStream.Length);

            //string program = GetText(buffer);

            //string[] lines = program.Split(new string[] { "\n", "\r\n" }, StringSplitOptions.None);
            string[] lines = File.ReadAllLines(fname);
            List<string> newLines = new List<string>(lines.Length);

            foreach (string line in lines)
            {
                //Check for all line comments

                if (line.Length >= LineCommentPreffix.Length && line.StartsWith(LineCommentPreffix))
                {
                    newLines.Add("");
                    continue;
                }
                
                //Check for inline comments

                bool isInString = false;
                StringBuilder accBuffer = new StringBuilder(line.Length);
                for (int i = 0; i < line.Length; ++i)
                {
                    if (line[i] == '"')
                    {
                        isInString = !isInString;
                        accBuffer.Append(line[i]);
                        continue;
                    }
                    if (!isInString && i + SubLineCommentPreffix.Length < line.Length
                        && line.Substring(i, SubLineCommentPreffix.Length) == SubLineCommentPreffix)
                    {
                        break;
                    }
                    accBuffer.Append(line[i]);
                }
                newLines.Add(accBuffer.ToString()); 
            }

            StringBuilder newProgram = new StringBuilder(1024);
            foreach (string line in newLines)
            {
                newProgram.Append(line);
                newProgram.Append("\n");
            }
            
            byte[] resultBuffer = Encoding.ASCII.GetBytes(newProgram.ToString());
            MemoryStream otputStream = new MemoryStream(resultBuffer);

            return otputStream;
        }


        #endregion

    }
}
