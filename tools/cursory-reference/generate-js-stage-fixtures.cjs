// Creates an instrumented development copy; the pinned upstream checkout is never modified.
// Requires CURSORY_JS_CHECKOUT at commit 16fff97fab05bb6b0c6753b2dc136a7692634cec.
const fs = require('node:fs');
const os = require('node:os');
const path = require('node:path');
const { execFileSync } = require('node:child_process');

const expectedCommit = '16fff97fab05bb6b0c6753b2dc136a7692634cec';
const expectedDataset = '1bf3af168719a580e2c5d6fb439f894f9bfff72fc90147149cc531dee80b6203';
const checkout = path.resolve(process.env.CURSORY_JS_CHECKOUT || '');
if (!process.env.CURSORY_JS_CHECKOUT) throw new Error('Set CURSORY_JS_CHECKOUT to the pinned cursory-js checkout.');
const commit = execFileSync('git', ['rev-parse', 'HEAD'], { cwd: checkout, encoding: 'utf8' }).trim();
if (commit !== expectedCommit) throw new Error(`Expected ${expectedCommit}, found ${commit}.`);

const temporary = fs.mkdtempSync(path.join(os.tmpdir(), 'cursory-js-instrumented-'));
try {
  fs.cpSync(checkout, temporary, {
    recursive: true,
    filter: source => !['.git', 'node_modules', 'dist', '.test-build'].includes(path.basename(source)),
  });
  fs.symlinkSync(path.join(checkout, 'node_modules'), path.join(temporary, 'node_modules'), 'junction');
  instrument(path.join(temporary, 'src', 'trajectorySelection.ts'), path.join(temporary, 'src', 'cursory.ts'));
  execFileSync(process.platform === 'win32' ? 'npm.cmd' : 'npm', ['run', 'build'], {
    cwd: temporary, stdio: 'inherit', shell: process.platform === 'win32',
  });

  const { generateTrajectory } = require(path.join(temporary, 'dist', 'index.js'));
  const cases = [
    { seed: 42, start: [0, 0], end: [640, 360], frequency: 60, frequencyRandomizer: 1, directness: 0.65 },
    { seed: 9007199254740991, start: [-17.25, 42.5], end: [321.75, -180.125], frequency: 120, frequencyRandomizer: 2, directness: 0.1 },
    { seed: 123456789, start: [1000, -700], end: [-200, 150], frequency: 30, frequencyRandomizer: 0.25, directness: 1 },
  ];
  const results = cases.map(input => {
    const stages = {};
    const result = generateTrajectory(input.start, input.end, {
      frequency: input.frequency,
      frequencyRandomizer: input.frequencyRandomizer,
      seed: input.seed,
      directness: input.directness,
      __trace: (stage, value) => { stages[stage] = value; },
    });
    return { input, stages, result };
  });
  const output = {
    schemaVersion: 1,
    source: `cursory-js@${expectedCommit}`,
    datasetSha256: expectedDataset,
    generator: 'tools/cursory-reference/generate-js-stage-fixtures.cjs (instrumented temporary source copy)',
    cases: results,
  };
  const outputPath = path.resolve(__dirname, '../../tests/SpyBrowser.Cursory.Tests/Fixtures/stage-parity.json');
  fs.writeFileSync(outputPath, `${JSON.stringify(output)}\n`);
  console.log(`Wrote ${results.length} stage traces to ${outputPath}`);
} finally {
  fs.rmSync(temporary, { recursive: true, force: true });
}

