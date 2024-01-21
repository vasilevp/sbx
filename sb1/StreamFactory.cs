using NAudio.Vorbis;
using NAudio.Wave;
using System.IO;

namespace sbx
{
    internal static class StreamFactory
    {
        /// <summary>
        /// Create a WaveStream from file
        /// </summary>
        /// <param name="filename">Input file</param>
        /// <returns>WaveStream that reads from the provided input file. The resulting WaveStream needs to be disposed of to release the file handle.</returns>
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
