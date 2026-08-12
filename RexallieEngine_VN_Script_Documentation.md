# Rexallie Visual Novel Scripting Language Documentation

This document explains how to write scripting assets (`.vns` files) for the Rexallie Visual Novel Engine. Scripts are processed by the script parser and parsed into dialogue lines, control flow actions, variables, and audio commands.

---

## 1. File Structure & Directory Rules

- **Extension**: Script files should end with `.vns` (Visual Novel Script).
- **Location**: Scripts must be located in your Unity project's `Assets/Resources/Dialogues/{LanguageCode}/` directory (e.g. `Assets/Resources/Dialogues/en/`).
- **Comments**: Any line starting with `#` or `//` is ignored by the parser and can be used for comments or organizational headings.
- **Labels**: Labels define jumping locations for choices and conditional jumps. A label is defined by a single word ending in a colon:
  ```vnscript
  start:
  ```

---

## 2. Writing Dialogue Lines

Dialogue lines are the core text spoken by characters. They use the following syntax:

```vnscript
CharacterID <portrait_override> [expression_override]:
Here goes the dialogue text. You can split dialogue
across multiple lines if needed.
```

- **`CharacterID`**: The unique ID of the character as configured in the character database scriptable assets (e.g., `Alice`, `Nikita`).
- **`<portrait_override>`** (Optional): Overrides the character's active portrait sheet (e.g. `<casual>`).
- **`[expression_override]`** (Optional): Overrides the character's face sprite expression (e.g. `[happy]`).
- **`:`**: A colon separates the speaker description from the dialogue.
- **Dialogue Text**: The text can span multiple lines. The parser wraps and joins multiline text with a newline `\n`. It stops reading the line when it detects an empty line, a command (`@`), a choice arrow (`->`), or a new label.

### Rich Text Styling
The dialogue display is powered by TextMeshPro, meaning you can use HTML-style rich text tags in your dialogue lines.
- **Bold**: `This is <b>bold</b> text.`
- **Italic**: `This is <i>italic</i> text.`
- **Color**: `This is <color=#FF0000>red</color> text.`
- **Text Size**: `Make it <size=130%>larger</size> or <size=70%>smaller</size>.`
- **Underline & Strikethrough**: `Check this <u>underlined</u> or <s>struck-through</s> text.`
- **Subscript & Superscript**: `H<sub>2</sub>O or X<sup>2</sup>`

### Dialogue Text Variable Interpolation
You can display the value of any story variable directly inside your dialogue by enclosing the variable name in curly braces `{}`:
- **Variable Display**: `Your affinity with Nikita is {affinity_nikita}.`

### Dialogue Typewriter Inline Commands
You can control the typewriter speed and pausing in the middle of sentences using inline tags:
- **`{w}`**: Wait for player click. The typewriter stops typing and waits for the player to press confirm/click before continuing the sentence.
- **`{p=duration}`**: Pause typing for a specific duration in seconds (e.g., `{p=0.5}` pauses for 0.5s before continuing automatically).
- **Example**: `Hello!{w} Welcome to the school.{p=0.5} Let's head inside.`

---

## 3. Script Actions & Commands

All engine commands start with the `@` symbol, followed by parameters.

### A. Dialogue Flow & Variables
- **`@jump label`**: Jump immediately to a label.
  ```vnscript
  @jump path_programming
  ```
- **`@set variable operator value`**: Set, add to, or subtract from a runtime variable.
  - Operators: `=`, `+=`, `-=`
  - Types: integer (`12`), boolean (`true`/`false`), or string
  ```vnscript
  @set affinity_nikita += 5
  @set met_nikita = true
  ```
- **`@if variable operator value jump label`**: Conditionally jump to a label.
  - Operators: `>`, `<`, `>=`, `<=`, `==`, `!=`
  ```vnscript
  @if affinity_nikita > 3 jump good_ending
  ```
- **`@choice` and `@endchoice`**: Present dialogue branch choices.
  ```vnscript
  @choice
      "Talk to Alice." -> talk_to_alice
      "Walk away." -> leave_scene
  @endchoice
  ```
- **`@loadScript filename`**: Load and transition to another visual novel script file instantly (useful for multi-chapter games).
  ```vnscript
  @loadScript chapter_2
  ```

### B. Character Actions
- **`@showCharacter character position [portrait] [expression] [fadeIn:time] [slideFrom:dir]`**:
  - `position`: `left`, `center`, `right`, `farleft`, `farright`
  - `fadeIn` (Optional): duration in seconds (e.g., `fadeIn:1.0`)
  - `slideFrom` (Optional): off-screen origin direction (`left`, `right`, `farleft`, `farright`)
  ```vnscript
  @showCharacter Alice left aya_base base fadeIn:1.0 slideFrom:left
  ```
