const vscode = require('vscode');
const path = require('path');
const fs = require('fs');

const COMMAND_DOCS = {
  jump: {
    signature: '@jump <label>',
    description: 'Jump immediately to a target label in the current script.',
    example: '@jump path_programming'
  },
  set: {
    signature: '@set <variable> <= | += | -=> <value>',
    description: 'Set, add to, or subtract from a runtime story variable (int, bool, or string).',
    example: '@set affinity_nikita += 5\n@set met_nikita = true'
  },
  if: {
    signature: '@if <variable> <== | != | > | < | >= | <=> <value> jump <label>',
    description: 'Conditionally jump to a label if the variable comparison evaluates to true.',
    example: '@if affinity_nikita > 3 jump good_ending'
  },
  choice: {
    signature: '@choice ... @endchoice',
    description: 'Present branching player dialogue choices. Each option uses `"Text" -> target_label`.',
    example: '@choice\n    "Talk to Alice." -> talk_to_alice\n    "Walk away." -> leave_scene\n@endchoice'
  },
  endchoice: {
    signature: '@endchoice',
    description: 'Marks the end of a `@choice` block.',
    example: '@endchoice'
  },
  loadscript: {
    signature: '@loadScript <filename>',
    description: 'Load and transition to another `.vns` script file immediately.',
    example: '@loadScript chapter_2'
  },
  load_script: {
    signature: '@load_script <filename>',
    description: 'Alias for `@loadScript`. Load and transition to another `.vns` script file.',
    example: '@load_script chapter_2'
  },
  showcharacter: {
    signature: '@showCharacter <character> <position> [portrait] [expression] [fadeIn:time] [slideFrom:dir] [slideDuration:time]',
    description: 'Display a character on screen. Positions: `left`, `center`, `right`, `farleft`, `farright`.',
    example: '@showCharacter Alice left aya_base base fadeIn:1.0 slideFrom:left'
  },
  hidecharacter: {
    signature: '@hideCharacter <character> [fadeOut:time] [slideTo:dir] [slideDuration:time]',
    description: 'Hide an active character with optional fade out or off-screen slide.',
    example: '@hideCharacter Alice fadeOut:0.5 slideTo:left'
  },
  movecharacter: {
    signature: '@moveCharacter <character> <position> <duration> [wait]',
    description: 'Move an active character to a new position (`left`, `center`, `right`, `farleft`, `farright`). Append `wait` to block until movement completes.',
    example: '@moveCharacter Alice center 0.5 wait'
  },
  setexpression: {
    signature: '@setExpression <character> <expression>',
    description: 'Change a character\'s active face sprite expression immediately.',
    example: '@setExpression Alice happy'
  },
  setportrait: {
    signature: '@setPortrait <character> <portrait> [expression]',
    description: 'Change a character\'s active portrait sheet (and optionally expression) immediately.',
    example: '@setPortrait Alice aya_blue happy'
  },
  breathe: {
    signature: '@breathe <character> [speed:value] [intensity:value] [enable:true/false]',
    description: 'Customize or toggle a character\'s idle breathing animation (default speed: `1.5`, intensity: `0.015`).',
    example: '@breathe Alice speed:2.0 intensity:0.03\n@breathe Nikita enable:false'
  },
  setbackground: {
    signature: '@setBackground <backgroundName> [instant | fade | crossfade]',
    description: 'Change the background image using the specified transition (`instant`, `fade`, or `crossfade`).',
    example: '@setBackground campus fade'
  },
  playmusic: {
    signature: '@playMusic <trackName> [fadeIn:time]',
    description: 'Play looping background music with optional fade-in duration in seconds.',
    example: '@playMusic Track_Chill fadeIn:2.0'
  },
  stopmusic: {
    signature: '@stopMusic [fadeOut:time]',
    description: 'Stop the currently playing background music with optional fade-out duration.',
    example: '@stopMusic fadeOut:1.5'
  },
  playsfx: {
    signature: '@playSFX <sfxName>',
    description: 'Play a one-shot sound effect.',
    example: '@playSFX se_cat01'
  },
  shake: {
    signature: '@shake <duration> <magnitude>',
    description: 'Shake the screen for `duration` seconds with `magnitude` strength (typically 10-20).',
    example: '@shake 0.5 15.0'
  },
  zoom: {
    signature: '@zoom x:<pos> y:<pos> percentage:<scale> time:<duration> | @zoom reset time:<duration>',
    description: 'Zoom the camera to focus on coordinates, or reset the camera to its default position.',
    example: '@zoom x:-200 y:0 percentage:50 time:1.5\n@zoom reset time:1.0'
  },
  wait: {
    signature: '@wait <duration>',
    description: 'Pause script progression for the specified duration in seconds.',
    example: '@wait 1.5'
  },
  fade: {
    signature: '@fade [color] [duration:time]',
    description: 'Wait/fade transition for the specified duration.',
    example: '@fade black duration:1.0'
  },
  fadeout: {
    signature: '@fadeOut [duration] [black | white | #RRGGBB]',
    description: 'Fade the entire screen (UI, dialogue, characters, backgrounds) to a solid color.',
    example: '@fadeOut 1.5 black'
  },
  fadein: {
    signature: '@fadeIn [duration]',
    description: 'Fade the entire screen back in from a solid overlay color.',
    example: '@fadeIn 1.0'
  },
  showui: {
    signature: '@showUI [time:duration]',
    description: 'Fade in the dialogue UI.',
    example: '@showUI time:0.3'
  },
  hideui: {
    signature: '@hideUI [time:duration]',
    description: 'Fade out and hide the dialogue UI.',
    example: '@hideUI time:0.2'
  },
  cleardialogue: {
    signature: '@clearDialogue',
    description: 'Empty the speaker name and dialogue text boxes.',
    example: '@clearDialogue'
  },
  unlock_cg: {
    signature: '@unlock_cg <cgID>',
    description: 'Unlock a CG asset in the persistent gallery file.',
    example: '@unlock_cg gallery_image_showcase'
  },
  unlockcg: {
    signature: '@unlockcg <cgID>',
    description: 'Alias for `@unlock_cg`. Unlock a CG asset in the persistent gallery file.',
    example: '@unlockcg gallery_image_showcase'
  },
  trigger: {
    signature: '@trigger <eventName> [param1] [param2] ...',
    description: 'Broadcast a custom event notification (`DialogueManager.OnVNSTrigger`) to external C# scripts.',
    example: '@trigger start_combat elite_guard 50'
  },
  event: {
    signature: '@event <eventName> [param1] [param2] ...',
    description: 'Alias for `@trigger`. Broadcast a custom event notification to external C# scripts.',
    example: '@event spawn cube'
  },
  replay_start: {
    signature: '@replay_start <cgId>',
    description: 'Define the start marker of a replayable gallery scene associated with `cgId`.',
    example: '@replay_start gallery_image_showcase'
  },
  replaystart: {
    signature: '@replaystart <cgId>',
    description: 'Alias for `@replay_start`. Define the start marker of a replayable gallery scene.',
    example: '@replaystart gallery_image_showcase'
  },
  replay_end: {
    signature: '@replay_end',
    description: 'Define the end marker of a replayable gallery scene.',
    example: '@replay_end'
  },
  replayend: {
    signature: '@replayend',
    description: 'Alias for `@replay_end`. Define the end marker of a replayable gallery scene.',
    example: '@replayend'
  }
};