function instrument(selectionPath, cursoryPath) {
  let selection = fs.readFileSync(selectionPath, 'utf8');
  let cursory = fs.readFileSync(cursoryPath, 'utf8');
  selection = replaceOnce(selection, '  directness?: number;\n', '  directness?: number;\n  __trace?: (stage: string, value: unknown) => void;\n');
  selection = replaceOnce(selection,
    '    directness = 0.65,\n  }: ClosestOptions = {},',
    '    directness = 0.65,\n    __trace,\n  }: ClosestOptions = {},');
  selection = replaceOnce(selection,
    '  return { trajectory: candidates[rng.choice(weights)], dxTarget, dyTarget, lengthTarget };',
    '  const selected = rng.choice(weights);\n' +
    '  __trace?.("selection", { candidateRecordingIndices: candidates.map(candidate => LOADED_TRAJECTORIES.indexOf(candidate)), weights: Array.from(weights), selectedCandidate: selected, selectedRecordingIndex: LOADED_TRAJECTORIES.indexOf(candidates[selected]) });\n' +
    '  return { trajectory: candidates[selected], dxTarget, dyTarget, lengthTarget };');
  selection = replaceOnce(selection,
    '  directness = 0.65,\n): FoundTrajectory {',
    '  directness = 0.65,\n  trace?: (stage: string, value: unknown) => void,\n): FoundTrajectory {');
  selection = replaceOnce(selection,
    '    { directness },\n  );',
    '    { directness, __trace: trace },\n  );');
  selection = replaceOnce(selection,
    '  const jittered = jitterTrajectory(trajectory.points as readonly Point[], lengthTarget, rng);\n' +
    '  const knotted = knotTrajectory(jittered, targetStart, targetEnd, rng);\n' +
    '  const points = morphTrajectory(knotted, targetStart, targetEnd, dxTarget, dyTarget, lengthTarget);',
    '  trace?.("recording", { points: trajectory.points, timings });\n' +
    '  const jittered = jitterTrajectory(trajectory.points as readonly Point[], lengthTarget, rng);\n' +
    '  trace?.("recordingJitter", jittered);\n' +
    '  const knotted = knotTrajectory(jittered, targetStart, targetEnd, rng);\n' +
    '  trace?.("recordingKnots", knotted);\n' +
    '  const points = morphTrajectory(knotted, targetStart, targetEnd, dxTarget, dyTarget, lengthTarget);\n' +
    '  trace?.("recordingMorph", points);');

  cursory = replaceOnce(cursory,
    '  directness?: number;\n}',
    '  directness?: number;\n  /** Development-only trace hook injected into the temporary reference copy. */\n  __trace?: (stage: string, value: unknown) => void;\n}');
  cursory = replaceOnce(cursory,
    '  const found = findTrajectory(targetStart, targetEnd, rng, directness);',
    '  const found = findTrajectory(targetStart, targetEnd, rng, directness, options.__trace);');
  cursory = replaceOnce(cursory,
    '  const timings = found.timings.map((timing) => timing - startTime);',
    '  const timings = found.timings.map((timing) => timing - startTime);\n' +
    '  options.__trace?.("recordingTimesNormalized", timings);');
  cursory = replaceOnce(cursory,
    '  const sampledTimings = sampleTimings(timings, frequency, frequencyRandomizer, rng);',
    '  const sampledTimings = sampleTimings(timings, frequency, frequencyRandomizer, rng);\n' +
    '  options.__trace?.("sampledTimings", sampledTimings);');
  cursory = replaceOnce(cursory,
    '  const dxTarget = targetEnd[0] - targetStart[0];',
    '  options.__trace?.("interpolatedPoints", sampledPoints);\n' +
    '  const dxTarget = targetEnd[0] - targetStart[0];');
  cursory = replaceOnce(cursory,
    '  const jittered = jitterTrajectory(knotted, trajectoryLength, rng);',
    '  options.__trace?.("sampledKnots", knotted);\n' +
    '  const jittered = jitterTrajectory(knotted, trajectoryLength, rng);\n' +
    '  options.__trace?.("sampledJitter", jittered);');
  cursory = replaceOnce(cursory,
    '  return { points, timings: sampledTimings };',
    '  options.__trace?.("sampledMorph", points);\n' +
    '  return { points, timings: sampledTimings };');
  fs.writeFileSync(selectionPath, selection);
  fs.writeFileSync(cursoryPath, cursory);
}

function replaceOnce(source, oldText, newText) {
  source = source.replaceAll('\r\n', '\n');
  const first = source.indexOf(oldText);
  if (first < 0 || source.indexOf(oldText, first + oldText.length) >= 0)
    throw new Error(`Pinned source instrumentation anchor missing or ambiguous: ${oldText.slice(0, 80)}`);
  return source.slice(0, first) + newText + source.slice(first + oldText.length);
}
