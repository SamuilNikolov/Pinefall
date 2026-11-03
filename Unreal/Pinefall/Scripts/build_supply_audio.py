"""Original seamless low aircraft rumble; no recording, mic, or external samples."""
from pathlib import Path
import numpy as np,wave
rate=24000;seconds=3;n=rate*seconds;t=np.arange(n)/rate;rng=np.random.default_rng(1979)
noise=rng.normal(0,1,n);freq=np.fft.rfftfreq(n,1/rate);spectrum=np.fft.rfft(noise);spectrum*=np.exp(-(freq/320)**2);noise=np.fft.irfft(spectrum,n);noise/=max(1e-6,np.std(noise))
signal=.07*noise+.07*np.sin(2*np.pi*90*t)*(1+.18*np.sin(2*np.pi*12*t))+.025*np.sin(2*np.pi*180*t)
signal=np.clip(signal,-.65,.65);out=Path(__file__).resolve().parents[1]/'SourceArt/Audio/Supplies/Propeller.wav';out.parent.mkdir(parents=True,exist_ok=True)
with wave.open(str(out),'wb') as f:f.setnchannels(1);f.setsampwidth(2);f.setframerate(rate);f.writeframes((signal*32767).astype('<i2').tobytes())
print(out.name)