const CANONICAL_COMMANDS = [
  'showCharacter', 'hideCharacter', 'moveCharacter', 'setExpression', 'setPortrait', 'breathe',
  'setBackground', 'playMusic', 'stopMusic', 'playSFX',
  'jump', 'set', 'if', 'choice', 'endchoice', 'loadScript',
  'wait', 'shake', 'zoom', 'fadeOut', 'fadeIn', 'showUI', 'hideUI', 'clearDialogue',
  'unlock_cg', 'trigger', 'replay_start', 'replay_end'
];

function discoverProjectCharacters(document) {
  const charDataMap = new Map(); // lowerId -> { id, displayName, portraits: [], expressions: [], assetPath }
  try {
    const folders = vscode.workspace.workspaceFolders || [];
    const roots = folders.map(f => f.uri.fsPath);
    const docPath = document.uri.fsPath;
    const assetsIdx = docPath.indexOf(`${path.sep}Assets${path.sep}`);
    if (assetsIdx !== -1) {
      const projectRoot = docPath.slice(0, assetsIdx);
      if (!roots.includes(projectRoot)) roots.push(projectRoot);
    }

    for (const root of roots) {
      const charDir = path.join(root, 'Assets', 'Resources', 'Characters');
      if (!fs.existsSync(charDir)) continue;
      const files = fs.readdirSync(charDir).filter(f => f.endsWith('.asset'));
      for (const file of files) {
        const fullPath = path.join(charDir, file);
        const content = fs.readFileSync(fullPath, 'utf8');
        const idMatch = content.match(/^\s*characterID:\s*(\S+)/m);
        const nameMatch = content.match(/^\s*characterName:\s*(.+)$/m);
        const id = idMatch ? idMatch[1].trim() : path.basename(file, '.asset');
        const displayName = nameMatch ? nameMatch[1].trim() : id;
        const portraits = [];
        const expressions = [];

        const pRegex = /^\s*-\s*portraitName:\s*(\S+)/gm;
        let m;
        while ((m = pRegex.exec(content)) !== null) {
          portraits.push(m[1].trim());
        }
        const eRegex = /^\s*-\s*expressionName:\s*(\S+)/gm;
        while ((m = eRegex.exec(content)) !== null) {
          expressions.push(m[1].trim());
        }

        charDataMap.set(id.toLowerCase(), {
          id,
          displayName,
          portraits,
          expressions,
          assetPath: fullPath
        });
      }
    }
  } catch (_) {
    // Ignore filesystem errors
  }
  return charDataMap;
}

