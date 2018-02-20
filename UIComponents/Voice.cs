using System;
using System.Configuration;
using System.Speech.Synthesis;

namespace GaitAndBalanceApp.UIComponents
{
    public class Voice : IDisposable
    {
        private SpeechSynthesizer speechSynth = new SpeechSynthesizer();
        public Voice()
        {
            speechSynth.SelectVoice(ConfigurationManager.AppSettings["SpeechSynthVoice"]);
            speechSynth.Volume = 100;
            speechSynth.SetOutputToDefaultAudioDevice();
        }
        public void Speak(string s)
        {
            speechSynth.SpeakAsync(s);
        }

        public void SpeakBlocking(string s)
        {
            speechSynth.Speak(s);
        }

        public void Dispose()
        {
            speechSynth.Dispose();
        }
    }

}
