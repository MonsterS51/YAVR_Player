<h1 align="center">YAVR Player</h1>

Yet Another VR Player for Google Cardboard based on <a href="https://unity.com">Unity</a>, <a href="https://github.com/googlevr/cardboard-xr-plugin">Cardboard XR Plugin</a> and <a href="https://code.videolan.org/videolan/vlc-unity">LibVLC</a>.

# Features:
* 180°/360° Over/Under or Side-by-Side frame layout.
* Up to 4096x2048 video dimension. 
* Any video format supported by your phone (and LibVLC).
* Fullspeed SMB LAN support over WiFi.
* Gaze control, gamepad support.
* File explorer with thumbnail generation.
* Android built-in VR headset configuration with QR codes
(make your custom QR config with <a href="http://www.sitesinvr.com">this</a>).
* Zoom preferences

## Bugs and problems:
* Distorted LibVLC watermark on bottom. Used a trial package and I don't have $100 for a license yet (and it's not worth it).
* App may not ask for all permissions, grant them manually.
* Crash app after multiple playback restart. 
Unity LibVLC <a href="https://code.videolan.org/videolan/vlc-unity/-/issues/180">issue</a>.
* LibVLC forced play in VR emulation mode if found VR metadata in video.
Unity LibVLC <a href="https://code.videolan.org/videolan/vlc-unity/-/issues/166">issue</a>.
Workaround - remove metadata with FFMPEG:
```
ffmpeg.exe -i Input.mp4 -vcodec copy -acodec copy Output.mp4
```
* SMB LAN authentication should be done manually outside of app.
```
Android --> data --> com.MonsterNest.YAVRPlayer --> files --> save_data.json
...
"NetLogin" : "YourLanLogin",
"NetPass" : "YourLanPass",
...
```

<p align="center"><img src="./ReadMe-assets/done.jpg" width="30%"></p>