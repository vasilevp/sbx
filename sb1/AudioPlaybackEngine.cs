using NAudio.Wave.SampleProviders;
using NAudio.Wave;
using System;
using System.Linq;

namespace sbx;
class AudioPlaybackEngine : IDisposable
{
    private IWavePlayer outputDevice;
    private readonly MixingSampleProvider mixer;

    public AudioPlaybackEngine(int sampleRate = 44100, int channelCount = 2)
    {
        mixer = new MixingSampleProvider(WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, channelCount))
        {
            ReadFully = true
        };
        mixer.MixerInputEnded += OnMixerInputEnded;
    }

    public event EventHandler AllInputEnded;

    private void OnMixerInputEnded(object sender, SampleProviderEventArgs e)
    {
        // check if there are any inputs left
        // OnMixerInputEnded gets invoked before the corresponding source is removed from the List so there should be exactly one source left
        if (mixer.MixerInputs.Count() == 1)
        {
            AllInputEnded?.Invoke(this, EventArgs.Empty);
        }
    }

    public void Init(int deviceNumber)
    {
        outputDevice?.Dispose();

        var output = new WaveOutEvent
        {
            DeviceNumber = deviceNumber
        };
        output.Init(mixer);
        output.Play();

        outputDevice = output;
    }

    public void PlaySound(IWaveProvider wave, float volume = 1)
    {
        var sampleProvider = new VolumeSampleProvider(wave.ToSampleProvider())
        {
            Volume = volume
        };
        AddMixerInput(sampleProvider);
    }

    public void StopAllSounds()
    {
        mixer.RemoveAllMixerInputs();
    }

    private ISampleProvider ConvertToRightChannelCount(ISampleProvider input)
    {
        if (input.WaveFormat.Channels == mixer.WaveFormat.Channels)
        {
            return input;
        }

        if (input.WaveFormat.Channels == 1 && mixer.WaveFormat.Channels == 2)
        {
            return new MonoToStereoSampleProvider(input);
        }

        throw new NotImplementedException("Not yet implemented this channel count conversion");
    }

    private void AddMixerInput(ISampleProvider input)
    {
        var resampled = new WdlResamplingSampleProvider(input, mixer.WaveFormat.SampleRate);
        mixer.AddMixerInput(ConvertToRightChannelCount(resampled));
    }

    public void Dispose()
    {
        if (outputDevice != null)
        {
            outputDevice.Dispose();
            outputDevice = null;
        }
    }
}