function isLineLabel(lines, index) {
  const line = lines[index].trim();
  if (!line.endsWith(':') || line.includes(' ') || line.includes('[') || line.includes('<')) {
    return false;
  }
  const name = line.slice(0, -1).trim();
  if (!name || !/^[A-Za-z_]\w*$/.test(name) || name.toLowerCase() === 'narrator') {
    return false;
  }
  for (let j = index + 1; j < lines.length; j++) {
    const nextLine = lines[j].trim();
    if (!nextLine || nextLine.startsWith('#') || nextLine.startsWith('//')) {
      continue;
    }
    if (nextLine.startsWith('@') || nextLine.includes('->') || nextLine.endsWith(':')) {
      return true;
    }
    return false;
  }
  return true;
}

function parseDocumentMetadata(document) {
  const text = document.getText();
  const lines = text.split(/\r?\n/);

  const labels = new Map(); // labelName -> { line, range, preview }
  const labelReferences = []; // { name, line, startCol, endCol }
  const variables = new Set();
  const characters = new Set(['narrator']);
  const portraitsByChar = new Map();
  const expressionsByChar = new Map();
  const choiceStarts = [];
  const symbols = [];

  const projectChars = discoverProjectCharacters(document);
  for (const [, info] of projectChars) {
    characters.add(info.displayName || info.id);
    portraitsByChar.set(info.id.toLowerCase(), new Set(info.portraits));
    expressionsByChar.set(info.id.toLowerCase(), new Set(info.expressions));
  }

  for (let i = 0; i < lines.length; i++) {
    const rawLine = lines[i];
    const trimmed = rawLine.trim();
    if (!trimmed || trimmed.startsWith('//') || trimmed.startsWith('#')) {
      continue;
    }

    if (isLineLabel(lines, i)) {
      const labelName = trimmed.slice(0, -1).trim();
      const startCol = rawLine.indexOf(labelName);
      const range = new vscode.Range(i, startCol, i, startCol + labelName.length);
      const previewLines = lines.slice(i, Math.min(lines.length, i + 6)).join('\n');
      if (!labels.has(labelName)) {
        labels.set(labelName, { line: i, range, preview: previewLines });
      }
      symbols.push(new vscode.DocumentSymbol(
        labelName,
        'Scene Label',
        vscode.SymbolKind.Function,
        new vscode.Range(i, 0, i, rawLine.length),
        range
      ));
      continue;
    }

    // Speaker header
    if (!trimmed.startsWith('@') && !trimmed.includes('->') && trimmed.includes(':')) {
      const colonIdx = trimmed.lastIndexOf(':');
      const headerPart = trimmed.slice(0, colonIdx).trim();
      let speakerPart = headerPart;
      if (speakerPart.includes('<')) speakerPart = speakerPart.slice(0, speakerPart.indexOf('<')).trim();
      if (speakerPart.includes('[')) speakerPart = speakerPart.slice(0, speakerPart.indexOf('[')).trim();
      if (/^[A-Za-z_]\w*$/.test(speakerPart)) {
        characters.add(speakerPart);
        const charKey = speakerPart.toLowerCase();
        const pMatch = headerPart.match(/<([^>]+)>/);
        if (pMatch) {
          if (!portraitsByChar.has(charKey)) portraitsByChar.set(charKey, new Set());
          portraitsByChar.get(charKey).add(pMatch[1].trim());
        }
        const eMatch = headerPart.match(/\[([^\]]+)\]/);
        if (eMatch) {
          if (!expressionsByChar.has(charKey)) expressionsByChar.set(charKey, new Set());
          expressionsByChar.get(charKey).add(eMatch[1].trim());
        }
      }
    }

    // Choice option reference: "Text" -> label
    const choiceMatch = rawLine.match(/^\s*"([^"]*)"\s*->\s*([A-Za-z_]\w*)\s*$/);
    if (choiceMatch) {
      const target = choiceMatch[2];
      const arrowIdx = rawLine.indexOf('->');
      const startCol = rawLine.indexOf(target, arrowIdx + 2);
      labelReferences.push({ name: target, line: i, startCol, endCol: startCol + target.length });
      continue;
    }

    if (trimmed.startsWith('@')) {
      const cmdMatch = rawLine.match(/^\s*@(\w+)/);
      if (!cmdMatch) continue;
      const cmdLower = cmdMatch[1].toLowerCase();

      if (cmdLower === 'choice') {
        choiceStarts.push(i);
        symbols.push(new vscode.DocumentSymbol(
          '@choice',
          `Line ${i + 1}`,
          vscode.SymbolKind.Enum,
          new vscode.Range(i, 0, i, rawLine.length),
          new vscode.Range(i, 0, i, rawLine.length)
        ));
      } else if (cmdLower === 'endchoice') {
        choiceStarts.pop();
      } else if (cmdLower === 'jump') {
        const jumpMatch = rawLine.match(/^\s*@jump\s+([A-Za-z_]\w*)/i);
        if (jumpMatch) {
          const target = jumpMatch[1];
          const startCol = rawLine.lastIndexOf(target);
          labelReferences.push({ name: target, line: i, startCol, endCol: startCol + target.length });
        }
      } else if (cmdLower === 'if') {
        const ifVarMatch = rawLine.match(/^\s*@if\s+([A-Za-z_]\w*)/i);
        if (ifVarMatch) variables.add(ifVarMatch[1]);
        const ifJumpMatch = rawLine.match(/\bjump\s+([A-Za-z_]\w*)\s*$/i);
        if (ifJumpMatch) {
          const target = ifJumpMatch[1];
          const startCol = rawLine.lastIndexOf(target);
          labelReferences.push({ name: target, line: i, startCol, endCol: startCol + target.length });
        }
      } else if (cmdLower === 'set') {
        const setMatch = rawLine.match(/^\s*@set\s+([A-Za-z_]\w*)/i);
        if (setMatch) variables.add(setMatch[1]);
      } else if (['showcharacter', 'hidecharacter', 'movecharacter', 'setexpression', 'setportrait', 'breathe'].includes(cmdLower)) {
        const charMatch = rawLine.match(/^\s*@\w+\s+([A-Za-z_]\w*)/);
        if (charMatch) characters.add(charMatch[1]);
      } else if (cmdLower === 'replay_start' || cmdLower === 'replaystart') {
        const replayMatch = rawLine.match(/^\s*@\w+\s+(\S+)/);
        const cgId = replayMatch ? replayMatch[1] : 'scene';
        symbols.push(new vscode.DocumentSymbol(
          `@replay_start ${cgId}`,
          'Replay Scene',
          vscode.SymbolKind.Event,
          new vscode.Range(i, 0, i, rawLine.length),
          new vscode.Range(i, 0, i, rawLine.length)
        ));
      }
    }
  }

  return {
    lines,
    labels,
    labelReferences,
    variables,
    characters,
    projectChars,
    portraitsByChar,
    expressionsByChar,
    choiceStarts,
    symbols
  };
}

