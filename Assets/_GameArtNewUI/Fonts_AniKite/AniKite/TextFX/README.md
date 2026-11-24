# Text FX by AniKite

Animated text effects for TextMesh Pro: pick an effect in the gallery, preview it with your own
words, and import it onto any TextMesh Pro text. Each effect comes with its entrance, hold and exit
motion, colours, border, glow, particles and its own font.

## Requirements

- Unity 2023.2 or newer (Unity 6 included). Built-in, URP and HDRP are all supported.
- TextMesh Pro Essentials (the window shows an Import button if they are missing).
- An AniKite account (free to create at https://anikite.com) for importing effects from the gallery.
  Each import costs AniKite credits; the price is shown on the Import button. It is the same
  account as AI GUI by AniKite.

The three demo effects in `Demo/Presets/` are free and need no account.

## Quick start

1. Open **Tools > Text FX (AniKite)**.
2. Type **Your text**: every preview in the gallery plays it.
3. Browse the gallery (All / Text / Game, tags and search). Click an effect to see it larger with
   its timeline (Enter, Hold, Exit).
4. Select a TextMesh Pro text in your scene (or nothing: a new text is made under a Canvas).
5. Press **Import**. Sign in the first time (your browser opens anikite.com once).

The effect is saved as a preset in `Assets/AniKiteTextFX/<Effect name>/` and added to the text
as an **AniKite / Text FX** component. Press Play, or use the Preview card in the Inspector.

## Components

- **AkTextFx** (Add Component > AniKite > Text FX): plays a preset on a TextMesh Pro text.
  The Inspector has collapsible cards: Preview, Effect, What you can change, Colours & border,
  Playback, Font, Events and From a script.
- **AkTextFxParticles** (Add Component > AniKite > Text FX Particles): the effect's particle
  bursts, with particle size and tint. Added for you when an effect has particles.
- **AkTextFxPreset** (Create > AniKite > Text FX Preset): the effect itself. Presets are small
  assets you can reuse on any number of texts.

From a script:

```csharp
using AniKite.TextFx;

var fx = GetComponent<AkTextFx>();
fx.SetText("Victory!");   // new text, plays the entrance again
fx.PlayIn();              // play the entrance
fx.PlayOut();             // play the exit now
fx.SetPreset(otherPreset); // switch to another effect
fx.onComplete.AddListener(() => gameObject.SetActive(false));
```

Everything in the runtime (`AniKite.TextFx.Runtime` assembly) is plain C# source with no
dependency other than TextMesh Pro, so it works in every build target.

## Fonts

24 Unicode fonts are bundled in `Runtime/Fonts/` under the SIL Open Font License (license texts in
`Runtime/Fonts/OFL/`): free to ship in your game. Their TextMesh Pro font assets are created in
your project the first time an effect uses them (or all at once with
**Tools > AniKite > TextFx > Build All Font Assets**). You can also turn the effect's font off and
use any TextMesh Pro font asset from the Font card.

Third-party licenses (the fonts and a small MIT-licensed maths routine) are listed in
`Third-Party Notices.txt`.

## Account, network and privacy

- The window talks only to `https://api.anikite.com` (and a one-time `127.0.0.1` listener
  during browser sign-in). Presets, the runtime and your builds never connect to anything.
- Your sign-in token is stored in this machine's EditorPrefs, never inside the project.
- Use **Sign out** in the window header to sign out.

## Documentation and demo

- Full documentation (PDF, with a step-by-step tutorial and the script reference):
  `Documentation/AniKite Text FX - Documentation.pdf`
- Demo scene: `Demo/AniKite Text FX Demo.unity` (no account needed). Press Play to see three
  effects loop.

## Support

https://anikite.com