- **`@hideCharacter character [fadeOut:time] [slideTo:dir]`**:
  - `fadeOut` (Optional): fade out duration in seconds
  - `slideTo` (Optional): direction the character slides off-screen
  ```vnscript
  @hideCharacter Alice fadeOut:0.5 slideTo:left
  ```
- **`@moveCharacter character position duration [wait]`**:
  - Moves a character to a new position. If `wait` is passed as the last parameter, the engine pauses dialogue progression until the movement is finished.
  ```vnscript
  @moveCharacter Alice center 0.5 wait
  ```
- **`@setExpression character expression`**: Change a character's active expression instantly.
  ```vnscript
  @setExpression Alice happy
  ```
- **`@setPortrait character portrait [expression]`**: Change a character's active portrait sheet instantly.
  ```vnscript
  @setPortrait Alice aya_blue happy
  ```

### C. Background Actions
- **`@setBackground backgroundName transition`**:
  - `transition`: `instant`, `fade`, or `crossfade`.
  ```vnscript
  @setBackground campus fade
  ```

### D. Audio Actions
- **`@playMusic trackName [fadeIn:time]`**: Play looping background music.
  ```vnscript
  @playMusic Track_Chill fadeIn:2.0
  ```
- **`@stopMusic [fadeOut:time]`**: Stop the background music.
  ```vnscript
  @stopMusic fadeOut:1.5
  ```
- **`@playSFX sfxName`**: Play a one-shot sound effect.
  ```vnscript
  @playSFX se_cat01
  ```

### E. Camera, Effects & Utility
- **`@shake duration magnitude`**: Shakes the screen. Shorter durations and higher magnitude create heavy impacts.
  ```vnscript
  @shake 0.5 15.0
  ```
- **`@zoom x:pos y:pos percentage:scale time:duration`**: Zooms the camera to focus on coordinates.
  - `x`: horizontal offset (typically `-400` to `400`)
  - `y`: vertical offset
  - `percentage`: zoom scale percentage
  - `time`: duration of the camera glide
  ```vnscript
  @zoom x:-200 y:0 percentage:50 time:1.5
  ```
- **`@zoom reset time:duration`**: Reset the camera to its default position.
  ```vnscript
  @zoom reset time:1.0
  ```
- **`@wait duration`**: Pause the engine for a specified duration in seconds.
  ```vnscript
  @wait 1.5
  ```
- **`@unlock_cg cgID`**: Unlock a CG asset in the persistent gallery file.
  ```vnscript
  @unlock_cg gallery_image_showcase
  ```
- **`@clearDialogue`**: Empty the speaker name and text boxes.
  ```vnscript
  @clearDialogue
  ```
- **`@fadeOut [duration] [color]`**: Fade the entire screen (including UI, dialogue, characters, backgrounds) to a solid color.
  - `duration` (Optional): duration in seconds (default: `1.0`)
  - `color` (Optional): `black`, `white`, or hex color code (default: `black`)
  ```vnscript
  @fadeOut 1.5 black
  ```
- **`@fadeIn [duration]`**: Fade the entire screen back in from the current overlay color.
  - `duration` (Optional): duration in seconds (default: `1.0`)
  ```vnscript
  @fadeIn 1.0
  ```
- **`@trigger eventName [parameter1] [parameter2] ...`**: Broadcast a custom event notification to external game scripts outside the visual novel dialogue scope (e.g., spawning game items, triggering combat scenes, or starting animations).
  ```vnscript
  @trigger spawn cube
  @trigger start_combat elite_guard 50
  ```

---

## 4. Script Example (Basic Scene)

Here is a short script demonstrating a typical setup:

```vnscript
// Chapter 1 School Entrance
start:
    @setBackground campus instant
    @playMusic Track_Chill fadeIn:1.0
    @showCharacter Alice left aya_base base fadeIn:0.5 slideFrom:left

    Alice [base]:
    Good morning! Welcome to school. It is a beautiful day.
    
    @showCharacter Nikita right base base fadeIn:0.5 slideFrom:right

    Nikita [base]:
    Hey there, Alice. Ready for classes?

    @choice
        "Say you are excited!" -> say_yes
        "Say you are tired..." -> say_no
    @endchoice

say_yes:
    @set affinity_nikita += 2
    Alice [happy]:
    Yeah, I can't wait!
    @jump finish

say_no:
    @set affinity_nikita -= 1
    Alice [base]:
    Ugh, not really. I'm exhausted...

finish:
    @if affinity_nikita > 1 jump friendly_ending
    @jump normal_ending

friendly_ending:
    Nikita [happy]:
    That's the spirit! Let's get going.
    @jump final

normal_ending:
    Nikita [base]:
    Ah, I see. Well, let's just get it over with.

final:
    @stopMusic fadeOut:2.0
    @hideCharacter Alice fadeOut:0.5
    @hideCharacter Nikita fadeOut:0.5
    @wait 1.0
```