function validateDocument(document, diagnosticCollection) {
  if (document.languageId !== 'vn-script') return;

  const { lines, labels, choiceStarts } = parseDocumentMetadata(document);
  const diagnostics = [];

  for (let i = 0; i < lines.length; i++) {
    const rawLine = lines[i];
    const trimmed = rawLine.trim();
    if (!trimmed || trimmed.startsWith('//') || trimmed.startsWith('#')) continue;

    // 1. Check @commands
    if (trimmed.startsWith('@')) {
      const cmdMatch = rawLine.match(/^\s*@(\w+)/);
      if (!cmdMatch) continue;
      const cmdName = cmdMatch[1];
      const cmdLower = cmdName.toLowerCase();

      if (!Object.prototype.hasOwnProperty.call(COMMAND_DOCS, cmdLower)) {
        const startCol = rawLine.indexOf('@');
        diagnostics.push(new vscode.Diagnostic(
          new vscode.Range(i, startCol, i, startCol + cmdName.length + 1),
          `Unknown VNS action '@${cmdName}'.`,
          vscode.DiagnosticSeverity.Warning
        ));
        continue;
      }

      if (cmdLower === 'jump') {
        const parts = trimmed.split(/\s+/);
        if (parts.length < 2) {
          diagnostics.push(new vscode.Diagnostic(
            new vscode.Range(i, 0, i, rawLine.length),
            '@jump action is missing a target label name.',
            vscode.DiagnosticSeverity.Error
          ));
        } else {
          const target = parts[1];
          if (!labels.has(target)) {
            const startCol = rawLine.lastIndexOf(target);
            diagnostics.push(new vscode.Diagnostic(
              new vscode.Range(i, startCol, i, startCol + target.length),
              `Broken @jump: target label '${target}' is not defined in this script.`,
              vscode.DiagnosticSeverity.Error
            ));
          }
        }
      } else if (cmdLower === 'if') {
        const parts = trimmed.split(/\s+/);
        const jumpIdx = parts.findIndex(p => p.toLowerCase() === 'jump');
        if (jumpIdx === -1 || jumpIdx + 1 >= parts.length || parts.length < 6) {
          diagnostics.push(new vscode.Diagnostic(
            new vscode.Range(i, 0, i, rawLine.length),
            'Malformed @if condition. Expected: @if <variable> <operator> <value> jump <label>',
            vscode.DiagnosticSeverity.Error
          ));
        } else {
          const target = parts[jumpIdx + 1];
          if (!labels.has(target)) {
            const startCol = rawLine.lastIndexOf(target);
            diagnostics.push(new vscode.Diagnostic(
              new vscode.Range(i, startCol, i, startCol + target.length),
              `Broken @if jump: target label '${target}' is not defined in this script.`,
              vscode.DiagnosticSeverity.Error
            ));
          }
        }
      }
    }

    // 2. Check choice arrow targets
    const choiceMatch = rawLine.match(/^\s*"([^"]*)"\s*->\s*([A-Za-z_]\w*)\s*$/);
    if (choiceMatch) {
      const target = choiceMatch[2];
      if (!labels.has(target)) {
        const arrowIdx = rawLine.indexOf('->');
        const startCol = rawLine.indexOf(target, arrowIdx + 2);
        diagnostics.push(new vscode.Diagnostic(
          new vscode.Range(i, startCol, i, startCol + target.length),
          `Choice target label '${target}' is not defined in this script.`,
          vscode.DiagnosticSeverity.Error
        ));
      }
    }
  }

  // Unclosed @choice blocks
  for (const choiceLine of choiceStarts) {
    diagnostics.push(new vscode.Diagnostic(
      new vscode.Range(choiceLine, 0, choiceLine, lines[choiceLine].length),
      'Unclosed @choice block (missing @endchoice).',
      vscode.DiagnosticSeverity.Error
    ));
  }

  diagnosticCollection.set(document.uri, diagnostics);
}

