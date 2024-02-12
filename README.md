# XR Conversational NPC

This demo project exposes a quick prototype that allows voice interaction with an NPC.

Features:
- NPC Reacts when the user stare at her face,
- User's voice is recorded after 5 seconds looking at the NPC, then it's and analyzed using [WhisperTiny](https://huggingface.co/unity/sentis-whisper-tiny) Speech-to-text model,
- The generated text is used as an input for the [TinyStories](https://huggingface.co/unity/sentis-tiny-stories) model,
- Story is spoken by the NPC using the [Meta-Text-To-Speech-SDK](https://developer.oculus.com/downloads/package/meta-voice-sdk/)
- Avatar includes multiple animations and makes use of the Lipsync provided by [ReadyPlayerMe](https://docs.readyplayer.me/ready-player-me/integration-guides/unity/quickstart)
- NPC can be interrupted by looking away during at least 5 seconds.
- Hand tracking (although none interaction is making use of it)

Potencial Improvements:
- Use IKinematics to make the NPC look at the player, at least while "listening" the command,
- Add different animations that react accordingly to the story (sentiment analysis),
- Add environment SFX,
- Use sentence-similarity to identify the intention of the user, allowing different conversations in addition to the TinyStories generation,
- Optimize application size.

## Demo

The build has been tested on the Meta Quest 2, using Air Link. Find the build for Windows [Here](https://drive.google.com/file/d/1MXefa0jIj3vLPv3KeWvZdBXA5cjzZEEC/view?usp=sharing)

## License

[MIT](https://choosealicense.com/licenses/mit/)
