using NAudio.Wave;
using System;
using System.IO;
using System.Text;

namespace sbx
{
    internal class FileInfo : IDisposable
    {
        public WaveStream Reader;
        public WaveStream Reader2;
        public string TagName;
        public string FileName;

        private static readonly Encoding enc1251 = Encoding.GetEncoding(1251);
        private static readonly Encoding enc1252 = Encoding.GetEncoding(1252);

        private static string process(string s)
        {
            if (s == null) { return "NULL"; }
            if (Options.trimAmount >= s.Length) return s.Trim();

            return s[Options.trimAmount..].Trim();
        }

        public override string ToString()
        {
            var fname = Path.GetFileNameWithoutExtension(FileName);
            if (!Options.useTags)
            {
                return process(fname);
            }

            if (!Options.cyrillicFix)
            {
                return process(TagName);
            }

            return process(enc1251.GetString(enc1252.GetBytes(TagName)));
        }

        public void Dispose()
        {
            Reader.Dispose();
            Reader2.Dispose();
        }
    }

}