function activate(context) {
  const diagnosticCollection = vscode.languages.createDiagnosticCollection('vn-script');
  context.subscriptions.push(diagnosticCollection);

  if (vscode.window.activeTextEditor) {
    validateDocument(vscode.window.activeTextEditor.document, diagnosticCollection);
  }

  context.subscriptions.push(
    vscode.workspace.onDidOpenTextDocument(doc => validateDocument(doc, diagnosticCollection)),
    vscode.workspace.onDidChangeTextDocument(e => validateDocument(e.document, diagnosticCollection)),
    vscode.workspace.onDidCloseTextDocument(doc => diagnosticCollection.delete(doc.uri))
  );

  // 1. Hover Provider
  context.subscriptions.push(
    vscode.languages.registerHoverProvider('vn-script', {
      provideHover(document, position) {
        const lineText = document.lineAt(position.line).text;

        // Inline pause tags {w} or {p=0.5}
        const pauseRegex = /\{(?:w|p=\d+(?:\.\d+)?)\}/gi;
        let match;
        while ((match = pauseRegex.exec(lineText)) !== null) {
          if (position.character >= match.index && position.character <= match.index + match[0].length) {
            const tag = match[0];
            const isWait = tag.toLowerCase() === '{w}';
            const md = new vscode.MarkdownString();
            md.appendCodeblock(tag, 'vn-script');
            md.appendMarkdown(
              isWait
                ? '**Inline Wait for Click**: Pauses typewriter text reveal until the player clicks or presses confirm.'
                : '**Inline Timed Pause**: Pauses typewriter text reveal for the specified duration in seconds before continuing automatically.'
            );
            return new vscode.Hover(md, new vscode.Range(position.line, match.index, position.line, match.index + tag.length));
          }
        }

        const wordRange = document.getWordRangeAtPosition(position, /@?[A-Za-z_]\w*/);
        if (!wordRange) return null;
        const word = document.getText(wordRange);

        // @command hover
        if (word.startsWith('@')) {
          const key = word.slice(1).toLowerCase();
          const info = COMMAND_DOCS[key];
          if (info) {
            const md = new vscode.MarkdownString();
            md.appendCodeblock(info.signature, 'vn-script');
            md.appendMarkdown(`${info.description}\n\n**Example:**\n`);
            md.appendCodeblock(info.example, 'vn-script');
            return new vscode.Hover(md, wordRange);
          }
        }

        const { labels, projectChars } = parseDocumentMetadata(document);

        // Label hover
        if (labels.has(word)) {
          const labelInfo = labels.get(word);
          const md = new vscode.MarkdownString();
          md.appendMarkdown(`**Scene Label** \`${word}\` *(Line ${labelInfo.line + 1})*\n`);
          md.appendCodeblock(labelInfo.preview, 'vn-script');
          return new vscode.Hover(md, wordRange);
        }

        // Character hover from CharacterData .asset
        const charInfo = projectChars.get(word.toLowerCase());
        if (charInfo) {
          const md = new vscode.MarkdownString();
          md.appendMarkdown(`**Character** \`${charInfo.displayName}\` (ID: \`${charInfo.id}\`)\n\n`);
          if (charInfo.portraits.length > 0) {
            md.appendMarkdown(`- **Portraits:** ${charInfo.portraits.map(p => `\`<${p}>\``).join(', ')}\n`);
          }
          if (charInfo.expressions.length > 0) {
            md.appendMarkdown(`- **Expressions:** ${charInfo.expressions.map(e => `\`[${e}]\``).join(', ')}\n`);
          }
          return new vscode.Hover(md, wordRange);
        }

        return null;
      }
    })
  );

  // 2. Definition Provider (Jump to label or @loadScript target file)
  context.subscriptions.push(
    vscode.languages.registerDefinitionProvider('vn-script', {
      provideDefinition(document, position) {
        const lineText = document.lineAt(position.line).text;

        // Check @loadScript <filename>
        const loadMatch = lineText.match(/^\s*@(?:loadscript|load_script)\s+(\S+)/i);
        if (loadMatch) {
          const scriptName = loadMatch[1].replace(/\.vns$/i, '');
          const currentDir = path.dirname(document.uri.fsPath);
          const candidatePath = path.join(currentDir, `${scriptName}.vns`);
          if (fs.existsSync(candidatePath)) {
            return new vscode.Location(vscode.Uri.file(candidatePath), new vscode.Position(0, 0));
          }
        }

        const wordRange = document.getWordRangeAtPosition(position, /[A-Za-z_]\w*/);
        if (!wordRange) return null;
        const word = document.getText(wordRange);

        const { labels } = parseDocumentMetadata(document);
        if (labels.has(word)) {
          return new vscode.Location(document.uri, labels.get(word).range);
        }
        return null;
      }
    })
  );

  // 3. Reference Provider (Find all references to a label)
  context.subscriptions.push(
    vscode.languages.registerReferenceProvider('vn-script', {
      provideReferences(document, position, refContext) {
        const wordRange = document.getWordRangeAtPosition(position, /[A-Za-z_]\w*/);
        if (!wordRange) return null;
        const word = document.getText(wordRange);

        const { labels, labelReferences } = parseDocumentMetadata(document);
        if (!labels.has(word)) return null;

        const locations = [];
        if (refContext.includeDeclaration) {
          locations.push(new vscode.Location(document.uri, labels.get(word).range));
        }
        for (const ref of labelReferences) {
          if (ref.name === word) {
            locations.push(new vscode.Location(
              document.uri,
              new vscode.Range(ref.line, ref.startCol, ref.line, ref.endCol)
            ));
          }
        }
        return locations;
      }
    })
  );

  // 4. Rename Provider (Rename a label and all jumps/choices pointing to it)
  context.subscriptions.push(
    vscode.languages.registerRenameProvider('vn-script', {
      provideRenameEdits(document, position, newName) {
        const wordRange = document.getWordRangeAtPosition(position, /[A-Za-z_]\w*/);
        if (!wordRange) return null;
        const word = document.getText(wordRange);

        const { labels, labelReferences } = parseDocumentMetadata(document);
        if (!labels.has(word)) return null;

        const edit = new vscode.WorkspaceEdit();
        edit.replace(document.uri, labels.get(word).range, newName);
        for (const ref of labelReferences) {
          if (ref.name === word) {
            edit.replace(
              document.uri,
              new vscode.Range(ref.line, ref.startCol, ref.line, ref.endCol),
              newName
            );
          }
        }
        return edit;
      }
    })
  );

  // 5. Document Symbol Provider (Outline & Breadcrumbs)
  context.subscriptions.push(
    vscode.languages.registerDocumentSymbolProvider('vn-script', {
      provideDocumentSymbols(document) {
        return parseDocumentMetadata(document).symbols;
      }
    })
  );

  // 6. Completion Item Provider (Commands, Labels, Variables, Characters, Portraits, Expressions, Inline Tags)
  context.subscriptions.push(
    vscode.languages.registerCompletionItemProvider(
      'vn-script',
      {
        provideCompletionItems(document, position) {
          const linePrefix = document.lineAt(position).text.slice(0, position.character);
          const { labels, variables, characters, portraitsByChar, expressionsByChar } = parseDocumentMetadata(document);
          const items = [];

          // A. Inside curly braces `{...}` -> inline tags & story variables
          if (linePrefix.endsWith('{')) {
            const waitItem = new vscode.CompletionItem('w', vscode.CompletionItemKind.Keyword);
            waitItem.detail = 'Wait for player click {w}';
            waitItem.insertText = 'w';
            items.push(waitItem);

            const pauseItem = new vscode.CompletionItem('p=0.5', vscode.CompletionItemKind.Keyword);
            pauseItem.detail = 'Pause typewriter for duration (seconds)';
            pauseItem.insertText = new vscode.SnippetString('p=${1:0.5}');
            items.push(pauseItem);

            for (const v of variables) {
              const varItem = new vscode.CompletionItem(v, vscode.CompletionItemKind.Variable);
              varItem.detail = 'Story Variable Interpolation';
              items.push(varItem);
            }
            return items;
          }

          // B. Portrait override `<...>` on speaker line
          const portraitHeaderMatch = linePrefix.match(/^\s*([A-Za-z_]\w*)\s+<([^>]*)$/);
          if (portraitHeaderMatch) {
            const charKey = portraitHeaderMatch[1].toLowerCase();
            const portraits = portraitsByChar.get(charKey) || new Set(['base']);
            for (const p of portraits) {
              const item = new vscode.CompletionItem(p, vscode.CompletionItemKind.EnumMember);
              item.detail = `Portrait for ${portraitHeaderMatch[1]}`;
              items.push(item);
            }
            return items;
          }

          // C. Expression override `[...]` on speaker line
          const exprHeaderMatch = linePrefix.match(/^\s*([A-Za-z_]\w*)(?:\s+<[^>]+>)?\s*\[([^\]]*)$/);
          if (exprHeaderMatch) {
            const charKey = exprHeaderMatch[1].toLowerCase();
            const expressions = expressionsByChar.get(charKey) || new Set(['base', 'happy']);
            for (const e of expressions) {
              const item = new vscode.CompletionItem(e, vscode.CompletionItemKind.EnumMember);
              item.detail = `Expression for ${exprHeaderMatch[1]}`;
              items.push(item);
            }
            return items;
          }

          // D. After `@jump `, `@if ... jump `, or `-> ` -> suggest defined labels
          if (/^\s*@jump\s+\w*$/i.test(linePrefix) || /\bjump\s+\w*$/i.test(linePrefix) || /->\s*\w*$/.test(linePrefix)) {
            for (const [labelName, info] of labels.entries()) {
              const item = new vscode.CompletionItem(labelName, vscode.CompletionItemKind.Function);
              item.detail = `Scene Label (Line ${info.line + 1})`;
              item.documentation = new vscode.MarkdownString().appendCodeblock(info.preview, 'vn-script');
              items.push(item);
            }
            return items;
          }

          // E. After `@loadScript ` -> suggest .vns files in same directory
          if (/^\s*@(?:loadScript|load_script)\s+\w*$/i.test(linePrefix)) {
            try {
              const dir = path.dirname(document.uri.fsPath);
              const vnsFiles = fs.readdirSync(dir).filter(f => f.endsWith('.vns'));
              for (const f of vnsFiles) {
                const name = path.basename(f, '.vns');
                const item = new vscode.CompletionItem(name, vscode.CompletionItemKind.File);
                item.detail = f;
                items.push(item);
              }
            } catch (_) {}
            return items;
          }

          // F. After `@set ` or `@if ` -> suggest known story variables
          if (/^\s*@(?:set|if)\s+\w*$/i.test(linePrefix)) {
            for (const v of variables) {
              const item = new vscode.CompletionItem(v, vscode.CompletionItemKind.Variable);
              item.detail = 'Story Variable';
              items.push(item);
            }
            return items;
          }

          // G. After character commands -> suggest characters in project/script
          if (/^\s*@(?:showCharacter|hideCharacter|moveCharacter|setExpression|setPortrait|breathe)\s+\w*$/i.test(linePrefix)) {
            for (const c of characters) {
              if (c.toLowerCase() === 'narrator') continue;
              const item = new vscode.CompletionItem(c, vscode.CompletionItemKind.Class);
              item.detail = 'Character ID';
              items.push(item);
            }
            return items;
          }

          // H. Starting an `@` command
          if (/^\s*@\w*$/.test(linePrefix)) {
            for (const cmd of CANONICAL_COMMANDS) {
              const info = COMMAND_DOCS[cmd.toLowerCase()];
              const item = new vscode.CompletionItem(`@${cmd}`, vscode.CompletionItemKind.Keyword);
              item.insertText = cmd;
              if (info) {
                item.detail = info.signature;
                item.documentation = new vscode.MarkdownString(`${info.description}\n\n\`\`\`vn-script\n${info.example}\n\`\`\``);
              }
              items.push(item);
            }
            return items;
          }

          return undefined;
        }
      },
      '@', '{', '<', '[', ' ', '>'
    )
  );
}

function deactivate() {}

module.exports = { activate, deactivate };