// Run with CURSORY_JS_DIST pointing at the built dist/cursory.js from the pinned checkout.
// The checked-in output is consumed by ordinary .NET tests; Node is maintenance-only.
const fs = require('node:fs');
const path = require('node:path');
const { generateTrajectory, defaultGenerator } = require(process.env.CURSORY_JS_DIST);
const { createPcg64 } = require(path.join(path.dirname(process.env.CURSORY_JS_DIST), 'random/pcg64.js'));

const datasetSha256 = '1bf3af168719a580e2c5d6fb439f894f9bfff72fc90147149cc531dee80b6203';
let state = 0x6d2b79f5;
function next() {
  state = (Math.imul(state ^ (state >>> 15), state | 1) + 0x6d2b79f5) | 0;
  state ^= state + Math.imul(state ^ (state >>> 7), state | 61);
  return ((state ^ (state >>> 14)) >>> 0) / 0x100000000;
}
function pick(items) { return items[Math.floor(next() * items.length)]; }
const cases = [];
for (let i = 0; i < 160; i++) {
  const start = [Math.round((next() * 2 - 1) * 2400 * 8) / 8, Math.round((next() * 2 - 1) * 1400 * 8) / 8];
  const end = [Math.round((next() * 2 - 1) * 2400 * 8) / 8, Math.round((next() * 2 - 1) * 1400 * 8) / 8];
  const input = {
    start, end,
    frequency: pick([20, 30, 60, 90, 120, 144, 240]),
    frequencyRandomizer: pick([0, 0.25, 1, 2, 5]),
    seed: Math.floor(next() * 0x1fffffffffffff),
    directness: pick([0, 0.1, 0.25, 0.5, 0.65, 0.85, 1]),
  };
  const { points, timings } = generateTrajectory(input.start, input.end, {
    frequency: input.frequency, frequencyRandomizer: input.frequencyRandomizer,
    seed: input.seed, directness: input.directness,
  });
  cases.push({ input, points, timings });
}
const seeds = [0n, 1n, (1n << 64n) + 12345n, (1n << 128n) - 1n];
const rng = seeds.map(seed => {
  const bitGenerator = createPcg64(seed);
  const raw = Array.from({ length: 16 }, () => bitGenerator.nextUint64().toString());
  const doubleGenerator = defaultGenerator(seed);
  const doubles = Array.from({ length: 16 }, () => doubleGenerator.random());
  const normalGenerator = defaultGenerator(seed);
  const normals = Array.from({ length: 4096 }, () => normalGenerator.standardNormal());
  const integerGenerator = defaultGenerator(seed);
  const integers = [1, 2, 3, 5, 86, 256, 1000, 2356, 65536, 1000000].map(high => integerGenerator.integers(high));
  const rejectingGenerator = defaultGenerator(seed);
  const rejectingIntegers = Array.from({ length: 500 }, () => rejectingGenerator.integers(1000000));
  return { seed: seed.toString(), raw, doubles, normals, integers, rejectingIntegers };
});
const integerRejectionProbeGenerator = defaultGenerator(0n);
const integerRejectionProbe = Array.from({ length: 128 }, () => integerRejectionProbeGenerator.integers(2000000000));
const output = { schemaVersion: 2, source: 'cursory-js@16fff97fab05bb6b0c6753b2dc136a7692634cec', datasetSha256, rng, integerRejectionProbe, cases };
const file = path.resolve(__dirname, '../../tests/SpyBrowser.Cursory.Tests/Fixtures/parity.json');
fs.writeFileSync(file, `${JSON.stringify(output)}\n`);
console.log(`Wrote ${cases.length} matching-dataset trajectory cases to ${file}`);
