using NAudio.Vorbis;
using NAudio.Wave;
using System.IO;

namespace sbx
{
    internal static class StreamFactory
    {
        public static WaveStream Create(string filename)
        {
            var ext = Path.GetExtension(filename);
            switch (ext)
            {
                case ".ogg":
                    return new VorbisWaveReader(filename);
                default:
                    return new AudioFileReader(filename);
            }
        }
    }
}
