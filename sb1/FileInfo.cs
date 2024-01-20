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
        public TagLib.Tag Tag;
        public string FileName;

        private static readonly Encoding enc1251 = Encoding.GetEncoding(1251);
        private static readonly Encoding enc1252 = Encoding.GetEncoding(1252);

        private static string process(string s)
        {
            if (Options.trimAmount >= s.Length) return s.Trim();

            return s[Options.trimAmount..].Trim();
        }

        public override string ToString()
        {
            var fname = Path.GetFileNameWithoutExtension(FileName);
            if (Tag.Title == null || !Options.useTags)
            {
                return process(fname);
            }

            if (!Options.cyrillicFix)
            {
                return process(Tag.Title);
            }

            return process(enc1251.GetString(enc1252.GetBytes(Tag.Title)));
        }

        public void Dispose()
        {
            Reader.Dispose();
            Reader2.Dispose();
        }
    }

}
